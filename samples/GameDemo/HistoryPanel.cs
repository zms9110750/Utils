using GameDemo.Events;
using MessagePipe;

namespace GameDemo;

/// <summary>
/// 历史面板 —— 订阅 StackChangedEvent，实时渲染事件栈
/// 
/// 这就是三国杀左侧那个历史面板的本质：
/// 一个只读订阅者，不干预游戏逻辑，只负责展示
/// </summary>
public class HistoryPanel : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly object _lock = new();
    private readonly List<string> _log = new();
    private bool _dirty;

    private static readonly ConsoleColor PushColor = ConsoleColor.Green;
    private static readonly ConsoleColor PopColor = ConsoleColor.DarkYellow;

    public HistoryPanel(IAsyncSubscriber<StackChangedEvent> subscriber)
    {
        _subscription = subscriber.Subscribe(async (evt, _) => {
            var depth = evt.Frame.Depth;
            var indent = new string('　', depth);
            var arrow = evt.Action == StackChangedEvent.ActionType.Push ? "▶" : "◀";
            var color = evt.Action == StackChangedEvent.ActionType.Push ? PushColor : PopColor;

            var line = $"{indent}{arrow} {evt.Frame.EventType,-12} {evt.Frame.Description}";

            lock (_lock)
            {
                _log.Add(line);
                _dirty = true;
            }

            await ValueTask.CompletedTask;
        });
    }

    /// <summary>刷新渲染 —— 把累积的日志一次性刷到控制台</summary>
    public void Flush()
    {
        lock (_lock)
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;

            Console.SetCursorPosition(0, Console.CursorTop - Math.Min(_log.Count, Console.WindowHeight - 3));
        }
    }

    /// <summary>打印当前全部历史</summary>
    public void PrintAll()
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║           结算历史 · 事件栈轨迹          ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");

        lock (_lock)
        {
            foreach (var line in _log)
            {
                if (line.Contains("▶"))
                {
                    Console.ForegroundColor = PushColor;
                }
                else
                {
                    Console.ForegroundColor = PopColor;
                }

                Console.WriteLine($"  {line}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine();
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
