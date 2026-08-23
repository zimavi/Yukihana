// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

internal enum Ext4SuperblockErrorPolicy : ushort
{
    Continue = 1,
    RemountRO = 2,
    Panic = 3
}
