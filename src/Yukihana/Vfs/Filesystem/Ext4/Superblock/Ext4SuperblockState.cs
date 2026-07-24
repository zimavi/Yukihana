// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4SuperblockState : ushort
{
    // Cleanly mounted
    CleanMnt = 0x0001,

    // Errors detected
    Errors = 0x0002,

    // Orphans being recovered
    OrphansRecover = 0x0004
}
