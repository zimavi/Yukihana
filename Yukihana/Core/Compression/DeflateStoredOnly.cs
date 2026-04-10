// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Buffers.Binary;

namespace Yukihana.Core.Compression;

internal static class DeflateStoredOnly
{
    public static byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var writer = new BitWriter();

        int offset = 0;
        while (offset < data.Length)
        {
            int blockLength = Math.Min(0xFFFF, data.Length - offset);
            bool final = offset + blockLength >= data.Length;

            writer.WriteBits(final ? 1u : 0u, 1); // BFINAL
            writer.WriteBits(0u, 2);              // BTYPE = 00 (stored)
            writer.AlignToByte();

            writer.WriteUInt16LE((ushort)blockLength);
            writer.WriteUInt16LE((ushort)~blockLength);

            writer.WriteBytes(data, offset, blockLength);
            offset += blockLength;
        }

        return writer.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var reader = new BitReader(data);
        using var output = new MemoryStream();

        while (!reader.EndOfStream)
        {
            uint bfinal = reader.ReadBits(1);
            uint btype = reader.ReadBits(2);

            if (btype != 0)
                throw new NotSupportedException("Only stored DEFLATE blocks are supported by this pure-C# implementation.");

            reader.AlignToByte();

            ushort len = reader.ReadUInt16LE();
            ushort nlen = reader.ReadUInt16LE();

            if ((ushort)~len != nlen)
                throw new InvalidDataException("Stored block length check failed.");

            byte[] block = reader.ReadBytes(len);
            output.Write(block, 0, block.Length);

            if (bfinal == 1)
                break;
        }

        if (!reader.EndOfStream)
        {
            // Any trailing bytes would indicate malformed data for the streams we generate.
            throw new InvalidDataException("Unexpected trailing bytes in DEFLATE stream.");
        }

        return output.ToArray();
    }

    private sealed class BitWriter
    {
        private readonly List<byte> _bytes = new();
        private int _bitCount;
        private byte _current;

        public void WriteBits(uint value, int count)
        {
            if (count < 0 || count > 32)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
            {
                uint bit = (value >> i) & 1u;
                _current |= (byte)(bit << _bitCount);
                _bitCount++;

                if (_bitCount == 8)
                {
                    _bytes.Add(_current);
                    _current = 0;
                    _bitCount = 0;
                }
            }
        }

        public void AlignToByte()
        {
            if (_bitCount > 0)
            {
                _bytes.Add(_current);
                _current = 0;
                _bitCount = 0;
            }
        }

        public void WriteUInt16LE(ushort value)
        {
            AlignToByte();
            _bytes.Add((byte)(value & 0xFF));
            _bytes.Add((byte)(value >> 8));
        }

        public void WriteBytes(byte[] data, int offset, int count)
        {
            AlignToByte();
            for (int i = 0; i < count; i++)
                _bytes.Add(data[offset + i]);
        }

        public byte[] ToArray()
        {
            AlignToByte();
            return _bytes.ToArray();
        }
    }

    private sealed class BitReader
    {
        private readonly byte[] _data;
        private int _bytePos;
        private int _bitPos; // 0..7

        public BitReader(byte[] data)
        {
            _data = data;
        }

        public bool EndOfStream => _bytePos >= _data.Length;

        public uint ReadBits(int count)
        {
            if (count < 0 || count > 32)
                throw new ArgumentOutOfRangeException(nameof(count));

            uint result = 0;
            for (int i = 0; i < count; i++)
            {
                if (_bytePos >= _data.Length)
                    throw new EndOfStreamException("Unexpected end of DEFLATE stream.");

                byte current = _data[_bytePos];
                uint bit = (uint)((current >> _bitPos) & 1);
                result |= bit << i;

                _bitPos++;
                if (_bitPos == 8)
                {
                    _bitPos = 0;
                    _bytePos++;
                }
            }

            return result;
        }

        public void AlignToByte()
        {
            if (_bitPos != 0)
            {
                _bitPos = 0;
                _bytePos++;
            }
        }

        public ushort ReadUInt16LE()
        {
            AlignToByte();
            if (_bytePos + 2 > _data.Length)
                throw new EndOfStreamException("Unexpected end of DEFLATE stream.");

            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_bytePos, 2));
            _bytePos += 2;
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            AlignToByte();
            if (count < 0 || _bytePos + count > _data.Length)
                throw new EndOfStreamException("Unexpected end of DEFLATE stream.");

            byte[] result = new byte[count];
            Buffer.BlockCopy(_data, _bytePos, result, 0, count);
            _bytePos += count;
            return result;
        }
    }
}
