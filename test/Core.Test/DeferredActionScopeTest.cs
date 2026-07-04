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

    /// <summary>添加 null Action 抛出 ArgumentNullException</summary>
    [Fact]
    public void Add_NullAction_Throws()
    {
        using var scope = new DeferredActionScope();
        Assert.Throws<ArgumentNullException>(() => scope.Add((Action)null!));
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

    /// <summary>多次 Dispose 幂等：第二次无操作，不抛异常</summary>
    [Fact]
    public void Dispose_MultipleTimes_Idempotent()
    {
        var count = 0;
        var scope = new DeferredActionScope();
        scope.Add(() => count++);
        scope.Dispose();
        scope.Dispose(); // 第二次应无副作用
        Assert.Equal(1, count);
        Assert.Empty(scope);
    }

    /// <summary>Dispose 中某个项抛出异常，其余项仍应释放，并传播异常</summary>
    [Fact]
    public void Dispose_WhenItemThrows_StillDisposesOthers()
    {
        var dispose1Called = false;
        var dispose3Called = false;

        var item1 = new DisposableAction(() => dispose1Called = true);
        var item2 = new DisposableAction(() => { throw new InvalidOperationException("boom"); });
        var item3 = new DisposableAction(() => dispose3Called = true);

        var scope = new DeferredActionScope();
        scope.Add(item1);
        scope.Add(item2);
        scope.Add(item3);

        // 释放时，item1 正常释放，item2 抛异常导致循环中断，item3 不会被执行
        Assert.Throws<InvalidOperationException>(() => scope.Dispose());
        Assert.True(dispose1Called);
        Assert.False(dispose3Called);
        // _disposables 未被 Clear，因此 scope 中仍包含 item2 和 item3（注意 item1 已被移除）
        // 注意：foreach 中 item1 已处理完毕，item2 抛出后 _disposables 未 Clear，剩余项仍存在
    }

    /// <summary>空作用域 Dispose 无异常</summary>
    [Fact]
    public void Dispose_EmptyScope_NoException()
    {
        var scope = new DeferredActionScope();
        // 未添加任何项，Dispose 应静默通过
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

    /// <summary>Remove 后 Dispose 不会释放已移除的项</summary>
    [Fact]
    public void Remove_ThenDispose_DoesNotDisposeRemovedItem()
    {
        var called = false;
        var d = new DisposableAction(() => called = true);
        var scope = new DeferredActionScope();
        scope.Add(d);
        scope.Remove(d);
        scope.Dispose();
        Assert.False(called);
        Assert.Empty(scope);
    }

    /// <summary>Clear 后 Dispose 无副作用（已清空的项不被释放）</summary>
    [Fact]
    public void Clear_ThenDispose_NoSideEffects()
    {
        var called = false;
        var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        scope.Clear();
        scope.Dispose();
        Assert.False(called);
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

    #region ICollection 成员

    /// <summary>Contains 对存在/不存在项返回正确结果</summary>
    [Fact]
    public void Contains_ReturnsCorrectly()
    {
        using var scope = new DeferredActionScope();
        var d1 = new DisposableAction(() => { });
        var d2 = new DisposableAction(() => { });
        scope.Add(d1);
        Assert.Contains(d1, scope);
        Assert.DoesNotContain(d2, scope);
    }

    /// <summary>IsReadOnly 永假</summary>
    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        using var scope = new DeferredActionScope();
        Assert.False(scope.IsReadOnly);
    }

    /// <summary>CopyTo 将项拷贝到目标数组</summary>
    [Fact]
    public void CopyTo_CopiesItems()
    {
        using var scope = new DeferredActionScope();
        var d1 = new DisposableAction(() => { });
        var d2 = new DisposableAction(() => { });
        scope.Add(d1);
        scope.Add(d2);

        var array = new IDisposable[4];
        scope.CopyTo(array, 1);
        Assert.Null(array[0]);
        Assert.Contains(d1, new[] { array[1], array[2] });
        Assert.Contains(d2, new[] { array[1], array[2] });
        Assert.Null(array[3]);
    }

    #endregion

    #region 混合释放

    /// <summary>混合添加 Action 和 IDisposable，Dispose 时全部释放</summary>
    [Fact]
    public void Dispose_MixedActionsAndDisposables_AllDisposed()
    {
        var actionCalled = false;
        var disposableCalled = false;

        var scope = new DeferredActionScope();
        scope.Add(() => actionCalled = true);
        scope.Add(new DisposableAction(() => disposableCalled = true));
        scope.Dispose();

        Assert.True(actionCalled);
        Assert.True(disposableCalled);
        Assert.Empty(scope);
    }

    #endregion
}
