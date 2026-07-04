# Utils.Canalot — 函数式工具

提供 C# 中常用的函数式编程辅助类型和扩展方法。

## 模块

| 文件 | 命名空间 | 说明 |
|------|---------|------|
| **ErrorOr** | `zms9110750.Utils.Canalot.Wrappers` | 结果包装：成功时含值，失败时含错误信息或异常 |
| **NoneOr** | `zms9110750.Utils.Canalot.Wrappers` | 可空值包装，比 `Nullable<T>` 更灵活（支持引用类型） |
| **When** | `zms9110750.Extensions.Utils.Canalot` | 条件执行扩展，替代 `if-else` 链式调用 |
| **Apply / Out** | `zms9110750.Extensions.Utils.Canalot` | 值应用与上下文输出 |
| **IfElse** | `zms9110750.Utils.Canalot.Wrappers` | 条件分支包装 |

## 目标框架

`netstandard2.0;net6.0`（`[MemberNotNullWhen]` 在 netstandard2.0 上通过 `#if` 跳过）
