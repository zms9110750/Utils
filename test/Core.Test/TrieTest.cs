using zms9110750.Utils.Core;

namespace Core.Test;

/// <summary>
/// 验证 Trie 添加单词功能
/// </summary>
public sealed class TrieAddTest
{
    #region 添加

    /// <summary>首次添加返回 true</summary>
    [Fact]
    public void Add_NewWord_ReturnsTrue()
    {
        Assert.True(new Trie().Add("apple"));
    }

    /// <summary>重复添加返回 false</summary>
    [Fact]
    public void Add_Duplicate_ReturnsFalse()
    {
        var t = new Trie();
        t.Add("apple");
        Assert.False(t.Add("apple"));
    }

    /// <summary>添加空单词抛出 ArgumentException</summary>
    [Fact]
    public void Add_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Trie().Add(""));
    }

    /// <summary>添加 null 抛出 ArgumentException 或 ArgumentNullException</summary>
    [Fact]
    public void Add_Null_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Trie().Add(null!));
    }

    /// <summary>添加已有词的前缀返回 true（新增词条）</summary>
    [Fact]
    public void Add_PrefixOfExistingWord_ReturnsTrue()
    {
        var t = new Trie();
        t.Add("apple");
        // "app" 是 "apple" 的前缀，但尚未作为独立词条添加，应返回 true
        Assert.True(t.Add("app"));
    }

    #endregion
}

/// <summary>
/// 验证 Trie 前缀搜索功能
/// </summary>
public sealed class TrieSearchTest
{
    #region 搜索

    /// <summary>搜索匹配的单词返回对应结果</summary>
    [Fact]
    public void Search_MatchingPrefix()
    {
        var t = new Trie();
        t.Add("apple");
        t.Add("application");
        t.Add("banana");
        var r = t.Search("app").ToList();
        Assert.Contains("apple", r);
        Assert.Contains("application", r);
    }

    /// <summary>搜索不存在的单词返回空</summary>
    [Fact]
    public void Search_NonExistent_Empty()
    {
        var t = new Trie();
        t.Add("apple");
        Assert.Empty(t.Search("xyz"));
    }

    /// <summary>空前缀返回空</summary>
    [Fact]
    public void Search_EmptyPrefix_Empty()
    {
        var t = new Trie();
        t.Add("apple");
        Assert.Empty(t.Search(""));
    }

    /// <summary>精确匹配搜索返回包含该词的结果</summary>
    [Fact]
    public void Search_ExactMatch_ReturnsWord()
    {
        var t = new Trie();
        t.Add("apple");
        var r = t.Search("apple").ToList();
        Assert.Equal(["apple"], r);
    }

    /// <summary>前缀词能搜到自身及其后裔（先长后短）</summary>
    [Fact]
    public void Search_PrefixOfExisting_IncludesBoth()
    {
        var t = new Trie();
        t.Add("apple");
        t.Add("app");
        var r = t.Search("app").ToList();
        Assert.Contains("app", r);
        Assert.Contains("apple", r);
        // 不应包含意外结果
        Assert.Equal(2, r.Count);
    }

    /// <summary>先短后长，前缀搜索依然返回全部</summary>
    [Fact]
    public void Search_ShorterWordFirst_StillMatchesPrefix()
    {
        var t = new Trie();
        t.Add("app");
        t.Add("apple");
        var r = t.Search("ap").ToList();
        Assert.Contains("app", r);
        Assert.Contains("apple", r);
        Assert.Equal(2, r.Count);
    }

    /// <summary>非单词的前缀节点不应出现在搜索结果中</summary>
    [Fact]
    public void Search_PrefixNotAdded_ExcludesIntermediate()
    {
        var t = new Trie();
        t.Add("apple");
        // "app" 从未被添加为独立词条，不应出现在搜索结果中
        var r = t.Search("app").ToList();
        Assert.DoesNotContain("app", r);
        Assert.Contains("apple", r);
    }

    /// <summary>搜索结果不重复</summary>
    [Fact]
    public void Search_NoDuplicates()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("abc");
        t.Add("ab-c");
        t.Add("a-bc");
        var r = t.Search("abc").ToList();
        Assert.Equal(r.Count, r.Distinct().Count());
    }

    #endregion

    #region 分隔符

    /// <summary>分隔符同时作为字符和分隔符处理</summary>
    [Fact]
    public void Search_SeparatorAffectsResults()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("a-b");
        t.Add("ab");
        var r = t.Search("ab").ToList();
        Assert.Contains("ab", r);
    }

    /// <summary>前缀含分隔符时两种解释都尝试匹配</summary>
    [Fact]
    public void Search_PrefixWithSeparator_MatchesBoth()
    {
        var t = new Trie(new HashSet<char> { ' ' });
        t.Add("a b");
        t.Add("ab");
        var r = t.Search("a b").ToList();
        Assert.Contains("a b", r);
    }

    /// <summary>搜索前缀中的分隔符匹配消耗更多字符的场景</summary>
    [Fact]
    public void Search_SeparatorPath_ExtraChars()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("ac-b");
        t.Add("acb");
        var r = t.Search("a-b").ToList();
        // a-b 可以通过 a→(跳过 c)→b 匹配 ac-b，或 a→-→b 匹配 ac-b
        Assert.Contains("ac-b", r);
    }

    /// <summary>以分隔符开头的单词能被搜索</summary>
    [Fact]
    public void Search_SeparatorAtStart()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("-abc");
        var r = t.Search("-abc").ToList();
        Assert.Contains("-abc", r);
    }

    /// <summary>以分隔符结尾的单词能被搜索</summary>
    [Fact]
    public void Search_SeparatorAtEnd()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("abc-");
        var r = t.Search("abc-").ToList();
        Assert.Contains("abc-", r);
    }

    /// <summary>连续分隔符被正确处理</summary>
    [Fact]
    public void Search_ConsecutiveSeparators()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("a--b");
        var r = t.Search("a--b").ToList();
        Assert.Contains("a--b", r);
    }

    /// <summary>分隔符在单词中间且前缀搜索正常工作</summary>
    [Fact]
    public void Search_SeparatorInMiddle_PrefixMatch()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("a-bc");
        t.Add("a-bcd");
        var r = t.Search("a-b").ToList();
        Assert.Contains("a-bc", r);
        Assert.Contains("a-bcd", r);
    }

    #endregion

    #region 无分隔符

    /// <summary>无分隔符时搜索按字符精确匹配</summary>
    [Fact]
    public void Search_NoSeparator_ExactCharacterMatch()
    {
        var t = new Trie(); // 无分隔符
        t.Add("abc");
        t.Add("a-c");
        var r = t.Search("abc").ToList();
        Assert.Contains("abc", r);
        Assert.DoesNotContain("a-c", r); // '-' 不是分隔符，不会跳过
    }

    /// <summary>无分隔符时包含分隔符字符的单词也能正常搜索</summary>
    [Fact]
    public void Search_NoSeparator_WithSpecialChars()
    {
        var t = new Trie(); // 无分隔符
        t.Add("a-b");
        var r = t.Search("a-b").ToList();
        Assert.Contains("a-b", r);
    }

    /// <summary>无分隔符时不会通过跳过字符来匹配</summary>
    [Fact]
    public void Search_NoSeparator_NoSkipBehavior()
    {
        var t = new Trie(); // 无分隔符
        t.Add("acb");
        // 没有分隔符，"ab" 不应该匹配 "acb"
        Assert.Empty(t.Search("ab"));
    }

    #endregion
}

/// <summary>
/// 验证 Trie 节点层级结构
/// </summary>
public sealed class TrieStructureTest
{
    #region 根节点

    /// <summary>根节点 Parent 为 null</summary>
    [Fact]
    public void Root_ParentIsNull()
    {
        Assert.Null(new Trie().Parent);
    }

    /// <summary>根节点 Root 指向自己</summary>
    [Fact]
    public void Root_RootIsSelf()
    {
        var t = new Trie();
        Assert.Same(t, t.Root);
    }

    /// <summary>根节点 Depth 为 0</summary>
    [Fact]
    public void Root_DepthIsZero()
    {
        Assert.Equal(0, new Trie().Depth);
    }

    /// <summary>根节点 Separator 默认为空集合</summary>
    [Fact]
    public void Root_Separator_DefaultEmpty()
    {
        Assert.Empty(new Trie().Separator);
    }

    /// <summary>根节点 Separator 继承自构造函数参数</summary>
    [Fact]
    public void Root_Separator_FromConstructor()
    {
        var sep = new HashSet<char> { '-', '/' };
        var t = new Trie(sep);
        Assert.Equal(2, t.Separator.Count);
        Assert.Contains('-', t.Separator);
        Assert.Contains('/', t.Separator);
    }

    #endregion

    #region 间接结构验证

    /// <summary>通过搜索行为验证子节点路径深度正确</summary>
    [Fact]
    public void Child_Depth_IndirectViaSearch()
    {
        var t = new Trie();
        t.Add("a");
        t.Add("ab");
        t.Add("abc");
        // "a" 是叶子节点也是中间节点，搜索 "a" 应返回所有三个词
        var r = t.Search("a").ToList();
        Assert.Contains("a", r);
        Assert.Contains("ab", r);
        Assert.Contains("abc", r);
        Assert.Equal(3, r.Count);
    }

    /// <summary>通过搜索行为验证子节点 Parent 关系正确（间接）</summary>
    [Fact]
    public void Child_Parent_IndirectViaSearch()
    {
        var t = new Trie();
        t.Add("abc");
        // 如果子节点的 Parent 链接错误，Search 将无法正确导航
        var r = t.Search("abc").ToList();
        Assert.Contains("abc", r);
        // 搜索中间前缀也能找到
        Assert.Contains("abc", t.Search("ab").ToList());
        Assert.Contains("abc", t.Search("a").ToList());
    }

    /// <summary>通过搜索行为验证子节点 Root 引用正确（间接）</summary>
    [Fact]
    public void Child_Root_IndirectViaSeparator()
    {
        var t = new Trie(new HashSet<char> { '-' });
        t.Add("ac-b");
        // 搜索 "a-b"：'-' 是分隔符，分隔符跳过机制允许匹配 "ac-b"
        // 如果子节点的 Root 引用错误导致 Separator 为空集合，则分隔符跳过不会触发
        var r = t.Search("a-b").ToList();
        Assert.Contains("ac-b", r);
    }

    /// <summary>通过搜索行为验证子节点 Separator 继承正确（间接）</summary>
    [Fact]
    public void Child_Separator_IndirectViaSearch()
    {
        var t = new Trie(new HashSet<char> { ' ', '.' });
        t.Add("ac b");
        t.Add("ac.b");
        // 搜索 "a b" 时 ' ' 是分隔符，分隔符跳过机制应该匹配 "ac b"
        Assert.Contains("ac b", t.Search("a b").ToList());
        // 搜索 "a.b" 时 '.' 是分隔符，分隔符跳过机制应该匹配 "ac.b"
        Assert.Contains("ac.b", t.Search("a.b").ToList());
    }

    #endregion
}
