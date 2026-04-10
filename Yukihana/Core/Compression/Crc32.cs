// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression;

internal static class Crc32
{
    private const uint POLYNOMINAL = 0xEDB88320u;
    private static readonly uint[] _table = CreateTable();

    public static uint Compute(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return Compute(data, 0, data.Length);
    }

    public static uint Compute(byte[] data, int offset, int count)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (offset < 0 || count < 0 || offset > data.Length - count)
            throw new ArgumentOutOfRangeException();

        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < count; i++)
        {
            crc = _table[(crc ^ data[offset + i]) & 0xFF] ^ (crc >> 8);
        }
        return ~crc;
    }

    private static uint[] CreateTable()
    {
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                if ((c & 1) != 0)
                    c = POLYNOMINAL ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[i] = c;
        }

        return table;
    }
}