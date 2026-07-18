// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;
using Cosmos.Kernel.System.Storage;
using Cosmos.Kernel.System.Vfs;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs.Probe;

namespace Yukihana.Core.IO.Vfs.Config;

public sealed class VfsConfigManager
{
    private readonly List<VfsMountConfig> _configs = [];
    private readonly Dictionary<string, IVfsFilesystemType> _filesystemTypes = [];

    private readonly Logger _logger = new ("vfsman");

    public void RegisterFilesystem(string name, IVfsFilesystemType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        _logger.Info($"Registered filesystem with name '{name}' of type '{type.GetType().Name}'");

        _filesystemTypes[name] = type;
    }

    public void LoadConfig(string configContent)
    {
        _logger.Info("Loading config");
        _configs.Clear();
        _configs.AddRange(VfsConfigParser.Parse(configContent));
    }

    public IReadOnlyList<VfsMountConfig> GetConfigs() => _configs;

    public bool TryMountAll(out List<VfsManager.VfsMount> mountedFilesystems)
    {
        _logger.Info("Trying to mount all disks");
        mountedFilesystems = [];
        
        foreach (VfsMountConfig config in _configs)
        {
            if (TryMount(config, out VfsManager.VfsMount? mount))
            {
                _logger.Info($"Mounted '{config.Source}' at '{config.MountPoint}'");
                mountedFilesystems.Add(mount!);
            }
            else
                _logger.Error($"Unable to mount '{config.Source}' at '{config.MountPoint}'");
        }
        
        return true;
    }

    public bool TryMount(VfsMountConfig config, out VfsManager.VfsMount? mount)
    {
        mount = null;

        if (string.IsNullOrEmpty(config.Source) || 
            string.IsNullOrEmpty(config.MountPoint) || 
            string.IsNullOrEmpty(config.FilesystemType))
        {
            _logger.Error($"Unable to mount {config.Source} at {config.MountPoint} because some fields are empty");
            return false;
        }
            

        if (!_filesystemTypes.TryGetValue(config.FilesystemType, out IVfsFilesystemType? fsType))
        {
            _logger.Error($"Unable to mount {config.Source} at {config.MountPoint} because could not find '{config.FilesystemType}' fs");
            return false;
        }

        
        _logger.Info($"Resolving deviceId for {config.Source}");

        string resolvedSource = ResolveDeviceIdentifier(config);
        if (string.IsNullOrEmpty(resolvedSource))
        {
            _logger.Error($"Resolved id is empty");
            return false;
        }

        return VfsManager.TryMount(
            config.FilesystemType,
            resolvedSource,
            config.Flags,
            config.MountPoint,
            out mount);
    }

    private string ResolveDeviceIdentifier(VfsMountConfig config)
    {
        switch(config.SourceType)
        {
            case SourceType.Path:
                return config.Source;
            case SourceType.Label:
                _logger.Warn("Source type is LABEL, which works incorrectly. If you want to use this, use device id (stat0, nvme0n1, etc.)");

                string label = config.Source.Substring(6); // skip "LABEL="
                return ResolveByLabel(label);
            case SourceType.Uuid:
                string uuid = config.Source.Substring(5); // skip "UUID="
                return ResolveByUuid(uuid);
            case SourceType.PartitionIndex:
                _logger.Warn("Source type is partition index. This is unrealiable way to identify drives, unless there is only one present.");

                if (int.TryParse(config.Source, out int idx))
                    return ResolveByPartitionIndex(idx);
                
                return string.Empty;
            
            default:
                return config.Source;
        }
    }

    // TODO: Search real label (this is device ID search)
    private string ResolveByLabel(string label)
    {
        _logger.Info($"Searching for partition with label '{label}'");
        IReadOnlyList<Partition> partitions = StorageManager.Partitions;
        for(int i = 0; i < partitions.Count; i++)
        {
            Partition part = partitions[i];
            if (part.Name == label)
            {
                _logger.Info($"Found matching partition at index '{i}'");
                return $"{i}";
            }
        }

        _logger.Error($"Couldn't find matching label!");
        
        return string.Empty;
    }

    private string ResolveByUuid(string uuid)
    {
        _logger.Info($"Searching for partition with uuid '{uuid}'");
        Guid guid = Guid.Parse(uuid);

        IReadOnlyList<Partition> partitions = StorageManager.Partitions;
        for(int i = 0; i < partitions.Count; i++)
        {
            Partition part = partitions[i];
            
            if (!FilesystemProber.ProbeFilesystem(part, out FilesystemProbeResult? result))
                continue;

            if (result!.Value.Uuid == guid)
            {
                _logger.Info($"Found matching partition at index '{i}'");
                return $"{i}";
            }
        }

        _logger.Error($"Filesystem couldn't be probed!");

        return string.Empty;
    }

    private string ResolveByPartitionIndex(int index)
    {
        _logger.Info($"Searching for partition with index '{index}'");
        if (index >= 0 && index < StorageManager.Partitions.Count)
        {
            _logger.Info($"Found matching partition at index '{index}'");
            return $"{index}";
        }
        
        return string.Empty;
    }

    public VfsManager.VfsMount? Mount(string filesystemType, string source, MountFlags flags, string mountPoint)
    {
        if (VfsManager.TryMount(filesystemType, source, flags, mountPoint, out VfsManager.VfsMount? mount))
            return mount;
        
        return null;
    }

    public bool Unmount(string mountPoint)
    {
        return VfsManager.TryUnmount(mountPoint);
    }
}