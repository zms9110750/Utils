# Utils.Extensions — .NET 实用扩展方法库

提供一组通用的扩展方法，涵盖**异步并发等待**、**字符串净化**、**集合拼接**、**参数校验**以及**调用上下文捕获**等常见需求。支持多目标框架，部分高级 API 仅在 .NET 6.0+ 上可用。

---

## 目标框架

| 框架 | 说明 |
|------|------|
| `netstandard2.0` | 基础兼容，所有手写扩展在此目标下均可用（部分 API 因条件编译被排除） |
| `net6.0+` | 完整功能，启用 `string.Create`、`[CallerArgumentExpression]` 等现代 API |

> 项目同时通过源生成器为 `System` 命名空间下的大量类型自动生成扩展方法（详细见下文「源生成器扩展」）。

---

## 功能总览

| API | 命名空间 | 最低目标框架 | 说明 |
|-----|---------|-------------|------|
| `TupleTaskAwaiter` + `GetAwaiter` | `zms9110750.Extensions.Utils` | `netstandard2.0` | 通过元组语法同时 `await` 多个 `Task<T>` |
| `ToSafeFileName` | `zms9110750.Extensions.Utils` | `net6.0` | 将字符串中的非法文件名字符替换为指定字符 |
| `ToString<T>(IEnumerable<T>, string)` | `zms9110750.Extensions.Utils` | `net6.0` | 使用分隔符拼接集合元素 |
| `ThrowIfOutOfRange` | `zms9110750.Extensions.Utils` | `net6.0` | 参数范围校验，越界时抛出 `ArgumentOutOfRangeException` |
| `WithContext<T> / OutContext` | `zms9110750.Extensions.Utils` | `net6.0` | 捕获调用表达式、成员名、行号、文件路径等上下文 |

> `TupleTaskAwaiter` 由 T4 模板生成，支持 2~7 个 `Task<T>` 的元组。

---

## 条件编译说明

源文件中的 `#if NET6_0_OR_GREATER` 预处理指令控制以下 API 的可用性：

- **仅在 `net6.0+` 下编译**：`UtilExtension` 类（包含 `ToSafeFileName`、`ToString`、`ThrowIfOutOfRange`）以及 `WithContext` / `OutContext` 结构体与扩展方法。
- **`netstandard2.0` 下不可用**：上述 API 因依赖 `string.Create`、`ImmutableHashSet`、`[CallerArgumentExpression]`、`readonly record struct` 等 .NET 6.0+ 特性而被排除。
- **始终可用**：`TupleTaskAwaiter` 及相关扩展方法（`GetAwaiter`），它们仅依赖 `Task`、`ICriticalNotifyCompletion` 等 `netstandard2.0` 已有类型。

---

## 功能详解与使用示例

### 1️⃣ TupleTaskAwaiter — 元组异步等待

同时等待多个 `Task<T>`，并以元组形式一次性取回所有结果。**无需手动编写 `Task.WhenAll` + 逐个读取 `.Result`**。

```csharp
using zms9110750.Extensions.Utils;

async Task UseTupleAwaiter()
{
    Task<int> taskInt = Task.FromResult(42);
    Task<string> taskStr = Task.FromResult("Hello");
    Task<double> taskDbl = Task.FromResult(3.14);

    // 同时等待三个任务，结果按元组顺序返回
    var (intVal, strVal, dblVal) = await (taskInt, taskStr, taskDbl);

    Console.WriteLine($"{intVal}, {strVal}, {dblVal}");
    // 输出: 42, Hello, 3.14
}
```

支持 2 到 7 个任务的元组。

> **实现原理**：`(Task<T1>, Task<T2>, ...)` 上的 `GetAwaiter` 扩展方法返回 `TupleTaskAwaiter<T1, T2, ...>` 结构体，其内部调用 `Task.WhenAll` 合并等待，`GetResult` 返回各任务的 `.Result`。

---

### 2️⃣ ToSafeFileName — 文件名净化

将字符串中的非法文件名字符替换为指定的替代字符（默认下划线 `_`）。适用于生成安全、跨平台的文件名。

```csharp
// 需要 using zms9110750.Extensions.Utils;
#if NET6_0_OR_GREATER
string raw = "file:name?<invalid>.txt";
string safe = raw.ToSafeFileName();         // "file_name__invalid_.txt"
string safe2 = raw.ToSafeFileName('-');     // "file-name--invalid-.txt"
#endif
```

---

### 3️⃣ ToString — 集合拼接

将任意 `IEnumerable<T>` 的元素用指定的分隔符拼接为字符串。支持单字符分隔符（效率更高）和字符串分隔符。

```csharp
using zms9110750.Extensions.Utils;

var numbers = new[] { 1, 2, 3, 4, 5 };

// 默认分隔符 ", "
Console.WriteLine(numbers.ToString());             // "1, 2, 3, 4, 5"

// 自定义字符串分隔符
Console.WriteLine(numbers.ToString(" | "));        // "1 | 2 | 3 | 4 | 5"

// 单字符分隔符（更高效）
Console.WriteLine(numbers.ToString("-"));           // "1-2-3-4-5"

// 空或 null 分隔符 → 直接拼接
Console.WriteLine(numbers.ToString(""));            // "12345"
```

> 注意：此方法会覆盖 `object.ToString()`？不，它是一个扩展方法签名 `ToString<T>(this IEnumerable<T> values, string separator = ", ")`，与 `object.ToString()` 不冲突，因为扩展方法要求至少一个参数，且 `object.ToString()` 是实例方法。

---

### 4️⃣ ThrowIfOutOfRange — 参数范围校验

检查值是否在 `[min, max]` 区间内，越界时自动抛出 `ArgumentOutOfRangeException`，并利用 `[CallerArgumentExpression]` 自动填充参数名。

```csharp
using zms9110750.Extensions.Utils;

void SetPercentage(int value)
{
    // 若 value < 0 或 value > 100 则抛出异常，paramName 自动为 "value"
    ThrowIfOutOfRange(value, 0, 100);
}

void SetTemperature(double celsius)
{
    // 支持自定义错误消息
    ThrowIfOutOfRange(celsius, -273.15, 1000.0, "温度超出合理范围");
}
```

---

### 5️⃣ WithContext / OutContext — 调用上下文捕获

通过 `WithContext<T>` 值元组（`readonly record struct`）捕获调用时的表达式、成员名、源文件路径和行号，便于调试和日志记录。

```csharp
using zms9110750.Extensions.Utils;

void ProcessData(string name)
{
    // OutContext 返回原始值，同时通过 out 参数提供上下文
    string result = name.OutContext(out var ctx);

    Console.WriteLine(ctx.ToString());
    // 输出: [name = someValue]

    // 也可直接构造 WithContext
    var wc = new WithContext<int>(42);
    Console.WriteLine(wc.ToString());
    // 输出: [42 = 42]   （CallerArgumentExpression 为 "42"）
}
```

`WithContext<T>` 会自动填充：
- `CallerArgumentExpression` — 调用处的源代码表达式
- `CallerMemberName` — 调用方法名
- `CallerLineNumber` — 调用行号
- `CallerFilePath` — 调用文件路径

---

## 源生成器扩展

项目引用了 `zms9110750.StaticMethodAsExtensionGenerator`（0.1.3）源生成器，该生成器会为 `System` 命名空间下的常见类型（如 `string`、`char`、`DateTime`、`FileInfo`、`Regex`、`IEnumerable<T>` 等）自动生成大量扩展方法。这些扩展在所有目标框架上均可用。

> 生成的代码位于 `obj/Debug/{tfm}/generated/` 目录下，可直接查阅了解可用扩展。

---

## 项目结构

```
Utils.Extensions/
├── Utils.Extensions.csproj    # 项目文件，多目标框架 + 源生成器配置
├── UtilExtension.cs           # 手写扩展：ToSafeFileName、ToString、ThrowIfOutOfRange（#if NET6_0_OR_GREATER）
├── WithContext.cs             # 手写扩展：WithContext<T> 结构体 + OutContext 扩展（#if NET6_0_OR_GREATER）
├── TupleTaskGetAwaiter.tt     # T4 模板：生成 2~7 元组 TupleTaskAwaiter 和 GetAwaiter 扩展
├── TupleTaskGetAwaiter.cs     # T4 模板自动生成的代码
└── README.md                  # 本文件
```

---

## 如何获取

通过 NuGet 搜索 `zms9110750.Utils.Extensions` 或直接引用项目：

```xml
<PackageReference Include="zms9110750.Utils.Extensions" Version="*" />
```

或添加项目引用：

```xml
<ProjectReference Include="..\Utils.Extensions\Utils.Extensions.csproj" />
```

---

## 许可

本项目基于 [MIT License](../../../LICENSE) 开源。
