using Microsoft.Extensions.Caching.Memory;

namespace zms9110750.Utils.Adapters;

/// <summary>
/// <see cref="CacheObjectPool{T}"/> 的配置选项。
/// </summary>
public class CacheObjectPoolOptions
{
    /// <summary>缓存条目的滑动过期时间，默认 5 分钟。</summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>是否在缓存逐出时自动释放对象（若实现 <see cref="IDisposable"/>）。</summary>
    public bool AutoDisposeOnEviction { get; set; } = true;

    internal MemoryCacheEntryOptions ToMemoryCacheEntryOptions()
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = SlidingExpiration
        };

        if (AutoDisposeOnEviction)
        {
            options.RegisterPostEvictionCallback(static (_, value, _, _) =>
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
        }

        return options;
    }
}
