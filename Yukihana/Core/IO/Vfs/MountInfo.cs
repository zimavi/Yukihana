// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.IO.Vfs.Backends;

namespace Yukihana.Core.IO.Vfs;

internal sealed class MountInfo
{
    public required string MountPoint { get; init; }
    public required IVfsBackend Backend { get; init; }
}

