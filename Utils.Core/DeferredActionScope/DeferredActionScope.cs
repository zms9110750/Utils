using System.Collections;

namespace zms9110750.Utils.Core.Primitives;

/// <summary>
/// 延迟操作作用域，用于批量管理释放操作和延迟执行的动作
/// </summary>
public sealed class DeferredActionScope : ICollection<IDisposable>, IDisposable
{
    private readonly HashSet<IDisposable> _disposables = new();

    public int Count => ((ICollection<IDisposable>)_disposables).Count;

    public bool IsReadOnly => ((ICollection<IDisposable>)_disposables).IsReadOnly;

    /// <summary>
    /// 添加一个释放资源
    /// </summary>
    public void Add(IDisposable item)
    {
        ((ICollection<IDisposable>)_disposables).Add(item);
    }

    /// <summary>
    /// 添加一个延迟执行的动作，会被包装为 <see cref="DisposableAction"/> 统一管理
    /// </summary>
    public void Add(Action action)
    {
        ((ICollection<IDisposable>)_disposables).Add(new DisposableAction(action));
    }

    public void Clear()
    {
        ((ICollection<IDisposable>)_disposables).Clear();
    }

    public bool Contains(IDisposable item)
    {
        return ((ICollection<IDisposable>)_disposables).Contains(item);
    }

    public void CopyTo(IDisposable[] array, int arrayIndex)
    {
        ((ICollection<IDisposable>)_disposables).CopyTo(array, arrayIndex);
    }

    public void Dispose()
    {
        foreach (var item in _disposables)
        {
            item.Dispose();
        }
        _disposables.Clear();
    }

    public IEnumerator<IDisposable> GetEnumerator()
    {
        return ((IEnumerable<IDisposable>)_disposables).GetEnumerator();
    }

    public bool Remove(IDisposable item)
    {
        return ((ICollection<IDisposable>)_disposables).Remove(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<IDisposable>)_disposables).GetEnumerator();
    }
}
