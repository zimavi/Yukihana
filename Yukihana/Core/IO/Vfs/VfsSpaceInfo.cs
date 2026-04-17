// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public readonly record struct VfsSpaceInfo(ulong TotalBytes, ulong UsedBytes)
{
    public ulong FreeBytes => TotalBytes - UsedBytes;
}
