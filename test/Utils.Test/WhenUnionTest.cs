using System;
using zms9110750.Extensions.Utils.Canalot;

namespace Utils.Test;

// ============ 测试 Utils.Canalot 库里的 union 版 When（Extensions/When.cs）============
// 被测代码在库里，这里只写断言。期望语义对齐原 When 重载族：
// 条件成立 → 按 trueValue 形态处理；不成立 → 返回原 value

public class WhenUnionTest
{
    [Fact]
    public void CondFalse_ReturnsOriginal_RegardlessOfTrueValue()
    {
        Assert.Equal(1, 1.When(false, 2));                                  // 裸值形态：条件假 → 原值
        Assert.Equal(1, 1.When(false, (Func<int>)(() => 7)));               // 无参 Func 形态：条件假 → 原值
        Assert.Equal(1, 1.When(false, (Func<int, int>)(v => v + 100)));     // 有参 Func 形态：条件假 → 原值
    }

    [Fact]
    public void CondTrue_BareValue_ReplacesValue()
    {
        Assert.Equal(5, 1.When(true, 5));   // 条件成立 + 裸值 → 替换为新值
    }

    [Fact]
    public void CondTrue_NoArgFunc_ReturnsItsResult()
    {
        Assert.Equal(7, 1.When(true, (Func<int>)(() => 7)));
    }

    [Fact]
    public void CondTrue_ValueFunc_AppliesToValue()
    {
        Assert.Equal(3, 1.When(true, (Func<int, int>)(v => v + 2)));
    }

    [Fact]
    public void CondTrue_Action_RunsSideEffectAndReturnsOriginal()
    {
        int side = 0;
        var result = 1.When(true, (Action<int>)(v => side = v));
        Assert.Equal(1, result);   // Action 形态：返回原值
        Assert.Equal(1, side);     // 副作用已执行
    }

    [Fact]
    public void CondFalse_Action_NotRun_ReturnsOriginal()
    {
        int side = 0;
        var result = 1.When(false, (Action<int>)(v => side = v));
        Assert.Equal(1, result);
        Assert.Equal(0, side);     // 条件假：Action 不应执行
    }
}
