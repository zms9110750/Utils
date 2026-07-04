using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace zms9110750.Extensions.Autofac;

/// <summary>
/// 基于内存缓存的智能对象池，支持自动清理长时间未使用的对象。
/// 可直接通过 <see cref="Get"/> / <see cref="Return"/> 手动管理（哥布林模式），
/// 也可通过 <see cref="CacheObjectPoolProvider"/> 接入 Autofac / MS DI 的池化注册。
/// </summary>
/// <typeparam name="T">池化对象的类型。</typeparam>
public sealed class CacheObjectPool<T>(
    IMemoryCache cache,
    IPooledObjectPolicy<T> policy,
    MemoryCacheEntryOptions? options = null) : ObjectPool<T>, IDisposable where T : class
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IPooledObjectPolicy<T> _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    private readonly MemoryCacheEntryOptions _defaultOptions = (options ?? new MemoryCacheEntryOptions {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    }).WithEvictionCallback();
    private readonly string _poolId = Guid.NewGuid().ToString("N");
    private int _currentIndex = -1;
    private bool _disposed;

    public override T Get()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheObjectPool<T>));
        }

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
        return _policy.Create();
    }

    public override void Return(T obj)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheObjectPool<T>));
        }

        if (obj is null ||
           !_policy.Return(obj) ||
           (obj is IResettable resettable && !resettable.TryReset()))
        {
            (obj as IDisposable)?.Dispose();
            return;
        }

        int newIndex = Interlocked.Increment(ref _currentIndex);
        _cache.Set((_poolId, newIndex), obj, _defaultOptions);
    }

    public void Clear()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheObjectPool<T>));
        }

        int lastIndex = Interlocked.Exchange(ref _currentIndex, -1);
        for (int i = 0; i <= lastIndex; i++)
        {
            _cache.Remove((_poolId, i));
        }
    }

    public int Count
    {
        get
        {
            int count = _currentIndex + 1;
            return count >= 0 ? count : 0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }
}

file static class MemoryCacheEntryOptionsExtensions
{
    public static MemoryCacheEntryOptions WithEvictionCallback(this MemoryCacheEntryOptions options)
    {
        options.RegisterPostEvictionCallback(static (_, value, _, _) => {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });
        return options;
    }
}
