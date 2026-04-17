// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.RamFS;

public sealed class RamFsStream : Stream
{
    private readonly IRamFsStreamBacking _backing;
    private readonly FileAccess _access;
    private long _position;
    private bool _disposed;

    public RamFsStream(IRamFsStreamBacking backing, FileAccess access, FileShare share = FileShare.Read, long initialPosition = 0)
    {
        _backing = backing ?? throw new ArgumentNullException(nameof(backing));
        _access = access;
        Share = share;

        if (initialPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(initialPosition));

        if (initialPosition > backing.Length)
            initialPosition = backing.Length;

        _position = initialPosition;
    }

    public FileShare Share { get; }

    public override bool CanRead => !_disposed && ((_access & FileAccess.Read) != 0) && _backing.CanRead;
    public override bool CanWrite => !_disposed && ((_access & FileAccess.Write) != 0) && _backing.CanWrite;
    public override bool CanSeek => !_disposed && _backing.CanSeek;
    public override long Length
    {
        get
        {
            EnsureNotDisposed();
            return _backing.Length;
        }
    }

    public override long Position
    {
        get
        {
            EnsureNotDisposed();
            return _position;
        }
        set
        {
            EnsureNotDisposed();

            if (!CanSeek)
                throw new NotSupportedException("Seek is not supported.");

            ArgumentOutOfRangeException.ThrowIfNegative(value);

            _position = value > _backing.Length ? _backing.Length : value;
        }
    }

    public override void Flush()
    {
        EnsureNotDisposed();
        _backing.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        EnsureNotDisposed();

        if (!CanRead)
            throw new NotSupportedException("Read is not supported.");

        ArgumentNullException.ThrowIfNull(buffer);

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length - count);

        int read = Read(new Span<byte>(buffer, offset, count));
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();

        if (!CanRead)
            throw new NotSupportedException("Read is not supported.");

        int read = _backing.Read(_position, buffer);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureNotDisposed();

        if (!CanSeek)
            throw new NotSupportedException("Seek is not supported.");

        long newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _backing.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (newPos < 0)
            throw new IOException("Seek before beginning of stream.");

        _position = newPos > _backing.Length ? _backing.Length : newPos;
        return _position;
    }

    public override void SetLength(long value)
    {
        EnsureNotDisposed();

        if (!CanWrite)
            throw new NotSupportedException("SetLength is not supported.");

        ArgumentOutOfRangeException.ThrowIfNegative(value);

        _backing.SetLength(value);
        if (_position > value)
            _position = value;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureNotDisposed();

        if (!CanWrite)
            throw new NotSupportedException("Write is not supported.");

        ArgumentNullException.ThrowIfNull(buffer);

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length - count);

        Write(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureNotDisposed();

        if (!CanWrite)
            throw new NotSupportedException("Write is not supported.");

        _backing.Write(_position, buffer);
        _position += buffer.Length;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _backing.Flush();

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
