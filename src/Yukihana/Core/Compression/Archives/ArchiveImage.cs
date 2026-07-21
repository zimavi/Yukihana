// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression.Archives;

public sealed class ArchiveImage
{
    private readonly List<ArchiveEntry> _entries = [];

    public IReadOnlyList<ArchiveEntry> Entries => _entries;
    public ArchiveKind Kind { get; internal set; } = ArchiveKind.Unknown;

    public void Add(ArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Add(entry);
    }

    public void AddRange(IEnumerable<ArchiveEntry> entries)
    {
        foreach (var entry in entries)
        {
            Add(entry);
        }
    }
}
