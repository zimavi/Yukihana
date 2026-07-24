// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Runtime.InteropServices;

namespace Yukihana.Vfs.Filesystem.Ext4.BlockGroupDescriptor;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Ext4BlockGroupDescriptor64
{
    public readonly uint BlockBitmapHi;
    public readonly uint InodeBitmapHi;
    public readonly uint InodeTableHi;
    public readonly ushort FreeBlocksCountHi;
    public readonly ushort FreeInodesCountHi;
    public readonly ushort UsedDirsCountHi;
    public readonly ushort InodeTableUnusedHi;
    public readonly ushort ExcludeBitmapHi;
    public readonly ushort BlockBitmapChecksumHi;
    public readonly ushort InodeBitmapChecksumHi;
    public readonly uint Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Ext4BlockGroupDescriptor32
{
    public readonly uint BlockBitmapLo;
    public readonly uint InodeBitmapLo;
    public readonly uint InodeTableLo;
    public readonly ushort FreeBlocksCountLo;
    public readonly ushort FreeInodesCountLo;
    public readonly ushort UsedDirsCountLo;
    public readonly Ext4BgdFlags Flags;
    public readonly uint ExcludeBitmapLo;
    public readonly ushort BlockBitmapChecksumLo;
    public readonly ushort InodeBitmapChecksumLo;

    // Lower 16-bits of unused inode count. 
    // If set, we needn’t scan past the (sb.s_inodes_per_group - gdt.bg_itable_unused) th entry in the inode table for this group.
    public readonly ushort InodeTableUnusedLo;
    public readonly ushort Checksum;
}
