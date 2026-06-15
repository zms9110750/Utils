using System.Reflection;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoSmart.Caching.Sqlite;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using ZiggyCreatures.Caching.Fusion;
using zms9110750.Utils.Adapters.Demo.Polly.Models;
using zms9110750.Extensions.DependencyInjection;
using zms9110750.Extensions.Polly;
using zms9110750.Extensions.Autofac;
using Autofac.Pooling;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

var services = new ServiceCollection();
services.AddLogging(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddMemoryCache();
services.AddSqliteCache("cache.sqlite.db");
services.AddFusionCacheSystemTextJsonSerializer();
services.AddFusionCache()
        .WithDefaultEntryOptions(o => o.DistributedCacheDuration = TimeSpan.FromDays(365 * 1000))
        .TryWithAutoSetup()
        .AsHybridCache();

var jsonPath = Path.Combine(
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
    "Polly", "Data", "monsters.json");

// 注册命名管道
services.AddResiliencePipeline<List<Monster>>("monster-pipeline",
    (pipeline, ctx) =>
    {
        var logger = ctx.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var cache = ctx.ServiceProvider.GetRequiredService<HybridCache>();
        pipeline.AddTimeout(TimeSpan.FromSeconds(5));
        pipeline.AddCaching(new Axion.Extensions.Polly.Caching.Hybrid.CachingStrategyOptions<List<Monster>>
        {
            HybridCache = cache
        });
        pipeline.AddRetry(new RetryStrategyOptions<List<Monster>>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args =>
            {
                if (Random.Shared.NextDouble() < 0.3)
                {
                    logger.LogWarning("⚠️  随机失败 (第 {A} 次)", args.AttemptNumber + 1);
                    return ValueTask.FromResult(true);
                }
                return ValueTask.FromResult(args.Outcome.Exception is not null);
            },
            OnRetry = args => { logger.LogInformation("🔄 重试 #{N}", args.AttemptNumber); return default; }
        });
        pipeline.AddFallback(new FallbackStrategyOptions<List<Monster>>
        {
            FallbackAction = _ =>
            {
                logger.LogWarning("🛡️  回退");
                return ValueTask.FromResult(Outcome.FromResult(new List<Monster>
                {
                    new() { Id = "fb-001", Name = "备份史莱姆", Level = 1, Hp = 10 }
                }));
            }
        });
    });

// 迁入 Autofac
var builder = new ContainerBuilder();
builder.Populate(services);

// 注册工厂：从 keyed pipeline 随机取一个怪物
builder.Register<Func<Task<Monster>>>((c, p) =>
{
    var pipeline = c.ResolveKeyed<ResiliencePipeline<List<Monster>>>("monster-pipeline");
    var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    return async () =>
    {
        var all = await pipeline.ExecuteGenericWithKeyAsync("monsters:all", async (ctx, ct) =>
        {
            await using var stream = File.OpenRead(jsonPath);
            return await JsonSerializer.DeserializeAsync<List<Monster>>(stream, jsonOpts, ct)
                   ?? new List<Monster>();
        });

        var idx = Random.Shared.Next(all.Count);
        return all[idx];
    };
}).SingleInstance();

// 运行
var container = builder.Build();
Console.WriteLine("╔═══════════════════════════════════╗");
Console.WriteLine("║  Utils.Adapters.Polly Demo        ║");
Console.WriteLine("║  FusionCache + SQLite + Polly     ║");
Console.WriteLine("╚═══════════════════════════════════╝\n");

using var scope = container.BeginLifetimeScope();
var factory = scope.Resolve<Func<Task<Monster>>>();
var log = scope.Resolve<ILogger<Program>>();

log.LogInformation("=== 批量生成怪物 ===");
for (int i = 0; i < 5; i++)
{
    var monster = await factory();
    log.LogInformation("   🐉 [{Id}] {Name} Lv.{Level} ♥{Hp}", monster.Id, monster.Name, monster.Level, monster.Hp);
}

log.LogInformation("\n=== 缓存验证 ===");
for (int i = 0; i < 3; i++)
{
    var m = await factory();
    log.LogInformation("   第 {N} 次 → {Name}", i + 1, m.Name);
}

Console.WriteLine("\n🏁 完成");

// ═══ Autofac 缓存池测试 ═══
Console.WriteLine("\n═══ Autofac 缓存池测试 ═══\n");

var poolCache = new MemoryCache(new MemoryCacheOptions());
var pool = new CacheObjectPool<PoolDummy>(poolCache, new DefaultPooledObjectPolicy<PoolDummy>());

// Path 1: 传 IPooledRegistrationPolicy
var poolBuilder = new ContainerBuilder();
poolBuilder.RegisterType<PoolDummy>().As<IPoolDummy>().PooledInstancePerLifetimeScope(pool);

// 偷看 Autofac 内部拿到的那个池到底是什么类型的
var poolContainer = poolBuilder.Build();
Console.WriteLine("\n═══ 缓存池测试完成 ═══\n");

interface IPoolDummy { int Id { get; }
}
class PoolDummy : IPoolDummy
{
    static int _gid;
    public int Id { get; } = Interlocked.Increment(ref _gid);
    public PoolDummy() => Console.WriteLine($"    [创建] #{Id}");
}
