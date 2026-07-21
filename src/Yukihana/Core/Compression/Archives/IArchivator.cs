// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression.Archives;

public interface IArchivator
{
    public ArchiveKind Kind { get; }
    public bool CanRead(ReadOnlySpan<byte> data);
    public ArchiveImage Read(ReadOnlySpan<byte> data);
    public byte[] Write(ArchiveImage image);
}
