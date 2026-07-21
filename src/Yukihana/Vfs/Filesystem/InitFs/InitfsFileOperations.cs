// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;
using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Debug;

namespace Yukihana.Vfs.Filesystem.InitFs;

internal sealed class InitfsFileOperations(IBlockDevice blockDevice) : IFileOperations
{
    private readonly Logger _logger = new("initfs");

    private readonly IBlockDevice _blockDevice = blockDevice ?? throw new ArgumentNullException(nameof(blockDevice));

    public long Read(IVfsOpenFile openFile, Span<byte> buffer)
    {
        _logger.Info("Call for Read()");
        ArgumentNullException.ThrowIfNull(openFile);

        if (openFile.Inode is not InitfsInode inode || inode.BlockOffset == 0)
        {
            _logger.Warn("openFile is not InitfsInode or BlockOffset is 0");
            return 0;
        }

        long fileSize = inode.Size;
        if (openFile.Position >= fileSize)
        {
            _logger.Warn($"Position is grater than fileSize ({openFile.Position} >= {fileSize})");
            return 0;
        }

        int blockSize = checked((int)_blockDevice.BlockSize);

        long toRead = Math.Min(buffer.Length, fileSize - openFile.Position);

        _logger.Trace($"toRead={toRead}");

        if (toRead <= 0)
        {
            return 0;
        }

        long remaining = toRead;
        int destinationOffset = 0;

        while (remaining > 0)
        {
            ulong blockIndex = (ulong)openFile.Position / (ulong)blockSize;
            ulong blockNumber = inode.BlockOffset + blockIndex;

            int blockOffset = (int)(openFile.Position % blockSize);
            int bytesThisBlock = Math.Min(blockSize - blockOffset, (int)remaining);

            if (blockOffset == 0 && bytesThisBlock == blockSize)
            {
                _logger.Trace("Whole block contains data, reading full block");
                // Read a full block directly into the destination buffer.
                _blockDevice.ReadBlock(
                    blockNumber,
                    1,
                    buffer.Slice(destinationOffset, blockSize));
            }
            else
            {
                _logger.Trace("Part of block contains data, reading it and slicing");
                // Read a partial block into a temporary buffer first.
                Span<byte> temp = new byte[blockSize];

                _blockDevice.ReadBlock(blockNumber, 1, temp);

                temp.Slice(blockOffset, bytesThisBlock)
                    .CopyTo(buffer.Slice(destinationOffset, bytesThisBlock));
            }

            openFile.Position += bytesThisBlock;
            destinationOffset += bytesThisBlock;
            remaining -= bytesThisBlock;
        }

        return toRead;
    }

    public long Write(IVfsOpenFile openFile, ReadOnlySpan<byte> buffer)
    {
        // Read-only filesystem - no writes allowed
        throw new NotSupportedException("Initramfs is read-only");
    }

    public bool Seek(IVfsOpenFile openFile, long offset, SeekWhence whence, out long newPosition)
    {
        _logger.Info("Call for Seek()");
        ArgumentNullException.ThrowIfNull(openFile);

        long baseOffset = 0;
        switch (whence)
        {
            case SeekWhence.Set:
                _logger.Trace("SeekWhence=set");
                baseOffset = 0;
                break;
            case SeekWhence.Cur:
                _logger.Trace("SeekWhence=cur");
                baseOffset = openFile.Position;
                break;
            case SeekWhence.End:
                _logger.Trace("SeekWhence=end");
                if (openFile.Inode is InitfsInode inode)
                {
                    baseOffset = inode.Size;
                }
                else
                {
                    baseOffset = 0;
                }

                break;
        }

        long newPos = baseOffset + offset;
        if (newPos < 0)
        {
            newPosition = -1;
            return false;
        }

        openFile.Position = newPos;
        newPosition = newPos;
        return true;
    }

    public bool Fsync(IVfsOpenFile openFile)
    {
        // No-op for read-only filesystem
        return true;
    }

    public void Release(IVfsOpenFile openFile)
    {
        // No cleanup needed
    }
}
