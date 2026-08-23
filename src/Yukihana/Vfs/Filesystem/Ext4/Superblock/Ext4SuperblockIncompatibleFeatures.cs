// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4SuperblockIncompatibleFeatures : uint
{
    Compression = 0x1,

    // Directory entries record file type
    DirFiletype = 0x2,

    // Filesystem needs recovery
    Recover = 0x4,

    // Has separate journal device
    JournalDevice = 0x8,
    MetaBlockGroups = 0x10,
    UseExtents = 0x40,
    Bits64 = 0x80,
    MultipleMountProtection = 0x100,
    FlexibleBlockGroups = 0x200,
    ExtendedAttributeInodes = 0x400,

    // Not implemented?
    DirectoryData = 0x1000,

    // Metadata checksum seed is stored in the superblock. This feature enables
    // administrator to change the UUID of metadata checksum filesystem while
    // filesystem is mounted; without it, the checksum definition requires all
    // metadata blocks to be rewritten.
    ChecksumSeed = 0x2000,

    // If enabled, directories can be larger then 4 GiB and have a maximum htree
    // depth of 3.
    LargeDirectories = 0x4000,

    // Inline data inside inode
    InlineData = 0x8000,

    // Encrypted inodes can be present
    Encrypted = 0x10000,

    // Directories can be markes as case-insensitive
    Casefold = 0x20000
}
