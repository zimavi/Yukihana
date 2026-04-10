// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;
using Yukihana.Core.IO;

namespace Yukihana.Core.Compression;

public sealed class GzipArchiveCompressor : IArchiveCompressor
{
    private const string SRC = "gzip";
    private const byte ID1 = 0x1F;
    private const byte ID2 = 0x8B;
    private const byte COMPRESSION_METHOD_DEFLATE = 8;
    private const byte FLAG_EXTRA = 1 << 2;
    private const byte FLAG_FNAME = 1 << 3;
    private const byte FLAG_FCOMMENT = 1 << 4;
    private const byte FLAG_FHCRC = 1 << 1;

    public bool IsSupportedFormat(byte[] data)
    {
        return data is not null &&
                data.Length >= 18 &&
                data[0] == ID1 &&
                data[1] == ID2 &&
                data[2] == COMPRESSION_METHOD_DEFLATE;
    }

    public byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        ShellPrint.InfoK($"Compressing {data.Length} bytes using pure-C# stored DEFLATE inside GZIP.", SRC);

        byte[] deflateBody = DeflateStoredOnly.Compress(data);

        using var ms = new MemoryStream();
        ms.WriteByte(ID1);
        ms.WriteByte(ID2);
        ms.WriteByte(COMPRESSION_METHOD_DEFLATE);
        ms.WriteByte(0); // flags
        WriteUInt32LE(ms, 0); // MTIME
        ms.WriteByte(0); // XFL
        ms.WriteByte(255); // OS = unknown

        ms.Write(deflateBody, 0, deflateBody.Length);

        uint crc32 = Crc32.Compute(data);
        WriteUInt32LE(ms, crc32);
        WriteUInt32LE(ms, (uint)data.Length);

        ShellPrint.OkK($"GZIP compression complete: input={data.Length} bytes, deflate={deflateBody.Length} bytes, output={ms.Length} bytes.", SRC);
        return ms.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (!IsSupportedFormat(data)) throw new NotSupportedException("Input is not a supported GZIP stream.");

        int pos = 0;
        using var output = new MemoryStream();

        while (pos < data.Length)
        {
            if (data.Length - pos < 10)
                throw new InvalidDataException("Truncated GZIP header.");

            if (data[pos++] != ID1 || data[pos++] != ID2)
                throw new InvalidDataException("Invalid GZIP magic.");

            byte cm = data[pos++];
            if (cm != COMPRESSION_METHOD_DEFLATE)
                throw new NotSupportedException($"Unsupported GZIP compression method: {cm}.");

            byte flg = data[pos++];
            pos += 4; // MTIME
            pos++;    // XFL
            pos++;    // OS

            if ((flg & FLAG_EXTRA) != 0)
            {
                EnsureAvailable(data, pos, 2);
                ushort xlen = ReadUInt16LE(data, pos);
                pos += 2;
                EnsureAvailable(data, pos, xlen);
                pos += xlen;
            }

            if ((flg & FLAG_FNAME) != 0)
                pos = SkipNullTerminated(data, pos);

            if ((flg & FLAG_FCOMMENT) != 0)
                pos = SkipNullTerminated(data, pos);

            if ((flg & FLAG_FHCRC) != 0)
            {
                EnsureAvailable(data, pos, 2);
                pos += 2;
            }

            if (data.Length - pos < 8)
                throw new InvalidDataException("Truncated GZIP member.");

            int footerPos = data.Length - 8;
            if (footerPos < pos)
                throw new InvalidDataException("Invalid GZIP layout.");

            int bodyLength = footerPos - pos;
            byte[] deflateBody = new byte[bodyLength];
            Buffer.BlockCopy(data, pos, deflateBody, 0, bodyLength);
            pos = footerPos;

            uint expectedCrc = ReadUInt32LE(data, pos);
            pos += 4;
            uint expectedSize = ReadUInt32LE(data, pos);
            pos += 4;

            byte[] uncompressed = DeflateStoredOnly.Decompress(deflateBody);
            uint actualCrc = Crc32.Compute(uncompressed);

            if (actualCrc != expectedCrc)
                throw new InvalidDataException("GZIP CRC32 mismatch.");

            if (((uint)uncompressed.Length) != expectedSize)
                throw new InvalidDataException("GZIP size mismatch.");

            output.Write(uncompressed, 0, uncompressed.Length);

            if (pos != data.Length)
                throw new InvalidDataException("Unexpected trailing bytes after GZIP member.");
        }

        return output.ToArray();
    }

    private static void EnsureAvailable(byte[] data, int pos, int count)
    {
        if (count < 0 || pos < 0 || pos > data.Length - count)
            throw new InvalidDataException("Truncated input.");
    }

    private static int SkipNullTerminated(byte[] data, int pos)
    {
        while (pos < data.Length)
        {
            if (data[pos++] == 0)
                return pos;
        }

        throw new InvalidDataException("Missing zero terminator.");
    }

    private static ushort ReadUInt16LE(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    private static uint ReadUInt32LE(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    private static void WriteUInt32LE(Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
        stream.Write(buf);
    }
}