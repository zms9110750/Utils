namespace zms9110750.Utils.Adapters.Demo.Polly.Models;

/// <summary>
/// 怪物定义
/// </summary>
public class Monster
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public string Element { get; set; } = "";
    public List<string> Skills { get; set; } = new();
}
