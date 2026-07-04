#if NET6_0_OR_GREATER
using zms9110750.Utils.Core.Pick;

namespace Core.Test;

/// <summary>
/// 验证 ValueRange 结构体基本功能
/// </summary>
public sealed class ValueRangeTest
{
    #region 构造

    /// <summary>位置构造器赋值 Start / End / Count</summary>
    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(2, 5, 3)]
    public void Ctor_AssignsProperties(int start, int end, int count)
    {
        var r = new ValueRange(start, end, count);
        Assert.Equal(start, r.Start);
        Assert.Equal(end, r.End);
        Assert.Equal(count, r.Count);
    }

    #endregion

    #region GetRandomIndex

    /// <summary>GetRandomIndex 返回 [Start, End) 内的值</summary>
    [Fact]
    public void GetRandomIndex_WithinRange()
    {
        var r = new ValueRange(0, 10, 10);
        for (int i = 0; i < 100; i++)
        {
            Assert.InRange(r.GetRandomIndex(), 0, 9);
        }
    }

    #endregion

    #region ShiftLeft

    /// <summary>删除索引在 start 前时整体左移</summary>
    [Fact]
    public void ShiftLeft_BeforeStart_ShiftsAll()
    {
        var r = new ValueRange(5, 10, 5);
        Assert.Equal(new ValueRange(4, 9, 5), r.ShiftLeft(3));
    }

    /// <summary>删除索引在范围内时 Count 减 1</summary>
    [Fact]
    public void ShiftLeft_InsideRange_ReducesCount()
    {
        var r = new ValueRange(3, 8, 5);
        Assert.Equal(new ValueRange(3, 7, 4), r.ShiftLeft(5));
    }

    /// <summary>删除索引在 end 外时不变</summary>
    [Fact]
    public void ShiftLeft_AfterEnd_Unchanged()
    {
        var r = new ValueRange(3, 8, 5);
        Assert.Equal(r, r.ShiftLeft(10));
    }

    #endregion
}

/// <summary>
/// 验证 BasePicker 的 SetConstraints 和 SelectKey 功能
/// </summary>
public sealed class BasePickerTest
{
    /// <summary>SetConstraints 设置约束后 CountMin / CountMax / PointMin / PointMax 正确</summary>
    [Theory]
    [InlineData(1, 3, 5, 15)]
    [InlineData(2, 2, 8, 8)]
    public void SetConstraints_AssignsCorrectly(int cMin, int cMax, int pMin, int pMax)
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(cMin, cMax, pMin, pMax);
        Assert.Equal(cMin, picker.CountMin);
        Assert.Equal(cMax, picker.CountMax);
        Assert.Equal(pMin, picker.PointMin);
        Assert.Equal(pMax, picker.PointMax);
    }
}

/// <summary>
/// 验证 ReplacementPicker 可放回抽取行为
/// </summary>
public sealed class ReplacementPickerTest
{
    /// <summary>Pick 返回池中的项</summary>
    [Fact]
    public void Pick_ReturnsItemFromPool()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 3, 2, 10);
        Assert.Contains(picker.Pick(), pool.Keys);
    }

    /// <summary>多次抽取直到约束耗尽，每次返回有效项</summary>
    [Fact]
    public void Pick_Multiple_UntilConstraintExhausted()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 3, 2, 10);
        while (picker.CountMin > 0 && picker.PointMin > 0)
        {
            Assert.Contains(picker.Pick(), pool.Keys);
        }
    }
}

/// <summary>
/// 验证 NonReplacementPicker 不放回抽取行为
/// </summary>
public sealed class NonReplacementPickerTest
{
    /// <summary>Pick 返回池中的项</summary>
    [Fact]
    public void Pick_ReturnsItemFromPool()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(1, 2, 3, 8);
        Assert.Contains(picker.Pick(), pool.Keys);
    }

    /// <summary>不放回抽取不能重复选中同一项</summary>
    [Fact]
    public void Pick_TwoDifferentItems()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(2, 2, 8, 8);
        Assert.NotEqual(picker.Pick(), picker.Pick());
    }

    /// <summary>约束不可满足时抛出 InvalidOperationException</summary>
    [Fact]
    public void Pick_Unsatisfiable_Throws()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(3, 3, 20, 20);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }
}
#endif
