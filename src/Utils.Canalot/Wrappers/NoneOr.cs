using System.Diagnostics.CodeAnalysis;

namespace zms9110750.Utils.Canalot.Wrappers;

/// <summary>表示一个可能为空的值，要么有值，要么没有</summary>
public readonly record struct NoneOr<T>
{
    /// <summary>
    /// 获取包装的值
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public T Value => HasValue ? field : throw new InvalidOperationException("Cannot convert an unsuccessful NoneOr to a value.");

    /// <summary>
    /// 指示是否包含有效值
    /// </summary>
#if NET6_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
#endif
    public bool HasValue { get; }

    /// <summary>创建一个表示空的 NoneOr。</summary>
    public NoneOr()
    {
        Value = default!;
        HasValue = false;
    }

    /// <summary>根据给定的值创建 NoneOr</summary>
    public NoneOr(T? value)
    {
        Value = value!;
        HasValue = value != null;
    }

    [Obsolete("Please use default(NoneOr<T>) instead.")]
    public static NoneOr<T> None => default;

    public static implicit operator NoneOr<T>(T? value)
    {
        return new NoneOr<T>(value);
    }

    public static implicit operator NoneOr<T>(ValueTuple value)
    {
        return new NoneOr<T>();
    }
}

public static class NoneOr
{
    public static ValueTuple Node => default;

    public static NoneOr<T> From<T>(T? value) where T : struct
    {
        return value == null ? new NoneOr<T>() : new NoneOr<T>(value.Value);
    }

    public static NoneOr<T> From<T>(T? value)
    {
        return value;
    }

    public static T? ToNullable<T>(this NoneOr<T> none) where T : struct
    {
        return none.HasValue ? none.Value : null;
    }

    public static NoneOr<T> ToNopeOr<T>(this T? value) where T : struct
    {
        return value == null ? new NoneOr<T>() : new NoneOr<T>(value.Value);
    }
}
