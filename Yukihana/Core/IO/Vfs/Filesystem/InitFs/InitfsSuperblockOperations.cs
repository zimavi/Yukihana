// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsSuperblockOperations : ISuperblockOperations
{
    public bool Sync(IVfsSuperblock superblock)
    {
        // No sync needed for read-only filesystem
        return true;
    }

    public bool StatFs(IVfsSuperblock superblock, out VfsStatFs statFs)
    {
        // For a read-only initramfs, we report 0 available space since it's not a real disk
        statFs = new VfsStatFs
        {
            Type = 0x696e6974, // "init" ASCII
            BlockSize = 512,
            Blocks = 0,
            Bfree = 0,
            Bavail = 0,
            Files = 0,
            Ffree = 0,
            NameMax = 255,
            Frsize = 512
        };
        return true;
    }

    public void Drop(IVfsSuperblock superblock)
    {
        // No cleanup needed for this filesystem
    }
}
