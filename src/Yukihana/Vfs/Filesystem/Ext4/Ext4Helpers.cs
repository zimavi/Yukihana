// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Numerics;
using Cosmos.Kernel.HAL.Interfaces.Devices;

namespace Yukihana.Vfs.Filesystem.Ext4;

internal static class Ext4Helpers
{
    public static unsafe TWide Combine<TSmall, TWide>(TSmall low, TSmall high)
        where TSmall : unmanaged, IBinaryInteger<TSmall>
        where TWide : unmanaged, IBinaryInteger<TWide>
    {
        int shift = sizeof(TSmall) * sizeof(byte);

        return (TWide.CreateTruncating(high) << shift)
            | TWide.CreateTruncating(low);
    }

    public static byte[] ReadBytes(IBlockDevice device, ulong startBlock, ulong offset, ulong count)
    {
        if (count == 0)
        {
            return [];
        }

        ulong blockSize = device.BlockSize;

        ulong firstBlock = startBlock + (offset / blockSize);
        ulong blockOffset = offset % blockSize;

        ulong totalBytes = blockOffset + count;
        ulong blocksToRead = (totalBytes + blockSize - 1) / blockSize;

        byte[] temp = new byte[checked((int)(blocksToRead * blockSize))];
        device.ReadBlock(firstBlock, blocksToRead, temp);

        byte[] result = GC.AllocateUninitializedArray<byte>(checked((int)count));
        temp.AsSpan(
            checked((int)blockOffset),
            checked((int)count))
            .CopyTo(result);

        return result;
    }
}
