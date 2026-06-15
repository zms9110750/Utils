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
    /// <remarks>
    /// 当操作失败时，异常包含错误信息；
    /// 当操作未初始化时，异常提示未初始化
    /// </remarks>
    public T? Value => IsSuccess ? field : throw new InvalidOperationException(Error);

    /// <summary>
    /// 获取失败时的错误信息
    /// </summary>
    /// <remarks>
    /// 如果操作未初始化，返回固定字符串 "Cannot convert an uninit ErrorOr to a value."
    /// </remarks>
    public string? Error => IsInitialized ? field : throw new InvalidOperationException("Cannot convert an uninit ErrorOr to a value.");

    /// <summary>
    /// 指示操作是否成功
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    private bool IsInitialized { get; }

    /// <summary>
    /// 创建一个未初始化的 ErrorOr。不要这样做。
    /// </summary>
    /// <remarks>
    /// 未初始化的包装不提供任何信息
    /// </remarks>
    [Obsolete("don't use this", true)]
    public ErrorOr()
    {
        IsInitialized = false;
    }

    /// <summary>
    /// 创建一个成功的 ErrorOr
    /// </summary>
    /// <param name="value">成功时的值</param>
    public ErrorOr(T value)
    {
        Value = value;
        Error = null;
        IsInitialized = true;
    }

    /// <summary>
    /// 创建一个失败的 ErrorOr
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <exception cref="ArgumentNullException">error 为 null 时抛出</exception>
    public ErrorOr(string error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        Error = error;
        Value = default;
        IsInitialized = true;
    }

    /// <summary>
    /// 将值隐式转换为成功的 ErrorOr
    /// </summary>
    public static implicit operator ErrorOr<T>(T value) => new(value);
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
    /// <exception cref="TError"/>
    /// <exception cref="InvalidOperationException"/>
    /// <remarks>
    /// 当操作失败时，抛出 TError 类型的异常；
    /// 当操作未初始化时，抛出 InvalidOperationException
    /// </remarks>
    public T? Value => IsSuccess ? field : throw Error!;

    /// <summary>
    /// 获取失败时的异常
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <remarks>
    /// 当操作未初始化时抛出
    /// </remarks>
    public TError? Error => IsInitialized ? field : throw new InvalidOperationException("Cannot convert an uninit ErrorOr to a value.");

    /// <summary>
    /// 指示操作是否成功
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    private bool IsInitialized { get; }

    /// <summary>
    /// 创建一个未初始化的 ErrorOr。不要这样做。
    /// </summary>
    /// <remarks>
    /// 未初始化的包装不提供任何信息
    /// </remarks>
    [Obsolete("don't use this", true)]
    public ErrorOr()
    {
        IsInitialized = false;
    }

    /// <summary>
    /// 创建一个成功的 ErrorOr
    /// </summary>
    /// <param name="value">成功时的值</param>
    public ErrorOr(T? value)
    {
        Value = value;
        Error = null;
        IsInitialized = true;
    }

    /// <summary>
    /// 创建一个失败的 ErrorOr
    /// </summary>
    /// <param name="error">失败时的异常</param>
    /// <exception cref="ArgumentNullException">error 为 null 时抛出</exception>
    public ErrorOr(TError error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        Error = error;
        Value = default;
        IsInitialized = true;
    }

    /// <summary>
    /// 将值隐式转换为成功的 ErrorOr
    /// </summary>
    public static implicit operator ErrorOr<T, TError>(T? value) => new(value);

    /// <summary>
    /// 将异常隐式转换为失败的 ErrorOr
    /// </summary>
    public static implicit operator ErrorOr<T, TError>(TError error) => new(error);

    /// <summary>
    /// 将 ErrorOr&lt;T, TError&gt; 隐式转换为 ErrorOr&lt;T&gt;
    /// </summary>
    /// <remarks>
    /// 失败时会将异常的 Message 作为错误信息
    /// </remarks>
    public static implicit operator ErrorOr<T>(ErrorOr<T, TError> error)
    {
        return error.IsSuccess
            ? new ErrorOr<T>(error.Value)
            : new ErrorOr<T>(error.Error.Message);
    }
}
