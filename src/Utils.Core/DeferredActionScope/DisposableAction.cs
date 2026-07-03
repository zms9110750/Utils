namespace zms9110750.Utils.Core.Primitives;

/// <summary>
/// 将 <see cref="Action"/> 包装为 <see cref="IDisposable"/>，调用 <see cref="Dispose()"/> 时执行该委托
/// </summary>
/// <remarks>
/// 包装一个委托，Dispose 时执行
/// </remarks>
/// <param name="action">要延迟执行的委托</param>
/// <exception cref="ArgumentNullException">action 为 null 时抛出</exception>
public sealed class DisposableAction(Action action) : IDisposable
{
    private Action? _action = action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// 执行包装的委托（仅一次）
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}
