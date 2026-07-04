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

    #endregion
}

/// <summary>
/// 验证 Trie 节点层级结构
/// </summary>
public sealed class TrieStructureTest
{
    #region 节点

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

    #endregion
}
