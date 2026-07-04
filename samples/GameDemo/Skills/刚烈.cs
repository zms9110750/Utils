using GameDemo.Events;
using MessagePipe;

namespace GameDemo.Skills;

/// <summary>
/// 夏侯惇 · 刚烈
/// 
/// 当你受到伤害后，你可以进行一次判定，
/// 若结果不为♥️，伤害来源选择一项：
///   1. 弃置两张手牌
///   2. 受到你对其造成的1点伤害
/// 
/// 这个技能展示了「嵌套事件」—— 伤害事件中插入判定事件
/// </summary>
public class 刚烈
{
    private readonly IAsyncPublisher<JudgeEvent> _judgePub;
    private readonly IAsyncPublisher<DamageEvent> _damagePub;
    private readonly GameEventPipeline _pipeline;

    public 刚烈(
        IAsyncPublisher<JudgeEvent> judgePub,
        IAsyncPublisher<DamageEvent> damagePub,
        GameEventPipeline pipeline)
    {
        _judgePub = judgePub;
        _damagePub = damagePub;
        _pipeline = pipeline;
    }

    /// <summary>检查并触发刚烈</summary>
    public async ValueTask TryTrigger(DamageEvent damage)
    {
        if (!damage.Target.ActiveSkills.Contains("刚烈"))
        {
            return;
        }

        if (!damage.Target.IsAlive)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ⚔ 【刚烈】{damage.Target.Name} 受到伤害，触发判定！");
        Console.ResetColor();

        // ── 插入结算：发布判定事件（嵌套！）──
        // 这里 await 保证了：判定事件及其子事件全部完成后，才回到这里
        var judgeFrame = _pipeline.Push("判定", $"{damage.Target.Name}·刚烈");
        try
        {
            var judge = new JudgeEvent {
                Target = damage.Target,
                Reason = "刚烈"
            };
            await _judgePub.PublishAsync(judge, AsyncPublishStrategy.Sequential);

            // 判定结果不为♥ → 伤害来源受1点伤害
            if (!judge.IsHeart)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ⚔ 【刚烈】判定 {judge.ResultCard} 不为♥，反伤！");
                Console.ResetColor();

                // ── 再次嵌套：反伤！──
                // 这是第三层嵌套：DamageEvent inside JudgeEvent inside DamageEvent
                var subFrame = _pipeline.Push("反伤", $"{damage.Target.Name} → {damage.Source.Name}");
                try
                {
                    await _damagePub.PublishAsync(new DamageEvent {
                        Source = damage.Target,
                        Target = damage.Source,
                        Amount = 1,
                        SourceCard = damage.SourceCard
                    }, AsyncPublishStrategy.Sequential);
                }
                finally
                {
                    _pipeline.Pop(subFrame);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  【刚烈】判定 {judge.ResultCard} 为♥，无事发生");
                Console.ResetColor();
            }
        }
        finally
        {
            _pipeline.Pop(judgeFrame);
        }
    }
}
