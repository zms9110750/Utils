using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Extensions.Autofac;

/// <summary>
/// 将两个委托包装为 <see cref="IPooledObjectPolicy{T}"/>。
/// </summary>
internal sealed class DelegatePooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : class
{
    private readonly Func<T> _createPolicy;
    private readonly Func<T, bool> _returnPolicy;

    public DelegatePooledObjectPolicy(Func<T> createPolicy, Func<T, bool> returnPolicy)
    {
        _createPolicy = createPolicy ?? throw new ArgumentNullException(nameof(createPolicy));
        _returnPolicy = returnPolicy ?? throw new ArgumentNullException(nameof(returnPolicy));
    }

    public T Create()
    {
        return _createPolicy();
    }

    public bool Return(T obj)
    {
        return _returnPolicy(obj);
    }
}
