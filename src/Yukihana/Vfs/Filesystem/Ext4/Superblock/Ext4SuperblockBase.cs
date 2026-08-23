// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.InteropServices;

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Ext4SuperblockBase
{
    public const ushort EXT4_SUPERBLOCK_MAGIC = 0xef53;

    // Revision
    public const uint EXT4_REVISION_ORIGINAL = 0;
    public const uint EXT4_DYNAMIC_REV = 1; // dynamic inode sizes;

    public readonly uint InodesCount;
    public readonly uint BlocksCountLo;
    public readonly uint RBlockCountLo; // This number of blocks can only be allocated by super-user.
    public readonly uint FreeBlocksCountLo;
    public readonly uint FreeInodesCount;
    public readonly uint FirstDataBlock;
    public readonly uint LogBlockSize; // Get block size by 1024 << LogBlockSize.
    public readonly uint LogClusterSize;
    public readonly uint BlocksPerGroup;
    public readonly uint ClustersPerGroup; // If bigalloc is enabled. Must match BlocksPerGroup otherwise.
    public readonly uint InodesPerGroup;
    public readonly uint MountTime;
    public readonly uint WriteTime;
    public readonly ushort NumberOfMounts; // Since last fsck.
    public readonly ushort MaxMountsCount; // Number after which fsck is required.
    public readonly ushort Magic;
    public readonly Ext4SuperblockState SuperblockState; // Flags
    public readonly Ext4SuperblockErrorPolicy SuperblockErrors;
    public readonly ushort MinorRevisionLevel;
    public readonly uint TimeOfLastCheck;
    public readonly uint CheckInterval; // Maximum time between checks, in seconds.
    public readonly Ext4SuperblockCreator CreatorOS;
    public readonly uint RevisionLevel;
    public readonly ushort DefaultUid; // For reserved blocks.
    public readonly ushort DefaultGid;

}
