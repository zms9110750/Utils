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
    #region 约束设置

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

    #endregion

    #region 空池 / 零值约束

    /// <summary>空字典构造不抛出异常</summary>
    [Fact]
    public void Ctor_EmptyDict_DoesNotThrow()
    {
        var picker = new ReplacementPicker<string>(new Dictionary<string, int>());
        Assert.NotNull(picker);
    }

    /// <summary>零值约束设置不抛出异常</summary>
    [Fact]
    public void SetConstraints_ZeroValues_DoesNotThrow()
    {
        var pool = new Dictionary<string, int> { { "A", 3 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(0, 0, 0, 0);
        Assert.Equal(0, picker.CountMin);
        Assert.Equal(0, picker.CountMax);
        Assert.Equal(0, picker.PointMin);
        Assert.Equal(0, picker.PointMax);
    }

    #endregion
}

/// <summary>
/// 验证 ReplacementPicker 可放回抽取行为
/// </summary>
public sealed class ReplacementPickerTest
{
    #region 基本抽取

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

    #endregion

    #region Low / High

    /// <summary>验证 Low / High 计算正确</summary>
    /// <remarks>
    /// ReplacementPicker.Low = PointMin - (CountMax - 1) * MaxVal
    /// ReplacementPicker.High = PointMax - (CountMin - 1) * MinVal
    /// 池 { A=3, B=5 } → MinVal=3, MaxVal=5
    /// </remarks>
    [Theory]
    [InlineData(1, 1, 3, 5, 3, 5)]
    [InlineData(1, 3, 2, 10, -8, 10)]
    [InlineData(2, 4, 8, 20, -7, 17)]
    public void LowHigh_ComputedCorrectly(int cMin, int cMax, int pMin, int pMax, int expectedLow, int expectedHigh)
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(cMin, cMax, pMin, pMax);
        Assert.Equal(expectedLow, picker.Low);
        Assert.Equal(expectedHigh, picker.High);
    }

    #endregion

    #region 约束验证

    /// <summary>抽取后所有抽取项的 V 值之和在 [原始 PointMin, 原始 PointMax] 内（固定抽取数）</summary>
    [Fact]
    public void Pick_SumWithinConstraints()
    {
        var pool = new Dictionary<string, int> { { "A", 2 }, { "B", 3 }, { "C", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        int originalCMin = 2, originalCMax = 2;
        int originalPMin = 4, originalPMax = 12;
        picker.SetConstraints(originalCMin, originalCMax, originalPMin, originalPMax);
        int sum = 0, count = 0;
        try
        {
            while (count < originalCMax)
            {
                var picked = picker.Pick();
                sum += pool[picked];
                count++;
            }
        }
        catch (InvalidOperationException)
        {
            // 无可候选 — 正常终止
        }
        Assert.InRange(count, originalCMin, originalCMax);
        Assert.InRange(sum, originalPMin, originalPMax);
    }

    #endregion

    #region 不可满足约束

    /// <summary>约束不可满足时抛出 InvalidOperationException</summary>
    [Fact]
    public void Pick_Unsatisfiable_Throws()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 1, 100, 100);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }

    /// <summary>CountMax 过小导致无候选时抛出异常</summary>
    [Fact]
    public void Pick_CountMaxTooSmall_Throws()
    {
        var pool = new Dictionary<string, int> { { "A", 5 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 1, 20, 20);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }

    #endregion

    #region 大量数据

    /// <summary>大量数据稳定性测试</summary>
    [Fact]
    public void Pick_LargePool_Stability()
    {
        var pool = Enumerable.Range(1, 1000).ToDictionary(i => i.ToString(), i => i % 10 + 1);
        var picker = new ReplacementPicker<string>(pool);
        int originalCMin = 50, originalCMax = 200;
        int originalPMin = 200, originalPMax = 2000;
        picker.SetConstraints(originalCMin, originalCMax, originalPMin, originalPMax);
        int sum = 0, count = 0;
        try
        {
            while (count < originalCMax)
            {
                var picked = picker.Pick();
                sum += pool[picked];
                count++;
            }
        }
        catch (InvalidOperationException)
        {
            // 无可候选 — 正常终止
        }
        Assert.InRange(count, originalCMin, originalCMax);
        Assert.InRange(sum, originalPMin, originalPMax);
    }

    #endregion
}

/// <summary>
/// 验证 NonReplacementPicker 不放回抽取行为
/// </summary>
public sealed class NonReplacementPickerTest
{
    #region 基本抽取

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

    #endregion

    #region Low / High

    /// <summary>验证 Low / High 计算正确</summary>
    /// <remarks>
    /// 池 { A=3, B=5 } → Ranges: {3: (0,1,1), 5: (1,2,1)}
    /// NonReplacementPicker.Low: needed=CountMax-1, 从大到小取 needed 项求和, Low=PointMin-sum
    /// NonReplacementPicker.High: needed=CountMin-1, 从小到大取 needed 项求和, High=PointMax-sum
    /// </remarks>
    [Theory]
    [InlineData(1, 2, 3, 8, -2, 8)]
    [InlineData(2, 2, 8, 8, 3, 5)]
    [InlineData(1, 1, 3, 5, 3, 5)]
    public void LowHigh_ComputedCorrectly(int cMin, int cMax, int pMin, int pMax, int expectedLow, int expectedHigh)
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(cMin, cMax, pMin, pMax);
        Assert.Equal(expectedLow, picker.Low);
        Assert.Equal(expectedHigh, picker.High);
    }

    #endregion

    #region 约束验证

    /// <summary>抽取后所有抽取项的 V 值之和在 [原始 PointMin, 原始 PointMax] 内</summary>
    [Fact]
    public void Pick_SumWithinConstraints()
    {
        var pool = new Dictionary<string, int> { { "A", 2 }, { "B", 3 }, { "C", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        int originalCMin = 2, originalCMax = 3;
        int originalPMin = 5, originalPMax = 10;
        picker.SetConstraints(originalCMin, originalCMax, originalPMin, originalPMax);
        int sum = 0, count = 0;
        try
        {
            while (count < originalCMax)
            {
                var picked = picker.Pick();
                sum += pool[picked];
                count++;
            }
        }
        catch (InvalidOperationException)
        {
            // 无可候选 — 正常终止
        }
        Assert.InRange(count, originalCMin, originalCMax);
        Assert.InRange(sum, originalPMin, originalPMax);
    }

    #endregion

    #region 不可满足约束

    /// <summary>约束不可满足时抛出 InvalidOperationException</summary>
    [Fact]
    public void Pick_Unsatisfiable_Throws()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(3, 3, 20, 20);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }

    /// <summary>CountMax 过小导致无候选时抛出异常</summary>
    [Fact]
    public void Pick_CountMaxTooSmall_Throws()
    {
        var pool = new Dictionary<string, int> { { "A", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(1, 1, 20, 20);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }

    #endregion

    #region 同 V 值多项 + ShiftLeft 多分组

    /// <summary>同 V 值多项及多分组的非放回抽取正确移除，验证 ShiftLeft 多分组场景</summary>
    [Fact]
    public void Pick_SameValueMultiGroup_RemovesCorrectly()
    {
        // 两个 V 值组各含多项，构造多分组场景
        var pool = new Dictionary<string, int>
        {
            { "A", 3 }, { "B", 3 },  // V=3 组
            { "C", 5 }, { "D", 5 }   // V=5 组
        };
        var picker = new NonReplacementPicker<string>(pool);
        // 需要抽全部 4 项
        picker.SetConstraints(4, 4, 16, 16);
        var picks = new List<string>
        {
            picker.Pick(),
            picker.Pick(),
            picker.Pick(),
            picker.Pick()
        };
        // 四项各不相同
        Assert.Equal(4, picks.Distinct().Count());
        // 池已空，再次 Pick 应抛出
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }

    /// <summary>同 V 值多组，部分抽取后剩余项仍可继续抽取</summary>
    [Fact]
    public void Pick_SameValueMultiGroup_PartialExtract()
    {
        var pool = new Dictionary<string, int>
        {
            { "A", 2 }, { "B", 2 }, { "C", 2 },  // V=2 组（3 项）
            { "D", 4 }, { "E", 4 }                // V=4 组（2 项）
        };
        var picker = new NonReplacementPicker<string>(pool);
        // 只抽 2 项，不耗尽
        picker.SetConstraints(2, 2, 4, 8);
        var first = picker.Pick();
        var second = picker.Pick();
        Assert.NotEqual(first, second);
        // 抽取后 CountMin 应为 0（已满足最小抽取数）
        Assert.Equal(0, picker.CountMin);
    }

    #endregion

    #region 大量数据

    /// <summary>大量数据稳定性测试</summary>
    [Fact]
    public void Pick_LargePool_Stability()
    {
        var pool = Enumerable.Range(1, 500).ToDictionary(i => i.ToString(), i => i % 10 + 1);
        var picker = new NonReplacementPicker<string>(pool);
        int originalCMin = 50, originalCMax = 100;
        int originalPMin = 200, originalPMax = 800;
        picker.SetConstraints(originalCMin, originalCMax, originalPMin, originalPMax);
        int sum = 0, count = 0;
        try
        {
            while (count < originalCMax)
            {
                var picked = picker.Pick();
                sum += pool[picked];
                count++;
            }
        }
        catch (InvalidOperationException)
        {
            // 无可候选 — 正常终止
        }
        Assert.InRange(count, originalCMin, originalCMax);
        Assert.InRange(sum, originalPMin, originalPMax);
    }

    #endregion
}
#endif
