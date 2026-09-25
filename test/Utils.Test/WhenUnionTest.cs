using System;
using zms9110750.Extensions.Utils.Canalot;
using static Utils.Test.WhenUnionTest;

namespace Utils.Test;

// ============ 测试 Utils.Canalot 的 union 版 When（Extensions/When.cs 当前版本）============
// 被测代码在库里，这里只写断言（静态分析后跳过"空 provider"场景，只测主路径）。
// 当前签名：
//   When<T>(T value, bool cond, TProvider<T> trueValue, TProvider<T> falseValue = default)
//   When<T,R>(T value, bool cond, TProvider<T,R> trueValue, TProvider<T,R> falseValue)

public class WhenUnionTest
{
    [Fact]
    public void CondFalse_ReturnsOriginal_RegardlessOfTrueValue()
    {

        Assert.Equal(1, 1.When(false, 2));                                  // 裸值形态
        Assert.Equal(1, 1.When(false, (Func<int>)(() => 7)));               // 无参 Func 形态
        Assert.Equal(1, 1.When(false, (Func<int, int>)(v => v + 100)));     // 有参 Func 形态
    }

    // ========== 单泛型：trueValue 四形态 ==========

    [Fact]
    public void CondTrue_BareValue_ReplacesValue()
    {
        Assert.Equal(5, 1.When(true, 5));
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
        Assert.Equal(1, result);
        Assert.Equal(1, side);
    }

    [Fact]
    public void CondFalse_Action_NotRun_ReturnsOriginal()
    {
        int side = 0;
        var result = 1.When(false, (Action<int>)(v => side = v));
        Assert.Equal(1, result);
        Assert.Equal(0, side);
    }

    // ========== 单泛型：显式 falseValue（条件假走 false 分支）==========

    [Fact]
    public void FalseBranch_ExplicitConst_UsedWhenConditionFalse()
    {
        Assert.Equal(5, 1.When(false, 999, falseValue: 5));   // 999 是占位 trueValue，不会执行
    }

    [Fact]
    public void FalseBranch_ExplicitFunc_AppliesToValue()
    {
        Assert.Equal(11, 1.When(false, 999, falseValue: (Func<int, int>)(v => v + 10)));
    }

    [Fact]
    public void TrueBranch_IgnoresFalseValue()
    {
        Assert.Equal(3, 1.When(true, 3, falseValue: 999));
    }

    // ========== 双泛型 When<T,R>：结果类型 R 与输入 T 不同 ==========

    [Fact]
    public void Bi_True_ConstR_ReturnsConstant()
    {
        Assert.Equal("ok", 1.When<int, string>(true, "ok", "no"));
    }

    [Fact]
    public void Bi_False_ConstR_ReturnsFalseConstant()
    {
        Assert.Equal("no", 1.When<int, string>(false, "ok", "no"));
    }

    [Fact]
    public void Bi_True_NoArgFuncR_ReturnsItsResult()
    {
        Assert.Equal("hi", 1.When<int, string>(true, (Func<string>)(() => "hi"), "no"));
    }

    [Fact]
    public void Bi_True_ValueFuncR_UsesInputValue()
    {
        Assert.Equal("v1", 1.When<int, string>(true, (Func<int, string>)(v => $"v{v}"), "no"));
    }

    [Fact]
    public void Bi_False_ValueFuncR_UsesInputValue()
    {
        Assert.Equal("f1", 1.When<int, string>(false, "ok", (Func<int, string>)(v => $"f{v}")));
    }
}
