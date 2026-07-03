#!/usr/bin/env -S dotnet --

#:property TargetFramework=net10.0
#:property PublishAot=false

using System.Collections.Immutable;
using System.Text;

namespace zms9110750.Utils.SingleExe;

public class Dice
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            RunInteractive();
            return;
        }
        foreach (var arg in args)
        {
            if (arg.Trim().ToLowerInvariant() is "-?" or "-h" or "-help" or "--help")
            {
                PrintHelp();
                return;
            }
        }
        foreach (var arg in args)
        {
            try
            {
                Console.WriteLine(Expr.Parse(arg.Trim()));
            }
            catch (FormatException e)
            {
                Console.Error.WriteLine($"错误: {e.Message}");
            }
        }
    }
    public static void RunInteractive()
    {
        Console.WriteLine("骰子表达式计算器 (输入空行退出)");
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            if (line.Trim().ToLowerInvariant() is "-?" or "-h" or "-help" or "--help")
            {
                PrintHelp();
                continue;
            }
            Console.WriteLine(Expr.Parse(line.Trim()));
        }
    }
    public static void PrintHelp()
    {
        Console.WriteLine("""
            骰子表达式计算器 - 用法
              dice <表达式> [表达式...]    计算一个或多个表达式
              dice                        交互模式（空行退出）
              dice -? | -h | -help | --help  显示此帮助

            表达式格式：
              dN              一个 N 面骰子（骰）
              XdY             X 个 Y 面骰子（多骰）
              (Expr)          括号分组，优先计算
              N               直接数值
              + - * /         算术运算，* / 优先于 + -
              A v B           比较两个表达式大小
              名字:表达式      给表达式命名
              Dice[t/l][Z]    优势/劣势，Z 默认 2 组
              [A, B, ...]       取最小值（中括号）
              {A, B, ...}       取最大值（大括号）

            示例：
              dice d20
              dice 4d6
              dice 4d6+2
              dice (4d6+2)*3
              dice (2+2)d6
              dice 4d(3+3)
              dice 4d6t2
              dice d20l
              dice 4d6+2v4d8+5
              dice 小明:4d6v小红:4d8+5
              dice [4d6, 8, 3+2]
              dice {3d8, 10}
            """);
    }
}

/// <summary>所有表达式类型的抽象基类。</summary>
public abstract class Expr
{
    /// <summary>原始输入字符串，反映此节点在输入中对应的原文（未 trim 的原始视图）。</summary>
    public required string RawValue { get; set; }
    /// <summary>格式化输出表达式结果和过程。</summary>
    public abstract override string ToString();
    /// <summary>表达式解析入口。按 v 分割路由到比较表达式，否则委托给 NamedExpr。</summary>
    public static Expr Parse(ReadOnlySpan<char> s)
    {
        s = s.Trim();
        Span<Range> ranges = stackalloc Range[256];
        int count = s.Split(ranges, 'v');
        switch (count)
        {
            case 1:
                return NamedExpr.Parse(s);
            case 2:
                return new CompareExpr(
                    NamedExpr.Parse(s[ranges[0]].Trim()),
                    NamedExpr.Parse(s[ranges[1]].Trim())) { RawValue = s.ToString() };
            default:
                var items = new NamedExpr[count];
                for (int i = 0; i < count; i++)
                {
                    items[i] = (NamedExpr)NamedExpr.Parse(s[ranges[i]].Trim(), i + 1);
                }
                return new MulCompareExpr(items) { RawValue = s.ToString() };
        }
    }
}

/// <summary>两路比较表达式：A v B。按命名情况决定输出视角。</summary>
public class CompareExpr : Expr
{
    public Expr Left { get; }
    public Expr Right { get; }
    public CompareExpr(Expr left, Expr right)
    {
        Left = left;
        Right = right;
    }
    public override string ToString()
    {
        switch ((Left, Right))
        {
            case (NamedExpr l, NamedExpr r):
            {
                int lv = l.Inner.Value;
                int rv = r.Inner.Value;
                var p = lv >= rv ? l : r;
                int sv = p == l ? lv : rv;
                int ov = p == l ? rv : lv;
                return $"{p.Name} {(sv > ov ? "胜" : ov > sv ? "负" : "平")} = {sv} vs {ov}";
            }
            case (NamedExpr l, _):
            {
                int lv = l.Inner.Value;
                int rv = ((NumExpr)Right).Value;
                return $"{l.Name} {(lv > rv ? "胜" : rv > lv ? "负" : "平")} = {lv} vs {rv}";
            }
            case (_, NamedExpr r):
            {
                int lv = ((NumExpr)Left).Value;
                int rv = r.Inner.Value;
                return $"{r.Name} {(rv > lv ? "胜" : lv > rv ? "负" : "平")} = {rv} vs {lv}";
            }
            default:
            {
                int lv = ((NumExpr)Left).Value;
                int rv = ((NumExpr)Right).Value;
                return $"{(lv > rv ? "胜" : rv > lv ? "负" : "平")} = {lv} vs {rv}";
            }
        }
    }
}

/// <summary>多路比较表达式：A v B v C ...。按值降序排列输出。</summary>
public class MulCompareExpr : Expr
{
    public NamedExpr[] Items { get; }
    public MulCompareExpr(NamedExpr[] items)
    {
        Items = items;
    }
    public override string ToString()
    {
        var r = Items.OrderByDescending(x => x.Inner.Value).ToArray();
        var sb = new StringBuilder();
        for (int i = 0; i < r.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(r[i - 1].Inner.Value > r[i].Inner.Value ? " > " : " = ");
            }
            sb.Append(r[i].Name).Append('[').Append(r[i].Inner.Value).Append(']');
        }
        return sb.ToString();
    }
}

/// <summary>命名表达式，为运算表达式附加一个显示名称。</summary>
public class NamedExpr : Expr
{
    public string Name { get; }
    public NumExpr Inner { get; }
    public NamedExpr(string name, NumExpr inner)
    {
        Name = name;
        Inner = inner;
    }
    public override string ToString()
    {
        return $"{Name} = {Inner}";
    }

    public static Expr Parse(ReadOnlySpan<char> s, int index = -1)
    {
        s = s.Trim();
        int colonPos = s.IndexOf(':');
        if (colonPos > 0)
        {
            return new NamedExpr(s.Slice(0, colonPos).Trim().ToString(), NumExpr.Parse(s.Slice(colonPos + 1).Trim())) { RawValue = s.ToString() };
        }
        if (index > 0)
        {
            return new NamedExpr($"({index})", NumExpr.Parse(s)) { RawValue = s.ToString() };
        }
        return NumExpr.Parse(s);
    }
}

/// <summary>所有可以计算出一个整数值的表达式类型的抽象基类。</summary>
public abstract class NumExpr : Expr
{
    private const int PREC_ADD = 10;
    private const int PREC_MUL = 20;
    private const int PREC_DICE = 30;

    public abstract int Value { get; }
    public abstract StringBuilder BuildDetail(StringBuilder sb);
    public override string ToString()
    {
        var sb = new StringBuilder();
        BuildDetail(sb);
        sb.Append(" = ");
        sb.Append(Value);
        return sb.ToString();
    }

    /// <summary>入口：整体解析一个数值表达式。调用方需传入已 trim 的 span。</summary>
    public static new NumExpr Parse(ReadOnlySpan<char> s)
    {
        s = s.Trim();
        if (s.IsEmpty)
        {
            throw new FormatException("表达式为空");
        }

        var result = ParseExpr(s, 0);
        var rest = s[result.RawValue.Length..].Trim();
        if (rest.Length > 0)
        {
            throw new FormatException("多余的输入: " + new string(rest));
        }

        return result;
    }

    /// <summary>Pratt 解析主循环。s 需已 trim（无前导空白）。</summary>
    internal static NumExpr ParseExpr(ReadOnlySpan<char> s, int minPrec)
    {
        var now = ParseAtomic(s);

        while (true)
        {
            var rest = s[now.RawValue.Length..];
            if (rest.IsEmpty)
            {
                break;
            }

            int prec = rest[0] switch {
                'd' => PREC_DICE,
                '*' or '/' => PREC_MUL,
                '+' or '-' => PREC_ADD,
                _ => -1
            };
            if (prec < minPrec)
            {
                break;
            }

            if (rest[0] == 'd')
            {
                var sides = ParseExpr(rest[1..], PREC_MUL);
                int diceLen = now.RawValue.Length + 1 + sides.RawValue.Length;
                now = new DiceExpr(now, sides) { RawValue = s[..diceLen].ToString() };

                var after = s[diceLen..];
                if (after is ['t' or 'l', ..])
                {
                    now = ParseAdvantageSuffix(s, (DiceExpr)now, diceLen, after);
                }
            }
            else
            {
                var right = ParseExpr(rest[1..], prec + 1);
                int consumed = now.RawValue.Length + 1 + right.RawValue.Length;
                now = new BinOpExpr(now, rest[0], right) { RawValue = s[..consumed].ToString() };
            }
        }
        return now;
    }

    /// <summary>解析基本元素：数字 / 括号 / 括号列表 / 裸骰。s 不允许前导空白。</summary>
    private static NumExpr ParseAtomic(ReadOnlySpan<char> s)
    {
        if (s.IsEmpty)
        {
            throw new FormatException("表达式不完整");
        }

        if (s is ['d', ..])
        {
            var sides = ParseExpr(s[1..], PREC_MUL);
            int diceLen = 1 + sides.RawValue.Length;
            var dice = new DiceExpr(new ConstExpr(1) { RawValue = "" }, sides) { RawValue = s[..diceLen].ToString() };

            var after = s[diceLen..];
            if (after is ['t' or 'l', ..])
            {
                return ParseAdvantageSuffix(s, dice, diceLen, after);
            }
            return dice;
        }

        return s[0] switch {
            >= '0' and <= '9' => ParseNumber(s),
            '(' => ParenExpr.ParseContent(s),
            '[' => MinExpr.Parse(s),
            '{' => MaxExpr.Parse(s),
            _ => throw new FormatException("无效表达式: " + new string(s))
        };
    }

    private static NumExpr ParseNumber(ReadOnlySpan<char> s)
    {
        int len = 0;
        while (len < s.Length && char.IsAsciiDigit(s[len]))
        {
            len++;
        }

        return new ConstExpr(int.Parse(s[..len])) { RawValue = s[..len].ToString() };
    }

    private static NumExpr ParseAdvantageSuffix(ReadOnlySpan<char> fullSpan, DiceExpr dice, int diceLen, ReadOnlySpan<char> after)
    {
        bool takeHighest = after[0] == 't';
        after = after[1..];
        NumExpr groups = AdvExpr.DefaultGroups;
        int suffixLen = 1;
        if (after is [_, ..] && char.IsAsciiDigit(after[0]))
        {
            int len = 0;
            while (len < after.Length && char.IsAsciiDigit(after[len]))
            {
                len++;
            }

            groups = new ConstExpr(int.Parse(after[..len])) { RawValue = "" };
            suffixLen += len;
        }
        return new AdvExpr(dice, takeHighest, groups) { RawValue = fullSpan[..(diceLen + suffixLen)].ToString() };
    }

    internal static NumExpr[] ParseBracketItems(ReadOnlySpan<char> s, char open, char close)
    {
        int end = FindMatchingClose(s, 1, open, close);
        var inner = s.Slice(1, end - 1);
        if (inner.IsEmpty)
        {
            throw new FormatException($"空列表 {open}{close}");
        }

        var list = new List<NumExpr>();
        int depth = 0, start = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '(' or '[' or '{': depth++; break;
                case ')' or ']' or '}': depth--; break;
                case ',' when depth == 0:
                    list.Add(ParseExpr(inner.Slice(start, i - start).Trim(), 0)); start = i + 1; break;
            }
        }
        if (start <= inner.Length)
        {
            list.Add(ParseExpr(inner.Slice(start).Trim(), 0));
        }

        return list.ToArray();
    }

    internal static int FindMatchingClose(ReadOnlySpan<char> s, int start, char open, char close)
    {
        int depth = 1;
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == open)
            {
                depth++;
            }
            else if (s[i] == close)
            {
                depth--;
            }

            if (depth == 0)
            {
                return i;
            }
        }
        throw new FormatException("缺少匹配的 " + close);
    }
}

/// <summary>整数常量表达式。</summary>
public class ConstExpr : NumExpr
{
    public override int Value { get; }
    public ConstExpr(int value)
    {
        Value = value;
    }
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        sb.Append(Value);
        return sb;
    }
}

/// <summary>取最小值表达式：[A, B, ...]。</summary>
public class MinExpr : NumExpr
{
    public NumExpr[] Items { get; }
    public MinExpr(NumExpr[] items)
    {
        Items = items;
    }
    public override int Value => Items.Min(e => e.Value);
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        sb.Append('[');
        for (int i = 0; i < Items.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            Items[i].BuildDetail(sb);
        }
        sb.Append(']');
        return sb;
    }

    internal static new NumExpr Parse(ReadOnlySpan<char> s)
    {
        var items = NumExpr.ParseBracketItems(s, '[', ']');
        int end = NumExpr.FindMatchingClose(s, 1, '[', ']');
        return new MinExpr(items) { RawValue = s[..(end + 1)].ToString() };
    }
}

/// <summary>取最大值表达式：{A, B, ...}。</summary>
public class MaxExpr : NumExpr
{
    public NumExpr[] Items { get; }
    public MaxExpr(NumExpr[] items)
    {
        Items = items;
    }
    public override int Value => Items.Max(e => e.Value);
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        sb.Append('{');
        for (int i = 0; i < Items.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            Items[i].BuildDetail(sb);
        }
        sb.Append('}');
        return sb;
    }

    internal static new NumExpr Parse(ReadOnlySpan<char> s)
    {
        var items = NumExpr.ParseBracketItems(s, '{', '}');
        int end = NumExpr.FindMatchingClose(s, 1, '{', '}');
        return new MaxExpr(items) { RawValue = s[..(end + 1)].ToString() };
    }
}

/// <summary>二元算术运算表达式：左 Op 右。</summary>
public class BinOpExpr : NumExpr
{
    public NumExpr Left { get; }
    public char Op { get; }
    public NumExpr Right { get; }
    public BinOpExpr(NumExpr left, char op, NumExpr right)
    {
        Left = left;
        Op = op;
        Right = right;
    }
    public override int Value => Op switch {
        '+' => Left.Value + Right.Value,
        '-' => Left.Value - Right.Value,
        '*' => Left.Value * Right.Value,
        '/' => Left.Value / Right.Value,
        _ => throw new InvalidOperationException("未知运算符: " + Op)
    };
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        Left.BuildDetail(sb);
        sb.Append(' ');
        sb.Append(Op);
        sb.Append(' ');
        Right.BuildDetail(sb);
        return sb;
    }
}

/// <summary>普通骰子掷法表达式：XdY，如 4d6。</summary>
public class DiceExpr : NumExpr
{
    public NumExpr CountExpr { get; }
    public NumExpr SidesExpr { get; }
    public DiceExpr(NumExpr count, NumExpr sides)
    {
        CountExpr = count;
        SidesExpr = sides;
    }
    private ImmutableList<int> Rolls => field ??= Enumerable.Range(0, CountExpr.Value)
        .Select(_ => Random.Shared.Next(1, SidesExpr.Value + 1)).ToImmutableList();
    public override int Value => Rolls.Sum();
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        var r = Rolls;
        if (r.Count <= 10)
        {
            sb.Append('[').Append(string.Join(", ", r)).Append(']');
        }
        else
        {
            sb.Append('[').Append(string.Join(", ", r.Take(5))).Append(", ... ").Append(r.Count - 5).Append(" more]");
        }
        return sb;
    }
}

/// <summary>优势/劣势骰子表达式：XdYtZ（取高）或 XdYlZ（取低）。</summary>
public class AdvExpr : NumExpr
{
    public static readonly NumExpr DefaultGroups = new ConstExpr(2) { RawValue = "" };
    public DiceExpr BaseDice { get; }
    public bool TakeHighest { get; }
    public NumExpr Groups { get; }
    public AdvExpr(DiceExpr baseDice, bool takeHighest, NumExpr groups)
    {
        BaseDice = baseDice;
        TakeHighest = takeHighest;
        Groups = groups;
    }
    private ImmutableList<int> GroupSums => field ??= Enumerable.Range(0, Groups.Value)
        .Select(_ => Enumerable.Range(0, BaseDice.CountExpr.Value)
            .Select(__ => Random.Shared.Next(1, BaseDice.SidesExpr.Value + 1)).Sum())
        .ToImmutableList();
    public override int Value => TakeHighest ? GroupSums.Max() : GroupSums.Min();
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        var g = GroupSums;
        sb.Append("掷").Append(g.Count).Append("次: [").Append(string.Join(", ", g)).Append("] ").Append(TakeHighest ? "取高" : "取低");
        return sb;
    }
}

/// <summary>括号分组表达式：(Expr)。</summary>
public class ParenExpr : NumExpr
{
    public NumExpr Inner { get; }
    public ParenExpr(NumExpr inner)
    {
        Inner = inner;
    }
    public override int Value => Inner.Value;
    public override StringBuilder BuildDetail(StringBuilder sb)
    {
        sb.Append('(');
        Inner.BuildDetail(sb);
        sb.Append(')');
        return sb;
    }

    public static new ParenExpr Parse(ReadOnlySpan<char> s)
    {
        var result = ParseContent(s);
        var rest = s[result.RawValue.Length..].Trim();
        if (rest.Length > 0)
        {
            throw new FormatException("括号后有多余内容: " + new string(rest));
        }

        return result;
    }

    public static ParenExpr ParseContent(ReadOnlySpan<char> s)
    {
        if (s.IsEmpty || s[0] != '(')
        {
            throw new FormatException("无效括号表达式: " + new string(s));
        }

        int close = NumExpr.FindMatchingClose(s, 1, '(', ')');
        var inner = s.Slice(1, close - 1);
        while (inner.Length > 0 && inner[0] == '(')
        {
            int innerClose = NumExpr.FindMatchingClose(inner, 1, '(', ')');
            if (innerClose == inner.Length - 1)
            {
                inner = inner.Slice(1, innerClose - 1);
            }
            else
            {
                break;
            }
        }
        var innerExpr = NumExpr.ParseExpr(inner, 0);
        if (inner.Length > innerExpr.RawValue.Length)
        {
            var check = inner[innerExpr.RawValue.Length..].Trim();
            if (check.Length > 0)
            {
                throw new FormatException("括号内有多余内容: " + new string(check));
            }
        }
        return new ParenExpr(innerExpr) { RawValue = s[..(close + 1)].ToString() };
    }
}
