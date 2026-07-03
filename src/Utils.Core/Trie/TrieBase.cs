#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
namespace zms9110750.Utils.Core.Trie;

/// <summary>
/// 字典树的节点抽象
/// </summary>
public abstract class TrieBase : INode<TrieBase>
{
    /// <inheritdoc/>
    public TrieBase? Parent { get; }
    /// <inheritdoc cref="INode{TrieBase}.Root"/>
    public Trie Root { get; }
    /// <inheritdoc/>
    public int Depth { get; }
    TrieBase INode<TrieBase>.Root => Root;

#if NET6_0_OR_GREATER
    /// <summary>
    /// 获取指定字符对应的子节点，如果不存在则创建
    /// </summary>
    protected TrieNode this[char c]
    {
        get
        {
            ref var childNode = ref CollectionsMarshal.GetValueRefOrAddDefault(Children, c, out var b);
            return childNode ??= new TrieNode(this);
        }
    }
#else
    /// <summary>
    /// 获取指定字符对应的子节点，如果不存在则创建
    /// </summary>
    protected TrieNode this[char c]
    {
        get
        {
            if (!Children.TryGetValue(c, out var childNode))
            {
                childNode = new TrieNode(this);
                Children[c] = childNode;
            }
            return childNode;
        }
    }
#endif
    /// <summary>
    /// 子节点集合
    /// </summary>
    protected Dictionary<char, TrieNode> Children { get; } = new();

    /// <summary>
    /// 分隔符集合
    /// </summary>
    public abstract ImmutableHashSet<char> Separator { get; }

    /// <summary>
    /// 传入一个父节点。初始化自身的深度、父节点、根节点
    /// </summary>
    /// <param name="parent"></param>
    protected TrieBase(TrieBase parent)
    {
        Depth = parent.Depth + 1;
        Parent = parent;
        Root = parent.Root;
    }

    /// <summary>
    /// 构造一个根节点。只允许<see cref="Trie"/>的派生调用
    /// </summary>
    private protected TrieBase()
    {
        Root = (Trie)this;
    }

    /// <summary>
    /// 添加单词到字典树中
    /// </summary>
    /// <param name="word">单词</param>
    public abstract bool Add(string word);
}
