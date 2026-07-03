# 外部包引用记录

本仓库已删除的自有实现，改用以下 NuGet 包替代：

## Axion.Extensions.Polly.Caching.Hybrid

替代了 `Utils.Adapters/Polly/` 下的 HybridCache 策略。

```
dotnet add package Axion.Extensions.Polly.Caching.Hybrid --version 10.4.0
dotnet add package Microsoft.Extensions.Caching.Hybrid
```

**用法：**

```csharp
services.AddHybridCache();

var pipeline = new ResiliencePipelineBuilder<string>()
    .AddCaching(new()
    {
        HybridCache = sp.GetRequiredService<HybridCache>(),
        CacheKeyProvider = ctx => new ValueTask<string>("your-cache-key"),
    })
    .Build();

var result = await pipeline.ExecuteAsync(async ct =>
{
    return await SomeExpensiveOperationAsync(ct);
});
```

- 缓存键通过 `CacheKeyProvider` 动态设置（也支持 `context.OperationKey`）
- 支持 Cache Hit/Miss/ReadError/WriteError 事件回调
- 缓存异常自动降级执行用户代码，不会炸

---

## Soenneker.HttpClients.LoggingHandler

替代了 `Utils.Adapters/HttpMessageHandler/` 下的 HTTP 日志 handler。

```
dotnet add package Soenneker.HttpClients.LoggingHandler
```

**用法：**

```csharp
services.AddHttpClient("MyClient")
    .AddHttpMessageHandler(sp =>
        new HttpClientLoggingHandler(
            sp.GetRequiredService<ILogger<HttpClientLoggingHandler>>(),
            new HttpClientLoggingOptions
            {
                LogRequestBody = true,
                LogResponseBody = true,
                LogRequestHeaders = true,
                LogResponseHeaders = true,
                RedactedHeaders = ["Authorization", "Cookie"],
                MaxBodyLogLength = 2000,
                LogLevel = LogLevel.Information
            }));
```

- 支持请求/响应的行、头、体日志（独立 bool 开关）
- 敏感头脱敏
- Body 长度截断
- 内建耗时记录
