using zms9110750.Utils.Core;

namespace Core.Test;

/// <summary>
/// 验证 Pagination 构造器和属性 setter 的边界校验
/// </summary>
public sealed class PaginationConstructorTest
{
    #region 位置构造器

    /// <summary>位置构造器直接赋值对应属性</summary>
    [Theory]
    [InlineData(1, 20, 100)]
    [InlineData(5, 10, 0)]
    [InlineData(3, 15, 200)]
    public void Ctor_Positional_AssignsProperties(int page, int pageSize, int total)
    {
        var p = new Pagination(page, pageSize, total);
        Assert.Equal(page, p.Page);
        Assert.Equal(pageSize, p.PageSize);
        Assert.Equal(total, p.Total);
    }

    /// <summary>位置构造器不走 setter 验证（Page=0 不抛）</summary>
    [Fact]
    public void Ctor_Positional_SkipsValidation()
    {
        var p = new Pagination(0, 0, -1);
        Assert.Equal(0, p.Page);
        Assert.Equal(0, p.PageSize);
        Assert.Equal(-1, p.Total);
    }

    #endregion

    #region 完整构造器

    /// <summary>完整构造器额外设置 ButtonCount 和 PreferEnd</summary>
    [Fact]
    public void Ctor_Full_SetsExtraProperties()
    {
        var p = new Pagination(1, 20, 100, 7, true);
        Assert.Equal(7, p.ButtonCount);
        Assert.True(p.PreferEnd);
    }

    /// <summary>ButtonCount 默认值为 5</summary>
    [Fact]
    public void Ctor_DefaultButtonCount()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Equal(5, p.ButtonCount);
    }

    /// <summary>PreferEnd 默认值为 false</summary>
    [Fact]
    public void Ctor_DefaultPreferEnd()
    {
        var p = new Pagination(1, 20, 100);
        Assert.False(p.PreferEnd);
    }

    #endregion

    #region Setter 验证

    /// <summary>构后 Page 设为 0 或超出 TotalPages 抛出异常</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Page_SetOutOfRange_Throws(int value)
    {
        var p = new Pagination(5, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Page = value);
    }

    /// <summary>构后 PageSize 设为 0 抛出异常</summary>
    [Fact]
    public void PageSize_SetZero_Throws()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.PageSize = 0);
    }

    /// <summary>构后 Total 设为负数抛出异常</summary>
    [Fact]
    public void Total_SetNegative_Throws()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Total = -1);
    }

    /// <summary>Total 设为 0 后 TotalPages 为 0，此时 Page 的合法范围变为 [1,0]，任何赋值都抛出异常</summary>
    [Fact]
    public void Page_SetAfterTotalZero_Throws()
    {
        var p = new Pagination(1, 20, 100);
        p.Total = 0; // TotalPages 变为 0
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Page = 1);
    }

    /// <summary>ButtonCount 设为 0 抛出异常</summary>
    [Fact]
    public void ButtonCount_SetZero_Throws()
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.ButtonCount = 0);
    }

    /// <summary>ButtonCount 设为负数抛出异常</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ButtonCount_SetNegative_Throws(int value)
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.ButtonCount = value);
    }

    #endregion
}

/// <summary>
/// 验证 Pagination 的计算属性
/// </summary>
public sealed class PaginationComputedPropertyTest
{
    #region TotalPages

    /// <summary>TotalPages = ceil(Total / PageSize)</summary>
    [Theory]
    [InlineData(1, 20, 100, 5)]
    [InlineData(1, 20, 101, 6)]
    [InlineData(1, 20, 0, 0)]
    [InlineData(1, 20, 5, 1)]
    public void TotalPages_CalculatedCorrectly(int page, int size, int total, int expected)
    {
        var p = new Pagination(page, size, total);
        Assert.Equal(expected, p.TotalPages);
    }

    #endregion

    #region HasPrevious / HasNext

    /// <summary>HasPrevious 在第一页为 false，之后为 true</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasPrevious_FirstPageOnlyFalse(int page, bool expected)
    {
        var p = new Pagination(page, 20, 100);
        Assert.Equal(expected, p.HasPrevious);
    }

    /// <summary>HasNext 在最后一页为 false，之前为 true</summary>
    [Theory]
    [InlineData(5, false)]
    [InlineData(1, true)]
    public void HasNext_LastPageOnlyFalse(int page, bool expected)
    {
        var p = new Pagination(page, 20, 100);
        Assert.Equal(expected, p.HasNext);
    }

    #endregion

    #region IsEmpty

    /// <summary>Total == 0 时 IsEmpty 为 true</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsEmpty_ZeroTotalOnly(int total, bool expected)
    {
        var p = new Pagination(1, 20, total);
        Assert.Equal(expected, p.IsEmpty);
    }

    #endregion
}

/// <summary>
/// 验证 DataRange 返回当前页的 0-based 索引范围
/// </summary>
public sealed class PaginationDataRangeTest
{
    /// <summary>DataRange 的 start 等于 (Page-1)*PageSize，end 不超过 Total</summary>
    [Theory]
    [InlineData(1, 20, 100, 0, 20)]
    [InlineData(2, 20, 100, 20, 40)]
    [InlineData(5, 20, 88, 80, 88)]
    [InlineData(1, 20, 5, 0, 5)]
    public void DataRange_CalculatedCorrectly(int page, int size, int total, int start, int end)
    {
        var p = new Pagination(page, size, total);
        Assert.Equal(start..end, p.DataRange);
    }

    /// <summary>Total=0 时 DataRange 为空范围 0..0</summary>
    [Fact]
    public void DataRange_TotalZero_ReturnsEmpty()
    {
        var p = new Pagination(1, 20, 0);
        Assert.Equal(0..0, p.DataRange);
    }
}

/// <summary>
/// 验证 ButtonRange 返回分页按钮显示的页码范围
/// </summary>
public sealed class PaginationButtonRangeTest
{
    /// <summary>首页区从 1 开始，末页区到 TotalPages 结束，中间区居中</summary>
    [Theory]
    [InlineData(1, 5, 1, 6)]     // 首页区
    [InlineData(5, 5, 3, 8)]     // 中间区
    [InlineData(10, 5, 6, 11)]   // 末页区
    public void ButtonRange_OddCount_Centered(int page, int count, int start, int end)
    {
        var p = new Pagination(page, 10, 100) { ButtonCount = count };
        Assert.Equal(start..end, p.ButtonRange);
    }

    /// <summary>偶数按钮时 PreferEnd 影响偏移方向</summary>
    [Theory]
    [InlineData(5, false, 2, 8)]
    [InlineData(5, true, 3, 9)]
    public void ButtonRange_EvenCount_RespectsPreferEnd(int page, bool preferEnd, int start, int end)
    {
        var p = new Pagination(page, 10, 100) { ButtonCount = 6, PreferEnd = preferEnd };
        Assert.Equal(start..end, p.ButtonRange);
    }

    /// <summary>按钮数多于总页数时显示全部</summary>
    [Fact]
    public void ButtonRange_CountExceedsTotal_ShowsAll()
    {
        var p = new Pagination(3, 10, 50) { ButtonCount = 10 };
        Assert.Equal(1..6, p.ButtonRange);
    }

    /// <summary>TotalPages=0 时 ButtonRange 返回空范围 1..1</summary>
    [Fact]
    public void ButtonRange_TotalPagesZero_ReturnsEmpty()
    {
        var p = new Pagination(1, 20, 0); // TotalPages=0
        Assert.Equal(1..1, p.ButtonRange);
    }

    /// <summary>Page=1, ButtonCount=2 时两种 PreferEnd 均 clamp 到起始页 1</summary>
    [Theory]
    [InlineData(false, 1, 3)]
    [InlineData(true, 1, 3)]
    public void ButtonRange_EvenCountTwoAtFirstPage_ClampsStart(bool preferEnd, int start, int end)
    {
        var p = new Pagination(1, 10, 100) { ButtonCount = 2, PreferEnd = preferEnd };
        Assert.Equal(start..end, p.ButtonRange);
    }
}

/// <summary>
/// 验证 GoToRecord 跳转到指定记录所在页
/// </summary>
public sealed class PaginationGoToRecordTest
{
    /// <summary>GoToRecord 返回正确的页码并更新 Page</summary>
    [Theory]
    [InlineData(35, 2)]
    [InlineData(1, 1)]
    [InlineData(100, 5)]
    public void GoToRecord_ReturnsCorrectPage(int record, int expectedPage)
    {
        var p = new Pagination(1, 20, 100);
        var page = p.GoToRecord(record);
        Assert.Equal(expectedPage, page);
        Assert.Equal(expectedPage, p.Page);
    }

    /// <summary>GoToRecord 越界时抛出异常</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GoToRecord_OutOfRange_Throws(int record)
    {
        var p = new Pagination(1, 20, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GoToRecord(record));
    }
}
