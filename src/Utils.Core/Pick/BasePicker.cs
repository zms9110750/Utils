namespace zms9110750.Utils.Core.Pick;

/// <summary>
/// 区间约束随机抽取基类。外部循环：SetConstraints + 反复 Pick()。
/// </summary>
/// <typeparam name="T">项的类型</typeparam>
public abstract class BasePicker<T>
{
    /// <summary>名称列表（按 V 值升序）。</summary>
    protected List<T> Items { get; }

    /// <summary>V 值到索引范围的映射。Key=V，Value=在 Items 中的范围。</summary>
    protected SortedList<int, ValueRange> Ranges { get; }

    /// <summary>剩余可取数量下限。</summary>
    public int CountMin { get; protected set; }
    /// <summary>剩余可取数量上限。</summary>
    public int CountMax { get; protected set; }
    /// <summary>剩余点数和下限。</summary>
    public int PointMin { get; protected set; }
    /// <summary>剩余点数和上限。</summary>
    public int PointMax { get; protected set; }

    /// <summary>当前步可行 V 值下限（子类实现）。</summary>
    public abstract int Low { get; }
    /// <summary>当前步可行 V 值上限（子类实现）。</summary>
    public abstract int High { get; }

    protected BasePicker(IReadOnlyDictionary<T, int> items)
    {
        var groups = items.ToLookup(k => k.Value, k => k.Key);
        Ranges = new SortedList<int, ValueRange>(groups.Count);
        Items = new List<T>(items.Count);
        int start = 0;

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var count = Items.Count;
            Items.AddRange(group);
            count = Items.Count - count;
            Ranges[group.Key] = new ValueRange(start, start + count, count);
            start += count;
        }
    }

    /// <summary>设置约束并开始一次新抽取。</summary>
    public void SetConstraints(int cMin, int cMax, int pMin, int pMax)
    {
        CountMin = cMin;
        CountMax = cMax;
        PointMin = pMin;
        PointMax = pMax;
    }

    /// <summary>在 [Low, High] 中随机选一个 key（V 值）。</summary>
    protected int SelectKey()
    {
        int lo = Low, hi = High;
        var buffer = ArrayPool<int>.Shared.Rent(Ranges.Count);
        try
        {
            int count = Ranges.Count;
            Ranges.Keys.CopyTo(buffer, 0);
            var candidates = buffer.AsSpan(0, count);

            int start = candidates.BinarySearch(lo);
            if (start < 0)
            {
                start = ~start;
            }

            int end = candidates.BinarySearch(hi);
            if (end < 0)
            {
                end = ~end - 1;
            }

            return start > end
                ? throw new InvalidOperationException($"无可行候选 [Low={lo}, High={hi}]")
                : candidates[start + Random.Shared.Next(end - start + 1)];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(buffer);
        }
    }

    /// <summary>按当前约束随机抽取一项。内部自减约束值。</summary>
    public virtual T Pick()
    {
        int key = SelectKey();
        var range = Ranges[key];
        T picked = Items[range.GetRandomIndex()];
        CountMin--;
        CountMax--;
        PointMin -= key;
        PointMax -= key;
        return picked;
    }
}
