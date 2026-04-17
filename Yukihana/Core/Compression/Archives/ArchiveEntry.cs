// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.Compression.Archives;

public sealed record ArchiveHardLinkKey(uint DeviceMajor, uint DeviceMinor, uint Inode);

public sealed class ArchiveEntry
{
    public string Path { get; init; } = string.Empty;
    public ArchiveEntryKind Kind { get; init; } = ArchiveEntryKind.File;
    public byte[] Data { get; init; } = [];
    public string? LinkTarget { get; init; }
    public int Mode { get; init; }
    public int UserId { get; init; }
    public int GroupId { get; init; }
    public long ModifiedUnixTimeSeconds { get; init; }
    public ArchiveHardLinkKey? HardLinkKey { get; init; }

    public long Size => Data?.LongLength ?? 0;

    public Stream OpenRead()
    {
        if (Kind == ArchiveEntryKind.SymbolicLink && LinkTarget is not null)
            return new MemoryStream(Encoding.UTF8.GetBytes(LinkTarget), writable: false);

        return new MemoryStream(Data ?? [], writable: false);
    }
}
