using GameDemo.Events;

namespace GameDemo.Skills;

/// <summary>
/// 曹操 · 奸雄
/// 
/// 当你受到伤害后，你可以获得对你造成伤害的牌。
/// 
/// 这个技能在伤害事件「后」触发，展示了 after-hook 模式
/// </summary>
public class 奸雄
{
    private readonly GameEventPipeline _pipeline;

    public 奸雄(GameEventPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async ValueTask TryTrigger(DamageEvent damage)
    {
        if (!damage.Target.ActiveSkills.Contains("奸雄"))
        {
            return;
        }

        if (!damage.Target.IsAlive)
        {
            return;
        }

        if (damage.SourceCard == null)
        {
            return;
        }

        var frame = _pipeline.Push("奸雄", $"{damage.Target.Name} 获得牌 {damage.SourceCard}");
        try
        {
            damage.Target.HandCards.Add(damage.SourceCard);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  👑 【奸雄】{damage.Target.Name} 获得了 {damage.SourceCard}");
            Console.ResetColor();
        }
        finally
        {
            _pipeline.Pop(frame);
        }

        await ValueTask.CompletedTask;
    }
}
