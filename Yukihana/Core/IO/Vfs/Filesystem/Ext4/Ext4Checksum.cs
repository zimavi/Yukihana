// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4;

internal static class Ext4Checksum
{
    private static readonly uint[] s_table = CreateTable();

    private static uint[] CreateTable()
    {
        const uint poly = 0x82F63B78;

        uint[] table = new uint[256];

        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;

            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ poly;
                else
                    crc >>= 1;
            }

            table[i] = crc;
        }

        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        return Append(0xFFFFFFFF, data) ^ 0xFFFFFFFF;
    }

    public static uint Append(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (byte b in data)
            crc = s_table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        return crc;
    }
}
