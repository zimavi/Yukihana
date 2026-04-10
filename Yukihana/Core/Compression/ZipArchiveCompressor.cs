// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;
using System.Text;

namespace Yukihana.Core.Compression;

public sealed class ZipArchiveCompressor : IArchiveCompressor
{
    private const uint LOCAL_FILE_HEADER_SIGNATURE = 0x04034B50;
    private const uint CENTRAL_DIRECTORY_HEADER_SIGNATURE = 0x02014B50;
    private const uint END_OF_CENTRAL_DIRECTORY_SIGNATURE = 0x06054B50;
    private const ushort COMPRESSION_METHOD_STORED = 0;
    private const ushort VERSION_NEEDED = 20;
    private const ushort VERSION_MADE_BY = 20;

    public bool IsSupportedFormat(byte[] data)
    {
        return data != null &&
               data.Length >= 4 &&
               ReadUInt32LE(data, 0) == LOCAL_FILE_HEADER_SIGNATURE;
    }

    public byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        const string fileName = "data.bin";
        byte[] fileNameBytes = Encoding.ASCII.GetBytes(fileName);
        uint crc32 = Crc32.Compute(data);
        uint size = (uint)data.Length;

        using var ms = new MemoryStream();

        long localHeaderOffset = ms.Position;

        // Local file header
        WriteUInt32LE(ms, LOCAL_FILE_HEADER_SIGNATURE);
        WriteUInt16LE(ms, VERSION_NEEDED);
        WriteUInt16LE(ms, 0); // flags
        WriteUInt16LE(ms, COMPRESSION_METHOD_STORED);
        WriteUInt16LE(ms, 0); // mod time
        WriteUInt16LE(ms, 0); // mod date
        WriteUInt32LE(ms, crc32);
        WriteUInt32LE(ms, size);
        WriteUInt32LE(ms, size);
        WriteUInt16LE(ms, (ushort)fileNameBytes.Length);
        WriteUInt16LE(ms, 0); // extra length
        ms.Write(fileNameBytes, 0, fileNameBytes.Length);
        ms.Write(data, 0, data.Length);

        long centralDirectoryOffset = ms.Position;

        // Central directory header
        WriteUInt32LE(ms, CENTRAL_DIRECTORY_HEADER_SIGNATURE);
        WriteUInt16LE(ms, VERSION_MADE_BY);
        WriteUInt16LE(ms, VERSION_NEEDED);
        WriteUInt16LE(ms, 0); // flags
        WriteUInt16LE(ms, COMPRESSION_METHOD_STORED);
        WriteUInt16LE(ms, 0); // mod time
        WriteUInt16LE(ms, 0); // mod date
        WriteUInt32LE(ms, crc32);
        WriteUInt32LE(ms, size);
        WriteUInt32LE(ms, size);
        WriteUInt16LE(ms, (ushort)fileNameBytes.Length);
        WriteUInt16LE(ms, 0); // extra length
        WriteUInt16LE(ms, 0); // comment length
        WriteUInt16LE(ms, 0); // disk number start
        WriteUInt16LE(ms, 0); // internal attrs
        WriteUInt32LE(ms, 0); // external attrs
        WriteUInt32LE(ms, (uint)localHeaderOffset);
        ms.Write(fileNameBytes, 0, fileNameBytes.Length);

        long centralDirectoryEnd = ms.Position;
        uint centralDirectorySize = (uint)(centralDirectoryEnd - centralDirectoryOffset);

        // End of central directory
        WriteUInt32LE(ms, END_OF_CENTRAL_DIRECTORY_SIGNATURE);
        WriteUInt16LE(ms, 0); // number of this disk
        WriteUInt16LE(ms, 0); // disk with central directory
        WriteUInt16LE(ms, 1); // entries on this disk
        WriteUInt16LE(ms, 1); // total entries
        WriteUInt32LE(ms, centralDirectorySize);
        WriteUInt32LE(ms, (uint)centralDirectoryOffset);
        WriteUInt16LE(ms, 0); // comment length

        return ms.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (!IsSupportedFormat(data)) throw new NotSupportedException("Input is not a supported ZIP stream.");

        int pos = 0;

        uint sig = ReadUInt32LE(data, pos);
        if (sig != LOCAL_FILE_HEADER_SIGNATURE)
            throw new InvalidDataException("Invalid ZIP local file header.");

        pos += 4;

        ushort versionNeeded = ReadUInt16LE(data, pos); pos += 2;
        ushort flags = ReadUInt16LE(data, pos); pos += 2;
        ushort method = ReadUInt16LE(data, pos); pos += 2;
        pos += 2; // mod time
        pos += 2; // mod date

        uint crc32 = ReadUInt32LE(data, pos); pos += 4;
        uint compressedSize = ReadUInt32LE(data, pos); pos += 4;
        uint uncompressedSize = ReadUInt32LE(data, pos); pos += 4;

        ushort nameLength = ReadUInt16LE(data, pos); pos += 2;
        ushort extraLength = ReadUInt16LE(data, pos); pos += 2;

        if (versionNeeded > VERSION_NEEDED)
        {
            // Not a hard failure, but keep the parser conservative.
        }

        if ((flags & 0x0008) != 0)
            throw new NotSupportedException("ZIP data descriptor entries are not supported by this minimal pure-C# implementation.");

        EnsureAvailable(data, pos, nameLength + extraLength);
        string fileName = Encoding.ASCII.GetString(data, pos, nameLength);
        pos += nameLength;
        pos += extraLength;

        EnsureAvailable(data, pos, checked((int)compressedSize));
        byte[] content = new byte[compressedSize];
        Buffer.BlockCopy(data, pos, content, 0, (int)compressedSize);
        pos += (int)compressedSize;

        if (method != COMPRESSION_METHOD_STORED)
            throw new NotSupportedException($"ZIP compression method {method} is not supported by this implementation.");

        byte[] result = content;

        if (result.Length != (int)uncompressedSize)
            throw new InvalidDataException("ZIP uncompressed size mismatch.");

        uint actualCrc = Crc32.Compute(result);
        if (actualCrc != crc32)
            throw new InvalidDataException("ZIP CRC32 mismatch.");

        return result;
    }

    private static void EnsureAvailable(byte[] data, int pos, int count)
    {
        if (count < 0 || pos < 0 || pos > data.Length - count)
            throw new InvalidDataException("Truncated input.");
    }

    private static ushort ReadUInt16LE(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    private static uint ReadUInt32LE(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    private static void WriteUInt16LE(Stream stream, ushort value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buf, value);
        stream.Write(buf);
    }

    private static void WriteUInt32LE(Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
        stream.Write(buf);
    }
}