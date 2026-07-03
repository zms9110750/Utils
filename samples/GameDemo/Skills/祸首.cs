using GameDemo.Events;
using GameDemo.Models;

namespace GameDemo.Skills;

/// <summary>
/// 孟获 · 祸首
/// 
/// 锁定技：
///   ①【南蛮入侵】对你无效
///   ②当其他角色使用【南蛮入侵】指定目标后，
///     你代替其成为此牌的伤害来源。
/// 
/// 展示了「事件 Source 替换」
/// </summary>
public class 祸首
{
    public bool Is南蛮无效(Player target, Card card)
    {
        if (card.Type != CardType.南蛮入侵) return false;
        return target.ActiveSkills.Contains("祸首");
    }

    /// <summary>替换伤害来源为孟获</summary>
    public bool TryRedirectSource(DamageEvent damage, Player mengHuo)
    {
        if (!mengHuo.ActiveSkills.Contains("祸首")) return false;
        if (damage.SourceCard?.Type != CardType.南蛮入侵) return false;
        if (damage.Source == mengHuo) return true;  // 已经就是祸首

        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"  🪓 【祸首】{mengHuo.Name} 代替成为南蛮入侵的伤害来源！");
        Console.ResetColor();

        damage.Source = mengHuo;   // ← 修改事件！
        return true;
    }

    public ValueTask On南蛮入侵Used(Player source, Card card, List<Player> targets)
    {
        // 祸首让南蛮对孟获无效
        targets.RemoveAll(t => t.ActiveSkills.Contains("祸首"));

        return ValueTask.CompletedTask;
    }
}
