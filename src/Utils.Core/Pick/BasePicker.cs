#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>区间约束随机抽取基类。外部循环：SetConstraints + 反复 Pick()。</summary>
public abstract class BasePicker<T>
{
	/// <summary>所有项的名称列表（按 V 值升序）。</summary>
	public abstract IReadOnlyList<T> Names { get; }
	/// <summary>V 值到索引范围的映射。Key=V，Value=在 Names 中的范围。</summary>
	public abstract IReadOnlyDictionary<int, ValueRange> ValueRanges { get; }

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

	protected int MinVal => ValueRanges.Keys.Min();
	protected int MaxVal => ValueRanges.Keys.Max();

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
		var candidates = new List<int>();
		foreach (var kvp in ValueRanges)
			if (kvp.Key >= lo && kvp.Key <= hi)
				candidates.Add(kvp.Key);

		if (candidates.Count == 0)
			throw new InvalidOperationException($"无可行候选 [Low={lo}, High={hi}]");

		return candidates[Random.Shared.Next(candidates.Count)];
	}

	/// <summary>按当前约束随机抽取一项。内部自减约束值。</summary>
	public virtual T Pick()
	{
		int key = SelectKey();
		var range = ValueRanges[key];
		int idx = Random.Shared.Next(range.Start, range.End);
		T picked = Names[idx];
		CountMin--;
		CountMax--;
		PointMin -= key;
		PointMax -= key;
		return picked;
	}
}
#endif
