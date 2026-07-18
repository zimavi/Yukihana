// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsOpenFile(InitfsInode inode, InitfsFileOperations operations) : IVfsOpenFile
{
    public IVfsInode Inode { get; } = inode;

    public IFileOperations Operations { get; } = operations;

    public long Position { get; set; }
}