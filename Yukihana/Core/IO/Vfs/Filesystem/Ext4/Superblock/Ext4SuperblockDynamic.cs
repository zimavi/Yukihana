// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.InteropServices;
using Yukihana.Core.IO.Vfs.Filesystem.Ext4.Interop;

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Ext4SuperblockDynamic
{
    public readonly uint FirstInode; // Non-reserved;
    public readonly ushort InodeSize;
    public readonly ushort BlockGroupNum; // Block group number of this superblock.
    public readonly Ext4SuperblockCompatibleFeatures CompatibleFeatures;
    public readonly Ext4SuperblockIncompatibleFeatures IncompatibleFeatures;
    public readonly Ext4SuperblockRoFeatures RoCompatibleFeatures;
    public readonly Byte16 Uuid;
    public readonly Byte64 VolumeLabel;
    public readonly uint AlgorithmUsageBitmap; // For compression. Not used.

    // Directory preallocation must only happend if coresponding feature is set.

    public readonly byte BlocksToPreallocate; // Not used.
    public readonly byte BlocksToPreallocateForDirectories; // Not used.
    public readonly ushort ReservedGdtBlocks;

    // Journaling support is valid only if coresponding feature is set.

    public readonly Byte16 JournalUuid;
    public readonly uint JournalInodeNumber;
    public readonly uint JournalDeviceNumber;
    public readonly uint LastOrphanList;
    public readonly UInt32x4 HtreeHashSeed;
    public readonly Ext4HashVersion DefaultHashAlgorithm;

    // f this value is 0 or EXT3_JNL_BACKUP_BLOCKS (1), then the s_jnl_blocks field 
    // contains a duplicate copy of the inode’s i_block[] array and i_size.
    public readonly byte JournalBackupType;
    public readonly ushort GroupDescriptorSize; // If 64bit incompat is set;
    public readonly Ext4DefaultMountOptions DefaulMountOpts;
    public readonly uint FirstMetaBG;
    public readonly uint MkfsTime;

    // Backup copy of the journal inode’s i_block[] array in the first 15 elements 
    // and i_size_high and i_size in the 16th and 17th elements, respectively.
    public readonly UInt32x17 JournalBlocks;
    public readonly uint BlockCountHi;
    public readonly uint RBlockCountHi;
    public readonly uint FreeBlocksCountHi;
    public readonly ushort MinExtraInodeSize; // All inodes have at leat N bytes.
    public readonly ushort WantExtraInodeSize; // New inodes should reserve N bytes.
    public readonly Ext4SuperblockFlags SuperblockFlags;
    public readonly ushort RaidStride;
    public readonly ushort MmpInterval; // not implemented.
    public readonly ulong MmpBlock;
    public readonly uint RaidStripeWidth;
    public readonly byte LogGroupsPerFlex; // Size of flexible block grpoups. (1024 << LogGroupsPerFlex)
    public readonly Ext4ChecksumType ChecksumType;
    public readonly byte EncryptionLevel;
    public readonly byte ReservedPadding; // Padding to next 32bits.
    public readonly ulong KBytesWritten; // Total number of KiB written to this filesystem.
    public readonly uint SnapshotInodeNum; // Not used.
    public readonly uint SnapshotId; // Not used.
    public readonly ulong SnapshotReservedBlocks; // Not used.
    public readonly uint SnapshotList; // Not used.
    public readonly uint ErrorCount; // Number of errors seen.
    public readonly uint FirstErrorTime;
    public readonly uint FirstErrorInode;
    public readonly uint FirstErrorBlock;
    public readonly Byte32 FirstErrorFunction; // Name of function where the error happened.
    public readonly uint FirstErrorLine;
    public readonly uint LastErrorTime;
    public readonly uint LastErrorInode;
    public readonly uint LastErrorLine;
    public readonly uint LastErrorBlock;
    public readonly Byte32 LastErrorFunction; // Name of function where the error happened.
    public readonly Byte64 MountOpts; // ASCIIZ string of mount options.
    public readonly uint UserInodeQuotaFile;
    public readonly uint GroupInodeQuotaFile;

    // Overhead blocks/clusters in fs. 
    // (Huh? This field is always zero, which means that the kernel calculates it dynamically.)
    public readonly uint OverheadBlocks;
    public readonly UInt32x2 BackupBlockGroups; // If SparseSuper2

    // Array with 4 elements
    public readonly Ext4EncryptAlgorithms EncryptAlgos0;
    public readonly Ext4EncryptAlgorithms EncryptAlgos1;
    public readonly Ext4EncryptAlgorithms EncryptAlgos2;
    public readonly Ext4EncryptAlgorithms EncryptAlgos3;

    public readonly Byte16 EncryptPwSalt;
    public readonly uint LostFoundInode;
    public readonly uint ProjQuotaInode;

    // Checksum seed used for metadata_csum calculations. This value is crc32c(~0, $orig_fs_uuid).
    public readonly uint ChecksumSeed;
    public readonly byte WriteTimeHi;
    public readonly byte MountTimeHi;
    public readonly byte MkfsTimeHi;
    public readonly byte TimeOfLastCheckHi;
    public readonly byte FirstErrorTimeHi;
    public readonly byte LastErrorTimeHi;
    public readonly byte FirstErrorCode;
    public readonly byte LastErrorCode;
    public readonly ushort Encoding;
    public readonly ushort EncodingFlags;
    public readonly uint OrphanFileInode;
    public readonly UInt32x94 Reserved;
    public readonly uint Checksum;
}
