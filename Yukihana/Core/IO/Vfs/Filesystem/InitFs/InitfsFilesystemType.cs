// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;
using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Core.Compression.Archives;
using Yukihana.Core.Debug;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsFilesystemType : IVfsFilesystemType
{
    private readonly ArchiveImage _archive;
    private readonly IBlockDevice _blockDevice;
    private readonly ulong _blockSize;
    private readonly ulong _blockCount;
    private readonly List<InitfsInode> _inodes = [];

    private static ReadOnlySpan<byte> MAGIC => [0x69, 0x6e, 0x69, 0x74]; // ASCII "init"

    public InitfsFilesystemType(IBlockDevice device, ArchiveImage image)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(image);
        _blockDevice = device;
        _archive = image;
        _blockSize = device.BlockSize;
        _blockCount = device.BlockCount;
    }

    public bool TryMount(ReadOnlySpan<char> source, MountFlags flags, out IVfsSuperblock? superblock)
    {
        superblock = null;

        if (_blockDevice is null || _blockDevice.BlockSize == 0 || _blockDevice.BlockCount == 0)
            return false;

        InitfsInode? rootInode = LoadArchiveData();
        if (rootInode is null)
            return false;

        // Write Magic to blockdevice
        Span<byte> buffer = stackalloc byte[(int)_blockSize];
        MAGIC.CopyTo(buffer);
        _blockDevice.WriteBlock(0, 1, buffer);

        superblock = new InitfsSuperblock(_blockDevice, rootInode);
        return true;
    }

    public bool TryFormat(ReadOnlySpan<char> source, IVfsFormatOptions? options)
    {
        // We don't support formatting for initramfs
        return false;
    }

    public bool TryDestroy(ReadOnlySpan<char> source)
    {
        // We don't support destroying for initramfs
        return false;
    }

    private InitfsInode? LoadArchiveData()
    {
        Logger logger = new("initfs");

        logger.Info("Creating root directory '/'");
        var root = new InitfsInode("/", ArchiveEntryKind.Directory, "/");
        _inodes.Add(root);

        ulong usedBlocks = 1;
        ulong maxBlocks = _blockCount;

        foreach (ArchiveEntry entry in _archive.Entries)
        {
            logger.Info($"Processing entry '{entry.Path}'");

            if (string.IsNullOrEmpty(entry.Path))
                continue;

            string path = entry.Path;
            if (path.StartsWith('/'))
                path = path.Substring(1);

            string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            string currentPath = "";
            InitfsInode parent = root;

            logger.Info("Processing path tree");

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                currentPath = Path.Combine(currentPath, part);

                logger.Info($"Current part is '{part}' with path '{currentPath}'");

                InitfsInode? child = parent.Children.FirstOrDefault(c => c.Name == part && c.Kind == ArchiveEntryKind.Directory);
                if (child is null)
                {
                    logger.Info("No child exists for this one. Creating new directory");

                    child = new InitfsInode(part, ArchiveEntryKind.Directory, currentPath);
                    _inodes.Add(child);
                    child.Parent = parent;
                    parent.Children.Add(child);
                }

                parent = child;
            }

            string leafName = parts[parts.Length - 1];
            currentPath = Path.Combine(currentPath, leafName);

            logger.Info($"Creating leaf with name '{leafName}' with path '{currentPath}'");

            ArchiveEntryKind kind = entry.Kind;
            if (entry.Kind == ArchiveEntryKind.File &&
                parent.Children.Any(c => c.Name == leafName && c.Kind == ArchiveEntryKind.Directory))
            {
                logger.Warn("Collision of file and directory. Prefering file");
                // If there is collision between file and directory, prefer file
                kind = ArchiveEntryKind.File;
            }

            logger.Info("Creating leaf inode");

            InitfsInode inode = new(leafName, kind, currentPath)
            {
                UserId = entry.UserId,
                GroupId = entry.GroupId,
                Mode = GetModeFromArchive(entry),
                Timestamp = new VfsTimespec(entry.ModifiedUnixTimeSeconds, 0),
                FileOperations = new InitfsFileOperations(_blockDevice)
            };

            if (kind == ArchiveEntryKind.File && entry.Data is not null)
            {
                logger.Info("Data is not null");

                ulong fileSize = (ulong)entry.Data.Length;
                ulong requiredBlocks = (fileSize + _blockSize - 1) / _blockSize;

                if (usedBlocks + requiredBlocks > maxBlocks)
                    continue;

                byte[] buffer = new byte[requiredBlocks * _blockSize];
                Array.Copy(entry.Data, 0, buffer, 0, (long)fileSize);

                logger.Info($"Writing to block device start={usedBlocks}, len={requiredBlocks}");

                _blockDevice.WriteBlock(usedBlocks, requiredBlocks, buffer);
                inode.BlockOffset = usedBlocks;
                inode.Size = (long)fileSize;
                usedBlocks += requiredBlocks;
            }

            logger.Info($"Setting this inode parent to '{parent.Name}'");

            parent.Children.Add(inode);
            _inodes.Add(inode);
            inode.Parent = parent;
        }

        logger.Info("Finished processing inodes");

        return root;
    }

    private ModeEnum GetModeFromArchive(ArchiveEntry entry)
    {
        return entry.Kind switch
        {
            ArchiveEntryKind.Directory => ModeEnum.Directory,
            ArchiveEntryKind.SymbolicLink => ModeEnum.SymbolicLink,
            _ => ModeEnum.RegularFile
        };
    }
}
