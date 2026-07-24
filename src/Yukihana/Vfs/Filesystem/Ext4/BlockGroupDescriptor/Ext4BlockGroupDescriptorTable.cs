// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Runtime.InteropServices;

namespace Yukihana.Vfs.Filesystem.Ext4.BlockGroupDescriptor;

internal sealed class Ext4BlockGroupDescriptorTable
{
    private readonly ReadOnlyMemory<byte> _data;
    private readonly int _descriptorSize;

    public Ext4BlockGroupDescriptorTable(ReadOnlyMemory<byte> data, ushort descriptorSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(descriptorSize, 32);

        if (data.Length % descriptorSize != 0)
        {
            throw new ArgumentException("Descriptor table is not aligned.");
        }

        _data = data;
        _descriptorSize = descriptorSize;
    }

    public int Count => _data.Length / _descriptorSize;

    public bool Has64BitDescriptors => _descriptorSize >= 64;

    public ref readonly Ext4BlockGroupDescriptor32 Get32(int idx)
    {
        ValidateIndex(idx);

        ReadOnlySpan<byte> span = _data.Span.Slice(idx * _descriptorSize, 32);

        return ref MemoryMarshal.AsRef<Ext4BlockGroupDescriptor32>(span);
    }

    public ref readonly Ext4BlockGroupDescriptor64 Get64(int idx)
    {
        if (!Has64BitDescriptors)
        {
            throw new InvalidOperationException("Filesystem uses 32-byte descriptors.");
        }

        ValidateIndex(idx);

        ReadOnlySpan<byte> span = _data.Span.Slice(idx * _descriptorSize + 32, 32);

        return ref MemoryMarshal.AsRef<Ext4BlockGroupDescriptor64>(span);
    }

    public ulong GetBlockBitmap(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.BlockBitmapLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<uint, ulong>(lo.BlockBitmapLo, hi.BlockBitmapHi);
    }

    public ulong GetInodeBitmap(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.InodeBitmapLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<uint, ulong>(lo.InodeBitmapLo, hi.InodeBitmapHi);
    }

    public ulong GetInodeTable(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.InodeTableLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<uint, ulong>(lo.InodeTableLo, hi.InodeTableHi);
    }

    public uint GetFreeBlocksCount(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.FreeBlocksCountLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<ushort, uint>(lo.FreeBlocksCountLo, hi.FreeBlocksCountHi);
    }

    public uint GetFreeInodesCount(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.FreeInodesCountLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<ushort, uint>(lo.FreeInodesCountLo, hi.FreeInodesCountHi);
    }

    public uint GetUsedDirsCount(int index)
    {
        ref readonly Ext4BlockGroupDescriptor32 lo = ref Get32(index);

        if (!Has64BitDescriptors)
        {
            return lo.UsedDirsCountLo;
        }

        ref readonly Ext4BlockGroupDescriptor64 hi = ref Get64(index);

        return Ext4Helpers.Combine<ushort, uint>(lo.UsedDirsCountLo, hi.UsedDirsCountHi);
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
