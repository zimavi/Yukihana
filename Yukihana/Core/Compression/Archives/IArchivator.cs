// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression.Archives;

public interface IArchivator
{
    ArchiveKind Kind { get; }
    bool CanRead(ReadOnlySpan<byte> data);
    ArchiveImage Read(ReadOnlySpan<byte> data);
    byte[] Write(ArchiveImage image);
}