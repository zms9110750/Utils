using MessagePipe;
using GameDemo.Events;
using GameDemo.Models;

namespace GameDemo.Skills;

/// <summary>
/// 黄月英 · 集智
/// 
/// 当你使用一张非转化的普通锦囊牌时，你可以摸一张牌。
/// 
/// 展示了「事件发生后触发新事件」
/// </summary>
public class 集智
{
    private readonly IAsyncPublisher<DrawCardEvent> _drawPub;
    private readonly GameEventPipeline _pipeline;
    private static readonly CardType[] 锦囊牌 = {
        CardType.过河拆桥, CardType.顺手牵羊,
        CardType.无中生有, CardType.南蛮入侵, CardType.决斗
    };

    public 集智(IAsyncPublisher<DrawCardEvent> drawPub, GameEventPipeline pipeline)
    {
        _drawPub = drawPub;
        _pipeline = pipeline;
    }

    public async ValueTask TryTrigger(CardUsedEvent cardEvent)
    {
        if (!cardEvent.Source.ActiveSkills.Contains("集智")) return;

        // 只有锦囊牌触发
        if (!锦囊牌.Contains(cardEvent.Card.Type)) return;

        var frame = _pipeline.Push("集智", $"{cardEvent.Source.Name} 使用锦囊摸牌");
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  📜 【集智】{cardEvent.Source.Name} 使用 {cardEvent.Card}，摸一张牌！");
            Console.ResetColor();

            await _drawPub.PublishAsync(new DrawCardEvent
            {
                Target = cardEvent.Source,
                Reason = "集智"
            }, AsyncPublishStrategy.Sequential);
        }
        finally
        {
            _pipeline.Pop(frame);
        }
    }
}
