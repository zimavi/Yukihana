// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.Compression.Archives;

public sealed class CpioArchivator : IArchivator
{
    public ArchiveKind Kind => ArchiveKind.Cpio;

    public bool CanRead(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6)
            return false;

        string magic = Encoding.ASCII.GetString(data.Slice(0, 6));
        return magic is "070701" or "070702" or "070707";
    }

    public ArchiveImage Read(ReadOnlySpan<byte> data)
    {
        var image = new ArchiveImage { Kind = ArchiveKind.Cpio };
        int offset = 0;

        var hardLinkTargets = new Dictionary<ArchiveHardLinkKey, string>();
        var hardLinkData = new Dictionary<ArchiveHardLinkKey, byte[]>();

        while (offset + 6 <= data.Length)
        {
            string magic = Encoding.ASCII.GetString(data.Slice(offset, 6));

            if (magic == "070701" || magic == "070702")
            {
                bool crc = magic == "070702";
                if (!ReadNewcOrCrc(data, ref offset, crc, image, hardLinkTargets, hardLinkData))
                    break;
                continue;
            }

            if (magic == "070707")
            {
                if (!ReadOdc(data, ref offset, image, hardLinkTargets, hardLinkData))
                    break;
                continue;
            }

            if (CpioIsAllZero(data.Slice(offset)))
                break;

            throw new InvalidDataException($"Unknown CPIO magic '{magic}' at offset {offset}.");
        }

        return image;
    }

    public byte[] Write(ArchiveImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var ms = new MemoryStream();

        var hardLinkGroups = image.Entries
            .Where(e => e.HardLinkKey is not null)
            .GroupBy(e => e.HardLinkKey!)
            .ToDictionary(g => g.Key, g => g.Count());

        var writtenRoots = new HashSet<ArchiveHardLinkKey>();
        uint nextInode = 1;

        foreach (var entry in image.Entries)
        {
            WriteNewcEntry(ms, entry, hardLinkGroups, writtenRoots, ref nextInode);
        }

        WriteNewcTrailer(ms);
        return ms.ToArray();
    }

    private static bool ReadNewcOrCrc(
        ReadOnlySpan<byte> data,
        ref int offset,
        bool validateCrc,
        ArchiveImage image,
        Dictionary<ArchiveHardLinkKey, string> hardLinkTargets,
        Dictionary<ArchiveHardLinkKey, byte[]> hardLinkData)
    {
        const int headerSize = 110;

        if (offset + headerSize > data.Length)
            throw new InvalidDataException("Truncated CPIO newc/crc header.");

        int p = offset + 6;

        uint ino = CpioReadHexUInt32(data, p, 8); p += 8;
        uint mode = CpioReadHexUInt32(data, p, 8); p += 8;
        uint uid = CpioReadHexUInt32(data, p, 8); p += 8;
        uint gid = CpioReadHexUInt32(data, p, 8); p += 8;
        uint nlink = CpioReadHexUInt32(data, p, 8); p += 8;
        uint mtime = CpioReadHexUInt32(data, p, 8); p += 8;
        long fileSize = CpioReadHexUInt32(data, p, 8); p += 8;
        uint devMajor = CpioReadHexUInt32(data, p, 8); p += 8;
        uint devMinor = CpioReadHexUInt32(data, p, 8); p += 8;
        uint rdevMajor = CpioReadHexUInt32(data, p, 8); p += 8;
        uint rdevMinor = CpioReadHexUInt32(data, p, 8); p += 8;
        long nameSize = CpioReadHexUInt32(data, p, 8); p += 8;
        uint check = CpioReadHexUInt32(data, p, 8);

        offset += headerSize;

        if (nameSize <= 0 || nameSize > int.MaxValue)
            throw new InvalidDataException($"Invalid CPIO filename size: {nameSize}.");

        if (offset + nameSize > data.Length)
            throw new InvalidDataException("Truncated CPIO filename.");

        string name = CpioReadAscii(data, offset, (int)nameSize).TrimEnd('\0');
        offset = ArchivePath.Align(offset + (int)nameSize, 4);

        if (name == "TRAILER!!!")
            return false;

        if (fileSize < 0 || fileSize > int.MaxValue || offset + fileSize > data.Length)
            throw new InvalidDataException($"Truncated CPIO payload for '{name}'.");

        byte[] payload = fileSize == 0 ? Array.Empty<byte>() : ArchivePath.CopyBytes(data, offset, (int)fileSize);

        if (validateCrc)
        {
            uint sum = 0;
            for (int i = 0; i < payload.Length; i++)
                sum += payload[i];

            if (sum != check)
                throw new InvalidDataException($"CPIO checksum mismatch for '{name}'.");
        }

        offset = ArchivePath.Align(offset + (int)fileSize, 4);

        string path = ArchivePath.Normalize(name);
        var key = new ArchiveHardLinkKey(devMajor, devMinor, ino);
        ArchiveEntry entry;

        switch (mode & 0xF000)
        {
            case 0x4000:
                entry = new ArchiveEntry
                {
                    Path = path,
                    Kind = ArchiveEntryKind.Directory,
                    Data = Array.Empty<byte>(),
                    Mode = (int)(mode & 0x1FF),
                    UserId = (int)uid,
                    GroupId = (int)gid,
                    ModifiedUnixTimeSeconds = mtime,
                };
                break;

            case 0xA000:
                entry = new ArchiveEntry
                {
                    Path = path,
                    Kind = ArchiveEntryKind.SymbolicLink,
                    Data = [],
                    LinkTarget = ArchivePath.NormalizeLinkTarget(CpioReadAscii(payload, 0, payload.Length).TrimEnd('\0')),
                    Mode = (int)(mode & 0x1FF),
                    UserId = (int)uid,
                    GroupId = (int)gid,
                    ModifiedUnixTimeSeconds = mtime,
                };
                break;

            case 0x8000:
            default:
                if (nlink > 1)
                {
                    if (!hardLinkTargets.TryGetValue(key, out string? firstPath))
                    {
                        hardLinkTargets[key] = path;
                        hardLinkData[key] = payload;

                        entry = new ArchiveEntry
                        {
                            Path = path,
                            Kind = ArchiveEntryKind.File,
                            Data = payload,
                            HardLinkKey = key,
                            Mode = (int)(mode & 0x1FF),
                            UserId = (int)uid,
                            GroupId = (int)gid,
                            ModifiedUnixTimeSeconds = mtime,
                        };
                    }
                    else
                    {
                        entry = new ArchiveEntry
                        {
                            Path = path,
                            Kind = ArchiveEntryKind.HardLink,
                            Data = [],
                            LinkTarget = firstPath,
                            HardLinkKey = key,
                            Mode = (int)(mode & 0x1FF),
                            UserId = (int)uid,
                            GroupId = (int)gid,
                            ModifiedUnixTimeSeconds = mtime,
                        };
                    }
                }
                else
                {
                    entry = new ArchiveEntry
                    {
                        Path = path,
                        Kind = ArchiveEntryKind.File,
                        Data = payload,
                        Mode = (int)(mode & 0x1FF),
                        UserId = (int)uid,
                        GroupId = (int)gid,
                        ModifiedUnixTimeSeconds = mtime,
                    };
                }
                break;
        }

        image.Add(entry);
        return true;
    }

    private static bool ReadOdc(
        ReadOnlySpan<byte> data,
        ref int offset,
        ArchiveImage image,
        Dictionary<ArchiveHardLinkKey, string> hardLinkTargets,
        Dictionary<ArchiveHardLinkKey, byte[]> hardLinkData)
    {
        const int headerSize = 76;

        if (offset + headerSize > data.Length)
            throw new InvalidDataException("Truncated CPIO odc header.");

        int p = offset + 6;

        uint dev = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint ino = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint mode = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint uid = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint gid = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint nlink = CpioReadOctalUInt32(data, p, 6); p += 6;
        uint rdev = CpioReadOctalUInt32(data, p, 6); p += 6;
        long mtime = CpioReadOctalUInt32(data, p, 11); p += 11;
        long nameSize = CpioReadOctalUInt32(data, p, 6); p += 6;
        long fileSize = CpioReadOctalUInt32(data, p, 11);

        offset += headerSize;

        if (nameSize <= 0 || nameSize > int.MaxValue)
            throw new InvalidDataException($"Invalid CPIO filename size: {nameSize}.");

        if (offset + nameSize > data.Length)
            throw new InvalidDataException("Truncated CPIO filename.");

        string name = CpioReadAscii(data, offset, (int)nameSize).TrimEnd('\0');
        offset = ArchivePath.Align(offset + (int)nameSize, 2);

        if (name == "TRAILER!!!")
            return false;

        if (fileSize < 0 || fileSize > int.MaxValue || offset + fileSize > data.Length)
            throw new InvalidDataException($"Truncated CPIO payload for '{name}'.");

        byte[] payload = fileSize == 0 ? [] : ArchivePath.CopyBytes(data, offset, (int)fileSize);
        offset = ArchivePath.Align(offset + (int)fileSize, 2);

        string path = ArchivePath.Normalize(name);
        var key = new ArchiveHardLinkKey(dev, 0, ino);
        ArchiveEntry entry;

        switch (mode & 0xF000)
        {
            case 0x4000:
                entry = new ArchiveEntry
                {
                    Path = path,
                    Kind = ArchiveEntryKind.Directory,
                    Data = [],
                    Mode = (int)(mode & 0x1FF),
                    UserId = (int)uid,
                    GroupId = (int)gid,
                    ModifiedUnixTimeSeconds = mtime,
                };
                break;

            case 0xA000:
                entry = new ArchiveEntry
                {
                    Path = path,
                    Kind = ArchiveEntryKind.SymbolicLink,
                    Data = [],
                    LinkTarget = ArchivePath.NormalizeLinkTarget(CpioReadAscii(payload, 0, payload.Length).TrimEnd('\0')),
                    Mode = (int)(mode & 0x1FF),
                    UserId = (int)uid,
                    GroupId = (int)gid,
                    ModifiedUnixTimeSeconds = mtime,
                };
                break;

            case 0x8000:
            default:
                if (nlink > 1)
                {
                    if (!hardLinkTargets.TryGetValue(key, out string? firstPath))
                    {
                        hardLinkTargets[key] = path;
                        hardLinkData[key] = payload;

                        entry = new ArchiveEntry
                        {
                            Path = path,
                            Kind = ArchiveEntryKind.File,
                            Data = payload,
                            HardLinkKey = key,
                            Mode = (int)(mode & 0x1FF),
                            UserId = (int)uid,
                            GroupId = (int)gid,
                            ModifiedUnixTimeSeconds = mtime,
                        };
                    }
                    else
                    {
                        entry = new ArchiveEntry
                        {
                            Path = path,
                            Kind = ArchiveEntryKind.HardLink,
                            Data = [],
                            LinkTarget = firstPath,
                            HardLinkKey = key,
                            Mode = (int)(mode & 0x1FF),
                            UserId = (int)uid,
                            GroupId = (int)gid,
                            ModifiedUnixTimeSeconds = mtime,
                        };
                    }
                }
                else
                {
                    entry = new ArchiveEntry
                    {
                        Path = path,
                        Kind = ArchiveEntryKind.File,
                        Data = payload,
                        Mode = (int)(mode & 0x1FF),
                        UserId = (int)uid,
                        GroupId = (int)gid,
                        ModifiedUnixTimeSeconds = mtime,
                    };
                }
                break;
        }

        image.Add(entry);
        return true;
    }

    private static void WriteNewcEntry(
        Stream output,
        ArchiveEntry entry,
        Dictionary<ArchiveHardLinkKey, int> hardLinkGroups,
        HashSet<ArchiveHardLinkKey> writtenRoots,
        ref uint nextInode)
    {
        const uint iFREG = 0x8000;
        const uint iFDIR = 0x4000;
        const uint iFLNK = 0xA000;

        string path = ArchivePath.Normalize(entry.Path);
        byte[] payload = [];
        uint mode = (uint)(entry.Mode & 0x1FF);
        uint typeBits = entry.Kind switch
        {
            ArchiveEntryKind.Directory => iFDIR,
            ArchiveEntryKind.SymbolicLink => iFLNK,
            _ => iFREG,
        };

        uint ino;
        uint devMajor;
        uint devMinor;
        uint nlink;

        if (entry.HardLinkKey is ArchiveHardLinkKey key)
        {
            ino = key.Inode;
            devMajor = key.DeviceMajor;
            devMinor = key.DeviceMinor;
            nlink = (uint)Math.Max(1, hardLinkGroups.TryGetValue(key, out int count) ? count : 1);
        }
        else
        {
            ino = nextInode++;
            devMajor = 0;
            devMinor = 0;
            nlink = 1;
        }

        payload = entry.Kind switch
        {
            ArchiveEntryKind.Directory => [],
            ArchiveEntryKind.SymbolicLink => Encoding.ASCII.GetBytes(entry.LinkTarget ?? string.Empty),
            ArchiveEntryKind.HardLink => [],
            _ => entry.Data ?? [],
        };
        if (entry.Kind == ArchiveEntryKind.File && entry.HardLinkKey is not null)
        {
            if (!writtenRoots.Contains(entry.HardLinkKey))
                writtenRoots.Add(entry.HardLinkKey);
        }

        byte[] header = new byte[110];
        TarLikeFill(header, (byte)'0');

        CpioWriteAscii(header, 0, 6, "070701");
        CpioWriteHex(header, 6, 8, ino);
        CpioWriteHex(header, 14, 8, typeBits | mode);
        CpioWriteHex(header, 22, 8, (uint)entry.UserId);
        CpioWriteHex(header, 30, 8, (uint)entry.GroupId);
        CpioWriteHex(header, 38, 8, nlink);
        CpioWriteHex(header, 46, 8, (uint)Math.Max(0, entry.ModifiedUnixTimeSeconds));
        CpioWriteHex(header, 54, 8, (uint)payload.Length);
        CpioWriteHex(header, 62, 8, devMajor);
        CpioWriteHex(header, 70, 8, devMinor);
        CpioWriteHex(header, 78, 8, 0);
        CpioWriteHex(header, 86, 8, 0);
        CpioWriteHex(header, 94, 8, (uint)(ArchivePath.Normalize(path).Length + 1));
        CpioWriteHex(header, 102, 8, 0);

        output.Write(header, 0, header.Length);

        byte[] nameBytes = Encoding.ASCII.GetBytes(path);
        output.Write(nameBytes, 0, nameBytes.Length);
        output.WriteByte(0);

        int namePad = ArchivePath.Align(nameBytes.Length + 1, 4) - (nameBytes.Length + 1);
        if (namePad > 0)
            output.Write(new byte[namePad], 0, namePad);

        if (payload.Length > 0)
        {
            output.Write(payload, 0, payload.Length);
            int pad = ArchivePath.Align(payload.Length, 4) - payload.Length;
            if (pad > 0)
                output.Write(new byte[pad], 0, pad);
        }
    }

    private static void WriteNewcTrailer(Stream output)
    {
        byte[] header = new byte[110];
        TarLikeFill(header, (byte)'0');

        CpioWriteAscii(header, 0, 6, "070701");
        for (int i = 6; i < 110; i += 8)
            CpioWriteHex(header, i, 8, 0);

        CpioWriteHex(header, 94, 8, 11);

        output.Write(header, 0, header.Length);

        byte[] trailerName = Encoding.ASCII.GetBytes("TRAILER!!!");
        output.Write(trailerName, 0, trailerName.Length);
        output.WriteByte(0);

        int pad = ArchivePath.Align(trailerName.Length + 1, 4) - (trailerName.Length + 1);
        if (pad > 0)
            output.Write(new byte[pad], 0, pad);
    }

    private static string CpioReadAscii(ReadOnlySpan<byte> buffer, int offset, int count)
    {
        return Encoding.ASCII.GetString(buffer.Slice(offset, count));
    }

    private static uint CpioReadHexUInt32(ReadOnlySpan<byte> buffer, int offset, int count)
    {
        ulong value = 0;

        for (int i = 0; i < count; i++)
        {
            byte b = buffer[offset + i];
            if (b == 0 || b == (byte)' ')
                continue;

            uint digit;
            if (b >= (byte)'0' && b <= (byte)'9')
                digit = (uint)(b - (byte)'0');
            else if (b >= (byte)'a' && b <= (byte)'f')
                digit = (uint)(10 + (b - (byte)'a'));
            else if (b >= (byte)'A' && b <= (byte)'F')
                digit = (uint)(10 + (b - (byte)'A'));
            else
                throw new InvalidDataException($"Invalid hex digit '{(char)b}' in CPIO header.");

            value = (value << 4) | digit;
        }

        if (value > uint.MaxValue)
            throw new InvalidDataException("CPIO field exceeds UInt32.");

        return (uint)value;
    }

    private static uint CpioReadOctalUInt32(ReadOnlySpan<byte> buffer, int offset, int count)
    {
        ulong value = 0;

        for (int i = 0; i < count; i++)
        {
            byte b = buffer[offset + i];
            if (b == 0 || b == (byte)' ')
                continue;

            if (b < (byte)'0' || b > (byte)'7')
                throw new InvalidDataException($"Invalid octal digit '{(char)b}' in CPIO header.");

            value = (value << 3) | (uint)(b - (byte)'0');
        }

        if (value > uint.MaxValue)
            throw new InvalidDataException("CPIO field exceeds UInt32.");

        return (uint)value;
    }

    private static bool CpioIsAllZero(ReadOnlySpan<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0)
                return false;
        }

        return true;
    }

    private static void CpioWriteAscii(byte[] buffer, int offset, int length, string value)
    {
        Array.Fill(buffer, (byte)'0', offset, length);
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        int count = Math.Min(length, bytes.Length);
        Array.Copy(bytes, 0, buffer, offset, count);
    }

    private static void CpioWriteHex(byte[] buffer, int offset, int length, uint value)
    {
        string hex = value.ToString("X");
        if (hex.Length > length)
            throw new InvalidDataException($"Value {value} does not fit in CPIO field of size {length}.");

        int pad = length - hex.Length;
        for (int i = 0; i < pad; i++)
            buffer[offset + i] = (byte)'0';

        for (int i = 0; i < hex.Length; i++)
            buffer[offset + pad + i] = (byte)hex[i];
    }

    private static void TarLikeFill(byte[] buffer, byte value)
    {
        Array.Fill(buffer, value);
    }
}