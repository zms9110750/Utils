#!/usr/bin/env -S dotnet --

#:property TargetFramework=net10.0
#:property PublishAot=false

using System.Collections.Immutable;
using System.Text.Json;

namespace zms9110750.Utils.SingleExe;

/// <summary>值在排序列表中的索引范围。</summary>
public readonly record struct ValueRange(int Start, int End, int Count)
{
    /// <summary>移除索引后偏移。Start>removedIdx 整体左移，End>removedIdx 减 Count。</summary>
    public ValueRange ShiftLeft(int removedIdx)
    {
        if (Start > removedIdx)
        {
            return new(Start - 1, End - 1, Count);
        }

        if (End > removedIdx)
        {
            return new(Start, End - 1, Count - 1);
        }

        return this;
    }
}

/// <summary>解析抽取池。内联 k:v 或 file:// 殊途同归。</summary>
public static class PoolParser
{
    /// <summary>解析表达式或文件路径为名称-值字典。</summary>
    public static Dictionary<string, int> Parse(ReadOnlySpan<char> arg)
    {
        var s = arg.Trim();
        if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var path = s["file://".Length..].Trim().ToString();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"鏂囦欢涓嶅瓨鍦? {path}");
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                    ?? throw new FormatException("JSON 涓虹┖");
            }
            catch (JsonException ex)
            {
                throw new FormatException($"JSON 瑙ｆ瀽澶辫触: {ex.Message}");
            }
        }

        var result = new Dictionary<string, int>();
        Span<char> buf = stackalloc char[s.Length];
        s.CopyTo(buf);
        for (int i = 0; i < buf.Length; i++)
        {
            if (buf[i] == '，')
            {
                buf[i] = ',';
            }
        }

        Span<Range> ranges = stackalloc Range[buf.Length];
        int count = buf.Split(ranges, ',');
        for (int i = 0; i < count; i++)
        {
            var part = buf[ranges[i]].Trim();
            if (part.IsEmpty)
            {
                continue;
            }

            var colonIdx = part.IndexOfAny(':', '：');
            if (colonIdx < 0)
            {
                continue;
            }

            var name = part[..colonIdx].Trim();
            var valStr = part[(colonIdx + 1)..].Trim();
            if (name.Length > 0 && int.TryParse(valStr, out int val))
            {
                result[name.ToString()] = val;
            }
        }
        return result;
    }
}

/// <summary>区间约束随机抽取基类。外部循环：SetConstraints + 反复 Pick()。</summary>
public abstract class BasePicker<T>
{
    /// <summary>所有项的名称列表（按 V 值升序）。</summary>
    public abstract IReadOnlyList<T> Names { get; }
    /// <summary>V 值到索引范围的映射。Key=V，Value=在 Names 中的范围。</summary>
    public abstract IReadOnlyDictionary<int, ValueRange> ValueRanges { get; }

    /// <summary>剩余可取数量下限。</summary>
    public int CountMin { get; protected set; }
    /// <summary>剩余可取数量上限。</summary>
    public int CountMax { get; protected set; }
    /// <summary>剩余点数和下限。</summary>
    public int PointMin { get; protected set; }
    /// <summary>剩余点数和上限。</summary>
    public int PointMax { get; protected set; }

    /// <summary>当前步可行 V 值下限（子类实现）。</summary>
    public abstract int Low { get; }
    /// <summary>当前步可行 V 值上限（子类实现）。</summary>
    public abstract int High { get; }

    protected int MinVal => ValueRanges.Keys.Min();
    protected int MaxVal => ValueRanges.Keys.Max();

    /// <summary>设置约束并开始一次新抽取。</summary>
    public void SetConstraints(int cMin, int cMax, int pMin, int pMax)
    {
        CountMin = cMin;
        CountMax = cMax;
        PointMin = pMin;
        PointMax = pMax;
    }

    /// <summary>在 [Low, High] 中随机选一个 key（V 值）。</summary>
    protected int SelectKey()
    {
        int lo = Low, hi = High;
        var candidates = new List<int>();
        foreach (var kvp in ValueRanges)
        {
            if (kvp.Key >= lo && kvp.Key <= hi)
            {
                candidates.Add(kvp.Key);
            }
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"无可行候选 [Low={lo}, High={hi}]");
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    /// <summary>按当前约束随机抽取一项。内部自减约束值。</summary>
    public virtual T Pick()
    {
        int key = SelectKey();
        var range = ValueRanges[key];
        int idx = Random.Shared.Next(range.Start, range.End);
        T picked = Names[idx];
        CountMin--;
        CountMax--;
        PointMin -= key;
        PointMax -= key;
        return picked;
    }
}

/// <summary>可放回抽取。池不变，每次从全集中随机选。</summary>
public class ReplacementPicker<T> : BasePicker<T>
{
    private readonly ImmutableList<T> _names;
    private readonly SortedList<int, ValueRange> _ranges;

    /// <summary>构造可放回抽取器。Key=名称，Value=V值。</summary>
    public ReplacementPicker(IReadOnlyDictionary<T, int> items)
    {
        var sorted = items
            .Select(kvp => (kvp.Key, kvp.Value))
            .OrderBy(x => x.Value).ThenBy(x => x.Key)
            .ToList();

        var names = new List<T>(sorted.Count);
        var ranges = new SortedList<int, ValueRange>();
        int start = 0;
        int? lastVal = null;

        for (int i = 0; i < sorted.Count; i++)
        {
            var (name, val) = sorted[i];
            names.Add(name);
            if (lastVal != null && val != lastVal)
            {
                ranges[lastVal.Value] = new ValueRange(start, i, i - start);
                start = i;
            }
            lastVal = val;
        }
        if (sorted.Count > 0)
        {
            ranges[lastVal!.Value] = new ValueRange(start, sorted.Count, sorted.Count - start);
        }

        _names = names.ToImmutableList();
        _ranges = ranges;
    }

    public override IReadOnlyList<T> Names => _names;
    public override IReadOnlyDictionary<int, ValueRange> ValueRanges => _ranges;

    public override int Low => PointMin - (CountMax - 1) * MaxVal;
    public override int High => PointMax - (CountMin - 1) * MinVal;
}

/// <summary>不可放回抽取。每抽一项即从池中移除。</summary>
public class NonReplacementPicker<T> : BasePicker<T>
{
    private List<T> _names;
    private SortedList<int, ValueRange> _ranges;

    /// <summary>构造不可放回抽取器。Key=名称，Value=V值。</summary>
    public NonReplacementPicker(IReadOnlyDictionary<T, int> items)
    {
        var sorted = items
            .Select(kvp => (kvp.Key, kvp.Value))
            .OrderBy(x => x.Value).ThenBy(x => x.Key)
            .ToList();

        _names = new List<T>(sorted.Count);
        _ranges = new SortedList<int, ValueRange>();
        int start = 0;
        int? lastVal = null;

        for (int i = 0; i < sorted.Count; i++)
        {
            var (name, val) = sorted[i];
            _names.Add(name);
            if (lastVal != null && val != lastVal)
            {
                _ranges[lastVal.Value] = new ValueRange(start, i, i - start);
                start = i;
            }
            lastVal = val;
        }
        if (sorted.Count > 0)
        {
            _ranges[lastVal!.Value] = new ValueRange(start, sorted.Count, sorted.Count - start);
        }
    }

    public override IReadOnlyList<T> Names => _names;
    public override IReadOnlyDictionary<int, ValueRange> ValueRanges => _ranges;

    public override int Low {
        get {
            int needed = CountMax - 1;
            if (needed <= 0)
            {
                return PointMin;
            }

            int count = 0, sum = 0;
            var keys = ValueRanges.Keys.ToList();
            for (int i = keys.Count - 1; i >= 0 && count < needed; i--)
            {
                int v = keys[i];
                int take = Math.Min(ValueRanges[v].Count, needed - count);
                count += take;
                sum += v * take;
            }
            return PointMin - sum;
        }
    }

    public override int High {
        get {
            int needed = CountMin - 1;
            if (needed <= 0)
            {
                return PointMax;
            }

            int count = 0, sum = 0;
            foreach (var kvp in ValueRanges)
            {
                if (count >= needed)
                {
                    break;
                }

                int v = kvp.Key;
                int take = Math.Min(kvp.Value.Count, needed - count);
                count += take;
                sum += v * take;
            }
            return PointMax - sum;
        }
    }

    /// <summary>抽取一项并从池中移除。内部自减约束值。</summary>
    public override T Pick()
    {
        int key = SelectKey();
        var range = ValueRanges[key];
        int idx = Random.Shared.Next(range.Start, range.End);
        T picked = Names[idx];

        _names.RemoveAt(idx);
        var newRanges = new SortedList<int, ValueRange>(_ranges.Count);
        foreach (var kvp in _ranges)
        {
            var shifted = kvp.Value.ShiftLeft(idx);
            if (shifted.Count > 0)
            {
                newRanges.Add(kvp.Key, shifted);
            }
        }
        _ranges = newRanges;

        CountMin--;
        CountMax--;
        PointMin -= key;
        PointMax -= key;
        return picked;
    }
}

// ── CLI 入口 ──

public class Pick
{
    /// <summary>参数解析与执行入口。</summary>
    public static void Main(string[] args)
    {
        if (args.Length == 0 || args.Any(a => a is "-?" or "-h" or "-help" or "--help"))
        {
            PrintHelp();
            return;
        }

        bool single = args.Any(a => a is "-s" or "-single");

        foreach (var arg in args)
        {
            if (arg is "-s" or "-single")
            {
                continue;
            }

            try
            {
                var s = arg.AsSpan().Trim();

                string expr = s.ToString();
                if (expr.EndsWith("-single"))
                {
                    single = true;
                    expr = expr[..^7];
                }
                else if (expr.EndsWith("-s"))
                {
                    single = true;
                    expr = expr[..^2];
                }
                var m = System.Text.RegularExpressions.Regex.Match(expr,
                    @"^\((\d+),(\d+)\)\[(\d+),(\d+)\]\{(.+)\}$");
                int cMin, cMax, pMin, pMax;
                if (m.Success)
                {
                    cMin = int.Parse(m.Groups[1].Value);
                    cMax = int.Parse(m.Groups[2].Value);
                    pMin = int.Parse(m.Groups[3].Value);
                    pMax = int.Parse(m.Groups[4].Value);
                }
                else
                {
                    m = System.Text.RegularExpressions.Regex.Match(expr,
                        @"^\((\d+)\)\[(\d+)\]\{(.+)\}$");
                    if (!m.Success)
                    {
                        Console.Error.WriteLine($"鏃犳晥琛ㄨ揪寮? {arg}");
                        continue;
                    }
                    int cv = int.Parse(m.Groups[1].Value);
                    int pv = int.Parse(m.Groups[2].Value);
                    cMin = cMax = cv;
                    pMin = pMax = pv;
                }
                var pool = PoolParser.Parse(m.Groups[m.Groups.Count - 1].Value.AsSpan().Trim());

                BasePicker<string> picker = single
                    ? new NonReplacementPicker<string>(pool)
                    : new ReplacementPicker<string>(pool);

                picker.SetConstraints(cMin, cMax, pMin, pMax);

                var result = new List<string>();
                while (picker.CountMin > 0 && picker.PointMin > 0)
                {
                    var item = picker.Pick();
                    result.Add(item);
                }

                int total = result.Sum(n => pool[n]);
                Console.WriteLine($"{string.Join(", ", result)} = {total}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"閿欒: {ex.Message}");
            }
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("""
pick 鈥?鍖洪棿绾︽潫闅忔満鎶藉彇锛堣繃婊ら掑綊鐗堬級

鐢ㄦ硶:
  pick '(2,4)[5,8]{鐦熺柅:3,榛戞:2,澶嶆椿:4}'
  pick '(1,1)[10,10]{A:3,B:5,C:8}-single'
  pick '(2,3)[6,10]{file://pool.json}'

鏍煎紡:
  (min,max)        鏁伴噺鑼冨洿
  [min,max]        鐐规暟鎬诲拰鑼冨洿
  {k:v,k:v,...}    鎶藉彇姹狅紙鍚嶇О:鐐规暟锛?  {file://璺緞}    浠?JSON 鏂囦欢璇诲彇鎶藉彇姹?  -s / -single     涓嶆斁鍥烇紙鎺ュ湪琛ㄨ揪寮忓悗鏃犵┖鏍硷級

绀轰緥:
  pick '(2,4)[5,8]{鐦熺柅:3,榛戞:2,澶嶆椿:4,榛戞礊:3}'
  pick '(3,4)[9,12]{涓у案:2,鍧﹀厠:5,鑷垎:3}-single'
  pick '(2,3)[6,10]{file://pool.json}'
""");
    }
}
