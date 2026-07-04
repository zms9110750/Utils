# Utils.Core — 核心工具库

通用的基础设施类型。

## 模块

| 模块 | 命名空间 | 说明 |
|------|---------|------|
| **Trie** | `zms9110750.Utils.Core` | 前缀树，支持自定义分隔符的模糊搜索 |
| **Pagination** | `zms9110750.Utils.Core` | 分页模型，DataRange/ButtonRange、可设按钮数 |
| **DeferredActionScope** | `zms9110750.Utils.Core` | 延迟操作作用域，批量管理 IDisposable |
| **DisposableAction** | `zms9110750.Utils.Core` | 将 Action 包装为 IDisposable |
| **ProgressStream** | `zms9110750.Utils.Core` | 带进度报告的 Stream 包装 |
| **Pick** | `zms9110750.Utils.Core.Pick` | 区间约束随机抽取器（可放回/不放回） |

## 目标框架

`netstandard2.1;net6.0;net8.0`
