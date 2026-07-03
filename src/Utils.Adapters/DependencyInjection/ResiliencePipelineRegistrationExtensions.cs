using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace zms9110750.Extensions.DependencyInjection;

/// <summary>
/// 弹性管道的注册扩展方法，用于在 IServiceCollection 上注册 keyed resilience pipeline。
/// </summary>
public static class ResiliencePipelineRegistrationExtensions
{
    /// <summary>
    /// 注册一个带 key 的弹性管道。管道以 keyed singleton 方式注册，
    /// 首次解析时才构建，因此 <see cref="ResiliencePipelineBuilderContext.ServiceProvider"/> 可用。
    /// </summary>
    /// <typeparam name="T">管道结果类型。</typeparam>
    /// <param name="services">服务容器。</param>
    /// <param name="name">管道名称（同时也是 DI key）。</param>
    /// <param name="configure">配置管道的回调。</param>
    /// <remarks>
    /// 在 Autofac 中可通过 <c>container.ResolveKeyed&lt;ResiliencePipeline&lt;T&gt;&gt;(name)</c> 解析；
    /// 在 MS DI 中可通过 <c>sp.GetRequiredKeyedService&lt;ResiliencePipeline&lt;T&gt;&gt;(name)</c> 解析。
    /// </remarks>
    public static void AddResiliencePipeline<T>(
        this IServiceCollection services,
        string name,
        Action<ResiliencePipelineBuilder<T>, ResiliencePipelineBuilderContext> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddKeyedSingleton(name, (sp, key) =>
        {
            var pipelineBuilder = new ResiliencePipelineBuilder<T>();
            var ctx = new ResiliencePipelineBuilderContext(sp, name);
            configure(pipelineBuilder, ctx);
            return pipelineBuilder.Build();
        });
    }
}
