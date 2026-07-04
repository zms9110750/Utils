using zms9110750.Utils.Core;

namespace Core.Test;

class TestObserver : IObserver<long>
{
    public long Value { get; private set; }
    public void OnNext(long value)
    {
        Value = value;
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }
}

/// <summary>
/// 验证 ProgressStream 构造器参数验证
/// </summary>
public sealed class ProgressStreamCtorTest
{
    /// <summary>传入 null 的 innerStream 抛出 ArgumentNullException</summary>
    [Fact]
    public void Ctor_NullInner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProgressStream(null!));
    }
}

/// <summary>
/// 验证 ProgressStream 读操作和进度报告
/// </summary>
public sealed class ProgressStreamReadTest
{
    #region 读

    /// <summary>Read 返回内层 Stream 的字节数</summary>
    [Fact]
    public void Read_ReturnsBytes()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        var buf = new byte[1024];
        Assert.Equal(5, stream.Read(buf, 0, buf.Length));
    }

    /// <summary>ReadAsync 返回内层 Stream 的字节数</summary>
    [Fact]
    public void ReadAsync_ReturnsBytes()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        var buf = new byte[1024];
        Assert.Equal(5, stream.ReadAsync(buf, 0, buf.Length).Result);
    }

    #endregion

    #region 进度报告

    /// <summary>Read 触发 readObserver 报告累计字节数</summary>
    [Fact]
    public void Read_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("Hello World"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        stream.Read(new byte[1024], 0, 1024);
        Assert.Equal(11, obs.Value);
    }

    #endregion
}

/// <summary>
/// 验证 ProgressStream 写操作和进度报告
/// </summary>
public sealed class ProgressStreamWriteTest
{
    #region 写

    /// <summary>Write 数据写入内层 Stream</summary>
    [Fact]
    public void Write_ToInnerStream()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.Write("Hi"u8);
        Assert.Equal(2, inner.Length);
    }

    /// <summary>WriteAsync 数据写入内层 Stream</summary>
    [Fact]
    public void WriteAsync_ToInnerStream()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.WriteAsync("Hi"u8.ToArray(), 0, 2).Wait();
        Assert.Equal(2, inner.Length);
    }

    #endregion

    #region 进度报告

    /// <summary>Write 触发 writeObserver 报告累计字节数</summary>
    [Fact]
    public void Write_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);
        stream.Write("Hi"u8);
        Assert.Equal(2, obs.Value);
    }

    #endregion
}

/// <summary>
/// 验证 ProgressStream 属性透传
/// </summary>
public sealed class ProgressStreamPropertyTest
{
    /// <summary>CanRead / CanSeek / CanWrite 透传内层</summary>
    [Fact]
    public void Capabilities_PassThrough()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.True(stream.CanRead && stream.CanSeek && stream.CanWrite);
    }

    /// <summary>Length / Position / Seek 透传内层</summary>
    [Fact]
    public void Seek_PassThrough()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        Assert.Equal(2, stream.Seek(2, SeekOrigin.Begin));
        Assert.Equal(2, stream.Position);
    }

    /// <summary>SetLength 改变内层长度</summary>
    [Fact]
    public void SetLength_PassThrough()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.SetLength(100);
        Assert.Equal(100, stream.Length);
    }
}

/// <summary>
/// 验证 ProgressStream 的释放等生命周期行为
/// </summary>
public sealed class ProgressStreamLifecycleTest
{
    #region Dispose

    /// <summary>Dispose 后内层 Stream 不可读</summary>
    [Fact]
    public void Dispose_InnerStreamClosed()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    /// <summary>DisposeAsync 异步释放内层 Stream</summary>
    [Fact]
    public async Task DisposeAsync_InnerStreamClosed()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    #endregion

    #region Close

    /// <summary>Close 释放内层 Stream</summary>
    [Fact]
    public void Close_InnerStreamClosed()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Close();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    #endregion
}
