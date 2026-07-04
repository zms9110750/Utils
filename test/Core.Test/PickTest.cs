using zms9110750.Utils.Core.Pick;

namespace Core.Test;

/// <summary>
/// 验证 ValueRange 的基本功能。
/// 预期：GetRandomIndex 返回 [Start, End) 内的值；ShiftLeft 正确调整删除后的偏移。
/// </summary>
public class ValueRangeTest
{
    [Fact]
    public void 构造器设置属性()
    {
        var r = new ValueRange(2, 5, 3);
        Assert.Equal(2, r.Start);
        Assert.Equal(5, r.End);
        Assert.Equal(3, r.Count);
    }

    [Fact]
    public void 记录相等()
    {
        var a = new ValueRange(1, 4, 3);
        var b = new ValueRange(1, 4, 3);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GetRandomIndex_在范围内()
    {
        var r = new ValueRange(0, 10, 10);
        for (int i = 0; i < 100; i++)
        {
            int idx = r.GetRandomIndex();
            Assert.InRange(idx, 0, 9);
        }
    }

    [Fact]
    public void ShiftLeft_索引在_start_前_不变()
    {
        var r = new ValueRange(5, 10, 5);
        var shifted = r.ShiftLeft(3);
        Assert.Equal(new ValueRange(4, 9, 5), shifted);
    }

    [Fact]
    public void ShiftLeft_索引在范围内_减_count()
    {
        var r = new ValueRange(3, 8, 5);
        var shifted = r.ShiftLeft(5);
        Assert.Equal(new ValueRange(3, 7, 4), shifted);
    }

    [Fact]
    public void ShiftLeft_索引不在范围内_不变()
    {
        var r = new ValueRange(3, 8, 5);
        var shifted = r.ShiftLeft(10);
        Assert.Equal(r, shifted);
    }
}

/// <summary>
/// 验证可放回抽取器 ReplacementPicker 的基本行为。
/// 预期：每次 Pick 返回池中的一个项，多次抽取不改变池。
/// </summary>
public class ReplacementPickerTest
{
    [Fact]
    public void 抽取返回池中的项()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 }, { "C", 2 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 3, 2, 10);
        var result = picker.Pick();
        Assert.Contains(result, pool.Keys);
    }

    [Fact]
    public void 多次抽取_直到约束耗尽()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 }, { "C", 2 } };
        var picker = new ReplacementPicker<string>(pool);
        picker.SetConstraints(1, 3, 2, 10);
        int count = 0;
        while (picker.CountMin > 0 && picker.PointMin > 0)
        {
            var item = picker.Pick();
            Assert.Contains(item, pool.Keys);
            count++;
        }
        Assert.True(count > 0);
    }
}

/// <summary>
/// 验证不放回抽取器 NonReplacementPicker 的基本行为。
/// 预期：每次 Pick 从池中移除一项，池耗尽后继续抽取抛 InvalidOperationException。
/// </summary>
public class NonReplacementPickerTest
{
    [Fact]
    public void 抽取返回池中的项()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(1, 2, 3, 8);
        var result = picker.Pick();
        Assert.Contains(result, pool.Keys);
    }

    [Fact]
    public void 不放回_两次抽取不同()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(2, 2, 8, 8);
        var r1 = picker.Pick();
        var r2 = picker.Pick();
        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public void 约束不满足时抛异常()
    {
        var pool = new Dictionary<string, int> { { "A", 3 }, { "B", 5 } };
        var picker = new NonReplacementPicker<string>(pool);
        picker.SetConstraints(3, 3, 20, 20);
        Assert.Throws<InvalidOperationException>(() => picker.Pick());
    }
}
