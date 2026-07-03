using MessagePipe;
using GameDemo.Events;
using GameDemo.Models;
using GameDemo.Skills;

namespace GameDemo;

/// <summary>
/// 游戏引擎 —— 编排技能结算流程，展示事件栈
///
/// 架构：
///   GameEngine 编排事件流
///   ├─ Push() 入栈 → 记录栈帧
///   ├─ PublishAsync() → MessagePipe 广播（Handler 处理扣血等）
///   ├─ 技能触发 → 可能 Push/Publish 子事件（嵌套！）
///   └─ Pop() 出栈
///
///   MessagePipe 的角色：解耦的事件总线，只负责广播
///   GameEngine 的角色：游戏规则编排者，决定"谁在什么时候触发"
///   GameEventPipeline 的角色：栈追踪，给历史面板提供数据
///   async/await 的角色：维持LIFO栈顺序
/// </summary>
public class GameEngine
{
    private readonly IAsyncPublisher<DamageEvent> _damagePub;
    private readonly IAsyncPublisher<JudgeEvent> _judgePub;
    private readonly GameEventPipeline _pipeline;
    private readonly 刚烈 _gangLie;
    private readonly 奸雄 _jianXiong;
    private readonly 天香 _tianXiang;
    private readonly 祸首 _huoShou;
    private readonly 集智 _jiZhi;

    private Player 曹操 = null!;
    private Player 夏侯惇 = null!;
    private Player 小乔 = null!;
    private Player 孟获 = null!;
    private Player 黄月英 = null!;
    public List<Player> AllPlayers = null!;

    public GameEngine(
        IAsyncPublisher<DamageEvent> damagePub,
        IAsyncPublisher<JudgeEvent> judgePub,
        GameEventPipeline pipeline,
        刚烈 gangLie,
        奸雄 jianXiong,
        天香 tianXiang,
        祸首 huoShou,
        集智 jiZhi)
    {
        _damagePub = damagePub;
        _judgePub = judgePub;
        _pipeline = pipeline;
        _gangLie = gangLie;
        _jianXiong = jianXiong;
        _tianXiang = tianXiang;
        _huoShou = huoShou;
        _jiZhi = jiZhi;
    }

    public void SetupGame()
    {
        曹操   = new Player("曹操",   "奸雄", 4);
        夏侯惇 = new Player("夏侯惇", "刚烈", 4);
        小乔   = new Player("小乔",   "天香", 3);
        孟获   = new Player("孟获",   "祸首", 4);
        黄月英 = new Player("黄月英", "集智", 3);

        AllPlayers = new List<Player> { 曹操, 夏侯惇, 小乔, 孟获, 黄月英 };

        foreach (var p in AllPlayers)
        {
            p.HandCards.Add(new Card(CardType.杀, Suit.Spade, 7, "♠7杀"));
            p.HandCards.Add(new Card(CardType.闪, Suit.Heart, 2, "♥2闪"));
            if (p == 小乔)
                p.HandCards.Add(new Card(CardType.桃, Suit.Heart, 3, "♥3桃"));
        }

        Console.WriteLine("═══ 游戏初始化 ═══");
        foreach (var p in AllPlayers)
            Console.WriteLine($"  {p.Name}  体力:{p.Hp}/{p.MaxHp}  技能:{p.SkillName}");
        Console.WriteLine();
    }

    /// <summary>
    /// 【核心演示】曹操杀夏侯惇 → 刚烈判定 → 反伤 → 奸雄
    ///
    /// 事件栈流程：
    ///   Depth 0 │ 使用牌 曹操 杀 → 夏侯惇
    ///   Depth 0 │ ├伤害 曹操 → 夏侯惇 1点
    ///   Depth 0 │ │         ← MessagePipe 处理扣血
    ///   Depth 0 │ ├刚烈 夏侯惇判定...
    ///   Depth 1 │ │ ├判定 夏侯惇 刚烈判定
    ///   Depth 1 │ │ │     ← 判定结果不为♥
    ///   Depth 1 │ │ ├反伤 夏侯惇 → 曹操 1点
    ///   Depth 2 │ │ │ ├伤害 夏侯惇 → 曹操 1点
    ///   Depth 2 │ │ │ │      ← MessagePipe 处理扣血
    ///   Depth 2 │ │ │ └奸雄 曹操 获得杀牌
    ///   Depth 0 │ └── 完成
    /// </summary>
    public async Task Scenario_曹操杀夏侯惇()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  【场景1】曹操使用【杀】→ 夏侯惇");
        Console.WriteLine("═══════════════════════════════════════");

        var slashCard = new Card(CardType.杀, Suit.Spade, 7, "♠7杀");

        // ═══ 使用牌 ═══
        var useFrame = _pipeline.Push("使用牌", $"曹操 使用【杀】→ 夏侯惇");
        try
        {
            Console.WriteLine($"\n  {曹操.Name} 对 {夏侯惇.Name} 使用【杀】");

            // ═══ 造成伤害 ═══
            var dmgFrame = _pipeline.Push("伤害", $"曹操 → 夏侯惇 1点");
            try
            {
                var damage = new DamageEvent
                {
                    Source = 曹操,
                    Target = 夏侯惇,
                    Amount = 1,
                    SourceCard = slashCard
                };

                // ── 步骤A: MessagePipe 广播 DamageEvent ──
                await _damagePub.PublishAsync(damage, AsyncPublishStrategy.Sequential);
                // DamageExecutor 被执行，夏侯惇扣血

                // ── 步骤B: After-damage 技能 ──
                // 注意：以下技能触发是在 DamageEvent 处理完之后的
                // 但技能本身可能发布子事件（嵌套！）

                // B1: 夏侯惇·刚烈 → 嵌套判定事件
                await _gangLie.TryTrigger(damage);

                // B2: 曹操·奸雄 → 获得牌（如果曹操是受伤害者）
                await _jianXiong.TryTrigger(damage);

                Console.WriteLine($"\n  ═ 结算：{夏侯惇.Name} 剩余 {夏侯惇.Hp}/{夏侯惇.MaxHp}");
            }
            finally
            {
                _pipeline.Pop(dmgFrame);
            }
        }
        finally
        {
            _pipeline.Pop(useFrame);
        }
    }

    /// <summary>
    /// 孟获南蛮入侵 — 祸首替换伤害来源
    /// </summary>
    public async Task Scenario_孟获南蛮入侵()
    {
        Console.WriteLine("\n═══════════════════════════════════════");
        Console.WriteLine("  【场景2】孟获使用【南蛮入侵】");
        Console.WriteLine("═══════════════════════════════════════");

        var 南蛮Card = new Card(CardType.南蛮入侵, Suit.Spade, 1, "♠1南蛮入侵");
        var targets = AllPlayers.Where(p => p != 孟获).ToList();

        await _huoShou.On南蛮入侵Used(孟获, 南蛮Card, targets);

        Console.WriteLine($"\n  {孟获.Name} 使用【南蛮入侵】");
        Console.WriteLine($"  祸首→{孟获.Name}免疫，来源为{孟获.Name}");

        foreach (var target in targets)
        {
            var damage = new DamageEvent
            {
                Source = 孟获,
                Target = target,
                Amount = 1,
                SourceCard = 南蛮Card
            };
            _huoShou.TryRedirectSource(damage, 孟获);
            _huoShou.TryRedirectSource(damage, 孟获);

            var frame = _pipeline.Push("伤害", $"南蛮入侵→ {target.Name}");
            try
            {
                await _damagePub.PublishAsync(damage, AsyncPublishStrategy.Sequential);
                await _jianXiong.TryTrigger(damage);
            }
            finally
            {
                _pipeline.Pop(frame);
            }
        }
    }

    /// <summary>
    /// 黄月英使用锦囊 — 集智摸牌（链式触发）
    /// </summary>
    public async Task Scenario_黄月英集智()
    {
        Console.WriteLine("\n═══════════════════════════════════════");
        Console.WriteLine("  【场景3】黄月英使用【无中生有】");
        Console.WriteLine("═══════════════════════════════════════");

        var 无中Card = new Card(CardType.无中生有, Suit.Heart, 7, "♥7无中生有");

        var frame = _pipeline.Push("使用牌", $"{黄月英.Name} 使用【无中生有】");
        try
        {
            await _jiZhi.TryTrigger(new CardUsedEvent
            {
                Source = 黄月英, Card = 无中Card
            });
            Console.WriteLine($"\n  {黄月英.Name} 手牌数：{黄月英.HandCards.Count}");
        }
        finally
        {
            _pipeline.Pop(frame);
        }
    }

    /// <summary>
    /// 小乔天香 — 伤害目标重定向
    /// </summary>
    public async Task Scenario_小乔天香()
    {
        Console.WriteLine("\n═══════════════════════════════════════");
        Console.WriteLine("  【场景4】小乔天香转移伤害");
        Console.WriteLine("═══════════════════════════════════════");

        var damage = new DamageEvent
        {
            Source = 曹操,
            Target = 小乔,
            Amount = 2,
            SourceCard = new Card(CardType.决斗, Suit.Club, 3, "♣3决斗")
        };

        Console.WriteLine($"\n  原目标：{小乔.Name} 即将受到 {damage.Amount} 点伤害\n");

        var frame = _pipeline.Push("天香", $"小乔转移伤害→{夏侯惇.Name}");
        try
        {
            if (await _tianXiang.TryRedirect(damage, 夏侯惇))
            {
                Console.WriteLine($"  → 新目标：{夏侯惇.Name} 承受 {damage.Amount} 点伤害");
                await _damagePub.PublishAsync(damage, AsyncPublishStrategy.Sequential);
                await _gangLie.TryTrigger(damage);
                await _jianXiong.TryTrigger(damage);
            }
        }
        finally
        {
            _pipeline.Pop(frame);
        }
    }
}
