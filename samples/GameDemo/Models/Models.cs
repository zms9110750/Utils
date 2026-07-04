namespace GameDemo.Models;

/// <summary>卡牌花色</summary>
public enum Suit { Spade, Club, Diamond, Heart }

/// <summary>卡牌类型</summary>
public enum CardType { 杀, 闪, 桃, 过河拆桥, 顺手牵羊, 无中生有, 南蛮入侵, 决斗 }

/// <summary>一张卡牌</summary>
public record Card(CardType Type, Suit Suit, int Number, string Name)
{
    public override string ToString()
    {
        return $"{Name}";
    }
}

/// <summary>玩家</summary>
public class Player
{
    public string Name { get; }
    public string SkillName { get; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public List<Card> HandCards { get; } = new();
    public List<string> ActiveSkills { get; } = new();

    public Player(string name, string skill, int hp)
    {
        Name = name;
        SkillName = skill;
        Hp = MaxHp = hp;
        ActiveSkills.Add(skill);
    }

    public bool IsAlive => Hp > 0;
    public override string ToString()
    {
        return $"{Name}[{Hp}/{MaxHp}]";
    }
}

/// <summary>事件栈帧 —— 记录当前在栈中的位置</summary>
public class EventFrame
{
    public string EventType { get; }
    public int Depth { get; set; }
    public string Description { get; }
    public DateTime Time { get; } = DateTime.Now;

    public EventFrame(string eventType, int depth, string description)
    {
        EventType = eventType;
        Depth = depth;
        Description = description;
    }

    public string Indent => new(' ', Depth * 2);
}
