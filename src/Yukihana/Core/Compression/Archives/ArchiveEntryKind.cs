// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression.Archives;

public enum ArchiveEntryKind
{
    File = 0,
    Directory,
    SymbolicLink,
    HardLink,
    Other,
}
