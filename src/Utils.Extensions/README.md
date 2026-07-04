# Utils.Extensions — 扩展方法集

手写扩展方法 + 源生成器批量生成的扩展方法。

## 手写模块

| 文件 | 命名空间 | 说明 |
|------|---------|------|
| **UtilExtension** | `zms9110750.Extensions.Utils` | `ToSafeFileName`（文件名净化）、`ToString`（集合拼接）、`ThrowIfOutOfRange`（参数验证） |
| **WithContext** | `zms9110750.Extensions.Utils` | `OutContext`，捕获调用者表达式、文件名、行号等上下文信息 |

## 源生成器

`zms9110750.StaticMethodAsExtensionGenerator` 0.1.3 会为 `System` 命名空间下的类型自动生成大量扩展方法（如 `char`、`string`、`DateTime`、`FileInfo` 等类型的辅助扩展）。

## 目标框架

`netstandard2.0;netstandard2.1;net6.0;net8.0;net10.0`

低版本上 `UtilExtension` 和 `WithContext` 因 `System.Index`、`string.Create`、`[CallerArgumentExpression]` 等 API 不可用而通过 `#if` 跳过。但源生成器的扩展方法在所有目标上均可用。
