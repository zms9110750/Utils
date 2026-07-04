#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>可放回抽取。池不变，每次从全集中随机选。</summary>
public class ReplacementPicker<T> : BasePicker<T>
{
    public ReplacementPicker(IReadOnlyDictionary<T, int> items) : base(items)
    {
    }

    public override int Low => PointMin - (CountMax - 1) *
#if NET7_0_OR_GREATER
        Ranges.GetKeyAtIndex(Ranges.Count - 1);
#else
         Ranges.Keys.Max();
#endif
    public override int High => PointMax - (CountMin - 1) *
#if NET7_0_OR_GREATER
        Ranges.GetKeyAtIndex(0);
#else
         Ranges.Keys.Min();
#endif

}
#endif
