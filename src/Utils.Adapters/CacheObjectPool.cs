using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Utils.Adapters;

/// <summary>
/// 基于内存缓存的智能对象池，支持自动清理长时间未使用的对象。
/// 可直接通过 <see cref="Get"/> / <see cref="Return"/> 手动管理（哥布林模式），
/// 也可通过 <see cref="CacheObjectPoolProvider"/> 接入 Autofac / MS DI 的池化注册。
/// </summary>
/// <typeparam name="T">池化对象的类型。</typeparam>
public sealed class CacheObjectPool<T> : ObjectPool<T>, IDisposable where T : class
{
    private readonly IMemoryCache _cache;
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly MemoryCacheEntryOptions _defaultOptions;
    private readonly string _poolId;
    private readonly IServiceProvider? _serviceProvider;
    private int _currentIndex;
    private bool _disposed;

    /// <summary>初始化。</summary>
    /// <param name="cache">内存缓存实例。</param>
    /// <param name="policy">对象池策略。</param>
    /// <param name="options">缓存选项（可选）。</param>
    /// <param name="serviceProvider">服务提供器，供内部按需解析依赖（可选）。</param>
    public CacheObjectPool(
        IMemoryCache cache,
        IPooledObjectPolicy<T> policy,
        MemoryCacheEntryOptions? options = null,
        IServiceProvider? serviceProvider = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _defaultOptions = CreateDefaultEntryOptions(options);
        _poolId = Guid.NewGuid().ToString("N");
        _serviceProvider = serviceProvider;
        _currentIndex = -1;
    }

    /// <summary>
    /// 从池中获取一个对象。如果池为空，则使用策略创建新对象。
    /// </summary>
    public override T Get()
    {
        return TryTakeFromCache() ?? _policy.Create();
    }

    /// <summary>
    /// 将对象返回到池中。如果策略不允许返回，则对象不会被池化。
    /// </summary>
    public override void Return(T obj)
    {
        if (!TryPutInCache(obj))
        {
            (obj as IDisposable)?.Dispose();
        }
    }

    /// <summary>清空池中的所有缓存对象，但不释放池本身。</summary>
    public void Clear()
    {
        EnsureNotDisposed();
        int lastIndex = Interlocked.Exchange(ref _currentIndex, -1);
        for (int i = 0; i <= lastIndex; i++)
        {
            _cache.Remove((_poolId, i));
        }
    }

    /// <summary>获取池中当前缓存的对象数量（近似值）。</summary>
    public int Count
    {
        get
        {
            int count = _currentIndex + 1;
            return count >= 0 ? count : 0;
        }
    }

    /// <summary>获取服务提供器，可在策略外部按需解析依赖。</summary>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>释放对象池，清空缓存并销毁缓存对象。</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }

    private static MemoryCacheEntryOptions CreateDefaultEntryOptions(MemoryCacheEntryOptions? options)
    {
        var result = options ?? new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };

        result.RegisterPostEvictionCallback(static (_, value, _, _) =>
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });

        return result;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheObjectPool<T>));
        }
    }

    private T? TryTakeFromCache()
    {
        EnsureNotDisposed();
        int startIndex = Volatile.Read(ref _currentIndex);
        for (int i = startIndex; i >= 0; i = Interlocked.Decrement(ref _currentIndex))
        {
            var key = (_poolId, i);
            if (_cache.TryGetValue(key, out T obj))
            {
                _cache.Remove(key);
                return obj;
            }
        }
        return null;
    }

    private bool TryPutInCache(T obj)
    {
        EnsureNotDisposed();
        if (obj is null ||
           !_policy.Return(obj) ||
           (obj is IResettable resettable && !resettable.TryReset()))
        {
            return false;
        }

        int newIndex = Interlocked.Increment(ref _currentIndex);
        _cache.Set((_poolId, newIndex), obj, _defaultOptions);
        return true;
    }
}
