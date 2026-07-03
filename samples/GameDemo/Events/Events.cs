using GameDemo.Models;

namespace GameDemo.Events;

// ══════════════════════════════════════════════
//  游戏事件 —— 所有事件都是可变的 class，技能可以修改属性
// ══════════════════════════════════════════════

/// <summary>卡牌被使用</summary>
public class CardUsedEvent
{
    public Player Source { get; init; } = null!;
    public Card Card { get; init; } = null!;
    public List<Player> Targets { get; init; } = new();
    public bool IsCancelled { get; set; }
}

/// <summary>伤害事件 (核心展示对象)</summary>
public class DamageEvent
{
    public Player Source { get; set; } = null!;   // 可被技能改（孟获祸首）
    public Player Target { get; set; } = null!;    // 可被改（小乔天香）
    public int Amount { get; set; }                 // 可被改
    public Card? SourceCard { get; init; }          // 来源牌（曹操奸雄要拿这个）
    public bool IsCancelled { get; set; }           // 可被取消
    public bool IsProcessed { get; set; }           // 实际扣血是否执行

    public override string ToString()
        => $"{Source.Name} → {Target.Name} {Amount}点";
}

/// <summary>判定事件 (刚烈触发)</summary>
public class JudgeEvent
{
    public Player Target { get; init; } = null!;
    public string Reason { get; init; } = "";
    public Card? ResultCard { get; set; }
    public bool IsRed => ResultCard?.Suit is Suit.Heart or Suit.Diamond;
    public bool IsHeart => ResultCard?.Suit == Suit.Heart;
}

/// <summary>摸牌事件 (黄月英集智)</summary>
public class DrawCardEvent
{
    public Player Target { get; init; } = null!;
    public int Count { get; set; } = 1;
    public string Reason { get; init; } = "";
}

/// <summary>栈变化事件 —— 给 HistoryPanel 渲染用</summary>
public class StackChangedEvent
{
    public enum ActionType { Push, Pop }

    public ActionType Action { get; }
    public EventFrame Frame { get; }

    public StackChangedEvent(ActionType action, EventFrame frame)
    {
        Action = action;
        Frame = frame;
    }
}
