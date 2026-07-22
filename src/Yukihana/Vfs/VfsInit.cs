// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;
using Cosmos.Kernel.System.Filesystems.Fat;
using Cosmos.Kernel.System.Vfs;
using Yukihana.Debug;
using Yukihana.Vfs.Config;

namespace Yukihana.Vfs;

internal static class VfsInit
{
    internal static readonly Dictionary<string, IVfsFilesystemType> s_filesystemTypes = new(StringComparer.Ordinal)
    {
        { "fat", new FatFilesystemType() },
    };

    public static void InitVfs(Logger logger, VfsConfigManager vfsMan)
    {
        foreach ((string name, IVfsFilesystemType type) in s_filesystemTypes)
        {
            logger.Info($"Registering fs type '{name}'");
            vfsMan.RegisterFilesystem(name, type);
            if (!VfsManager.RegisterFilesystem(name, type))
            {
                logger.Error("Unable to register filesystem!");
            }
            else
            {
                logger.Info("Registered filesystem successfully");
            }
        }
    }
}
