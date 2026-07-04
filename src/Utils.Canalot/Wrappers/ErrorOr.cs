using System.Diagnostics.CodeAnalysis;

namespace zms9110750.Utils.Canalot.Wrappers;

/// <summary>
/// 表示一个操作的结果，可能成功（包含值）或失败（包含错误信息）
/// </summary>
/// <typeparam name="T">成功时返回值的类型</typeparam>
public readonly record struct ErrorOr<T>
{
    /// <summary>
    /// 获取成功时的值
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public T? Value => IsSuccess ? field : throw new InvalidOperationException(Error);

    /// <summary>
    /// 获取失败时的错误信息
    /// </summary>
    public string? Error => IsInitialized ? field : throw new InvalidOperationException("Cannot convert an uninit ErrorOr to a value.");

    /// <summary>
    /// 指示操作是否成功
    /// </summary>
#if NET6_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
#endif
    public bool IsSuccess => Error is null;

    private bool IsInitialized { get; }

    [Obsolete("don't use this", true)]
    public ErrorOr()
    {
        IsInitialized = false;
    }

    /// <summary>创建一个成功的 ErrorOr</summary>
    public ErrorOr(T value)
    {
        Value = value;
        Error = null;
        IsInitialized = true;
    }

    /// <summary>创建一个失败的 ErrorOr</summary>
    public ErrorOr(string error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Error = error;
        Value = default;
        IsInitialized = true;
    }

    public static implicit operator ErrorOr<T>(T value)
    {
        return new(value);
    }
}

/// <summary>
/// 表示一个操作的结果，可能成功（包含值）或失败（包含指定类型的异常）
/// </summary>
/// <typeparam name="T">成功时返回值的类型</typeparam>
/// <typeparam name="TError">失败时异常的类型，必须继承自 Exception</typeparam>
public readonly record struct ErrorOr<T, TError> where TError : Exception
{
    /// <summary>
    /// 获取成功时的值
    /// </summary>
    public T? Value => IsSuccess ? field : throw Error!;

    /// <summary>
    /// 获取失败时的异常
    /// </summary>
    public TError? Error => IsInitialized ? field : throw new InvalidOperationException("Cannot convert an uninit ErrorOr to a value.");

    /// <summary>
    /// 指示操作是否成功
    /// </summary>
#if NET6_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
#endif
    public bool IsSuccess => Error is null;

    private bool IsInitialized { get; }

    [Obsolete("don't use this", true)]
    public ErrorOr()
    {
        IsInitialized = false;
    }

    /// <summary>创建一个成功的 ErrorOr</summary>
    public ErrorOr(T? value)
    {
        Value = value;
        Error = null;
        IsInitialized = true;
    }

    /// <summary>创建一个失败的 ErrorOr</summary>
    public ErrorOr(TError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Error = error;
        Value = default;
        IsInitialized = true;
    }

    public static implicit operator ErrorOr<T, TError>(T? value)
    {
        return new(value);
    }

    public static implicit operator ErrorOr<T, TError>(TError error)
    {
        return new(error);
    }

    public static implicit operator ErrorOr<T>(ErrorOr<T, TError> error)
    {
        return error.IsSuccess
            ? new ErrorOr<T>(error.Value)
            : new ErrorOr<T>(error.Error.Message);
    }
}
