using Autofac;
using Autofac.Core;
using Autofac.Pooling;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Extensions.Autofac;

/// <summary>
/// 基于内存缓存的智能对象池，支持自动清理长时间未使用的对象
/// </summary>
/// <typeparam name="T">池化对象的类型</typeparam>
public sealed class CacheObjectPool<T> : ObjectPool<T>, IPooledRegistrationPolicy<T>, IDisposable where T : class
{
    private readonly IMemoryCache _cache;
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly MemoryCacheEntryOptions _defaultOptions;
    private readonly string _poolId;
    private int _currentIndex;
    private bool _disposed;

    /// <summary>
    /// 初始化对象池的新实例
    /// </summary>
    /// <param name="cache">内存缓存实例</param>
    /// <param name="policy">对象池策略</param>
    /// <param name="options">缓存选项（可选）</param>
    /// <exception cref="ArgumentNullException">当cache或policy为null时抛出</exception>
    public CacheObjectPool(IMemoryCache cache, IPooledObjectPolicy<T> policy, MemoryCacheEntryOptions? options = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _defaultOptions = WithEvictionCallback(options ?? CreateDefaultOptions());
        _poolId = Guid.NewGuid().ToString("N");
        _currentIndex = -1;
    }

    /// <summary>
    /// 使用委托策略初始化对象池的新实例
    /// </summary>
    /// <param name="cache">内存缓存实例</param>
    /// <param name="createPolicy">对象创建委托</param>
    /// <param name="returnPolicy">对象返回委托</param>
    /// <param name="options">缓存选项（可选）</param>
    /// <exception cref="ArgumentNullException">当cache、createPolicy或returnPolicy为null时抛出</exception>
    public CacheObjectPool(IMemoryCache cache, Func<T> createPolicy, Func<T, bool> returnPolicy, MemoryCacheEntryOptions? options = null)
        : this(cache, new DelegatePooledObjectPolicy<T>(createPolicy, returnPolicy), options)
    {
    }

    /// <summary>
    /// 从池中获取一个对象。如果池为空，则使用策略创建新对象
    /// </summary>
    /// <returns>池化对象实例</returns>
    /// <exception cref="ObjectDisposedException">当对象池已释放时抛出</exception>
    public override T Get()
    {
        return TryTakeFromCache() ?? _policy.Create();
    }

    /// <summary>
    /// 将对象返回到池中。如果策略不允许返回，则对象不会被池化
    /// </summary>
    /// <param name="obj">要返回的对象</param>
    /// <exception cref="ObjectDisposedException">当对象池已释放时抛出</exception>
    public override void Return(T obj)
    {
        if (!TryPutInCache(obj))
        {
            (obj as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 清空池中的所有对象，但不释放对象池本身
    /// </summary>
    /// <remarks>
    /// 此方法会移除所有缓存的对象，但对象池实例仍然可用。
    /// 适用于需要强制刷新所有池化对象的场景。
    /// 在高并发环境下，可能会清理掉同时存入的对象。
    /// </remarks>
    public void Clear()
    {
        EnsureNotDisposed();

        // 原子获取当前索引并重置为-1
        int lastIndex = Interlocked.Exchange(ref _currentIndex, -1);

        // 清理从0到lastIndex的所有对象
        for (int i = 0; i <= lastIndex; i++)
        {
            var key = (_poolId, i);
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// 获取池中当前缓存的对象数量
    /// </summary>
    /// <remarks>
    /// 注意：此值是一个近似值，因为在多线程环境下可能发生变化
    /// </remarks>
    public int Count
    {
        get
        {
            int count = _currentIndex + 1;
            return count >= 0 ? count : 0;
        }
    }

    int IPooledRegistrationPolicy<T>.MaximumRetained { get; }

    private static MemoryCacheEntryOptions CreateDefaultOptions()
    {
        return WithEvictionCallback(new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        });
    }

    private static MemoryCacheEntryOptions WithEvictionCallback(MemoryCacheEntryOptions options)
    {
        options.RegisterPostEvictionCallback(static (_, value, _, _) =>
        {
            if (value is IDisposable disposable)
                disposable.Dispose();
        });
        return options;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheObjectPool<T>));
        }
    }

    /// <summary>
    /// 释放对象池使用的所有资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }

    T IPooledRegistrationPolicy<T>.Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<T> getFromPool)
    {
        return TryTakeFromCache() ?? getFromPool();
    }

    bool IPooledRegistrationPolicy<T>.Return(T pooledObject)
    {
        return TryPutInCache(pooledObject);
    }

    /// <summary>
    /// 从缓存中取出一个对象。没有时返回 null。
    /// </summary>
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

    /// <summary>
    /// 将对象存入缓存。成功返回 true，对象被拒绝时返回 false。
    /// </summary>
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
        var key = (_poolId, newIndex);
        _cache.Set(key, obj, _defaultOptions);
        return true;
    }
} 