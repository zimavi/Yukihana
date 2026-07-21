// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cosmos.Kernel.System.Storage;
using Yukihana.Core.IO.Vfs.Device;
using Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

namespace Yukihana.Core.IO.Vfs.Probe.Filesystem;

public sealed class Ext4Probe : IFilesystemProbe
{
    public bool TryProbe(Partition part, out FilesystemProbeResult result)
    {
        result = default;
        BlockDeviceStream deviceStream = new(part);

        Span<byte> buffer = stackalloc byte[1024];
        deviceStream.Read(1024, buffer);

        // Validate magic before parsing, to avoid enum exceptions
        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(56, 2));
        if (magic != Ext4SuperblockBase.EXT4_SUPERBLOCK_MAGIC)
        {
            return false;
        }

        Ext4SuperblockBase superblockBase = MemoryMarshal.Read<Ext4SuperblockBase>(buffer);

        // Check if we have dynamic block part
        if (superblockBase.RevisionLevel != Ext4SuperblockBase.EXT4_DYNAMIC_REV)
        {
            result = new()
            {
                Success = true,
                Filesystem = "ext4"
            };
            return true;
        }

        int offset = Unsafe.SizeOf<Ext4SuperblockBase>();
        Ext4SuperblockDynamic superblockDynamic = MemoryMarshal.Read<Ext4SuperblockDynamic>(buffer.Slice(offset));

        Guid fsUuid = superblockDynamic.Uuid.ToGuid();
        string label = superblockDynamic.VolumeLabel.GetAsciiString();

        result = new()
        {
            Success = true,
            Filesystem = "ext4",
            Uuid = fsUuid,
            Label = label
        };
        return true;
    }
}
