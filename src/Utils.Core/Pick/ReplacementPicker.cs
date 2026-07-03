#if NET6_0_OR_GREATER
using System.Collections.Immutable;

namespace zms9110750.Utils.Core.Pick;

/// <summary>可放回抽取。池不变，每次从全集中随机选。</summary>
public class ReplacementPicker<T> : BasePicker<T>
{
    private readonly ImmutableList<T> _names;
    private readonly SortedList<int, ValueRange> _ranges;

    /// <summary>构造可放回抽取器。Key=名称，Value=V值。</summary>
    public ReplacementPicker(IReadOnlyDictionary<T, int> items)
    {
        var (names, ranges) = BuildStructure(items);
        _names = names.ToImmutableList();
        _ranges = ranges;
    }

    public override IReadOnlyList<T> Names => _names;
    public override IReadOnlyDictionary<int, ValueRange> ValueRanges => _ranges;

    public override int Low => PointMin - (CountMax - 1) * MaxVal;
    public override int High => PointMax - (CountMin - 1) * MinVal;
}
#endif
