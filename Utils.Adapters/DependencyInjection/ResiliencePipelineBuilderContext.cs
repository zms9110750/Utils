namespace zms9110750.Extensions.DependencyInjection;

/// <summary>
/// 构建弹性管道时的上下文，提供运行时所需的信息。
/// 与 HTTP 弹性包中的 <c>AddResilienceHandlerContext</c> 作用相同。
/// </summary>
public class ResiliencePipelineBuilderContext
{
    /// <summary>
    /// 初始化 <see cref="ResiliencePipelineBuilderContext"/> 的新实例。
    /// </summary>
    /// <param name="serviceProvider">用于解析依赖的服务提供器。</param>
    /// <param name="pipelineName">正在配置的管道名称。</param>
    public ResiliencePipelineBuilderContext(IServiceProvider serviceProvider, string pipelineName)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        PipelineName = pipelineName ?? throw new ArgumentNullException(nameof(pipelineName));
    }

    /// <summary>获取服务提供器，用于解析依赖。</summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>获取正在配置的管道名称。</summary>
    public string PipelineName { get; }
}
