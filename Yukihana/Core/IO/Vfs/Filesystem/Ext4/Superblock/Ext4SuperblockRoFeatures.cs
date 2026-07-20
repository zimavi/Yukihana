// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4SuperblockRoFeatures : uint
{
    SparseSuperblocks = 0x1,

    // Can store files larger than 2GiB
    LargeFile = 0x2,

    // This filesystem has files whose sizes are represented in units of logical blocks, 
    // not 512-byte sectors. This implies a very large file indeed!
    HugeFile = 0x4,

    // Group descriptors have checksums. In addition to detecting corruption, 
    // this is useful for lazy formatting with uninitialized groups
    GdtChecksums = 0x10,

    // Indicates that the old ext3 32,000 subdirectory limit no longer applies.
    // A directory’s i_links_count will be set to 1 if it is incremented past 64,999.
    DirectoryNlink = 0x20,
    ExtraInodeSize = 0x40,
    HasSnapshots = 0x80,
    Quota = 0x100,

    // This filesystem supports “bigalloc”, which means that file extents are tracked 
    // in units of clusters (of blocks) instead of blocks.
    BigAlloc = 0x200,

    // This filesystem supports metadata checksumming. Implies GdtChecksums,
    // through GdtChecksum must not be set
    MetadataChecksum = 0x400,

    // Filesystem supports replicas. This feature is neither in the kernel nor e2fsprogs.
    Replica = 0x800,
    ReadOnly = 0x1000,
    
    // Filesystem tracks project quotas.
    Project = 0x2000,

    // Verity inodes may be present on the filesystem.
    Verity = 0x8000,
    OrphanPresent = 0x10000
}