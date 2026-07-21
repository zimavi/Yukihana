// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.Compression.Archives;

public sealed class TarArchivator : IArchivator
{
    public ArchiveKind Kind => ArchiveKind.Tar;

    public bool CanRead(ReadOnlySpan<byte> data)
    {
        if (data.Length < 512)
        {
            return false;
        }

        if (TarIsZeroBlock(data[..512]))
        {
            return true;
        }

        return TarLooksLikeTarHeader(data);
    }

    public ArchiveImage Read(ReadOnlySpan<byte> data)
    {
        var image = new ArchiveImage { Kind = ArchiveKind.Tar };
        int offset = 0;

        string? pendingName = null;
        string? pendingLinkName = null;

        while (offset + 512 <= data.Length)
        {
            ReadOnlySpan<byte> header = data.Slice(offset, 512);

            if (TarIsZeroBlock(header))
            {
                break;
            }

            if (!TarCheckHeaderChecksum(header))
            {
                throw new InvalidDataException($"Invalid TAR checksum at offset {offset}.");
            }

            string name = TarReadString(header, 0, 100);
            int mode = (int)TarReadOctal(header, 100, 8);
            int uid = (int)TarReadOctal(header, 108, 8);
            int gid = (int)TarReadOctal(header, 116, 8);
            long size = (long)TarReadOctal(header, 124, 12);
            long mtime = (long)TarReadOctal(header, 136, 12);
            char typeFlag = (char)header[156];
            string linkName = TarReadString(header, 157, 100);
            string prefix = TarReadString(header, 345, 155);
            string magic = TarReadString(header, 257, 6);

            offset += 512;

            if (!string.IsNullOrEmpty(pendingName))
            {
                name = pendingName;
                pendingName = null;
            }

            if (!string.IsNullOrEmpty(pendingLinkName))
            {
                linkName = pendingLinkName;
                pendingLinkName = null;
            }

            string fullPath = ArchivePath.Normalize(string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}");

            if (typeFlag == 'L')
            {
                if (size < 0 || size > int.MaxValue || offset + size > data.Length)
                {
                    throw new InvalidDataException("Invalid TAR long name entry.");
                }

                pendingName = ArchivePath.Normalize(TarReadString(data.Slice(offset, (int)size), 0, (int)size).TrimEnd('\0'));
                offset = ArchivePath.Align(offset + (int)size, 512);
                continue;
            }

            if (typeFlag == 'K')
            {
                if (size < 0 || size > int.MaxValue || offset + size > data.Length)
                {
                    throw new InvalidDataException("Invalid TAR long link entry.");
                }

                pendingLinkName = ArchivePath.Normalize(TarReadString(data.Slice(offset, (int)size), 0, (int)size).TrimEnd('\0'));
                offset = ArchivePath.Align(offset + (int)size, 512);
                continue;
            }

            if (typeFlag == 'x' || typeFlag == 'g')
            {
                offset = ArchivePath.Align(offset + (int)size, 512);
                continue;
            }

            if (size < 0 || size > int.MaxValue || offset + size > data.Length)
            {
                throw new InvalidDataException($"Truncated TAR payload for '{fullPath}'.");
            }

            byte[] payload = size == 0 ? Array.Empty<byte>() : ArchivePath.CopyBytes(data, offset, (int)size);
            ArchiveEntry entry = typeFlag switch
            {
                '5' => new ArchiveEntry
                {
                    Path = fullPath,
                    Kind = ArchiveEntryKind.Directory,
                    Data = [],
                    Mode = mode,
                    UserId = uid,
                    GroupId = gid,
                    ModifiedUnixTimeSeconds = mtime,
                },
                '2' => new ArchiveEntry
                {
                    Path = fullPath,
                    Kind = ArchiveEntryKind.SymbolicLink,
                    Data = [],
                    LinkTarget = ArchivePath.NormalizeLinkTarget(linkName),
                    Mode = mode,
                    UserId = uid,
                    GroupId = gid,
                    ModifiedUnixTimeSeconds = mtime,
                },
                '1' => new ArchiveEntry
                {
                    Path = fullPath,
                    Kind = ArchiveEntryKind.HardLink,
                    Data = [],
                    LinkTarget = ArchivePath.Normalize(linkName),
                    Mode = mode,
                    UserId = uid,
                    GroupId = gid,
                    ModifiedUnixTimeSeconds = mtime,
                },
                _ => new ArchiveEntry
                {
                    Path = fullPath,
                    Kind = ArchiveEntryKind.File,
                    Data = payload,
                    Mode = mode,
                    UserId = uid,
                    GroupId = gid,
                    ModifiedUnixTimeSeconds = mtime,
                },
            };
            image.Add(entry);
            offset = ArchivePath.Align(offset + (int)size, 512);
        }

        return image;
    }

    public byte[] Write(ArchiveImage image)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        using var ms = new MemoryStream();

        foreach (ArchiveEntry entry in image.Entries)
        {
            WriteEntry(ms, entry);
        }

        ms.Write(new byte[512], 0, 512);
        return ms.ToArray();
    }

    private static void WriteEntry(Stream output, ArchiveEntry entry)
    {
        string normalizedPath = ArchivePath.Normalize(entry.Path);
        TarSplitPath(normalizedPath, out string name, out string prefix);

        byte[] header = new byte[512];
        Array.Fill(header, (byte)' ');

        TarWriteString(header, 0, 100, name);
        TarWriteOctal(header, 100, 8, entry.Mode & 0x1FF);
        TarWriteOctal(header, 108, 8, entry.UserId);
        TarWriteOctal(header, 116, 8, entry.GroupId);

        byte typeFlag = entry.Kind switch
        {
            ArchiveEntryKind.Directory => (byte)'5',
            ArchiveEntryKind.SymbolicLink => (byte)'2',
            ArchiveEntryKind.HardLink => (byte)'1',
            _ => (byte)'0',
        };

        byte[] payload = [];
        string? linkTarget = entry.LinkTarget;

        if (entry.Kind == ArchiveEntryKind.File)
        {
            payload = entry.Data ?? [];
        }
        else if (entry.Kind == ArchiveEntryKind.SymbolicLink)
        {
            payload = [];
            linkTarget ??= string.Empty;
        }
        else
        {
            payload = [];
        }

        TarWriteOctal(header, 124, 12, payload.LongLength);
        TarWriteOctal(header, 136, 12, entry.ModifiedUnixTimeSeconds);
        header[156] = typeFlag;
        TarWriteString(header, 157, 100, linkTarget ?? string.Empty);
        TarWriteString(header, 257, 6, "ustar");
        header[263] = (byte)'0';
        header[264] = (byte)'0';
        TarWriteString(header, 265, 32, string.Empty);
        TarWriteString(header, 297, 32, string.Empty);
        TarWriteString(header, 345, 155, prefix);

        for (int i = 148; i < 156; i++)
        {
            header[i] = (byte)' ';
        }

        long checksum = 0;
        for (int i = 0; i < header.Length; i++)
        {
            checksum += header[i];
        }

        TarWriteChecksum(header, 148, 8, checksum);
        output.Write(header, 0, header.Length);

        if (payload.Length > 0)
        {
            output.Write(payload, 0, payload.Length);
            int pad = ArchivePath.Align(payload.Length, 512) - payload.Length;
            if (pad > 0)
            {
                output.Write(new byte[pad], 0, pad);
            }
        }
    }

    private static bool TarLooksLikeTarHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < 512)
        {
            return false;
        }

        string magic = TarReadString(header, 257, 6);
        if (magic.StartsWith("ustar", StringComparison.Ordinal))
        {
            return TarCheckHeaderChecksum(header.Slice(0, 512));
        }

        return TarCheckHeaderChecksum(header.Slice(0, 512));
    }

    private static bool TarCheckHeaderChecksum(ReadOnlySpan<byte> header)
    {
        if (header.Length < 512)
        {
            return false;
        }

        long stored = TarReadOctal(header, 148, 8);

        long computed = 0;
        for (int i = 0; i < 512; i++)
        {
            computed += (i >= 148 && i < 156) ? 32 : header[i];
        }

        return stored == computed;
    }

    private static bool TarIsZeroBlock(ReadOnlySpan<byte> block)
    {
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string TarReadString(ReadOnlySpan<byte> buffer, int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + length, buffer.Length);

        int end = offset + length;
        while (end > offset && buffer[end - 1] == 0)
        {
            end--;
        }

        return Encoding.ASCII.GetString(buffer[offset..end]);
    }

    private static long TarReadOctal(ReadOnlySpan<byte> buffer, int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + length, buffer.Length);

        long value = 0;
        for (int i = 0; i < length; i++)
        {
            byte b = buffer[offset + i];
            if (b == 0 || b == (byte)' ')
            {
                continue;
            }

            if (b < (byte)'0' || b > (byte)'7')
            {
                break;
            }

            value = (value << 3) + (b - (byte)'0');
        }

        return value;
    }

    private static void TarWriteString(byte[] buffer, int offset, int length, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        int count = Math.Min(length, bytes.Length);
        Array.Copy(bytes, 0, buffer, offset, count);
        for (int i = count; i < length; i++)
        {
            buffer[offset + i] = 0;
        }
    }

    private static void TarWriteOctal(byte[] buffer, int offset, int length, long value)
    {
        if (length <= 0)
        {
            return;
        }

        string oct = Convert.ToString(value, 8) ?? "0";
        if (oct.Length + 1 > length)
        {
            throw new InvalidDataException($"Value {value} does not fit in octal field of size {length}.");
        }

        int pad = length - oct.Length - 1;
        for (int i = 0; i < pad; i++)
        {
            buffer[offset + i] = (byte)'0';
        }

        for (int i = 0; i < oct.Length; i++)
        {
            buffer[offset + pad + i] = (byte)oct[i];
        }

        buffer[offset + length - 1] = 0;
    }

    private static void TarWriteChecksum(byte[] buffer, int offset, int length, long checksum)
    {
        string oct = Convert.ToString(checksum, 8) ?? "0";
        if (oct.Length + 1 > length)
        {
            throw new InvalidDataException("Checksum does not fit into TAR field.");
        }

        int pad = length - oct.Length - 2;
        for (int i = 0; i < pad; i++)
        {
            buffer[offset + i] = (byte)'0';
        }

        for (int i = 0; i < oct.Length; i++)
        {
            buffer[offset + pad + i] = (byte)oct[i];
        }

        buffer[offset + length - 1] = (byte)' ';
    }

    private static void TarSplitPath(string path, out string name, out string prefix)
    {
        path = ArchivePath.Normalize(path);

        if (path.Length <= 100)
        {
            name = path;
            prefix = string.Empty;
            return;
        }

        int split = path.LastIndexOf('/');
        while (split > 0)
        {
            string candidateName = path[(split + 1)..];
            string candidatePrefix = path[..split];

            if (candidateName.Length <= 100 && candidatePrefix.Length <= 155)
            {
                name = candidateName;
                prefix = candidatePrefix;
                return;
            }

            split = path.LastIndexOf('/', split - 1);
        }

        throw new InvalidDataException($"Path '{path}' is too long for TAR USTAR header.");
    }
}
