# Utils.UnionPolyfill — C# 15 union 的旧框架运行时支撑

在 `netstandard2.0 / net6.0`（以及任何非 .NET 11 目标）上使用 **C# 15 `union` 语法**所需的运行时类型 polyfill。

## 为什么需要它

.NET 11 起 runtime 在 `System.Runtime.CompilerServices` 内置了 `UnionAttribute` 与 `IUnion`（自 Preview 5）。C# 编译器对这两个类型**只按全名识别、不锁定程序集**——因此旧 TFM 上声明同名同命名空间的类型，编译器就会把 `union` 声明正常 lowering，运行时也只依赖标准 IL（`box`/`isinst`/`callvirt`），无需 .NET 11。

本库提供：

| 类型 | 位置 | 说明 |
|------|------|------|
| `UnionAttribute` | `System.Runtime.CompilerServices` | 标记 union 类型（class/struct） |
| `IUnion` | `System.Runtime.CompilerServices` | `object? Value` 访问契约 |
| （PolySharp 生成） | `System.Runtime.CompilerServices` | `IsExternalInit` 等缺失的编译器类型，供 `record`/`init` 等使用 |

## 使用前提

```xml
<PropertyGroup>
  <LangVersion>preview</LangVersion>   <!-- union 需要 preview 语言版本 -->
</PropertyGroup>
<ItemGroup>
  <ProjectReference Include="..\Utils.UnionPolyfill\Utils.UnionPolyfill.csproj" />
</ItemGroup>
```

> 引用方若想用 `record` 作 case（需要 `IsExternalInit`），需自行引用 PolySharp——analyzer 的生成能力不随本库传递。

## 示例

```csharp
public record class Cat(string Name);
public record class Dog(string Name);

public union Pet(Cat, Dog);   // netstandard2.0 上即可编译

Pet pet = new Dog("Rex");
string s = pet switch
{
    Dog d => $"Dog {d.Name}",
    Cat c => $"Cat {c.Name}",
};
```

## 注意

- 只给**非 .NET 11** 目标引用；net11 目标 runtime 已内置同名类型，再引用会类型冲突。
- union 默认表示为单 `object?` 字段：引用类型 case 零拷贝，值类型 case 装箱（与 .NET 11 官方行为一致）。
- 需要值类型 case 零装箱的精准布局 → 手写 `[Union]` 类型并实现 `HasValue` / `TryGetValue(out T)`（non-boxing access pattern），编译器匹配会优先走 `TryGetValue`。
