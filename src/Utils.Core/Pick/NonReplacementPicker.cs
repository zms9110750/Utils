#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>不可放回抽取。每抽一项即从池中移除。</summary>
public class NonReplacementPicker<T> : BasePicker<T>
{
	private List<T> _names;
	private SortedList<int, ValueRange> _ranges;

	/// <summary>构造不可放回抽取器。Key=名称，Value=V值。</summary>
	public NonReplacementPicker(IReadOnlyDictionary<T, int> items)
	{
		var sorted = items
			.Select(kvp => (kvp.Key, kvp.Value))
			.OrderBy(x => x.Value).ThenBy(x => x.Key)
			.ToList();

		_names = new List<T>(sorted.Count);
		_ranges = new SortedList<int, ValueRange>();
		int start = 0;
		int? lastVal = null;

		for (int i = 0; i < sorted.Count; i++)
		{
			var (name, val) = sorted[i];
			_names.Add(name);
			if (lastVal != null && val != lastVal)
			{
				_ranges[lastVal.Value] = new ValueRange(start, i, i - start);
				start = i;
			}
			lastVal = val;
		}
		if (sorted.Count > 0)
			_ranges[lastVal!.Value] = new ValueRange(start, sorted.Count, sorted.Count - start);
	}

	public override IReadOnlyList<T> Names => _names;
	public override IReadOnlyDictionary<int, ValueRange> ValueRanges => _ranges;

	public override int Low
	{
		get
		{
			int needed = CountMax - 1;
			if (needed <= 0) return PointMin;

			int count = 0, sum = 0;
			var keys = ValueRanges.Keys.ToList();
			for (int i = keys.Count - 1; i >= 0 && count < needed; i--)
			{
				int v = keys[i];
				int take = Math.Min(ValueRanges[v].Count, needed - count);
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
			if (needed <= 0) return PointMax;

			int count = 0, sum = 0;
			foreach (var kvp in ValueRanges)
			{
				if (count >= needed) break;
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
		var range = ValueRanges[key];
		int idx = Random.Shared.Next(range.Start, range.End);
		T picked = Names[idx];

		_names.RemoveAt(idx);
		var newRanges = new SortedList<int, ValueRange>(_ranges.Count);
		foreach (var kvp in _ranges)
		{
			var shifted = kvp.Value.ShiftLeft(idx);
			if (shifted.Count > 0)
				newRanges.Add(kvp.Key, shifted);
		}
		_ranges = newRanges;

		CountMin--;
		CountMax--;
		PointMin -= key;
		PointMax -= key;
		return picked;
	}
}
#endif
