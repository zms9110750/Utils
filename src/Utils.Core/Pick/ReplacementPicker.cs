#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>可放回抽取。池不变，每次从全集中随机选。</summary>
public class ReplacementPicker<T>(IReadOnlyDictionary<T, int> items) : BasePicker<T>(items)
{
#if NET7_0_OR_GREATER
    int MinVal => Ranges.GetKeyAtIndex(0);
    int MaxVal => Ranges.GetKeyAtIndex(Ranges.Count - 1);
#else
    int MinVal => Ranges.Keys.Min();
    int MaxVal => Ranges.Keys.Max();
#endif

    public override int Low => PointMin - (CountMax - 1) * MaxVal;
    public override int High => PointMax - (CountMin - 1) * MinVal;

}
#endif
