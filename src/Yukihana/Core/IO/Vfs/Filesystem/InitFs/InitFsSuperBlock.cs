// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;
using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsSuperblock(IBlockDevice blockDevice, InitfsInode root) : IVfsSuperblock
{
    private readonly IBlockDevice _blockDevice = blockDevice ?? throw new ArgumentNullException(nameof(blockDevice));
    private readonly InitfsInode _root = root ?? throw new ArgumentNullException(nameof(root));

    public IVfsInode Root => _root;

    public ISuperblockOperations SuperOperations => new InitfsSuperblockOperations();

    public long BlockSize => (long)_blockDevice.BlockSize;

    public ulong MaxNameLength => 255UL;
}
