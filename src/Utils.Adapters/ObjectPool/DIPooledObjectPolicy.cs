using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Extensions.Autofac;

/// <summary>
/// 通过 <see cref="IServiceProvider"/> 解析实例的对象池策略。
/// </summary>
/// <typeparam name="T">池化对象的类型。</typeparam>
public class DIPooledObjectPolicy<T>(IServiceProvider serviceProvider) : IPooledObjectPolicy<T> where T : class
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public T Create()
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public bool Return(T obj)
    {
        return obj is not IResettable || ((IResettable)obj).TryReset();
    }
}
