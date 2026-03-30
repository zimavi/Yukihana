namespace Yukihana.Core.IO.RamFS;

public sealed class RamFsStream : Stream
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _length;

    private int _position;

    public RamFsStream(byte[] buffer, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length - length);

        _buffer = buffer;
        _start = offset;
        _length = length;
        _position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(0, value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_length, value);

            _position = (int)value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        int remaining = _length - _position;
        if (remaining <= 0)
            return 0;
        
        int toCopy = Math.Min(count, remaining);

        Buffer.BlockCopy(_buffer, _start + _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }

    public override int Read(Span<byte> buffer)
    {
        int remaining = _length - _position;
        if (remaining <= 0)
            return 0;
        
        int toCopy = Math.Min(buffer.Length, remaining);

        new Span<byte>(_buffer, _start + _position, toCopy)
            .CopyTo(buffer);

        _position += toCopy;
        return toCopy;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        int newPos = origin switch
        {
            SeekOrigin.Begin => (int)offset,
            SeekOrigin.Current => _position + (int)offset,
            SeekOrigin.End => _length + (int)offset,
            _ => throw new ArgumentException(nameof(origin))
        };

        if (newPos < 0 || newPos > _length)
            throw new IOException("Seek out of bounds");
        
        _position = newPos;
        return _position;
    }

    public override void Flush() {}

    public override void SetLength(long value) => throw new NotSupportedException("Read-only stream");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Read-only stream");

    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException("Read-only stream");
}