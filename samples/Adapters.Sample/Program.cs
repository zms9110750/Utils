using Microsoft.Extensions.DependencyInjection;
using Polly;
using zms9110750.Extensions.DependencyInjection;
using zms9110750.Extensions.Polly;

// ─── ResiliencePipeline 注册与使用 ──────────────────
Console.WriteLine("=== Adapters ===");
var services = new ServiceCollection();
services.AddResiliencePipeline<string>("my-pipeline", (builder, ctx) =>
{
    builder.AddRetry(new() { MaxRetryAttempts = 3, Delay = TimeSpan.FromMilliseconds(100) });
});

var sp = services.BuildServiceProvider();
var pipeline = sp.GetRequiredKeyedService<ResiliencePipeline<string>>("my-pipeline");
Console.WriteLine($"  管道已创建");

// ─── 带 Key 的执行 ──────────────────────────────────
using var cts = new CancellationTokenSource();
var result = await pipeline.ExecuteGenericWithKeyAsync("my-key", async (ctx, ct) =>
{
    await Task.Delay(10, ct);
    return "OK";
}, cts.Token);
Console.WriteLine($"  结果: {result}");
