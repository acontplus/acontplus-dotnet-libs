using System.Buffers;

namespace Acontplus.Reports.Services;

/// <summary>
/// Thread-safe object pool for MemoryStream instances to reduce GC pressure
/// </summary>
internal class MemoryStreamPool : IDisposable
{
    private readonly ArrayPool<byte> _arrayPool;
    private bool _disposed;

    public MemoryStreamPool()
    {
        _arrayPool = ArrayPool<byte>.Shared;
    }

    /// <summary>
    /// Gets a pooled MemoryStream
    /// </summary>
    public PooledMemoryStream GetStream() => new(_arrayPool);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// MemoryStream wrapper that returns rented arrays to the pool when disposed
/// </summary>
internal class PooledMemoryStream : MemoryStream
{
    private readonly ArrayPool<byte> _pool;
    private byte[]? _rentedBuffer;

    public PooledMemoryStream(ArrayPool<byte> pool, int capacity = 0) : base()
    {
        _pool = pool;
        if (capacity > 0)
        {
            _rentedBuffer = _pool.Rent(capacity);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _rentedBuffer != null)
        {
            _pool.Return(_rentedBuffer, clearArray: true);
            _rentedBuffer = null;
        }
        base.Dispose(disposing);
    }
}
