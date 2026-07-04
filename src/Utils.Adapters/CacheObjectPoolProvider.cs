using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Utils.Adapters;

/// <summary>
/// 基于内存缓存的 <see cref="ObjectPoolProvider"/>，创建 <see cref="CacheObjectPool{T}"/>。
/// </summary>
public class CacheObjectPoolProvider : ObjectPoolProvider
{
    private readonly IMemoryCache _cache;
    private readonly CacheObjectPoolOptions _options;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>初始化。</summary>
    /// <param name="cache">内存缓存实例。</param>
    /// <param name="options">池选项（可选）。</param>
    /// <param name="serviceProvider">服务提供器，供池内策略按需解析依赖（可选）。</param>
    public CacheObjectPoolProvider(
        IMemoryCache cache,
        CacheObjectPoolOptions? options = null,
        IServiceProvider? serviceProvider = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? new CacheObjectPoolOptions();
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        return new CacheObjectPool<T>(_cache, policy, _options.ToMemoryCacheEntryOptions(), _serviceProvider);
    }
}
