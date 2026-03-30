using System.Text;

namespace Yukihana.Core.IO.RamFS;

public static class RamFsArchive
{
    public static RamFs LoadFromArchive(byte[] archiveBytes)
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);

        ShellPrint.InfoK($"archive bytes: {archiveBytes.Length}", "ramfs");

        byte[] tarBytes = DecompressGZipStoredDeflate(archiveBytes);

        ShellPrint.InfoK($"tar bytes: {tarBytes.Length}", "ramfs");

        var files = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);
        using var flatBlob = new MemoryStream();

        int offset = 0;
        int fileCount = 0;

        while (offset + 512 <= tarBytes.Length)
        {
            if (IsAllZero(tarBytes, offset, 512))
            {
                ShellPrint.InfoK($"end marker reached at block {offset / 512}", "ramfs.tar");
                break;
            }

            string name = ReadAsciiString(tarBytes, offset + 0, 100);
            string prefix = ReadAsciiString(tarBytes, offset + 345, 155);
            char typeFlag = (char)tarBytes[offset + 156];
            long sizeLong = ReadOctal(tarBytes, offset + 124, 12);

            if (sizeLong < 0 || sizeLong > int.MaxValue)
                throw new InvalidDataException($"File too large for RamFs loader: {sizeLong} bytes");

            int size = (int)sizeLong;
            string fullName = string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
            string path = Normalize(fullName);

            offset += 512;

            if (typeFlag == '5')
            {
                ShellPrint.InfoK("directory  : {path}", "ramfs.tar");
                offset = Align512(offset);
                continue;
            }

            if (typeFlag != '\0' && typeFlag != '0')
            {
                ShellPrint.WarnK($"skip type '{typeFlag}'  : {path} ({size} bytes)");
                offset = Align512(offset + size);
                continue;
            }

            if (offset + size > tarBytes.Length)
                throw new InvalidDataException($"TAR entry overruns archive: {path}");
            
            int blobOffset = (int)flatBlob.Length;
            flatBlob.Write(tarBytes, offset, size);

            files[path] = (blobOffset, size);
            fileCount++;

            ShellPrint.InfoK($"file       : {path} -> offset={blobOffset}, length={size}", "ramfs.tar");

            offset = Align512(offset + size);
        }

        ShellPrint.OkK($"files loaded: {fileCount}", "ramfs.tar");
        return new RamFs(flatBlob.ToArray(), files);
    }

    private static byte[] DecompressGZipStoredDeflate(byte[] gzipBytes)
    {
        if (gzipBytes.Length < 18)
            throw new InvalidDataException("Input is too short to be a valid gzip stream.");
        
        if (gzipBytes[0] != 0x1F | gzipBytes[1] != 0x8B)
            throw new InvalidDataException("Invalid gzip magic.");
        
        if (gzipBytes[2] != 8)
            throw new InvalidDataException("Usupported gzip compression method.");
        
        int flags = gzipBytes[3];
        int pos = 10;

        if ((flags & 0x04) != 0)
        {
            if (pos + 2 > gzipBytes.Length)
                throw new InvalidDataException("Corrupt gzip extra field.");
            
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
            throw new InvalidDataException("Corrupt gzip stream.");
        
        ShellPrint.InfoK($"deflate payload begins at {pos}, footer at {footerPos}", "ramfs.gzip");

        var reader = new BitReader(gzipBytes, pos, footerPos);
        using var output = new MemoryStream();

        bool finalBlock;
        do
        {
            finalBlock = reader.ReadBits(1) != 0;
            int blockType = reader.ReadBits(2);

            if (blockType != 0)
                throw new InvalidDataException("This loader currently only supports only stored DEFLATE blocks.");
            
            reader.AlignToByte();

            ushort len = (ushort)reader.ReadBits(16);
            ushort nlen = (ushort)reader.ReadBits(16);

            if((ushort)~len != nlen)
                throw new InvalidDataException("Stored DEFLATE block length check failed.");
            
            reader.CopyBytesTo(output, len);

            ShellPrint.InfoK($"stored block: {len} bytes, final={finalBlock}", "ramfs.gzip");
        } while (!finalBlock);

        byte[] result = output.ToArray();

        uint expectedCrc = ReadUInt32LE(gzipBytes, footerPos + 0);
        uint expectedSize = ReadUInt32LE(gzipBytes, footerPos + 4);

        if (expectedSize != (uint)result.Length)
            throw new InvalidDataException($"GZip size footer mismatch. Expected {expectedSize}, got {result.Length}.");
        
        uint actualCrc = Crc32(result);
        if (actualCrc != expectedCrc)
            throw new InvalidDataException($"GZip CRC mismatch. Expected 0x{expectedCrc:X8}, got 0x{actualCrc:X8}.");
        
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