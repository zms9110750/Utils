 
using System.Text.Json;
using System.Text.RegularExpressions;

// ══════ pick — Main 入口 ══════

class Pick
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("pick <表达式1> [表达式2] ...");
            Console.Error.WriteLine("pick -h");
            return;
        }

        foreach (var a in args)
        {
            if ((a ?? "") is "-?" or "-h" or "-help" or "--help")
            { PrintHelp(); return; }
        }

        foreach (var arg in args)
            Console.WriteLine(Process(arg ?? ""));
    }

    static void PrintHelp()
    {
        Console.WriteLine("pick — 区间约束随机抽取（过滤递归版）");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine(@"  pick '(2,4)[5,8]{瘟疫:3,黑死:2,复活:4}'");
        Console.WriteLine(@"  pick '(1,1)[10,10]{A:3,B:5,C:8}' -s");
        Console.WriteLine(@"  pick '(2,3)[6,10]{file://pool.json}'");
        Console.WriteLine();
        Console.WriteLine("格式:");
        Console.WriteLine("  (min,max)        数量范围");
        Console.WriteLine("  [min,max]        点数总和范围");
        Console.WriteLine("  {k:v,k:v,...}    抽取池（名称:点数）");
        Console.WriteLine("  {file://路径}    从 JSON 文件读取抽取池");
        Console.WriteLine("                     JSON 格式: {\"瘟疫\": 3, \"黑死\": 2}");
        Console.WriteLine("  -s / -single     不放回（抽过的项不再出现）");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine(@"  pick '(2,4)[5,8]{瘟疫:3,黑死:2,复活:4,黑洞:3}'");
        Console.WriteLine(@"  pick '(3,4)[9,12]{丧尸:2,坦克:5,自爆:3}' -single");
        Console.WriteLine(@"  pick '(2,3)[6,10]{file://pool.json}'");
    }

    static string Process(string expr)
    {
        bool single = false;
        string body = expr;

        var optMatch = Regex.Match(body, @"\s+(-s|-single)$", RegexOptions.IgnoreCase);
        if (optMatch.Success) { single = true; body = body[..optMatch.Index]; }

        var m = Regex.Match(body, @"^\((\d+),(\d+)\)\[(\d+),(\d+)\]\{(.+)\}$");
        if (!m.Success) return $"无效表达式: {expr}";

        int cMin = int.Parse(m.Groups[1].Value);
        int cMax = int.Parse(m.Groups[2].Value);
        int pMin = int.Parse(m.Groups[3].Value);
        int pMax = int.Parse(m.Groups[4].Value);

        var items = ParsePool(m.Groups[5].Value.Trim());
        if (items.Count == 0) return $"因子池为空: {expr}";

        var bag = new PickBag(items, single);
        var result = bag.Pick(cMin, cMax, pMin, pMax, []);
        if (result == null) return $"不可行: {expr}";

        return $"{string.Join(", ", result)} = {result.Sum(n => bag.GetVal(n))}";
    }

    static List<(string name, int val)> ParsePool(string s)
    {
        var fileMatch = Regex.Match(s, @"^file://(.+)$", RegexOptions.IgnoreCase);
        if (fileMatch.Success)
        {
            string path = fileMatch.Groups[1].Value;
            if (!File.Exists(path)) { Console.Error.WriteLine($"文件不存在: {path}"); return []; }
            try
            {
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                if (dict == null) return [];
                return dict.Where(kv => kv.Key.Length > 0)
                           .Select(kv => (kv.Key, kv.Value)).ToList();
            }
            catch (Exception ex) { Console.Error.WriteLine($"读取文件失败: {ex.Message}"); return []; }
        }

        var result = new List<(string, int)>();
        foreach (var part in s.Split(',', '，'))
        {
            var t = part.Trim();
            if (t.Length == 0) continue;
            var kv = t.Split(':', '：');
            if (kv.Length != 2) continue;
            if (int.TryParse(kv[1].Trim(), out int val) && kv[0].Trim().Length > 0)
                result.Add((kv[0].Trim(), val));
        }
        return result;
    }
}

// ══════ ValueRange 记录 ══════

record struct ValueRange(int Start, int End, int Count, int RunningTotal);

// ══════ PickBag 核心类 ══════

class PickBag
{
    readonly List<string> _names;
    readonly SortedList<int, ValueRange> _valToRange;
    readonly Dictionary<string, int> _nameToVal;
    readonly bool _single;

    public PickBag(IEnumerable<(string name, int val)> items, bool single)
    {
        _single = single;
        _nameToVal = [];

        var sorted = items
            .Select(x => (x.name, x.val))
            .OrderBy(x => x.val).ThenBy(x => x.name)
            .ToList();

        _names = new(sorted.Count);
        _valToRange = [];
        int runningTotal = 0;
        int? lastVal = null;

        for (int i = 0; i < sorted.Count; i++)
        {
            var (name, val) = sorted[i];
            _names.Add(name);
            _nameToVal[name] = val;
            runningTotal += val;

            if (lastVal != null && val != lastVal)
            {
                var prev = _valToRange[lastVal.Value];
                _valToRange[lastVal.Value] = prev with {
                    End = i, Count = i - prev.Start
                };
            }

            if (lastVal == null || val != lastVal)
                _valToRange[val] = new(i, i + 1, 1, runningTotal);

            lastVal = val;
        }
    }

    // ── 公开属性 ──

    public IList<int> Keys => _valToRange.Keys;
    public int NameCount => _names.Count;
    public int MinVal => Keys[0];
    public int MaxVal => Keys[^1];
    public int TotalSum => _valToRange.Values[^1].RunningTotal;
    public int Lo { get; private set; }
    public int Hi { get; private set; }
    public int GetVal(string name) => _nameToVal[name];

    // ── 计算宽边界 ──

    public void ComputeBounds(int cMin, int cMax, int pMin, int pMax)
    {
        int effMin = cMin <= 0 ? 1 : cMin;

        if (_single)
        {
            int maxRem = cMax <= 1 ? 0
                : _names.TakeLast(Math.Min(cMax - 1, _names.Count)).Sum(n => _nameToVal[n]);
            int minRem = effMin <= 1 ? 0
                : _names.Take(Math.Min(effMin - 1, _names.Count)).Sum(n => _nameToVal[n]);
            Lo = Math.Max(pMin - maxRem, MinVal);
            Hi = Math.Min(pMax - minRem, MaxVal);
        }
        else
        {
            Lo = Math.Max(pMin - (cMax - 1) * MaxVal, MinVal);
            Hi = Math.Min(pMax - (effMin - 1) * MinVal, MaxVal);
        }
    }

    // ── 二分查找 ──

    public int LowerBound(int val)
    {
        var k = Keys;
        int lo = 0, hi = k.Count;
        while (lo < hi) { int m = (lo + hi) / 2; if (k[m] < val) lo = m + 1; else hi = m; }
        return lo;
    }

    public int UpperBound(int val)
    {
        var k = Keys;
        int lo = 0, hi = k.Count;
        while (lo < hi) { int m = (lo + hi) / 2; if (k[m] <= val) lo = m + 1; else hi = m; }
        return lo;
    }

    // ── 不放回：移除一个怪物后重建 ──

    public PickBag RemoveAt(int nameIdx)
    {
        var remaining = _names
            .Select((n, i) => (n, _nameToVal[n], i))
            .Where(x => x.i != nameIdx)
            .Select(x => (x.n, x.Item2));
        return new(remaining, true);
    }

    // ── 递归选取（无回溯） ──

    public List<string>? Pick(int cMin, int cMax, int pMin, int pMax, List<string> chosen)
    {
        if (cMin <= 0 && pMin <= 0) return chosen;
        if (cMax <= 0) return null;

        ComputeBounds(cMin, cMax, pMin, pMax);
        if (Lo > Hi) return null;

        int leftKey = LowerBound(Lo), rightKey = UpperBound(Hi);
        if (leftKey >= rightKey) return null;

        var keys = Keys;
        var safeIdxs = new List<int>();

        for (int i = leftKey; i < rightKey; i++)
        {
            int v = keys[i];
            int nCMin = cMin - 1, nCMax = cMax - 1;
            int nPMin = pMin - v, nPMax = pMax - v;

            if (nCMin <= 0 && nPMin <= 0) { safeIdxs.Add(i); continue; }
            if (nCMax <= 0) continue;

            int nEff = nCMin <= 0 ? (nPMin > 0 ? 1 : 0) : nCMin;
            if (nEff == 0) { safeIdxs.Add(i); continue; }

            int loN, hiN;
            if (_single)
            {
                int maxVn = v == MaxVal && Keys.Count > 1 ? Keys[^2] : MaxVal;
                int minVn = v == MinVal && Keys.Count > 1 ? Keys[1] : MinVal;
                int maxRem = nCMax <= 1 ? 0
                    : _names.TakeLast(Math.Min(nCMax - 1, _names.Count - 1)).Sum(n => _nameToVal[n]);
                int minRem = nEff <= 1 ? 0
                    : _names.Take(Math.Min(nEff - 1, _names.Count - 1)).Sum(n => _nameToVal[n]);
                loN = Math.Max(nPMin - maxRem, minVn);
                hiN = Math.Min(nPMax - minRem, maxVn);
            }
            else
            {
                loN = Math.Max(nPMin - (nCMax - 1) * MaxVal, MinVal);
                hiN = Math.Min(nPMax - (nEff - 1) * MinVal, MaxVal);
            }

            int l = LowerBound(loN), r = UpperBound(hiN);
            if (l < r) safeIdxs.Add(i);
        }

        if (safeIdxs.Count == 0) return null;

        int keyIdx = safeIdxs[Random.Shared.Next(safeIdxs.Count)];
        int chosenVal = keys[keyIdx];
        var rng = _valToRange[chosenVal];
        int nameIdx = Random.Shared.Next(rng.Start, rng.End);
        string picked = _names[nameIdx];

        var next = new List<string>(chosen) { picked };

        if (_single)
            return RemoveAt(nameIdx).Pick(
                cMin - 1, cMax - 1, pMin - _nameToVal[picked], pMax - _nameToVal[picked], next);

        return Pick(cMin - 1, cMax - 1, pMin - _nameToVal[picked], pMax - _nameToVal[picked], next);
    }
}
