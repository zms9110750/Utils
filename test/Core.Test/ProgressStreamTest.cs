using zms9110750.Utils.Core;

namespace Core.Test;

class TestObserver : IObserver<long>
{
    public long Value { get; private set; }
    public int CallCount { get; private set; }

    public void OnNext(long value)
    {
        Value = value;
        CallCount++;
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

    /// <summary>可以只传 innerStream 不传 observer</summary>
    [Fact]
    public void Ctor_WithoutObservers_DoesNotThrow()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.NotNull(stream);
    }

    /// <summary>可以同时传入 readObserver 和 writeObserver</summary>
    [Fact]
    public void Ctor_WithBothObservers_DoesNotThrow()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, new TestObserver(), new TestObserver());
        Assert.NotNull(stream);
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
    public async Task ReadAsync_ReturnsBytes()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        var buf = new byte[1024];
        Assert.Equal(5, await stream.ReadAsync(buf, 0, buf.Length));
    }

    #endregion

    #region Span/Memory 重载

    /// <summary>Read(Span) 返回内层 Stream 的字节数</summary>
    [Fact]
    public void Read_Span_ReturnsBytes()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        var buf = new byte[1024];
        Assert.Equal(5, stream.Read(buf.AsSpan()));
    }

    /// <summary>ReadAsync(Memory) 返回内层 Stream 的字节数</summary>
    [Fact]
    public async Task ReadAsync_Memory_ReturnsBytes()
    {
        using var inner = new MemoryStream("Hello"u8.ToArray());
        using var stream = new ProgressStream(inner);
        var buf = new byte[1024];
        Assert.Equal(5, await stream.ReadAsync(buf.AsMemory()));
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

    /// <summary>ReadAsync 触发 readObserver 报告累计字节数</summary>
    [Fact]
    public async Task ReadAsync_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("Hello World"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        await stream.ReadAsync(new byte[1024], 0, 1024);
        Assert.Equal(11, obs.Value);
    }

    /// <summary>Read(Span) 触发 readObserver 报告累计字节数</summary>
    [Fact]
    public void Read_Span_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("Hello World"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        stream.Read(new byte[1024].AsSpan());
        Assert.Equal(11, obs.Value);
    }

    /// <summary>ReadAsync(Memory) 触发 readObserver 报告累计字节数</summary>
    [Fact]
    public async Task ReadAsync_Memory_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("Hello World"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        await stream.ReadAsync(new byte[1024].AsMemory());
        Assert.Equal(11, obs.Value);
    }

    #endregion

    #region 累计值

    /// <summary>连续多次 Read 后 TotalBytesRead 累计正确</summary>
    [Fact]
    public void MultipleReads_TotalBytesReadCorrect()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("ABCDEFGHIJ"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        var buf = new byte[3];

        // 三次读取，分别读 3、3、3 字节（最后剩 1 字节不读）
        Assert.Equal(3, stream.Read(buf, 0, 3));
        Assert.Equal(3, stream.Read(buf, 0, 3));
        Assert.Equal(3, stream.Read(buf, 0, 3));

        Assert.Equal(9, stream.TotalBytesRead);
        // Observer 最后报告的值也应该是 9
        Assert.Equal(9, obs.Value);
    }

    /// <summary>连续多次 ReadAsync 后 TotalBytesRead 累计正确</summary>
    [Fact]
    public async Task MultipleReadAsync_TotalBytesReadCorrect()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream("ABCDEFGHIJ"u8.ToArray());
        using var stream = new ProgressStream(inner, obs);
        var buf = new byte[3];

        Assert.Equal(3, await stream.ReadAsync(buf, 0, 3));
        Assert.Equal(3, await stream.ReadAsync(buf, 0, 3));
        Assert.Equal(3, await stream.ReadAsync(buf, 0, 3));

        Assert.Equal(9, stream.TotalBytesRead);
        Assert.Equal(9, obs.Value);
    }

    #endregion

    #region 零字节边界

    /// <summary>空流 Read 返回 0 且不触发 OnNext</summary>
    [Fact]
    public void Read_EmptyStream_NoProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream(Array.Empty<byte>());
        using var stream = new ProgressStream(inner, obs);
        var buf = new byte[1024];

        Assert.Equal(0, stream.Read(buf, 0, buf.Length));
        Assert.Equal(0, stream.TotalBytesRead);
        Assert.Equal(0, obs.Value);
        Assert.Equal(0, obs.CallCount);
    }

    /// <summary>空流 ReadAsync 返回 0 且不触发 OnNext</summary>
    [Fact]
    public async Task ReadAsync_EmptyStream_NoProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream(Array.Empty<byte>());
        using var stream = new ProgressStream(inner, obs);
        var buf = new byte[1024];

        Assert.Equal(0, await stream.ReadAsync(buf, 0, buf.Length));
        Assert.Equal(0, stream.TotalBytesRead);
        Assert.Equal(0, obs.Value);
        Assert.Equal(0, obs.CallCount);
    }

    /// <summary>空流 Read(Span) 返回 0 且不触发 OnNext</summary>
    [Fact]
    public void Read_Span_EmptyStream_NoProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream(Array.Empty<byte>());
        using var stream = new ProgressStream(inner, obs);
        var buf = new byte[1024];

        Assert.Equal(0, stream.Read(buf.AsSpan()));
        Assert.Equal(0, stream.TotalBytesRead);
        Assert.Equal(0, obs.Value);
        Assert.Equal(0, obs.CallCount);
    }

    #endregion

    #region CopyToAsync

    /// <summary>CopyToAsync 复制所有数据并报告进度</summary>
    [Fact]
    public async Task CopyToAsync_CopiesAllData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var obs = new TestObserver();
        using var inner = new MemoryStream(data);
        using var dst = new MemoryStream();
        using var stream = new ProgressStream(inner, obs);

        await stream.CopyToAsync(dst, 4096, CancellationToken.None);

        Assert.Equal(data.Length, dst.Length);
        Assert.Equal(data, dst.ToArray());
        Assert.Equal(data.Length, obs.Value);
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
    public async Task WriteAsync_ToInnerStream()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        await stream.WriteAsync("Hi"u8.ToArray(), 0, 2);
        Assert.Equal(2, inner.Length);
    }

    #endregion

    #region Span/Memory 重载

    /// <summary>Write(Span) 数据写入内层 Stream</summary>
    [Fact]
    public void Write_Span_WritesToInner()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.Write("Hi"u8);
        Assert.Equal(2, inner.Length);
    }

    /// <summary>WriteAsync(ReadOnlyMemory) 数据写入内层 Stream</summary>
    [Fact]
    public async Task WriteAsync_ReadOnlyMemory_WritesToInner()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        await stream.WriteAsync("Hi"u8.ToArray().AsMemory());
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

    /// <summary>WriteAsync 触发 writeObserver 报告累计字节数</summary>
    [Fact]
    public async Task WriteAsync_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);
        await stream.WriteAsync("Hi"u8.ToArray(), 0, 2);
        Assert.Equal(2, obs.Value);
    }

    /// <summary>Write(Span) 触发 writeObserver 报告累计字节数</summary>
    [Fact]
    public void Write_Span_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);
        stream.Write("Hello"u8);
        Assert.Equal(5, obs.Value);
    }

    /// <summary>WriteAsync(ReadOnlyMemory) 触发 writeObserver 报告累计字节数</summary>
    [Fact]
    public async Task WriteAsync_ReadOnlyMemory_ReportsProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);
        await stream.WriteAsync("Hello"u8.ToArray().AsMemory());
        Assert.Equal(5, obs.Value);
    }

    #endregion

    #region 累计值

    /// <summary>连续多次 Write 后 TotalBytesWritten 累计正确</summary>
    [Fact]
    public void MultipleWrites_TotalBytesWrittenCorrect()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        stream.Write("AB"u8);
        stream.Write("CDE"u8);
        stream.Write("F"u8);

        Assert.Equal(6, stream.TotalBytesWritten);
        Assert.Equal(6, obs.Value);
    }

    /// <summary>连续多次 WriteAsync 后 TotalBytesWritten 累计正确</summary>
    [Fact]
    public async Task MultipleWriteAsync_TotalBytesWrittenCorrect()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        await stream.WriteAsync("AB"u8.ToArray(), 0, 2);
        await stream.WriteAsync("CDE"u8.ToArray(), 0, 3);
        await stream.WriteAsync("F"u8.ToArray(), 0, 1);

        Assert.Equal(6, stream.TotalBytesWritten);
        Assert.Equal(6, obs.Value);
    }

    #endregion

    #region 零字节边界

    /// <summary>写入 0 字节不触发 OnNext</summary>
    [Fact]
    public void Write_ZeroBytes_NoProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        stream.Write(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0, stream.TotalBytesWritten);
        Assert.Equal(0, obs.Value);
        Assert.Equal(0, obs.CallCount);
    }

    /// <summary>异步写入 0 字节不触发 OnNext</summary>
    [Fact]
    public async Task WriteAsync_ZeroBytes_NoProgress()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        await stream.WriteAsync(ReadOnlyMemory<byte>.Empty);
        Assert.Equal(0, stream.TotalBytesWritten);
        Assert.Equal(0, obs.Value);
        Assert.Equal(0, obs.CallCount);
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

    /// <summary>TotalBytesRead 初始为 0</summary>
    [Fact]
    public void TotalBytesRead_InitiallyZero()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.Equal(0, stream.TotalBytesRead);
    }

    /// <summary>TotalBytesWritten 初始为 0</summary>
    [Fact]
    public void TotalBytesWritten_InitiallyZero()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.Equal(0, stream.TotalBytesWritten);
    }

    /// <summary>TotalBytesRead setter 更新值并通知观察者</summary>
    [Fact]
    public void TotalBytesRead_Setter_NotifiesObserver()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, obs);

        stream.TotalBytesRead = 42;
        Assert.Equal(42, stream.TotalBytesRead);
        Assert.Equal(42, obs.Value);
    }

    /// <summary>TotalBytesWritten setter 更新值并通知观察者</summary>
    [Fact]
    public void TotalBytesWritten_Setter_NotifiesObserver()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        stream.TotalBytesWritten = 100;
        Assert.Equal(100, stream.TotalBytesWritten);
        Assert.Equal(100, obs.Value);
    }

    /// <summary>TotalBytesRead setter 值不变时不通知观察者</summary>
    [Fact]
    public void TotalBytesRead_Setter_SameValue_NoNotification()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, obs);

        stream.TotalBytesRead = 10;
        Assert.Equal(1, obs.CallCount);

        stream.TotalBytesRead = 10; // 相同值，不应再次通知
        Assert.Equal(1, obs.CallCount);
    }

    /// <summary>TotalBytesWritten setter 值不变时不通知观察者</summary>
    [Fact]
    public void TotalBytesWritten_Setter_SameValue_NoNotification()
    {
        var obs = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: obs);

        stream.TotalBytesWritten = 20;
        Assert.Equal(1, obs.CallCount);

        stream.TotalBytesWritten = 20;
        Assert.Equal(1, obs.CallCount);
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

    /// <summary>Dispose 后 Read 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Dispose_ReadThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        var buf = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => stream.Read(buf, 0, buf.Length));
    }

    /// <summary>Dispose 后 Read(Span) 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Dispose_ReadSpanThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        var buf = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => stream.Read(buf.AsSpan()));
    }

    /// <summary>Dispose 后 ReadAsync 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task Dispose_ReadAsyncThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        var buf = new byte[4];
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.ReadAsync(buf, 0, buf.Length));
    }

    /// <summary>Dispose 后 ReadAsync(Memory) 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task Dispose_ReadAsyncMemoryThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        var buf = new byte[4];
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.ReadAsync(buf.AsMemory()).AsTask());
    }

    /// <summary>Dispose 后 Write 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Dispose_WriteThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stream.Write("data"u8));
    }

    /// <summary>Dispose 后 Write(Span) 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Dispose_WriteSpanThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stream.Write("data"u8));
    }

    /// <summary>Dispose 后 WriteAsync 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task Dispose_WriteAsyncThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.WriteAsync("data"u8.ToArray(), 0, 4));
    }

    /// <summary>Dispose 后 WriteAsync(ReadOnlyMemory) 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task Dispose_WriteAsyncReadOnlyMemoryThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.WriteAsync("data"u8.ToArray().AsMemory()).AsTask());
    }

    /// <summary>Dispose 后 Seek 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Dispose_SeekThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    #endregion

    #region DisposeAsync

    /// <summary>DisposeAsync 异步释放内层 Stream</summary>
    [Fact]
    public async Task DisposeAsync_InnerStreamClosed()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    /// <summary>DisposeAsync 后 Read 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task DisposeAsync_ReadThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        var buf = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => stream.Read(buf, 0, buf.Length));
    }

    /// <summary>DisposeAsync 后 Write 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task DisposeAsync_WriteThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => stream.Write("data"u8));
    }

    /// <summary>DisposeAsync 后 ReadAsync 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task DisposeAsync_ReadAsyncThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        var buf = new byte[4];
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.ReadAsync(buf, 0, buf.Length));
    }

    /// <summary>DisposeAsync 后 WriteAsync 抛出 ObjectDisposedException</summary>
    [Fact]
    public async Task DisposeAsync_WriteAsyncThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.WriteAsync("data"u8.ToArray(), 0, 4));
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

    /// <summary>Close 后 Read 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Close_ReadThrowsObjectDisposedException()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Close();
        var buf = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => stream.Read(buf, 0, buf.Length));
    }

    /// <summary>Close 后 Write 抛出 ObjectDisposedException</summary>
    [Fact]
    public void Close_WriteThrowsObjectDisposedException()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner);
        stream.Close();
        Assert.Throws<ObjectDisposedException>(() => stream.Write("data"u8));
    }

    #endregion

    #region 多次 Dispose 安全

    /// <summary>多次 Dispose 不抛出异常</summary>
    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        var stream = new ProgressStream(inner);
        stream.Dispose();
        // 第二次 Dispose 不应抛出
        stream.Dispose();
    }

    /// <summary>多次 DisposeAsync 不抛出异常</summary>
    [Fact]
    public async Task DisposeAsync_MultipleTimes_DoesNotThrow()
    {
        var inner = new MemoryStream("data"u8.ToArray());
        await using var stream = new ProgressStream(inner);
        await stream.DisposeAsync();
        // 第二次 DisposeAsync 不应抛出
        await stream.DisposeAsync();
    }

    #endregion
}
