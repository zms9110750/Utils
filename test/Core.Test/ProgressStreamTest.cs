using zms9110750.Utils.Core;

namespace Core.Test;

class TestObserver : IObserver<long>
{
    public long Value { get; private set; }
    public void OnNext(long value) => Value = value;
    public void OnError(Exception error) { }
    public void OnCompleted() { }
}

/// <summary>
/// 验证 ProgressStream 的读操作和进度报告。
/// 预期：读操作透传内层 Stream 行为，注册的 readObserver 收到累计字节数。
/// </summary>
public class ProgressStreamReadTest
{
    [Fact]
    public void Read_返回正确字节数()
    {
        var data = "Hello"u8.ToArray();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner);
        var buffer = new byte[1024];
        int read = stream.Read(buffer, 0, buffer.Length);
        Assert.Equal(5, read);
    }

    [Fact]
    public void ReadAsync_返回正确字节数()
    {
        var data = "Hello"u8.ToArray();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner);
        var buffer = new byte[1024];
        int read = stream.ReadAsync(buffer, 0, buffer.Length).Result;
        Assert.Equal(5, read);
    }

    [Fact]
    public void Read_报告进度()
    {
        var data = "Hello World"u8.ToArray();
        var observer = new TestObserver();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner, observer);
        var buffer = new byte[1024];
        stream.Read(buffer, 0, buffer.Length);
        Assert.Equal(11, observer.Value);
    }

    [Fact]
    public void CanRead_透传内层()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void CanSeek_透传内层()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void CanWrite_透传内层()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        Assert.True(stream.CanWrite);
    }
}

/// <summary>
/// 验证 ProgressStream 的写操作和进度报告。
/// 预期：写操作透传内层 Stream，注册的 writeObserver 收到累计字节数。
/// </summary>
public class ProgressStreamWriteTest
{
    [Fact]
    public void Write_写入内层()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        var data = "Hello"u8.ToArray();
        stream.Write(data, 0, data.Length);
        Assert.Equal(5, inner.Length);
    }

    [Fact]
    public void WriteAsync_写入内层()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        var data = "Hello"u8.ToArray();
        stream.WriteAsync(data, 0, data.Length).Wait();
        Assert.Equal(5, inner.Length);
    }

    [Fact]
    public void Write_报告进度()
    {
        var observer = new TestObserver();
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner, writeObserver: observer);
        var data = "Hi"u8.ToArray();
        stream.Write(data, 0, data.Length);
        Assert.Equal(2, observer.Value);
    }

    [Fact]
    public void Flush_不抛出()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.Flush();
    }

    [Fact]
    public void Close_不抛出()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.Close();
    }
}

/// <summary>
/// 验证 ProgressStream 的 Seek / Length / Position 透传。
/// 预期：Seek、Length、Position 行为与内层 MemoryStream 一致。
/// </summary>
public class ProgressStreamSeekTest
{
    [Fact]
    public void Length_透传内层()
    {
        var data = "Hello"u8.ToArray();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner);
        Assert.Equal(5, stream.Length);
    }

    [Fact]
    public void Seek_返回正确位置()
    {
        var data = "Hello"u8.ToArray();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner);
        long pos = stream.Seek(2, SeekOrigin.Begin);
        Assert.Equal(2, pos);
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void Position_设置后读取位置正确()
    {
        var data = "Hello"u8.ToArray();
        using var inner = new MemoryStream(data);
        using var stream = new ProgressStream(inner);
        stream.Position = 1;
        int b = stream.ReadByte();
        Assert.Equal('e', b);
    }

    [Fact]
    public void SetLength_改变长度()
    {
        using var inner = new MemoryStream();
        using var stream = new ProgressStream(inner);
        stream.SetLength(100);
        Assert.Equal(100, stream.Length);
    }
}
