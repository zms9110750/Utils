// Copyright (c) zms9110750. All rights reserved.
// Licensed under the MIT License.

using Polly;

namespace zms9110750.Extensions.Polly;

/// <summary>
/// <see cref="ResilienceProperties"/> 的扩展方法，
/// 支持直接用 <see cref="string"/> 作为键，省去手动创建 <see cref="ResiliencePropertyKey{T}"/>。
/// </summary>
public static class ResiliencePropertiesExtensions
{
    /// <summary>
    /// 设置属性值。内部自动创建 <see cref="ResiliencePropertyKey{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="properties">属性集合。</param>
    /// <param name="key">属性键（字符串）。</param>
    /// <param name="value">要设置的值。</param>
    public static void Set<T>(this ResilienceProperties properties, string key, T value)
    {
        properties.Set(new ResiliencePropertyKey<T>(key), value);
    }

    /// <summary>
    /// 获取属性值，若不存在则返回 <paramref name="defaultValue"/>。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="properties">属性集合。</param>
    /// <param name="key">属性键（字符串）。</param>
    /// <param name="defaultValue">未找到时的默认值。</param>
    /// <returns>属性值或默认值。</returns>
    public static T GetValue<T>(this ResilienceProperties properties, string key, T defaultValue)
    {
        return properties.GetValue(new ResiliencePropertyKey<T>(key), defaultValue);
    }

    /// <summary>
    /// 尝试获取属性值。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="properties">属性集合。</param>
    /// <param name="key">属性键（字符串）。</param>
    /// <param name="value">找到时输出属性值。</param>
    /// <returns>是否找到该键。</returns>
    public static bool TryGetValue<T>(this ResilienceProperties properties, string key, out T value)
    {
        return properties.TryGetValue(new ResiliencePropertyKey<T>(key), out value!);
    }
}
