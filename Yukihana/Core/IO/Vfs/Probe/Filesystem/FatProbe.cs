// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using acryptohashnet;
using Cosmos.Kernel.System.Filesystems.Fat;
using Cosmos.Kernel.System.Storage;

namespace Yukihana.Core.IO.Vfs.Probe.Filesystem;

public sealed class FatProbe : IFilesystemProbe
{
    private const int Fat1216BootSignature = 0x26;
    private const int Fat1216Serial = 0x27;
    private const int Fat1216Label = 0x2B;

    private const int Fat32BootSignature = 0x42;
    private const int Fat32Serial = 0x43;
    private const int Fat32Label = 0x47;

    public bool TryProbe(Partition part, out FilesystemProbeResult result)
    {
        result = default;

        Span<byte> sector = stackalloc byte[(int)part.BlockSize];

        Console.WriteLine("Reading sector");
        part.ReadBlock(0, 1, sector);

        Console.WriteLine("Parsing sector");
        if (!FatBootSector.TryParse(sector, out FatBootSector? boot))
            return false;

        bool isFat32 = boot!.Type == FatType.Fat32;

        int sigOffset = isFat32 ? Fat32BootSignature : Fat1216BootSignature;
        int serialOffset = isFat32 ? Fat32Serial : Fat1216Serial;
        int labelOffset = isFat32 ? Fat32Label : Fat1216Label;

        uint serial = 0;
        string? label = null;

        if (sector[sigOffset] == 0x29)
        {
            serial = BinaryPrimitives.ReadUInt32LittleEndian(sector.Slice(serialOffset, 4));

            label = Encoding.ASCII.GetString(sector.Slice(labelOffset, 11)).TrimEnd(' ');
        }

        Console.WriteLine("Generating uuid");
        Guid uuid = GenerateUuid(part, serial, label, boot.Type);

        result = new FilesystemProbeResult
        {
            Success = true,
            Filesystem = boot.Type switch
            {
                FatType.Fat12 => "fat12",
                FatType.Fat16 => "fat16",
                FatType.Fat32 => "fat32",
                _ => "fat"
            },
            Uuid = uuid,
            Label = label
        };


        Console.WriteLine("Returning with success");

        return true;
    }

    private static Guid GenerateUuid(Partition part, uint serial, string? label, FatType fatType)
    {
        using var sha256 = new Sha2_256();

        var data = new List<byte>();

        // Add partition start sector
        if (part.StartSector > 0)
            data.AddRange(BitConverter.GetBytes(part.StartSector));

        // Add partition size in sectors
        if (part.BlockCount > 0)
            data.AddRange(BitConverter.GetBytes(part.BlockCount));

        // Add serial numver
        data.AddRange(BitConverter.GetBytes(serial));

        // Add label
        if (!string.IsNullOrEmpty(label))
            data.AddRange(Encoding.ASCII.GetBytes(label));

        // Add filesystem type (FAT 12/16/32)
        data.Add((byte)fatType);

        // Add block size
        data.AddRange(BitConverter.GetBytes(part.BlockSize));

        // hash the data
        byte[] hash = sha256.ComputeHash([.. data]);

        Span<byte> uuidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(uuidBytes);

        // Set version to 4
        uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x40);

        // Set variant to RFC 4122
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);

        return new Guid(uuidBytes);
    }
}
