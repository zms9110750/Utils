namespace zms9110750.Utils.Core;

public class ProgressStream : Stream
{
    private readonly Stream innerStream;
    private readonly IObserver<long>? readObserver;
    private readonly IObserver<long>? writeObserver;
    private long totalBytesRead;
    private long totalBytesWritten;
    private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

    public ProgressStream(
        Stream innerStream,
        IObserver<long>? readObserver = null,
        IObserver<long>? writeObserver = null)
    {
        this.innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        this.readObserver = readObserver;
        this.writeObserver = writeObserver;
    }

    public override bool CanRead => innerStream.CanRead;
    public override bool CanSeek => innerStream.CanSeek;
    public override bool CanWrite => innerStream.CanWrite;
    public override bool CanTimeout => innerStream.CanTimeout;

    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override int ReadTimeout
    {
        get => innerStream.ReadTimeout;
        set => innerStream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => innerStream.WriteTimeout;
        set => innerStream.WriteTimeout = value;
    }

    #region 读取相关方法
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReportReadProgress(
            await innerStream.ReadAsync(buffer, offset, count, cancellationToken)
        );
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return ReportReadProgress(
            await innerStream.ReadAsync(buffer, cancellationToken)
        );
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReportReadProgress(
            innerStream.Read(buffer, offset, count)
        );
    }

    public override int Read(Span<byte> buffer)
    {
        return ReportReadProgress(
            innerStream.Read(buffer)
        );
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        lockSlim.EnterReadLock();
        try
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await innerStream.ReadAsync(buffer, 0, bufferSize, cancellationToken)) > 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    ReportReadProgress(bytesRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }
    #endregion

    #region 写入相关方法
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        ReportWriteProgress(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await innerStream.WriteAsync(buffer, cancellationToken);
        ReportWriteProgress(buffer.Length);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        innerStream.Write(buffer, offset, count);
        ReportWriteProgress(count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        innerStream.Write(buffer);
        ReportWriteProgress(buffer.Length);
    }
    #endregion

    #region 报告进度的方法
    private int ReportReadProgress(int bytesRead)
    {
        if (bytesRead > 0)
        {
            lockSlim.EnterReadLock();
            try
            {
                var newTotal = Interlocked.Add(ref totalBytesRead, bytesRead);
                readObserver?.OnNext(newTotal);
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }
        return bytesRead;
    }

    private int ReportWriteProgress(int bytesWritten)
    {
        if (bytesWritten > 0)
        {
            lockSlim.EnterReadLock();
            try
            {
                var newTotal = Interlocked.Add(ref totalBytesWritten, bytesWritten);
                writeObserver?.OnNext(newTotal);
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }
        return bytesWritten;
    }
    #endregion

    #region 其他Stream方法
    public override void Flush()
    {
        innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return innerStream.FlushAsync(cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        innerStream.SetLength(value);
    }

    public override void Close()
    {
        innerStream.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lockSlim.Dispose();
            innerStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        lockSlim.Dispose();
        await innerStream.DisposeAsync();
        await base.DisposeAsync();
    }
    #endregion

    #region 进度相关属性
    /// <summary>
    /// 获取或设置已读取的总字节数
    /// </summary>
    public long TotalBytesRead
    {
        get => Interlocked.Read(ref totalBytesRead);
        set
        {
            lockSlim.EnterWriteLock();
            try
            {
                var oldValue = Interlocked.Exchange(ref totalBytesRead, value);
                if (oldValue != value && readObserver != null)
                {
                    // 当值变化时通知观察者
                    readObserver.OnNext(value);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 获取或设置已写入的总字节数
    /// </summary>
    public long TotalBytesWritten
    {
        get => Interlocked.Read(ref totalBytesWritten);
        set
        {
            lockSlim.EnterWriteLock();
            try
            {
                var oldValue = Interlocked.Exchange(ref totalBytesWritten, value);
                if (oldValue != value && writeObserver != null)
                {
                    // 当值变化时通知观察者
                    writeObserver.OnNext(value);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
    }
    #endregion
}
