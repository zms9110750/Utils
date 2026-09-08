// =====================================================================
// C# 15 union 的运行时支撑类型（手写 polyfill）
//
// 背景：.NET 11 起 runtime 在 System.Runtime.CompilerServices 中内置
//       UnionAttribute 与 IUnion（自 Preview 5）。编译器对它们只按
//       “类型全名”识别、不锁定程序集（与 IsExternalInit 同理），
//       因此旧 TFM（netstandard2.0 / net6.0 / .NET Framework）上
//       声明同名同命名空间的类型，即可使用 C# 15 的 union 语法。
//
// 用法：引用本库后，配合 LangVersion=preview（以及使用 record 等
//       其它新特性时由 PolySharp 补 IsExternalInit 等类型）。
// =====================================================================

using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// 标记一个类型为 union 类型（class 或 struct）。编译器据此识别：
    /// 每个单参构造器对应一个 case 类型，并启用隐式转换 / 解包匹配 / 穷尽 switch。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute
    {
        public UnionAttribute()
        {
        }
    }

    /// <summary>
    /// union 类型的统一访问契约：无论 case 是什么，均可通过 <see cref="Value"/> 读取内容。
    /// </summary>
    public interface IUnion
    {
        /// <summary>union 中保存的内容值（值类型 case 会装箱）。</summary>
        object? Value { get; }
    }
}
