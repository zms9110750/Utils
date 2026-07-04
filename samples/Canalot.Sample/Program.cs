using zms9110750.Utils.Canalot.Wrappers;

// ─── NoneOr: 可空值包装 ────────────────────────────
Console.WriteLine("=== Canalot: NoneOr ===");
NoneOr<string> a = "hello";
NoneOr<string> b = default;
Console.WriteLine($"  a: HasValue={a.HasValue}, Value={a.Value}");
Console.WriteLine($"  b: HasValue={b.HasValue}");

NoneOr<int> n = 42;
Console.WriteLine($"  n: {n.Value}");
