// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;

namespace Yukihana.Vfs.Device;

internal sealed class BlockDeviceStream(IBlockDevice blockDevice)
{
    private readonly IBlockDevice _device = blockDevice ?? throw new ArgumentNullException(nameof(blockDevice));
    private readonly byte[] _blockBuffer = new byte[(int)blockDevice.BlockSize];
    private readonly ulong _blockSize = blockDevice.BlockSize;
    private readonly ulong _deviceSize = blockDevice.BlockCount * blockDevice.BlockSize;

    public ulong Length => _deviceSize;
    public ulong BlockSize => _blockSize;
    public void Flush() => _device.Flush();

    public int Read(ulong position, Span<byte> destination)
    {
        if (position >= _deviceSize)
        {
            return 0;
        }

        ulong remaining = _deviceSize - position;

        if ((ulong)destination.Length > remaining)
        {
            destination = destination[..(int)remaining];
        }

        if (destination.IsEmpty)
        {
            return 0;
        }

        int totalRead = destination.Length;

        ulong currentBlock = position / _blockSize;
        int blockOffset = (int)(position % _blockSize);

        int copied = 0;

        if (blockOffset != 0)
        {
            _device.ReadBlock(currentBlock, 1, _blockBuffer);

            int count = Math.Min(destination.Length, (int)_blockSize - blockOffset);

            _blockBuffer.AsSpan(blockOffset, count)
                .CopyTo(destination);

            copied += count;
            currentBlock++;
        }

        int remainingBytes = destination.Length - copied;
        ulong wholeBlocks = (ulong)remainingBytes / _blockSize;

        if (wholeBlocks != 0)
        {
            int bytes = (int)(wholeBlocks * _blockSize);

            _device.ReadBlock(
                currentBlock,
                wholeBlocks,
                destination.Slice(copied, bytes));

            copied += bytes;
            currentBlock += wholeBlocks;
        }

        remainingBytes = destination.Length - copied;

        if (remainingBytes != 0)
        {
            _device.ReadBlock(currentBlock, 1, _blockBuffer);

            _blockBuffer.AsSpan(0, remainingBytes)
                .CopyTo(destination[copied..]);
        }

        return totalRead;
    }

    public int Write(ulong position, ReadOnlySpan<byte> source)
    {
        if (position >= _deviceSize)
        {
            return 0;
        }

        ulong remaining = _deviceSize - position;

        if ((ulong)source.Length > remaining)
        {
            source = source[..(int)remaining];
        }

        if (source.IsEmpty)
        {
            return 0;
        }

        int totalWritten = source.Length;

        ulong currentBlock = position / _blockSize;
        int blockOffset = (int)(position % _blockSize);

        int copied = 0;

        if (blockOffset != 0)
        {
            _device.ReadBlock(currentBlock, 1, _blockBuffer);

            int count = Math.Min(source.Length, (int)_blockSize - blockOffset);

            source[..count]
                .CopyTo(_blockBuffer.AsSpan(blockOffset));

            _device.WriteBlock(currentBlock, 1, _blockBuffer);

            copied += count;
            currentBlock++;
        }

        int remainingBytes = source.Length - copied;
        ulong wholeBlocks = (ulong)remainingBytes / _blockSize;

        if (wholeBlocks != 0)
        {
            int bytes = (int)(wholeBlocks * _blockSize);

            _device.WriteBlock(
                currentBlock,
                wholeBlocks,
                source.Slice(copied, bytes));

            copied += bytes;
            currentBlock += wholeBlocks;
        }

        remainingBytes = source.Length - copied;

        if (remainingBytes != 0)
        {
            _device.ReadBlock(currentBlock, 1, _blockBuffer);

            source.Slice(copied)
                .CopyTo(_blockBuffer);

            _device.WriteBlock(currentBlock, 1, _blockBuffer);
        }

        return totalWritten;
    }
}
