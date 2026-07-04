using GameDemo.Events;
using GameDemo.Models;
using MessagePipe;

namespace GameDemo;

/// <summary>
/// 游戏事件管道 —— 包装 MessagePipe，添加栈追踪
/// 
/// 核心设计：
///   Publish 前 → 压 EventFrame 入栈 → Publish → 出栈
///   每个事件知道自己在栈中的深度，async/await 维持 LIFO 顺序
/// </summary>
public class GameEventPipeline
{
    private readonly Stack<EventFrame> _stack = new();
    private readonly IAsyncPublisher<StackChangedEvent> _stackPublisher;
    private readonly object _lock = new();
    public IReadOnlyList<EventFrame> CurrentStack
    {
        get
        {
            lock (_lock)
            {
                return _stack.Reverse().ToList();
            }
        }
    }

    public GameEventPipeline(IAsyncPublisher<StackChangedEvent> stackPublisher)
    {
        _stackPublisher = stackPublisher;
    }

    /// <summary>压入栈帧并发布栈变化</summary>
    public EventFrame Push(string eventType, string description)
    {
        var frame = new EventFrame(eventType, _stack.Count, description);
        lock (_lock)
        {
            _stack.Push(frame);
        }

        _stackPublisher.Publish(new StackChangedEvent(StackChangedEvent.ActionType.Push, frame));
        return frame;
    }

    /// <summary>弹出栈帧并发布栈变化</summary>
    public void Pop(EventFrame frame)
    {
        lock (_lock)
        {
            _stack.Pop();
        }

        _stackPublisher.Publish(new StackChangedEvent(StackChangedEvent.ActionType.Pop, frame));
    }
}
