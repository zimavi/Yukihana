// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cosmos.Kernel.HAL.Interfaces.Devices;
using Cosmos.Kernel.HAL.Vfs;
using Cosmos.Kernel.System.Storage;
using Yukihana.Debug;
using Yukihana.Vfs.Filesystem.Ext4.BlockGroupDescriptor;
using Yukihana.Vfs.Filesystem.Ext4.Superblock;

namespace Yukihana.Vfs.Filesystem.Ext4;

internal sealed class Ext4FilesystemType : IVfsFilesystemType
{
    private readonly IBlockDevice? _injectedDevice;

    private static readonly Logger s_logger = new("ext4");

    private const Ext4SuperblockCompatibleFeatures COMPATIBLE_FEATURES =
        Ext4SuperblockCompatibleFeatures.HasJournal | Ext4SuperblockCompatibleFeatures.DirIndex |
        Ext4SuperblockCompatibleFeatures.SparseSuper2;

    private const Ext4SuperblockIncompatibleFeatures INCOMPATIBLE_FEATURES =
        Ext4SuperblockIncompatibleFeatures.DirFiletype | Ext4SuperblockIncompatibleFeatures.UseExtents |
        Ext4SuperblockIncompatibleFeatures.Bits64 | Ext4SuperblockIncompatibleFeatures.FlexibleBlockGroups |
        Ext4SuperblockIncompatibleFeatures.InlineData | Ext4SuperblockIncompatibleFeatures.ChecksumSeed |
        Ext4SuperblockIncompatibleFeatures.Recover;

    private const Ext4SuperblockRoFeatures READ_ONLY_FEATURES =
        Ext4SuperblockRoFeatures.SparseSuperblocks | Ext4SuperblockRoFeatures.LargeFile | Ext4SuperblockRoFeatures.HugeFile |
        Ext4SuperblockRoFeatures.DirectoryNlink | Ext4SuperblockRoFeatures.ExtraInodeSize | Ext4SuperblockRoFeatures.MetadataChecksum |
        Ext4SuperblockRoFeatures.GdtChecksums | Ext4SuperblockRoFeatures.ReadOnly;

    private const int DecimalRadix = 10;

    public Ext4FilesystemType()
    { }

    public Ext4FilesystemType(IBlockDevice device)
    {
        _injectedDevice = device;
    }

    public bool TryDestroy(ReadOnlySpan<char> source) => throw new NotImplementedException();
    public bool TryFormat(ReadOnlySpan<char> source, [NotNullWhen(true)] IVfsFormatOptions? options) => throw new NotImplementedException();
    public bool TryMount(ReadOnlySpan<char> source, MountFlags flags, [NotNullWhen(true)] out IVfsSuperblock? superblock)
    {
        s_logger.Debug("Attempt to mount partition as ext4");

        superblock = null;

        IBlockDevice? device = ResolveDevice(source);
        if (device is null)
        {
            s_logger.Debug("Block device was not found");
            return false;
        }

        byte[] superblockBytes = Ext4Helpers.ReadBytes(device, 0, 1024, 1024);
        ReadOnlySpan<byte> superBuffer = superblockBytes;

        // Validate magic before parsing to avoid exceptions on non-ext4 filesystems
        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(superBuffer.Slice(56, 2));
        if (magic != Ext4SuperblockBase.EXT4_SUPERBLOCK_MAGIC)
        {
            s_logger.Debug("Magic mismatch");
            return false;
        }

        Ext4SuperblockBase superblockBase =
            MemoryMarshal.Read<Ext4SuperblockBase>(superBuffer);

        if (superblockBase.RevisionLevel != Ext4SuperblockBase.EXT4_DYNAMIC_REV)
        {
            s_logger.Debug("Revision level is 'legacy' (non-dynamic) which is not supported");
            return false;
        }

        int offset = Unsafe.SizeOf<Ext4SuperblockBase>();

        Ext4SuperblockDynamic superblockDynamic =
            MemoryMarshal.Read<Ext4SuperblockDynamic>(superBuffer[offset..]);

        if (!ValidateSuperblock(superblockDynamic, superBuffer))
        {
            s_logger.Debug("Failed validate superblock");
            return false;
        }

        if (!ValidateFeatures(superblockDynamic, flags, out _))
        {
            s_logger.Debug("Refused to mount");
            return false;
        }

        if (NeedsRecovery(superblockDynamic))
        {
            s_logger.Debug("Recovery required. Not implemented and refused to mount");

            // TODO: Implement recovery after journal is done.

            return false;
        }

        // Read Block Group Descriptor Table

        ulong blocksCount = Ext4Helpers.Combine<uint, ulong>(
            superblockBase.BlocksCountLo,
            superblockDynamic.BlockCountHi);

        ulong blockGroups =
            (blocksCount + superblockBase.BlocksPerGroup - 1) /
            superblockBase.BlocksPerGroup;

        ushort descriptorSize = superblockDynamic.IncompatibleFeatures.HasFlag(
            Ext4SuperblockIncompatibleFeatures.Bits64)
                ? superblockDynamic.GroupDescriptorSize
                : (ushort)32;

        uint tableSize = checked((uint)(blockGroups * descriptorSize));

        byte[] bgdtBytes = Ext4Helpers.ReadBytes(
            device,
            startBlock: 2,
            offset: 0,
            count: tableSize);

        Ext4BlockGroupDescriptorTable table = new(bgdtBytes.ToArray().AsMemory(), descriptorSize);

        s_logger.Debug("Filesystem looks fine and can be mounted");
        return false;
    }

    private IBlockDevice? ResolveDevice(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty || source.IsWhiteSpace())
        {
            return _injectedDevice;
        }

        if (!TryParseInt(source, out int partitionIndex))
        {
            return null;
        }

        IReadOnlyList<Partition> partitions = StorageManager.Partitions;
        if (partitionIndex < 0 || partitionIndex >= partitions.Count)
        {
            return null;
        }

        return partitions[partitionIndex];
    }

    private static bool TryParseInt(ReadOnlySpan<char> source, out int value)
    {
        value = 0;
        if (source.Length == 0)
        {
            return false;
        }

        int result = 0;
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (c < '0' || c > '9')
            {
                return false;
            }
            int digit = c - '0';
            // Unchecked accumulation wraps: "4294967297" would parse as 1
            // and alias destructive operations (format/destroy) onto a
            // valid low partition index.
            if (result > (int.MaxValue - digit) / DecimalRadix)
            {
                return false;
            }
            result = result * DecimalRadix + digit;
        }

        value = result;
        return true;
    }

    private static bool ValidateFeatures(in Ext4SuperblockDynamic sb, MountFlags mountFlags, out bool forceReadOnly)
    {
        forceReadOnly = false;

        uint unknownIncopat =
            (uint)(sb.IncompatibleFeatures & ~INCOMPATIBLE_FEATURES);

        if (unknownIncopat != 0) // there are incompatible features we do not support
        {
            s_logger.Debug($"Found unsupported features: {unknownIncopat:b32} ({(Ext4SuperblockIncompatibleFeatures)unknownIncopat})");
            return false;
        }

        uint unknownRo = (uint)(sb.RoCompatibleFeatures & ~READ_ONLY_FEATURES);

        if (unknownRo != 0)
        {
            s_logger.Debug($"Found unsupported features that require read-only mount: {unknownRo:b32} ({(Ext4SuperblockRoFeatures)unknownRo})");

            forceReadOnly = true;

            if ((mountFlags & MountFlags.ReadOnly) == 0)
            {
                return false;
            }
        }

        if (sb.RoCompatibleFeatures.HasFlag(Ext4SuperblockRoFeatures.ReadOnly))
        {
            s_logger.Debug("Filesystem requires to mount as read-only");

            forceReadOnly = true;

            if ((mountFlags & MountFlags.ReadOnly) == 0)
            {
                return false;
            }
        }

        if (sb.RoCompatibleFeatures.HasFlag(Ext4SuperblockRoFeatures.MetadataChecksum) &&
            sb.RoCompatibleFeatures.HasFlag(Ext4SuperblockRoFeatures.GdtChecksums))
        {
            s_logger.Error("Found contradictory feature flags");
            return false;
        }

        if (sb.IncompatibleFeatures.HasFlag(
                Ext4SuperblockIncompatibleFeatures.InlineData) &&
            !sb.IncompatibleFeatures.HasFlag(
                Ext4SuperblockIncompatibleFeatures.UseExtents))
        {
            s_logger.Error("Found contradictory feature flags");
            return false;
        }

        return true;
    }

    private bool NeedsRecovery(in Ext4SuperblockDynamic sb)
        => sb.IncompatibleFeatures.HasFlag(Ext4SuperblockIncompatibleFeatures.Recover);

    private bool ValidateSuperblock(Ext4SuperblockDynamic sb, ReadOnlySpan<byte> sbBytes)
    {
        s_logger.Debug("Validating superblock");

        if (sbBytes.Length != 1024)
        {
            s_logger.Debug("Superblock length invalid (1024 expected, got {sbBytes.Length})");
            return false;
        }

        bool hasChecksumFeature = sb.RoCompatibleFeatures.HasFlag(Ext4SuperblockRoFeatures.MetadataChecksum);

        if (!hasChecksumFeature)
        {
            s_logger.Debug("Filesystem does not support metadata checksums. Skipping superblock validation");
            return true; // Do not check checksum
        }

        uint finalChecksum = Ext4Checksum.Append(~0u, sbBytes.Slice(0, 1020));

        if (finalChecksum != sb.Checksum)
        {
            s_logger.Debug($"Bad superblock (expected {sb.Checksum:x}, got {finalChecksum:x})");
            return false;
        }

        s_logger.Debug("Superblock validated");
        return true;
    }
}
