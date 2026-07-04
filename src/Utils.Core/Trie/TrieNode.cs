namespace zms9110750.Utils.Core;

/// <summary>
/// 负责储存字符的字典树子节点
/// </summary>
/// <param name="parent">父节点</param>
public class TrieNode(TrieBase parent) : TrieBase(parent)
{
    /// <summary>
    /// 储存的字符。若字典树尚未添加此节点应该存在的文本。则为null。
    /// </summary>
    public string? Word { get; private set; }

    /// <inheritdoc/>
    public override ImmutableHashSet<char> Separator => Root.Separator;

    /// <inheritdoc/>
    public override bool Add(string word)
    {
        if (Depth > word.Length)
        {
            bool b = Word is null;
            Word = word;
            return b;
        }
        return base[word[Depth - 1]].Add(word);
    }

    internal IEnumerable<string> Search(string s, int index, HashSet<(TrieNode, int)> set)
    {
        if (!set.Add((this, index)))
        {
            yield break;
        }
        if (index >= s.Length)
        {
            if (Word != null)
            {
                yield return Word;
            }

            foreach (var child in Children.Values)
            {
                foreach (var result in child.Search(s, index, set))
                {
                    yield return result;
                }
            }
            yield break;
        }
        var currentChar = s[index];
        if (Children.TryGetValue(currentChar, out var childNode))
        {
            foreach (var match in childNode.Search(s, index + 1, set))
            {
                yield return match;
            }
        }
        if (Separator.Contains(currentChar))
        {
            foreach (var item in Children.Values)
            {
                foreach (var match in item.Search(s, index, set))
                {
                    yield return match;
                }
            }
            foreach (var item in Separator)
            {
                if (Children.TryGetValue(item, out var childNode2))
                {
                    foreach (var match in childNode2.Search(s, index + 1, set))
                    {
                        yield return match;
                    }
                }
            }
        }
    }
}
