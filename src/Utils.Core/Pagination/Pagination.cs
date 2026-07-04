using System.Runtime.CompilerServices;

namespace zms9110750.Utils.Core;

/// <summary>分页模型</summary>
public record struct Pagination(int Page, int PageSize, int Total)
{
    /// <summary>当前页码（从 1 开始）。</summary>
    public int Page { get; set => field = ThrowIfOutOfRange(value, 1, TotalPages, "Page"); } = Page;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set => field = ThrowIfOutOfRange(value, 1, int.MaxValue, "PageSize"); } = PageSize;

    /// <summary>总数据量。</summary>
    public int Total { get; set => field = ThrowIfOutOfRange(value, 0, int.MaxValue, "Total"); } = Total;

    /// <summary>分页按钮个数。</summary>
    public int ButtonCount { get; set => field = ThrowIfOutOfRange(value, 1, int.MaxValue, "ButtonCount"); } = 5;

    /// <summary>双数按钮时当前页是否更靠后。</summary>
    public bool PreferEnd { get; set; } = false;

    /// <summary>总页数。</summary>
    public readonly int TotalPages => Total == 0 ? 0 : (Total - 1) / PageSize + 1;

    /// <summary>当前页在整体数据中的索引范围。</summary>
    public readonly Range DataRange
    {
        get
        {
            int start = (Page - 1) * PageSize;
            int end = Math.Min(start + PageSize, Total);
            return start..end;
        }
    }

    /// <summary>分页按钮显示的页码范围（1-based 闭区间）。</summary>
    public readonly Range ButtonRange
    {
        get
        {
            if (ButtonCount >= TotalPages)
            {
                return 1..(TotalPages + 1);
            }

            int start = Page - Math.DivRem(ButtonCount, 2, out int odd);
            if (odd == 0 && PreferEnd)
            {
                start++;
            }

            start = Math.Clamp(start, 1, TotalPages - ButtonCount + 1);

            return start..(start + ButtonCount);
        }
    }

    /// <summary>是否有上一页。</summary>
    public readonly bool HasPrevious => Page > 1;

    /// <summary>是否有下一页。</summary>
    public readonly bool HasNext => Page < TotalPages;

    /// <summary>是否无数据。</summary>
    public readonly bool IsEmpty => Total == 0;

    /// <summary>
    /// 跳转到包含第 <paramref name="recordIndex"/> 条数据的页。
    /// </summary>
    /// <param name="recordIndex">数据序号（从 1 开始）。</param>
    /// <returns>跳转后的页码。</returns>
    public int GoToRecord(int recordIndex)
    {
        ThrowIfOutOfRange(recordIndex, 1, Total, nameof(recordIndex));

        Page = (recordIndex - 1) / PageSize + 1;
        return Page;
    }

    /// <summary>完整构造器。</summary>
    public Pagination(int page, int pageSize, int total, int buttonCount, bool preferEnd)
        : this(page, pageSize, total)
    {
        ButtonCount = buttonCount;
        PreferEnd = preferEnd;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static T ThrowIfOutOfRange<T>(T value, T min, T max, string paramName, string? message = null) where T : IComparable<T>
    {
        return value.CompareTo(min) < 0 || value.CompareTo(max) > 0
            ? throw new ArgumentOutOfRangeException(paramName, value, message ?? $"参数必须在 {min} 和 {max} 之间")
            : value;
    }
}
