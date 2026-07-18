// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.IO.Vfs.Config;

public sealed class VfsMountConfig
{
    public string Source { get; set; }
    public string MountPoint { get; set; }
    public string FilesystemType { get; set; }
    public MountFlags Flags { get; set; }
    public int DumpFrequency { get; set; } = 0;
    public int PassNumber { get; set; } = 0;

    public SourceType SourceType { get; set; } = SourceType.Path;

    public VfsMountConfig()
    {
        Source = string.Empty;
        MountPoint = string.Empty;
        FilesystemType = string.Empty;
    }
}
