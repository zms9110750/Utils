# Utils.Adapters — 第三方库适配层

为 Autofac、Polly、FusionCache、MS DI 等库提供便捷的扩展方法。

## 模块

| 文件夹 | 命名空间 | 说明 |
|--------|---------|------|
| **ObjectPool** | `zms9110750.Extensions.Autofac` | 对象池策略（CacheObjectPool、DelegatePooledObjectPolicy 等） |
| **DependencyInjection** | `zms9110750.Extensions.DependencyInjection` | `AddResiliencePipeline` 扩展，在 IServiceCollection 上注册 keyed 弹性管道 |
| **FusionCache** | `zms9110750.Extensions.FusionCache` | FusionCache 构建扩展 |
| **Polly** | `zms9110750.Extensions.Polly` | `ExecuteWithKeyAsync` / `ExecuteGenericWithKeyAsync`，支持 OperationKey 传播 |

## 依赖

- Autofac 9.3.0
- Polly.Core 8.7.0
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.9
- ZiggyCreatures.FusionCache 2.6.0
- NeoSmart.Caching.Sqlite.AspNetCore 9.0.1

## 目标框架

`netstandard2.1;net6.0`
