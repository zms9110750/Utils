using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace zms9110750.Extensions.Autofac;

/// <summary>
/// 基于内存缓存的 <see cref="ObjectPoolProvider"/>，创建 <see cref="CacheObjectPool{T}"/>。
/// </summary>
public class CacheObjectPoolProvider(
    IMemoryCache cache,
    IServiceProvider serviceProvider,
    MemoryCacheEntryOptions? cacheOptions = null) : ObjectPoolProvider
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private static readonly ConcurrentDictionary<Type, object> _policies = new();

    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class
    {
        if (_policies.Count > 16)
        {
            _policies.Clear();
        }
        var p = (IPooledObjectPolicy<T>)_policies.GetOrAdd(typeof(T), _ => new DIPooledObjectPolicy<T>(_serviceProvider));

        return new CacheObjectPool<T>(_cache, p, cacheOptions);
    }
}
