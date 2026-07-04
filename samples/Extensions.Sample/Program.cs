using zms9110750.Extensions.Utils;

// ─── ToSafeFileName: 文件名净化 ─────────────────────
Console.WriteLine("=== ToSafeFileName ===");
var bad = "file:name?.txt";
Console.WriteLine($"  原始: {bad}");
Console.WriteLine($"  安全: {bad.ToSafeFileName()}");

// ─── ToString: 集合拼接 ─────────────────────────────
Console.WriteLine("\n=== ToString ===");
var items = new[] { "A", "B", "C" };
Console.WriteLine($"  默认: {items.ToString()}");
Console.WriteLine($"  逗号: {items.ToString(", ")}");

// ─── ThrowIfOutOfRange ──────────────────────────────
Console.WriteLine("\n=== ThrowIfOutOfRange ===");
try
{
    ArgumentOutOfRangeException.ThrowIfOutOfRange(5, 1, 10);
    Console.WriteLine("  5 in [1,10]: OK");
    ArgumentOutOfRangeException.ThrowIfOutOfRange(15, 1, 10);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"  越界: {ex.Message}");
}

// ─── WithContext ────────────────────────────────────
Console.WriteLine("\n=== WithContext ===");
var value = "示例";
value.OutContext(out var ctx);
Console.WriteLine($"  {ctx}");
