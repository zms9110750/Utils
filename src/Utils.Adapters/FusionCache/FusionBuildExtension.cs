using Microsoft.Extensions.DependencyInjection;
using NeoSmart.Caching.Sqlite;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;

namespace zms9110750.Extensions.FusionCache;

/// <summary>
/// FusionCache 构建器的扩展方法，用于快速配置 HybridCache。
/// </summary>
public static class FusionBuildExtension
{
    /// <summary>
    /// 将 FusionCache 配置为 <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> 的实现，
    /// 并使用 SQLite 作为二级缓存。
    /// </summary>
    /// <param name="fusionBuild">FusionCache 构建器。</param>
    /// <param name="cachePath">SQLite 数据库文件路径，默认 <c>cache.sqlite.db</c>。</param>
    /// <param name="jsonOptions">可选的 JSON 序列化选项。</param>
    /// <param name="optionsAction">可选的回调，用于自定义默认缓存条目选项。
    /// 不传时默认二级缓存有效期 <c>365000 天</c>（几乎永久）。</param>
    /// <returns>配置后的 <see cref="IFusionCacheBuilder"/>，可继续链式调用。</returns>
    /// <remarks>
    /// 此方法自动完成以下配置：
    /// <list type="bullet">
    ///   <item><c>AddMemoryCache</c> — 一级内存缓存</item>
    ///   <item><c>AddSqliteCache</c> — 二级 SQLite 缓存</item>
    ///   <item><c>AddFusionCacheSystemTextJsonSerializer</c> — JSON 序列化</item>
    ///   <item><c>WithDefaultEntryOptions</c> — 默认条目选项</item>
    ///   <item><c>TryWithAutoSetup</c> — 自动设置后台清理</item>
    ///   <item><c>AsHybridCache</c> — 注册为 <c>HybridCache</c> 实现</item>
    /// </list>
    /// </remarks>
    public static IFusionCacheBuilder SetupSqliteCache(
        this IFusionCacheBuilder fusionBuild,
        string cachePath = "cache.sqlite.db",
        JsonSerializerOptions? jsonOptions = null,
        Action<FusionCacheEntryOptions>? optionsAction = null)
    {
        fusionBuild.Services
          .AddMemoryCache()
          .AddSqliteCache(cachePath)
          .AddFusionCacheSystemTextJsonSerializer(jsonOptions);

        return fusionBuild
                .WithDefaultEntryOptions(optionsAction ?? (options =>
                        options.DistributedCacheDuration = TimeSpan.FromDays(365 * 1000)))
                .TryWithAutoSetup()
                .AsHybridCache();
    }
}
