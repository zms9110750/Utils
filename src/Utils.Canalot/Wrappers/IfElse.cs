using System.Collections.Specialized;

namespace zms9110750.Utils.Canalot.Wrappers;

/// <summary>
/// 表示一个带有层级条件状态的包装，用于 if-else 链式调用
/// </summary>
/// <typeparam name="T">值的类型</typeparam>
public readonly partial record struct IfElse<T>
{
    private static InvalidOperationException UninitializedException => new("Instance not initialized");
    private static InvalidOperationException TopLevelException => new("Already at top level, cannot exit");
    private static InvalidOperationException MaxLevelException => new("Maximum level (31) reached, cannot enter next level");

    /// <summary>
    /// 当前持有的值
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// 嵌套层级
    /// </summary>
    /// <remarks>
    /// 从1开始。第0位留给<see cref="Handled"/>
    /// </remarks>
    private int Level { get; }
    private BitVector32 Condition { get; }

    /// <summary>
    /// 指示当前链是否已经处理完毕（即已经执行过 Then）
    /// 0=未处理，1=已处理（第0位）
    /// </summary>
    public bool Handled => Condition[1 << 0];

    /// <summary>
    /// 指示当前路径是否应该执行
    /// 条件：未处理 且 所有已进入的层条件都成立（位=0）
    /// </summary>
    /// <remarks>条件成立为0，则可以直接判断<code><see cref="BitVector32.Data"/> == 0</code>来检查是否可以执行</remarks>
    internal bool ShouldExecute => Condition.Data == 0;

    /// <summary>
    /// 指示实例是否已初始化（Level > 0）
    /// </summary>
    public bool IsInitialized => Level > 0;

    /// <summary>
    /// 初始化一个新的 IfElse 实例（从第1层开始，并设置第1层的条件结果）
    /// </summary>
    /// <param name="value">初始值</param>
    /// <param name="condition">第1层的条件结果：true=成立(0)，false=不成立(1)</param>
    public IfElse(T value, bool condition) : this(value, 1, new BitVector32(condition ? default : 1 << 1))
    {
    }

    private IfElse(T value, int level, BitVector32 condition)
    {
        Value = value;
        Level = level;
        Condition = condition;
    }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
        {
            throw UninitializedException;
        }
    }

    /// <summary>
    /// 进入下一层并设置条件结果
    /// </summary>
    /// <param name="condition">true=条件成立(0)，false=条件不成立(1)</param> 
    internal IfElse<T> EnterLevel(bool condition)
    {
        ThrowIfNotInitialized();

        if (Level >= 31)
        {
            throw MaxLevelException;
        }

        var nextLevel = Level + 1;
        var temp = Condition;
        temp[1 << nextLevel] = !condition;
        return new IfElse<T>(Value, nextLevel, temp);
    }

    /// <summary>
    /// 重置当前层的条件结果（用于 ElseIf）
    /// </summary>
    /// <param name="condition">true=条件成立(0)，false=条件不成立(1)</param>
    internal IfElse<T> ResetCurrentLevel(bool condition)
    {
        ThrowIfNotInitialized();

        var temp = Condition;
        temp[1 << Level] = !condition;
        return new IfElse<T>(Value, Level, temp);
    }

    /// <summary>
    /// 返回上一层，并把当前层的条件位重置为0
    /// </summary>
    internal IfElse<T> ExitLevel()
    {
        ThrowIfNotInitialized();

        if (Level <= 1)
        {
            throw TopLevelException;
        }

        var temp = Condition;
        temp[1 << Level] = false;
        return new IfElse<T>(Value, Level - 1, temp);
    }

    /// <summary>
    /// 标记当前链已处理（由 Then 方法调用）
    /// </summary>
    internal IfElse<T> HandledNow()
    {
        ThrowIfNotInitialized();

        var temp = Condition;
        temp[1 << 0] = true;
        return new IfElse<T>(Value, Level, temp);
    }
    /// <summary>
    /// 标记当前链已处理，并替换为新值（可改变类型）
    /// </summary>
    internal IfElse<TNew> HandledNow<TNew>(TNew newValue)
    {
        ThrowIfNotInitialized();

        var temp = Condition;
        temp[1 << 0] = true;
        return new IfElse<TNew>(newValue, Level, temp);
    }
}
public readonly partial record struct IfElse<T>
{
    /// <summary>
    /// 在当前层级进入 if 嵌套
    /// </summary>
    /// <remarks>最后通过<see cref="EndIf()"/>离开嵌套</remarks>
    public IfElse<T> AndIf(bool condition)
    {
        return EnterLevel(condition);
    }


    /// <summary>
    /// 当前条件成立时标记处理，不执行任何事情也不处理后续分支。
    /// </summary>
    public IfElse<T> Then()
    {
        if (ShouldExecute)
        {
            return HandledNow();
        }
        return this;
    }

    /// <summary>
    /// 当前条件成立时执行 then 分支（返回值）
    /// </summary>
    public IfElse<T> Then(T result)
    {
        if (ShouldExecute)
        {
            return HandledNow(result);
        }
        return this;
    }

    /// <summary>
    /// 当前条件成立时执行 then 分支（委托）
    /// </summary>
    public IfElse<T> Then(Func<T, T> func)
    {
        if (ShouldExecute)
        {
            return HandledNow(func(Value));
        }
        return this;
    }

    /// <summary>
    /// 当前条件成立时执行 then 分支（无参委托）
    /// </summary>
    public IfElse<T> Then(Func<T> func)
    {
        if (ShouldExecute)
        {
            return HandledNow(func());
        }
        return this;
    }

    /// <summary>
    /// 当前条件成立时执行操作
    /// </summary>
    public IfElse<T> Then(Action<T> action)
    {
        if (ShouldExecute)
        {
            action(Value);
            return HandledNow();
        }
        return this;
    }

    /// <summary>
    /// 否则进入 else if 分支
    /// </summary>
    /// <remarks>
    /// else要么是进入下一层，要么执行并返回上一层。
    /// <br/>请使用<see cref="AndIf(bool)"/>或<see cref="EndIf(T)"/>代替
    /// </remarks>
    public IfElse<T> ElseIf(bool condition)
    {
        return ResetCurrentLevel(condition);
    }

    /// <summary>
    /// 结束当前 if 层级
    /// </summary>
    public IfElse<T> EndIf()
    {
        return ExitLevel();
    }

    /// <summary>
    /// 结束当前 if 层级并返回值
    /// </summary>
    public IfElse<T> EndIf(T result)
    {
        return ExitLevel().HandledNow(result);
    }

    /// <summary>
    /// 结束当前 if 层级并通过委托返回值
    /// </summary>
    public IfElse<T> EndIf(Func<T, T> func)
    {
        return ExitLevel().HandledNow(func(Value));
    }

    /// <summary>
    /// 结束当前 if 层级并通过无参委托返回值
    /// </summary>
    public IfElse<T> EndIf(Func<T> func)
    {
        return ExitLevel().HandledNow(func());
    }

    /// <summary>
    /// 结束当前 if 层级并执行操作
    /// </summary>
    public IfElse<T> EndIf(Action<T> action)
    {
        action(Value);
        return ExitLevel().HandledNow();
    }
}
