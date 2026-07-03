using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using GameDemo;
using GameDemo.Events;
using GameDemo.Skills;

// ═══════════════════════════════════════════════════
//  GameDemo — MessagePipe + 中间件 = 游戏事件栈
//
//  演示目标：
//  1. MessagePipe 作为游戏事件总线
//  2. MessageHandlerFilter 作为 before/after 中间件
//  3. async/await 天然构成事件栈（LIFO）
//  4. HistoryPanel + StackChangedEvent 展示栈轨迹
//
//  武将：曹操·奸雄 / 夏侯惇·刚烈 / 小乔·天香
//       孟获·祸首 / 黄月英·集智
// ═══════════════════════════════════════════════════

// ── 1. DI 容器 + MessagePipe 配置 ──
var services = new ServiceCollection();
services.AddMessagePipe(options =>
{
    // 注册全局中间件：DamageEvent 的每次发布都经过这个 filter
    options.AddGlobalMessageHandlerFilter(typeof(DamageMiddlewareFilter), order: 0);
});

// ── 注册基础设施 ──
services.AddSingleton<GameEventPipeline>();
services.AddSingleton<HistoryPanel>();
services.AddSingleton<DamageExecutor>();

// ── 注册技能 ──
services.AddSingleton<刚烈>();
services.AddSingleton<奸雄>();
services.AddSingleton<天香>();
services.AddSingleton<祸首>();
services.AddSingleton<集智>();

// ── 注册游戏引擎 ──
services.AddSingleton<GameEngine>();

var provider = services.BuildServiceProvider();

// ── 2. 手动订阅 DamageEvent：把 DamageExecutor 挂到总线上 ──
//  MessagePipe 的 IAsyncMessageHandler<T> 需要显式注册
var damageSubscriber = provider.GetRequiredService<IAsyncSubscriber<DamageEvent>>();
var damageExecutor   = provider.GetRequiredService<DamageExecutor>();
damageSubscriber.Subscribe(async (evt, ct) => await damageExecutor.HandleAsync(evt, ct));

// ── 3. 启动历史面板（订阅 StackChangedEvent） ──
var history = provider.GetRequiredService<HistoryPanel>();

// ── 4. 启动游戏引擎 ──
var engine = provider.GetRequiredService<GameEngine>();

Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine(@"╔═══════════════════════════════════════════════╗");
Console.WriteLine(@"║     🎮  MessagePipe + 中间件 = 游戏事件栈      ║");
Console.WriteLine(@"║       三 国 杀 · 技 能 结 算 演 示             ║");
Console.WriteLine(@"╚═══════════════════════════════════════════════╝");
Console.WriteLine();
Console.ResetColor();

engine.SetupGame();

// ── 运行四个场景 ──
await engine.Scenario_曹操杀夏侯惇();
history.PrintAll();

await engine.Scenario_孟获南蛮入侵();
history.PrintAll();

await engine.Scenario_黄月英集智();
history.PrintAll();

await engine.Scenario_小乔天香();
history.PrintAll();

// ── 最终状态 ──
Console.WriteLine();
Console.WriteLine("═══ 最终状态 ═══");
foreach (var p in engine.AllPlayers)
    Console.WriteLine($"  {p.Name}  体力:{p.Hp}/{p.MaxHp}  手牌:{p.HandCards.Count}");

history.Dispose();

// ═══════════════════════════════════════════════════
//  DamageExecutor — 实际处理伤害：扣血
//  被订阅到 MessagePipe 的 DamageEvent 通道上
// ═══════════════════════════════════════════════════
public class DamageExecutor
{
    public async ValueTask HandleAsync(DamageEvent message, CancellationToken cancellationToken)
    {
        if (message.Amount <= 0) return;

        var actual = Math.Min(message.Amount, message.Target.Hp);
        message.Target.Hp -= actual;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  💥 扣血！{message.Target.Name} 失去 {actual} 点体力" +
                          $" ({message.Target.Hp + actual} → {message.Target.Hp})");
        Console.ResetColor();

        message.IsProcessed = true;
        await ValueTask.CompletedTask;
    }
}

// ═══════════════════════════════════════════════════
//  DamageMiddlewareFilter — MessagePipe 中间件
//  所有 DamageEvent 发布时自动经过此 filter
//  before/after 模式：next() 调用真实 handler
// ═══════════════════════════════════════════════════
public class DamageMiddlewareFilter : MessageHandlerFilter<DamageEvent>
{
    public override void Handle(DamageEvent message, Action<DamageEvent> next)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ── [中间件·BEFORE] {message}");
        Console.ResetColor();

        next(message);  // → 调用 DamageExecutor

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ── [中间件·AFTER]  {message.Target.Name} HP={message.Target.Hp}");
        Console.ResetColor();
    }
}
