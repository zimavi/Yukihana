// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4SuperblockFlags : uint
{
    SignedDirHash = 0x0001,
    UnsignedDirHash = 0x0002,
    TestDevCode = 0x0004
}
