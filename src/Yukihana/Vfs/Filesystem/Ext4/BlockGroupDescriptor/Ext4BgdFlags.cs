// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Vfs.Filesystem.Ext4.BlockGroupDescriptor;

[Flags]
internal enum Ext4BgdFlags : ushort
{
    // inode table and bitmap are not initialized
    InodeUninit = 0x1,

    // block bitmap is not initialized
    BlockUninit = 0x2,

    // inode table is zeroed
    InodeZeroed = 0x4
}
