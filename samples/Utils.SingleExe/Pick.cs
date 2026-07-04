using System.Text.Json;
using zms9110750.Utils.Core.Pick;

namespace zms9110750.Utils.SingleExe;

public class Pick
{
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
                        Console.Error.WriteLine($"无效表达式 {arg}");
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
                Console.Error.WriteLine($"错误: {ex.Message}");
            }
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("""
pick — 区间约束随机抽取（过滤归约版）

用法:
  pick '(2,4)[5,8]{瘟疫:3,黑死:2,复活:4}'
  pick '(1,1)[10,10]{A:3,B:5,C:8}-single'
  pick '(2,3)[6,10]{file://pool.json}'

格式:
  (min,max)        数量范围
  [min,max]        点数和范围
  {k:v,k:v,...}    抽取池（名称:点数）
  {file://路径}    从 JSON 文件读取抽取池
  -s / -single     不放回（接在表达式后无空格）

示例:
  pick '(2,4)[5,8]{瘟疫:3,黑死:2,复活:4,黑洞:3}'
  pick '(3,4)[9,12]{丧尸:2,坦克:5,自爆:3}-single'
  pick '(2,3)[6,10]{file://pool.json}'
""");
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
                throw new FileNotFoundException($"文件不存在 {path}");
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                    ?? throw new FormatException("JSON 为空");
            }
            catch (JsonException ex)
            {
                throw new FormatException($"JSON 解析失败: {ex.Message}");
            }
        }

        return ParseInline(s);
    }

    private static Dictionary<string, int> ParseInline(ReadOnlySpan<char> s)
    {
        var result = new Dictionary<string, int>();
        var text = s.ToString().Replace('，', ',');
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var colonIdx = trimmed.IndexOfAny(':', '：');
            if (colonIdx < 0)
            {
                continue;
            }

            var name = trimmed[..colonIdx].Trim();
            var valStr = trimmed[(colonIdx + 1)..].Trim();
            if (name.Length > 0 && int.TryParse(valStr, out int val))
            {
                result[name] = val;
            }
        }
        return result;
    }
}
