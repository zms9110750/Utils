using System.Collections.Concurrent;

namespace zms9110750.Utils.Core.Trie;

/// <summary>
/// 字典树根节点
/// </summary>
/// <param name="separator">分隔符集合</param>
public class Trie(HashSet<char>? separator = null) : TrieBase()
{
	/// <inheritdoc/>
	public override ImmutableHashSet<char> Separator { get; } =
		separator?.ToImmutableHashSet() ?? ImmutableHashSet<char>.Empty;

	/// <inheritdoc/>
	public override bool Add(string word)
	{
#if NET8_0_OR_GREATER
		ArgumentException.ThrowIfNullOrEmpty(word);
#else
		if (string.IsNullOrEmpty(word))
		{
			throw new ArgumentException("Value cannot be null or empty.", nameof(word));
		}
#endif
		return base[word[0]].Add(word);
	}

	/// <summary>
	/// 搜索字典树中是否存在指定前缀的单词
	/// </summary>
	/// <param name="prefix">前缀</param>
	/// <remarks>分隔符同时是字符也是分隔符。只要有一种解释方法可以匹配，那就匹配。<br/>
	/// <code>
	/// a b匹配:
	/// ac b
	/// ac cb b
	/// 不匹配
	/// ab
	/// ac cb
	/// </code>
	/// </remarks>
	public IEnumerable<string> Search(string prefix)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			yield break;
		}

		if (!_locks.TryTake(out var lockSet))
		{
			lockSet = [];
		}
		foreach (var child in Children.Values)
		{
			foreach (var result in child.Search(prefix, 0, lockSet))
			{
				yield return result;
			}
		}
		lockSet.Clear();
		_locks.Add(lockSet);
	}
	static readonly ConcurrentBag<HashSet<(TrieNode, int)>> _locks = [];
}
