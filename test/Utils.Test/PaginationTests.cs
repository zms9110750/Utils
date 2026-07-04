using zms9110750.Utils.Core;

namespace Utils.Test;

public class PaginationTests
{
    // ── 构造器 ──

    [Fact]
    public void 默认构造器_Page_为_1()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(1, p.Page);
    }

    [Fact]
    public void 默认构造器_PageSize_为传入值()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(20, p.PageSize);
    }

    [Fact]
    public void 默认构造器_Total_为传入值()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(100, p.Total);
    }

    [Fact]
    public void Page_设为_0_构造时不验证()
    {
        var p = new Pagination(0, 20, 100);
        Assert.Equal(0, p.Page);
    }

    [Fact]
    public void Page_构后设为_0_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Page = 0);
    }

    [Fact]
    public void PageSize_设为_0_构造时不验证()
    {
        var p = new Pagination(1, 0, 100);
        Assert.Equal(0, p.PageSize);
    }

    [Fact]
    public void PageSize_构后设为_0_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.PageSize = 0);
    }

    [Fact]
    public void Total_设为负数_构造时不验证()
    {
        var p = new Pagination(1, 20, -1);
        Assert.Equal(-1, p.Total);
    }

    [Fact]
    public void Total_构后设为负数_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Total = -1);
    }

    // ── 完整构造器 ──

    [Fact]
    public void 完整构造器_设置按钮数()
    {
        var p = new Pagination(1, 20, 100, 7, true);
        Assert.Equal(7, p.ButtonCount);
        Assert.True(p.PreferEnd);
    }

    // ── TotalPages ──

    [Fact]
    public void TotalPages_应为_分页总数()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(5, p.TotalPages);
    }

    [Fact]
    public void TotalPages_无数据时_为_0()
    {
        var p = new Pagination(1, 20, 0);
        Assert.Equal(0, p.TotalPages);
    }

    [Fact]
    public void TotalPages_不满一页_为_1()
    {
        var p = new Pagination(1, 20, 5);
        Assert.Equal(1, p.TotalPages);
    }

    // ── HasPrevious / HasNext ──

    [Fact]
    public void 第一页_HasPrevious_为_false()
    {
        var p = new Pagination(1, 20, 100);
        Assert.False(p.HasPrevious);
    }

    [Fact]
    public void 第二页_HasPrevious_为_true()
    {
        var p = new Pagination(2, 20, 100);
        Assert.True(p.HasPrevious);
    }

    [Fact]
    public void 最后一页_HasNext_为_false()
    {
        var p = new Pagination(5, 20, 100);
        Assert.False(p.HasNext);
    }

    [Fact]
    public void 第一页_HasNext_为_true()
    {
        var p = new Pagination(1, 20, 100);
        Assert.True(p.HasNext);
    }

    // ── IsEmpty ──

    [Fact]
    public void Total_为_0_时_IsEmpty_为_true()
    {
        var p = new Pagination(1, 20, 0);
        Assert.True(p.IsEmpty);
    }

    [Fact]
    public void Total_为_100_时_IsEmpty_为_false()
    {
        var p = new Pagination(1, 20, 100);
        Assert.False(p.IsEmpty);
    }

    // ── DataRange ──

    [Fact]
    public void DataRange_第一页从_0_开始()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(0..20, p.DataRange);
    }

    [Fact]
    public void DataRange_第二页从_20_开始()
    {
        var p = new Pagination(2, 20, 100);
        Assert.Equal(20..40, p.DataRange);
    }

    [Fact]
    public void DataRange_最后一页不超过_Total()
    {
        var p = new Pagination(5, 20, 88);
        Assert.Equal(80..88, p.DataRange);
    }

    // ── GoToRecord ──

    [Fact]
    public void GoToRecord_跳转到指定记录所在页()
    {
        var p = new Pagination(1, 20, 100);
        int page = p.GoToRecord(35);
        Assert.Equal(2, page);
        Assert.Equal(2, p.Page);
    }

    [Fact]
    public void GoToRecord_记录小于_1_抛出异常()
    {
        var p = new Pagination(3, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GoToRecord(0));
    }

    [Fact]
    public void GoToRecord_记录大于_Total_抛出异常()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GoToRecord(200));
    }

    // ── ButtonRange ──

    [Fact]
    public void ButtonRange_首页区_从_1_开始()
    {
        var p = new Pagination(1, 10, 100) { ButtonCount = 5 };
        Assert.Equal(1..6, p.ButtonRange);
    }

    [Fact]
    public void ButtonRange_中间区_居中()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 5 };
        Assert.Equal(3..8, p.ButtonRange);
    }

    [Fact]
    public void ButtonRange_末页区_到_TotalPages_结束()
    {
        var p = new Pagination(10, 10, 100) { ButtonCount = 5 };
        Assert.Equal(6..11, p.ButtonRange);
    }

    [Fact]
    public void ButtonRange_按钮数多于总页数_显示全部()
    {
        var p = new Pagination(3, 10, 50) { ButtonCount = 10 };
        Assert.Equal(1..6, p.ButtonRange);
    }

    [Fact]
    public void ButtonRange_偶数按钮_PreferEnd_false_偏左()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 6, PreferEnd = false };
        Assert.Equal(2..8, p.ButtonRange);
    }

    [Fact]
    public void ButtonRange_偶数按钮_PreferEnd_true_偏右()
    {
        var p = new Pagination(5, 10, 100) { ButtonCount = 6, PreferEnd = true };
        Assert.Equal(3..9, p.ButtonRange);
    }
}
