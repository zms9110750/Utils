// Copyright (c) zms9110750. All rights reserved.
// Licensed under the MIT License.

using Polly;
using System.Diagnostics.CodeAnalysis;

namespace zms9110750.Extensions.Polly;

/// <summary>
/// 弹性管道的扩展方法，支持将 key 传播到 <see cref="ResilienceContext.OperationKey"/>，
/// 供缓存策略（如 Axion.Extensions.Polly.Caching.Hybrid）作为缓存键使用。
/// </summary>
public static class PipelineExecutionExtensions
{
    /// <summary>
    /// 执行非泛型 <see cref="ResiliencePipeline"/>，将 key 注入 <see cref="ResilienceContext.OperationKey"/>。
    /// </summary>
    public static async Task<TResult> ExecuteWithKeyAsync<TResult>(
        this ResiliencePipeline pipeline,
        string key,
        Func<ResilienceContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNull(pipeline);
        ThrowIfNull(key);
        ThrowIfNull(operation);

        var context = ResilienceContextPool.Shared.Get(key, cancellationToken);
        try
        {
            return await pipeline.ExecuteAsync(
                ctx => operation(ctx, ctx.CancellationToken),
                context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// 执行泛型 <see cref="ResiliencePipeline{T}"/>，将 key 注入 <see cref="ResilienceContext.OperationKey"/>。
    /// </summary>
    public static async Task<TResult> ExecuteGenericWithKeyAsync<TResult>(
        this ResiliencePipeline<TResult> pipeline,
        string key,
        Func<ResilienceContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNull(pipeline);
        ThrowIfNull(key);
        ThrowIfNull(operation);

        var context = ResilienceContextPool.Shared.Get(key, cancellationToken);
        try
        {
            return await pipeline.ExecuteAsync(
                ctx => operation(ctx, ctx.CancellationToken),
                context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    static void ThrowIfNull([NotNull] object? argument, string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(argument, paramName);
#else
        if (argument is null) throw new ArgumentNullException(paramName ?? nameof(argument));
#endif
    }
}
