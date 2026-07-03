#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>可放回抽取。池不变，每次从全集中随机选。</summary>
public class ReplacementPicker<T> : BasePicker<T>
{
    public ReplacementPicker(IReadOnlyDictionary<T, int> items) : base(items)
    {
    }

    public override int Low => PointMin - (CountMax - 1) * MaxVal;
    public override int High => PointMax - (CountMin - 1) * MinVal;
}
#endif
