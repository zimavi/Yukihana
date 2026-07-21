// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Numerics;

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4;

internal static class Ext4Checksum
{
    public static uint Append(uint crc, ReadOnlySpan<byte> data)
    {
        Span<byte> bytes = stackalloc byte[data.Length];
        data.CopyTo(bytes);

        while (bytes.Length >= 8)
        {
            ulong chunk = BitConverter.ToUInt64(data);
            crc = BitOperations.Crc32C(crc, chunk);
            bytes = bytes[8..];
        }

        foreach (byte b in data)
        {
            crc = BitOperations.Crc32C(crc, b);
        }

        return crc;
    }
}
