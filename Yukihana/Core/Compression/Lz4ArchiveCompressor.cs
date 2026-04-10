// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression;

public sealed class Lz4ArchiveCompressor : IArchiveCompressor
{
    private const uint MAGIC = 0x184D2204;

    public bool IsSupportedFormat(byte[] data)
    {
        if (data == null || data.Length < 4) return false;
        uint magic = (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        return magic == MAGIC;
    }

    public byte[] Compress(byte[] input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        return CompressBlock(input);
    }

    public byte[] Decompress(byte[] input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        return DecompressBlock(input);
    }

    private static byte[] CompressBlock(byte[] src)
    {
        int srcLen = src.Length;
        int maxDst = srcLen + srcLen / 255 + 16;

        byte[] dst = new byte[maxDst];

        int[] hashTable = new int[1 << 16];
        Array.Fill(hashTable, -1);

        int srcPos = 0;
        int anchor = 0;
        int dstPos = 0;

        while (srcPos + 4 < srcLen)
        {
            uint sequence = ReadU32(src, srcPos);
            int hash = Hash(sequence);

            int refPos = hashTable[hash];
            hashTable[hash] = srcPos;

            if (refPos >= 0 &&
                (srcPos - refPos) <= 65535 &&
                ReadU32(src, refPos) == sequence)
            {
                int matchLen = 4;
                int maxMatch = srcLen - srcPos;

                while (matchLen < maxMatch &&
                       src[refPos + matchLen] == src[srcPos + matchLen])
                {
                    matchLen++;
                }

                int literalLen = srcPos - anchor;

                int tokenPos = dstPos++;
                byte token = 0;

                if (literalLen >= 15)
                {
                    token |= 15 << 4;
                    dstPos = WriteLength(dst, dstPos, literalLen - 15);
                }
                else
                {
                    token |= (byte)(literalLen << 4);
                }

                Buffer.BlockCopy(src, anchor, dst, dstPos, literalLen);
                dstPos += literalLen;

                int offset = srcPos - refPos;
                dst[dstPos++] = (byte)(offset);
                dst[dstPos++] = (byte)(offset >> 8);

                int ml = matchLen - 4;
                if (ml >= 15)
                {
                    token |= 15;
                    dstPos = WriteLength(dst, dstPos, ml - 15);
                }
                else
                {
                    token |= (byte)ml;
                }

                dst[tokenPos] = token;

                srcPos += matchLen;
                anchor = srcPos;
            }
            else
            {
                srcPos++;
            }
        }

        int lastLen = srcLen - anchor;
        int lastTokenPos = dstPos++;
        byte lastToken = 0;

        if (lastLen >= 15)
        {
            lastToken |= 15 << 4;
            dstPos = WriteLength(dst, dstPos, lastLen - 15);
        }
        else
        {
            lastToken |= (byte)(lastLen << 4);
        }

        Buffer.BlockCopy(src, anchor, dst, dstPos, lastLen);
        dstPos += lastLen;

        dst[lastTokenPos] = lastToken;

        byte[] result = new byte[dstPos];
        Buffer.BlockCopy(dst, 0, result, 0, dstPos);
        return result;
    }

    private static byte[] DecompressBlock(byte[] src)
    {
        int srcPos = 0;
        byte[] dst = new byte[EstimateDecompressedSize(src)];

        int dstPos = 0;

        while (srcPos < src.Length)
        {
            byte token = src[srcPos++];

            int literalLen = token >> 4;
            if (literalLen == 15)
                literalLen += ReadLength(src, ref srcPos);

            Buffer.BlockCopy(src, srcPos, dst, dstPos, literalLen);
            srcPos += literalLen;
            dstPos += literalLen;

            if (srcPos >= src.Length)
                break;

            int offset = src[srcPos++] | (src[srcPos++] << 8);

            int matchLen = token & 0x0F;
            if (matchLen == 15)
                matchLen += ReadLength(src, ref srcPos);
            matchLen += 4;

            int matchPos = dstPos - offset;

            for (int i = 0; i < matchLen; i++)
                dst[dstPos + i] = dst[matchPos + i];

            dstPos += matchLen;
        }

        byte[] result = new byte[dstPos];
        Buffer.BlockCopy(dst, 0, result, 0, dstPos);
        return result;
    }

    private static int Hash(uint sequence)
    {
        return (int)((sequence * 2654435761u) >> 16) & 0xFFFF;
    }

    private static uint ReadU32(byte[] data, int pos)
    {
        return (uint)(data[pos]
            | (data[pos + 1] << 8)
            | (data[pos + 2] << 16)
            | (data[pos + 3] << 24));
    }

    private static int WriteLength(byte[] dst, int pos, int length)
    {
        while (length >= 255)
        {
            dst[pos++] = 255;
            length -= 255;
        }
        dst[pos++] = (byte)length;
        return pos;
    }

    private static int ReadLength(byte[] src, ref int pos)
    {
        int len = 0;
        byte b;
        while ((b = src[pos++]) == 255)
            len += 255;
        len += b;
        return len;
    }

    private static int EstimateDecompressedSize(byte[] src)
    {
        return src.Length * 8 + 64;
    }
}