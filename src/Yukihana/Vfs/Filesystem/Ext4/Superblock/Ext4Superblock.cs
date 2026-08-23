// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Vfs.Filesystem.Ext4.Superblock;

internal sealed class Ext4Superblock(Ext4SuperblockBase sbBase, Ext4SuperblockDynamic? sbDynamic)
{
    private readonly Ext4SuperblockBase _base = sbBase;
    private readonly Option<Ext4SuperblockDynamic> _dynamic = sbDynamic ?? Option<Ext4SuperblockDynamic>.None();

    public bool HasDynamicSuperblock => _dynamic.IsSome;

    #region Base + Dynamic high values

    public uint InodesCount => _base.InodesCount;

    public ulong BlocksCount => HasDynamicSuperblock
        ? Ext4Helpers.Combine<uint, ulong>(_base.BlocksCountLo, _dynamic.Value.BlockCountHi)
        : _base.BlocksCountLo;

    public ulong RootBlocks => HasDynamicSuperblock
        ? Ext4Helpers.Combine<uint, ulong>(_base.RBlockCountLo, _dynamic.Value.RBlockCountHi)
        : _base.RBlockCountLo;

    public ulong FreeBlocksCount => HasDynamicSuperblock
        ? Ext4Helpers.Combine<uint, ulong>(_base.FreeBlocksCountLo, _dynamic.Value.FreeBlocksCountHi)
        : _base.FreeBlocksCountLo;

    public uint FreeInodesCount => _base.FreeInodesCount;

    public uint FirstDataBlock => _base.FirstDataBlock;

    public ulong BlockSize => 1u << (int)_base.LogBlockSize;

    public ulong ClusterSize => 1u << (int)_base.LogClusterSize;

    public uint BlocksPerGroup => _base.BlocksPerGroup;

    public uint ClustersPerGroup => _base.ClustersPerGroup;

    public uint InodesPerGroup => _base.InodesPerGroup;

    public VfsTimespec MountTime => new(_base.MountTime, 0);

    public VfsTimespec WriteTime => new(_base.WriteTime, 0);

    public ushort NumberOfMounts => _base.NumberOfMounts;

    public ushort MaxMountsCount => _base.MaxMountsCount;

    public ushort MinorRevisionLevel => _base.MinorRevisionLevel;

    public VfsTimespec LastCheckTime => new(_base.TimeOfLastCheck, 0);

    public ushort DefaultUid => _base.DefaultUid;

    public ushort DefaultGid => _base.DefaultGid;

    #endregion

    #region Dynamic region

    public uint FirstInodeId => _dynamic.Value.FirstInode;

    public ushort InodeSize => _dynamic.Value.InodeSize;

    public ushort BlockGroupNumber => _dynamic.Value.BlockGroupNum;

    public Guid UUID => _dynamic.Value.Uuid.ToGuid();

    public string VolumeLabel => _dynamic.Value.VolumeLabel.GetAsciiString();

    public string PathOfLastMount => _dynamic.Value.LastMounted.GetAsciiString();

    public ushort ReservedGdtBlocks => _dynamic.Value.ReservedGdtBlocks;

    public Guid JournalUUID => _dynamic.Value.JournalUuid.ToGuid();

    public uint JournalInodeId => _dynamic.Value.JournalInodeNumber;

    public uint JournalDeviceNumber => _dynamic.Value.JournalDeviceNumber;

    // Last orphan list

    // HTree seed

    // journal backup type

    public ushort GroupDescriptorSize => _dynamic.Value.GroupDescriptorSize;

    // First meta bg

    public VfsTimespec MakefsTime => new(_dynamic.Value.MkfsTime, 0);

    // journal blocks

    public ushort MinExtraInodeSize => _dynamic.Value.MinExtraInodeSize;

    public ushort DesiredExtraInodeSize => _dynamic.Value.WantExtraInodeSize;

    // raid stride

    // mmp interval

    // mmp block

    // raid stride width

    public ulong FlexibleBlockGroupsSize => 1u << _dynamic.Value.LogGroupsPerFlex;

    // encryption level

    public ulong WrittenKiB => _dynamic.Value.KBytesWritten;

    // snapshot inode num

    // snapshot id

    // snapshot reserved blocks

    // snapshot list

    // TODO: Errors

    // user inode quota file

    // group inode quota file

    // overhead blocks

    // backup block groups

    // exception algorithms

    // encryption salt

    // lost+found inode

    // project quota inode

    public uint ChecksumSeed => _dynamic.Value.ChecksumSeed;

    public uint SuperblockChecksum => _dynamic.Value.Checksum;

    #endregion

    // TODO: Enums
}
