// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
    private static readonly Logger _logger = new("vfs");

    private static readonly List<MountInfo> _mounts = new();
    public static string CurrentDirectory { get; private set; } = "/";
    public static VfsCredentials CurrentCredentials { get; private set; } = VfsCredentials.Root;

    public static void SetCredentials(VfsCredentials credentials)
    {
        CurrentCredentials = credentials;
    }

    public static void Mount(string mountPoint, IVfsBackend backend)
    {
        if (BootEnvironment.Stage == BootStage.EarlyKernel)
            MountEarly(mountPoint, backend);
        else
            MountCore(mountPoint, backend);
    }

    public static bool Unmount(string mountPoint) =>
        BootEnvironment.Stage == BootStage.EarlyKernel
            ? UnmountEarly(mountPoint)
            : UnmountCore(mountPoint);
        
    public static void Remount(string mountPoint, string oldMountPoint, IVfsBackend backend)
    {
        if (BootEnvironment.Stage == BootStage.EarlyKernel)
            RemountEarly(mountPoint, oldMountPoint, backend);
        else
            RemountCore(mountPoint, oldMountPoint, backend);
    }

    public static Option<KernelError> ChangeDirectory(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Option<KernelError>.Some(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Path is not a directory: {path}"));

        CurrentDirectory = resolved.Value.AbsolutePath;
        return Option<KernelError>.None();
    }

    public static bool Exists(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: false);
        return !resolved.IsFailure && resolved.Value.Kind != FsNodeKind.Missing;
    }

    public static FsNodeKind GetKind(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: false);
        return resolved.IsFailure ? FsNodeKind.Missing : resolved.Value.Kind;
    }

    public static bool IsDirectory(string path) => GetKind(path) == FsNodeKind.Directory;
    public static bool IsSymbolicLink(string path) => GetKind(path) == FsNodeKind.SymbolicLink;


    private static void MountEarly(string mountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        _logger.Info($"Mounting {mountPoint} with {backend.GetType().Name}");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts[i] = new MountInfo
                {
                    MountPoint = mountPoint,
                    Backend = backend
                };
                _logger.Info($"Remounting {mountPoint}");
                return;
            }
        }

        _mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });

        _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        _logger.Info($"Mounted {mountPoint}");
    }
    private static void MountCore(string mountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        var u = UnitManager.Start("Mount", $"{mountPoint} with {backend.GetType().Name}");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts[i] = new MountInfo
                {
                    MountPoint = mountPoint,
                    Backend = backend
                };
                u.Ok();
                return;
            }
        }

        _mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });

        _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        u.Ok();
    }

    private static bool UnmountEarly(string mountPoint)
    {
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        _logger.Info($"Unmounting {mountPoint}");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts.RemoveAt(i);
                _logger.Info($"Unmounted {mountPoint}");
                _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));
                return true;
            }
        }

        return false;
    }
    private static bool UnmountCore(string mountPoint)
    {
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        var u = UnitManager.Start("Unmount", $"{mountPoint}");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts.RemoveAt(i);
                _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));
                u.Ok();
                return true;
            }
        }

        return false;
    }

    private static void RemountEarly(string mountPoint, string oldMountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);
        oldMountPoint = FsPath.NormalizeAbsolute(oldMountPoint);

        _logger.Info($"Remounting {mountPoint} -> {oldMountPoint} with {backend.GetType().Name}");

        MountInfo? oldMount = null;

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                oldMount = _mounts[i];
                _mounts.RemoveAt(i);
                break;
            }
        }

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, oldMountPoint, StringComparison.Ordinal))
            {
                _mounts.RemoveAt(i);
                break;
            }
        }

        _mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });

        if (oldMount is not null)
        {
            _mounts.Add(new MountInfo
            {
                MountPoint = oldMountPoint,
                Backend = oldMount.Backend
            });

            _logger.Info($"Moved old mount {mountPoint} -> {oldMountPoint}");
        }

        _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        _logger.Info($"Remounted {mountPoint}");
    }

    private static void RemountCore(string mountPoint, string oldMountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

    mountPoint = FsPath.NormalizeAbsolute(mountPoint);
    oldMountPoint = FsPath.NormalizeAbsolute(oldMountPoint);

    var u = UnitManager.Start("Remount", $"{mountPoint} -> {oldMountPoint}");

    MountInfo? oldMount = null;

    for (int i = 0; i < _mounts.Count; i++)
    {
        if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
        {
            oldMount = _mounts[i];
            _mounts.RemoveAt(i);
            break;
        }
    }

    for (int i = 0; i < _mounts.Count; i++)
    {
        if (string.Equals(_mounts[i].MountPoint, oldMountPoint, StringComparison.Ordinal))
        {
            _mounts.RemoveAt(i);
            break;
        }
    }

    _mounts.Add(new MountInfo
    {
        MountPoint = mountPoint,
        Backend = backend
    });

    if (oldMount != null)
    {
        _mounts.Add(new MountInfo
        {
            MountPoint = oldMountPoint,
            Backend = oldMount.Backend
        });
    }

    _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

    u.Ok();
    }
}