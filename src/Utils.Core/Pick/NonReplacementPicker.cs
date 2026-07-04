#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>不可放回抽取。每抽一项即从池中移除。</summary>
public class NonReplacementPicker<T>(IReadOnlyDictionary<T, int> items) : BasePicker<T>(items)
{
    public override int Low
    {
        get
        {
            int needed = CountMax - 1;
            if (needed <= 0)
            {
                return PointMin;
            }

            int count = 0, sum = 0;
            var keys = Ranges.Keys.ToList();
            for (int i = keys.Count - 1; i >= 0 && count < needed; i--)
            {
                int v = keys[i];
                int take = Math.Min(Ranges[v].Count, needed - count);
                count += take;
                sum += v * take;
            }
            return PointMin - sum;
        }
    }

    public override int High
    {
        get
        {
            int needed = CountMin - 1;
            if (needed <= 0)
            {
                return PointMax;
            }

            int count = 0, sum = 0;
            foreach (var kvp in Ranges)
            {
                if (count >= needed)
                {
                    break;
                }

                int v = kvp.Key;
                int take = Math.Min(kvp.Value.Count, needed - count);
                count += take;
                sum += v * take;
            }
            return PointMax - sum;
        }
    }

    /// <summary>抽取一项并从池中移除。内部自减约束值。</summary>
    public override T Pick()
    {
        int key = SelectKey();
        var range = Ranges[key];
        int idx = range.GetRandomIndex();
        T picked = Items[idx];

        Items.RemoveAt(idx);
        var newRanges = new SortedList<int, ValueRange>(Ranges.Count);
        foreach (var kvp in Ranges)
        {
            var shifted = kvp.Value.ShiftLeft(idx);
            if (shifted.Count > 0)
            {
                newRanges.Add(kvp.Key, shifted);
            }
        }
        Ranges.Clear();
        foreach (var kvp in newRanges)
        {
            Ranges.Add(kvp.Key, kvp.Value);
        }

        CountMin--;
        CountMax--;
        PointMin -= key;
        PointMax -= key;
        return picked;
    }
}
#endif
