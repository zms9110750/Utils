using zms9110750.Utils.Core;

namespace Core.Test;

/// <summary>
/// 验证 DisposableAction 的构造和释放行为。
/// 预期：Dispose 执行传入的回调，回调只执行一次。
/// </summary>
public class DisposableActionTest
{
    [Fact]
    public void Dispose_执行回调()
    {
        bool called = false;
        var d = new DisposableAction(() => called = true);
        d.Dispose();
        Assert.True(called);
    }

    [Fact]
    public void 多次_Dispose_回调只执行一次()
    {
        int count = 0;
        var d = new DisposableAction(() => count++);
        d.Dispose();
        d.Dispose();
        Assert.Equal(1, count);
    }
}

/// <summary>
/// 验证 DeferredActionScope 的添加和释放行为。
/// 预期：添加的 IDisposable 在调用 Dispose 时一次性释放；可添加 Action 委托。
/// </summary>
public class DeferredActionScopeTest
{
    [Fact]
    public void Add_增加_Count()
    {
        using var scope = new DeferredActionScope();
        scope.Add(new DisposableAction(() => { }));
        Assert.Equal(1, scope.Count);
    }

    [Fact]
    public void Add_Action_包装为_Disposable()
    {
        bool called = false;
        using var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        Assert.Equal(1, scope.Count);
    }

    [Fact]
    public void Dispose_释放所有已添加项()
    {
        bool called = false;
        var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        scope.Dispose();
        Assert.True(called);
    }

    [Fact]
    public void Dispose_后_Count_为_0()
    {
        var scope = new DeferredActionScope();
        scope.Add(() => { });
        scope.Dispose();
        Assert.Equal(0, scope.Count);
    }

    [Fact]
    public void Clear_清空所有项()
    {
        var scope = new DeferredActionScope();
        scope.Add(() => { });
        scope.Clear();
        Assert.Equal(0, scope.Count);
    }

    [Fact]
    public void Clear_不触发_Dispose()
    {
        bool called = false;
        var scope = new DeferredActionScope();
        scope.Add(() => called = true);
        scope.Clear();
        Assert.False(called);
    }

    [Fact]
    public void Dispose_后_Add_不抛出()
    {
        var scope = new DeferredActionScope();
        scope.Dispose();
        scope.Add(() => { });
        Assert.Equal(1, scope.Count);
    }

    [Fact]
    public void 枚举返回所有项()
    {
        using var scope = new DeferredActionScope();
        var d1 = new DisposableAction(() => { });
        var d2 = new DisposableAction(() => { });
        scope.Add(d1);
        scope.Add(d2);
        var items = scope.ToList();
        Assert.Contains(d1, items);
        Assert.Contains(d2, items);
    }

    [Fact]
    public void Remove_移除指定项()
    {
        using var scope = new DeferredActionScope();
        var d = new DisposableAction(() => { });
        scope.Add(d);
        Assert.True(scope.Remove(d));
        Assert.Equal(0, scope.Count);
    }
}
