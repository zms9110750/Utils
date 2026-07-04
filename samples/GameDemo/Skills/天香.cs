using GameDemo.Events;
using GameDemo.Models;

namespace GameDemo.Skills;

/// <summary>
/// 小乔 · 天香
/// 
/// 当你受到伤害时，你可以弃置一张红色牌，
/// 将此伤害转移给另一名角色。
/// 
/// 展示了「事件修改」—— 在伤害发生前改变 Target
/// </summary>
public class 天香
{
    public async ValueTask<bool> TryRedirect(DamageEvent damage, Player? redirectTarget)
    {
        if (!damage.Target.ActiveSkills.Contains("天香"))
        {
            return false;
        }

        if (!damage.Target.IsAlive)
        {
            return false;
        }

        if (redirectTarget == null)
        {
            return false;
        }

        // 小乔弃一张红色牌（demo 简化：假设有）
        var redCard = damage.Target.HandCards.FirstOrDefault(c =>
            c.Suit is Suit.Heart or Suit.Diamond);

        if (redCard == null)
        {
            return false;
        }

        damage.Target.HandCards.Remove(redCard);

        // ── 修改事件：转移伤害目标 ──
        var originalTarget = damage.Target;
        damage.Target = redirectTarget;

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"  🌸 【天香】{originalTarget.Name} 弃 {redCard}，" +
                          $"转移伤害 → {redirectTarget.Name}");
        Console.ResetColor();

        await ValueTask.CompletedTask;
        return true;  // 伤害已转移
    }
}
