// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.RamFS;

public interface IRamFsStreamBacking
{
    long Length { get; }
    bool CanRead { get; }
    bool CanWrite { get; }
    bool CanSeek { get; }

    int Read(long position, Span<byte> buffer);
    void Write(long position, ReadOnlySpan<byte> buffer);
    void SetLength(long length);
    void Flush();
}
