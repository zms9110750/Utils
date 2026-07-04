using zms9110750.Utils.Core;

namespace Core.Test;

/// <summary>
/// 验证 DisposableAction 构造和释放行为
/// </summary>
public sealed class DisposableActionTest
{
    #region 构造

    /// <summary>传入 null 的 action 抛出 ArgumentNullException</summary>
    [Fact]
    public void Ctor_NullAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DisposableAction(null!));
    }

    #endregion

    #region 释放

    /// <summary>Dispose 执行回调</summary>
    [Fact]
    public void Dispose_InvokeAction()
    {
        var called = false;
        new DisposableAction(() => called = true).Dispose();
        Assert.True(called);
    }

    /// <summary>多次 Dispose 回调只执行一次</summary>
    [Fact]
    public void Dispose_MultipleTimes_Once()
    {
        var count = 0;
        var d = new DisposableAction(() => count++);
        d.Dispose();
        d.Dispose();
        Assert.Equal(1, count);
    }

    #endregion
}

/// <summary>
/// 验证 DeferredActionScope 的添加、释放、枚举等行为
/// </summary>
public sealed class DeferredActionScopeTest
{
    #region 添加与计数

    /// <summary>添加 IDisposable 后 Count 增加</summary>
    [Fact]
    public void Add_IncreaseCount()
    {
        using var scope = new DeferredActionScope();
        scope.Add(new DisposableAction(() => { }));
        Assert.Single(scope);
    }

    /// <summary>添加 Action 委托后 Count 增加</summary>
    [Fact]
    public void AddAction_IncreaseCount()
    {
        using var scope = new DeferredActionScope();
        scope.Add(() => { });
        Assert.Single(scope);
    }

    #endregion

    #region 释放

    /// <summary>Dispose 执行所有已添加的委托</summary>
    [Fact]
    public void Dispose_ExecuteAllActions()
    {
        var called = false;
        var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        scope.Dispose();
        Assert.True(called);
    }

    /// <summary>Dispose 后 Count 归零</summary>
    [Fact]
    public void Dispose_ClearsCount()
    {
        var scope = new DeferredActionScope();
        scope.Add(() => { });
        scope.Dispose();
        Assert.Empty(scope);
    }

    #endregion

    #region 清空与移除

    /// <summary>Clear 清空但不释放</summary>
    [Fact]
    public void Clear_RemoveItemsWithoutDispose()
    {
        var called = false;
        var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        scope.Clear();
        Assert.False(called);
        Assert.Empty(scope);
    }

    /// <summary>Remove 移除指定项</summary>
    [Fact]
    public void Remove_SpecificItem()
    {
        using var scope = new DeferredActionScope();
        var d = new DisposableAction(() => { });
        scope.Add(d);
        Assert.True(scope.Remove(d));
        Assert.Empty(scope);
    }

    #endregion

    #region 枚举

    /// <summary>枚举器遍历所有添加项</summary>
    [Fact]
    public void Enumerate_AllItems()
    {
        using var scope = new DeferredActionScope();
        var d1 = new DisposableAction(() => { });
        var d2 = new DisposableAction(() => { });
        scope.Add(d1);
        scope.Add(d2);
        Assert.Contains(d1, scope);
        Assert.Contains(d2, scope);
    }

    #endregion
}
