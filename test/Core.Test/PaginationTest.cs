using zms9110750.Utils.Core;

namespace Core.Test;

/// <summary>
/// 验证 Pagination 的构造器和基本属性。
/// 预期：位置参数直接赋值（不经过 setter 验证），构后赋值时 Page ∈ [1, TotalPages]、PageSize ≥ 1、Total ≥ 0。
/// </summary>
public class PaginationConstructorTest
{
    [Fact]
    public void 位置构造器_赋值给对应属性()
    {
        var p = new Pagination(3, 15, 200);
        Assert.Equal(3, p.Page);
        Assert.Equal(15, p.PageSize);
        Assert.Equal(200, p.Total);
    }

    [Fact]
    public void 完整构造器_额外设置按钮数()
    {
        var p = new Pagination(1, 20, 100, 7, true);
        Assert.Equal(7, p.ButtonCount);
        Assert.True(p.PreferEnd);
    }

    [Fact]
    public void 默认按钮数为_5()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(5, p.ButtonCount);
    }

    [Fact]
    public void 默认_PreferEnd_为_false()
    {
        var p = new Pagination(1, 20, 100);
        Assert.False(p.PreferEnd);
    }

    [Fact]
    public void 位置构造器不走_setter_验证()
    {
        var p = new Pagination(0, 0, -1);
        Assert.Equal(0, p.Page);
        Assert.Equal(0, p.PageSize);
        Assert.Equal(-1, p.Total);
    }

    [Fact]
    public void 构后_Page_设为_0_抛出_ArgumentOutOfRangeException()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Page = 0);
    }

    [Fact]
    public void 构后_Page_设为_超出_TotalPages_抛出异常()
    {
        var p = new Pagination(5, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Page = 6);
    }

    [Fact]
    public void 构后_PageSize_设为_0_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.PageSize = 0);
    }

    [Fact]
    public void 构后_Total_设为负数_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Total = -1);
    }
}

/// <summary>
/// 验证 Pagination 的计算属性。
/// 预期：TotalPages = ceil(Total / PageSize)，HasPrevious / HasNext 在边界正确，IsEmpty 反映 Total == 0。
/// </summary>
public class PaginationComputedPropertiesTest
{
    [Fact]
    public void TotalPages_整除时()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(5, p.TotalPages);
    }

    [Fact]
    public void TotalPages_余数时进一()
    {
        var p = new Pagination(1, 20, 101);
        Assert.Equal(6, p.TotalPages);
    }

    [Fact]
    public void TotalPages_无数据时为_0()
    {
        var p = new Pagination(1, 20, 0);
        Assert.Equal(0, p.TotalPages);
    }

    [Fact]
    public void 第一页_HasPrevious_false()
    {
        var p = new Pagination(1, 20, 100);
        Assert.False(p.HasPrevious);
    }

    [Fact]
    public void 第二页_HasPrevious_true()
    {
        var p = new Pagination(2, 20, 100);
        Assert.True(p.HasPrevious);
    }

    [Fact]
    public void 最后一页_HasNext_false()
    {
        var p = new Pagination(5, 20, 100);
        Assert.False(p.HasNext);
    }

    [Fact]
    public void 第一页_HasNext_true()
    {
        var p = new Pagination(1, 20, 100);
        Assert.True(p.HasNext);
    }

    [Fact]
    public void Total_为_0_时_IsEmpty_true()
    {
        var p = new Pagination(1, 20, 0);
        Assert.True(p.IsEmpty);
    }

    [Fact]
    public void Total_大于_0_时_IsEmpty_false()
    {
        var p = new Pagination(1, 20, 1);
        Assert.False(p.IsEmpty);
    }
}

/// <summary>
/// 验证 Pagination.DataRange 返回当前页在整体数据中的索引范围。
/// 预期：[0-based start)..[end)，最后一页 end 不超过 Total。
/// </summary>
public class PaginationDataRangeTest
{
    [Fact]
    public void 第一页_从_0_开始()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(0..20, p.DataRange);
    }

    [Fact]
    public void 第二页_从_20_开始()
    {
        var p = new Pagination(2, 20, 100);
        Assert.Equal(20..40, p.DataRange);
    }

    [Fact]
    public void 最后一页_不超过_Total()
    {
        var p = new Pagination(5, 20, 88);
        Assert.Equal(80..88, p.DataRange);
    }

    [Fact]
    public void 不满一页_范围正确()
    {
        var p = new Pagination(1, 20, 5);
        Assert.Equal(0..5, p.DataRange);
    }
}

/// <summary>
/// 验证 Pagination.ButtonRange 返回分页按钮的页码范围。
/// 预期：首页区从 1 开始，末页区到 TotalPages 结束，中间区居中。
/// </summary>
public class PaginationButtonRangeTest
{
    [Fact]
    public void 首页区_从_1_开始()
    {
        var p = new Pagination(1, 10, 100) { ButtonCount = 5 };
        Assert.Equal(1..6, p.ButtonRange);
    }

    [Fact]
    public void 中间区_居中()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 5 };
        Assert.Equal(3..8, p.ButtonRange);
    }

    [Fact]
    public void 末页区_到_TotalPages_结束()
    {
        var p = new Pagination(10, 10, 100) { ButtonCount = 5 };
        Assert.Equal(6..11, p.ButtonRange);
    }

    [Fact]
    public void 按钮数多于总页数_显示全部()
    {
        var p = new Pagination(3, 10, 50) { ButtonCount = 10 };
        Assert.Equal(1..6, p.ButtonRange);
    }

    [Fact]
    public void 偶数按钮_PreferEnd_false_偏左()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 6, PreferEnd = false };
        Assert.Equal(2..8, p.ButtonRange);
    }

    [Fact]
    public void 偶数按钮_PreferEnd_true_偏右()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 6, PreferEnd = true };
        Assert.Equal(3..9, p.ButtonRange);
    }

    [Fact]
    public void 首页_偶数按钮_PreferEnd_true()
    {
        var p = new Pagination(1, 10, 100) { ButtonCount = 6, PreferEnd = true };
        Assert.Equal(1..7, p.ButtonRange);
    }

    [Fact]
    public void 末页_奇数按钮()
    {
        var p = new Pagination(10, 10, 100) { ButtonCount = 5 };
        Assert.Equal(6..11, p.ButtonRange);
    }
}

/// <summary>
/// 验证 Pagination.GoToRecord 跳转到指定记录所在页。
/// 预期：recordIndex ∈ [1, Total] 时返回正确页码；越界时抛出 ArgumentOutOfRangeException。
/// </summary>
public class PaginationGoToRecordTest
{
    [Fact]
    public void 中间记录_页码正确()
    {
        var p = new Pagination(1, 20, 100);
        int page = p.GoToRecord(35);
        Assert.Equal(2, page);
        Assert.Equal(2, p.Page);
    }

    [Fact]
    public void 第一条记录_跳转到第_1_页()
    {
        var p = new Pagination(3, 20, 100);
        int page = p.GoToRecord(1);
        Assert.Equal(1, page);
    }

    [Fact]
    public void 最后一条记录_跳转到最后一页()
    {
        var p = new Pagination(1, 20, 100);
        int page = p.GoToRecord(100);
        Assert.Equal(5, page);
    }

    [Fact]
    public void 记录小于_1_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GoToRecord(0));
    }

    [Fact]
    public void 记录大于_Total_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GoToRecord(101));
    }
}
