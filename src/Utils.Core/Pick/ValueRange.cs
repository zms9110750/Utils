#if NET6_0_OR_GREATER
namespace zms9110750.Utils.Core.Pick;

/// <summary>值在排序列表中的索引范围。</summary>
public readonly record struct ValueRange(int Start, int End, int Count)
{
    /// <summary>移除索引后偏移。Start>removedIdx 整体左移，End>removedIdx 减 Count。</summary>
    public ValueRange ShiftLeft(int removedIdx)
    {
        if (Start > removedIdx)
        {
            return new(Start - 1, End - 1, Count);
        }

        if (End > removedIdx)
        {
            return new(Start, End - 1, Count - 1);
        }

        return this;
    }

    /// <summary>在 [Start, End) 中随机取一个索引。</summary>
    public int GetRandomIndex()
    {
        return Random.Shared.Next(Start, End);
    }
}
#endif
