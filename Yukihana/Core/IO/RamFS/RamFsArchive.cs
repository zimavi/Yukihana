// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.IO.Vfs.Backends;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.RamFS;

public static class RamFsArchive
{
    public static Result<RamFs, KernelError> LoadFromArchive(byte[] archiveBytes)
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);

        ShellPrint.InfoK($"archive bytes: {archiveBytes.Length}", "ramfs");

        var result = DecompressGZipStoredDeflate(archiveBytes);
        if (result.IsFailure)
            return Result<RamFs, KernelError>.Failure(result.Error);

        byte[] tarBytes = result.Value;

        ShellPrint.InfoK($"tar bytes: {tarBytes.Length}", "ramfs");

        var entries = new Dictionary<string, RamFsEntry>(StringComparer.Ordinal);
        using var flatBlob = new MemoryStream();

        int offset = 0;
        int fileCount = 0;
        int dirCount = 0;
        int symlinkCount = 0;

        while (offset + 512 <= tarBytes.Length)
        {
            if (IsAllZero(tarBytes, offset, 512))
            {
                ShellPrint.InfoK($"end marker reached at block {offset / 512}", "ramfs.tar");
                break;
            }

            string name = ReadAsciiString(tarBytes, offset + 0, 100);
            int mode = (int)ReadOctal(tarBytes, offset + 100, 8);
            int uid = (int)ReadOctal(tarBytes, offset + 108, 8);
            int gid = (int)ReadOctal(tarBytes, offset + 116, 8);
            long sizeLong = ReadOctal(tarBytes, offset + 124, 12);
            char typeFlag = (char)tarBytes[offset + 156];
            string linkName = ReadAsciiString(tarBytes, offset + 157, 100);
            string prefix = ReadAsciiString(tarBytes, offset + 347, 155);

            if (sizeLong < 0 || sizeLong > int.MaxValue)
                return Result<RamFs, KernelError>.Failure(KernelError.Corrupted($"File too large for RamFs loader: {sizeLong} bytes"));

            int size = (int)sizeLong;
            string fullName = string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
            string path = Normalize(fullName);

            offset += 512;

            if (string.IsNullOrEmpty(path))
            {
                offset = Align512(offset + size);
                continue;
            }

            EnsureParentDirectories(entries, path);

            if (typeFlag == '5')
            {
                var entry = new RamFsEntry(FsNodeKind.Directory)
                {
                    Permissions = ParsePermissions(mode, FsNodeKind.Directory),
                    UserId = uid,
                    GroupId = gid
                };

                entries[path] = entry;
                dirCount++;
                ShellPrint.InfoK($"directory  : {path} perms={FsPermissionUtil.ToSymbolicString(entry.Permissions)} uid={uid} gid={gid}", "ramfs.tar");
                offset = Align512(offset + size);
                continue;
            }

            if (typeFlag == '2' || typeFlag == '1')
            {
                var entry = new RamFsEntry(FsNodeKind.SymbolicLink)
                {
                    LinkTarget = FsPath.SanitizeSymlinkTarget(linkName),
                    Permissions = ParsePermissions(mode, FsNodeKind.SymbolicLink),
                    UserId = uid,
                    GroupId = gid
                };

                entries[path] = entry;
                symlinkCount++;
                ShellPrint.InfoK($"symlink    : {path} -> {entry.LinkTarget}", "ramfs.tar");
                offset = Align512(offset + size);
                continue;
            }

            if (typeFlag != '\0' && typeFlag != '0')
            {
                ShellPrint.WarnK($"skip type '{typeFlag}'  : {path} ({size} bytes)");
                offset = Align512(offset + size);
                continue;
            }

            if (offset + size > tarBytes.Length)
                return Result<RamFs, KernelError>.Failure(KernelError.Corrupted($"TAR entry overruns archive: {path}"));

            int blobOffset = (int)flatBlob.Length;
            flatBlob.Write(tarBytes, offset, size);

            entries[path] = new RamFsEntry(blobOffset, size)
            {
                Permissions = ParsePermissions(mode, FsNodeKind.File),
                UserId = uid,
                GroupId = gid
            };

            fileCount++;
            ShellPrint.InfoK($"file       : {path} -> offset={blobOffset}, length={size} perms={FsPermissionUtil.ToSymbolicString(entries[path].Permissions)}", "ramfs.tar");

            offset = Align512(offset + size);
        }

        return new RamFs(flatBlob.ToArray(), entries);
    }

    private static FsPermissions ParsePermissions(int mode, FsNodeKind kind)
    {
        int bits = mode & 0x1FF;

        if (bits == 0)
        {
            bits = kind switch
            {
                FsNodeKind.Directory => 755,
                FsNodeKind.SymbolicLink => 777,
                _ => 644
            };
        }

        return FsPermissionUtil.FromUnixMode(bits);
    }

    private static void EnsureParentDirectories(Dictionary<string, RamFsEntry> entries, string path)
    {
        string parent = FsPath.GetParent(path);
        if (string.IsNullOrEmpty(parent))
        {
            entries[string.Empty] = new RamFsEntry(FsNodeKind.Directory);
            return;
        }

        string current = string.Empty;
        foreach (var segment in FsPath.SplitRelative(parent))
        {
            string next = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;

            if (!entries.ContainsKey(next))
                entries[next] = new RamFsEntry(FsNodeKind.Directory);

            current = next;
        }
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();

        while (path.StartsWith("./", StringComparison.Ordinal))
            path = path[2..];

        while (path.StartsWith("/", StringComparison.Ordinal))
            path = path[1..];

        while (path.Contains("//", StringComparison.Ordinal))
            path = path.Replace("//", "/");

        return path;
    }

    private static Result<byte[], KernelError> DecompressGZipStoredDeflate(byte[] gzipBytes)
    {
        if (gzipBytes.Length < 18)
            throw new ArgumentException("Input is too short to be a valid gzip stream.");

        if (gzipBytes[0] != 0x1F || gzipBytes[1] != 0x8B)
            throw new ArgumentException("Invalid gzip magic.");

        if (gzipBytes[2] != 8)
            throw new ArgumentException("Unsupported gzip compression method.");

        int flags = gzipBytes[3];
        int pos = 10;

        if ((flags & 0x04) != 0)
        {
            if (pos + 2 > gzipBytes.Length)
                return Result<byte[], KernelError>.Failure(KernelError.Corrupted("Corrupt gzip extra field."));

            int xlen = gzipBytes[pos] | (gzipBytes[pos + 1] << 8);
            pos += 2 + xlen;
        }

        if ((flags & 0x08) != 0)
            pos = SkipNullTerminated(gzipBytes, pos);

        if ((flags & 0x10) != 0)
            pos = SkipNullTerminated(gzipBytes, pos);

        if ((flags & 0x02) != 0)
            pos += 2;

        int footerPos = gzipBytes.Length - 8;
        if (footerPos <= pos)
            return Result<byte[], KernelError>.Failure(KernelError.Corrupted("Corrupt gzip stream."));

        ShellPrint.InfoK($"deflate payload begins at {pos}, footer at {footerPos}", "ramfs.gzip");

        var reader = new BitReader(gzipBytes, pos, footerPos);
        using var output = new MemoryStream();

        bool finalBlock;
        do
        {
            finalBlock = reader.ReadBits(1) != 0;
            int blockType = reader.ReadBits(2);

            if (blockType != 0)
                return Result<byte[], KernelError>.Failure(KernelError.Corrupted("This loader currently only supports stored DEFLATE blocks."));

            reader.AlignToByte();

            ushort len = (ushort)reader.ReadBits(16);
            ushort nlen = (ushort)reader.ReadBits(16);

            if ((ushort)~len != nlen)
                return Result<byte[], KernelError>.Failure(KernelError.Corrupted("Stored DEFLATE block length check failed."));

            reader.CopyBytesTo(output, len);

            ShellPrint.InfoK($"stored block: {len} bytes, final={finalBlock}", "ramfs.gzip");
        } while (!finalBlock);

        byte[] result = output.ToArray();

        uint expectedCrc = ReadUInt32LE(gzipBytes, footerPos + 0);
        uint expectedSize = ReadUInt32LE(gzipBytes, footerPos + 4);

        if (expectedSize != (uint)result.Length)
            return Result<byte[], KernelError>.Failure(KernelError.Corrupted($"GZip size footer mismatch. Expected {expectedSize}, got {result.Length}."));

        uint actualCrc = Crc32(result);
        if (actualCrc != expectedCrc)
            return Result<byte[], KernelError>.Failure(KernelError.Corrupted($"GZip CRC mismatch. Expected 0x{expectedCrc:X8}, got 0x{actualCrc:X8}."));

        ShellPrint.OkK($"crc ok: 0x{actualCrc:X8}", "ramfs.gzip");
        return result;
    }

    private static int SkipNullTerminated(byte[] data, int pos)
    {
        while (pos < data.Length && data[pos] != 0)
            pos++;

        if (pos >= data.Length)
            throw new InvalidDataException("Corrupt gzip string field.");

        return pos + 1;
    }

    private static string ReadAsciiString(byte[] data, int offset, int length)
    {
        int end = offset;
        int max = offset + length;

        while (end < max && data[end] != 0)
            end++;

        return Encoding.ASCII.GetString(data, offset, end - offset);
    }

    private static long ReadOctal(byte[] data, int offset, int length)
    {
        long value = 0;
        bool started = false;

        for (int i = 0; i < length; i++)
        {
            byte b = data[offset + i];

            if (b == 0 || b == (byte)' ')
            {
                if (started)
                    break;

                continue;
            }

            if (b < (byte)'0' || b > (byte)'7')
                break;

            started = true;
            value = (value << 3) + (b - (byte)'0');
        }

        return value;
    }

    private static bool IsAllZero(byte[] data, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (data[offset + i] != 0)
                return false;
        }

        return true;
    }

    private static int Align512(int value) => (value + 511) & ~511;

    private static uint ReadUInt32LE(byte[] data, int offset)
    {
        return (uint)(data[offset + 0]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFFu;

        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];

            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320u;
                else
                    crc >>= 1;
            }
        }

        return ~crc;
    }

    private sealed class BitReader
    {
        private readonly byte[] _data;
        private readonly int _end;
        private int _bytePos;
        private int _bitPos;

        public BitReader(byte[] data, int start, int end)
        {
            _data = data;
            _bytePos = start;
            _end = end;
            _bitPos = 0;
        }

        public int ReadBits(int count)
        {
            if (count < 0 || count > 24)
                throw new ArgumentOutOfRangeException(nameof(count));

            int value = 0;

            for (int i = 0; i < count; i++)
            {
                if (_bytePos >= _end)
                    throw new InvalidDataException("Unexpected end of DEFLATE stream.");

                int bit = (_data[_bytePos] >> _bitPos) & 1;
                value |= bit << i;

                _bitPos++;
                if (_bitPos == 8)
                {
                    _bitPos = 0;
                    _bytePos++;
                }
            }

            return value;
        }

        public void AlignToByte()
        {
            if (_bitPos != 0)
            {
                _bitPos = 0;
                _bytePos++;
            }
        }

        public void CopyBytesTo(Stream output, int count)
        {
            AlignToByte();

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (_bytePos + count > _end)
                throw new InvalidDataException("Unexpected end of DEFLATE stored block.");

            output.Write(_data, _bytePos, count);
            _bytePos += count;
        }
    }
}