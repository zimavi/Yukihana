// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.IO.Vfs.Backends;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
    private static readonly Logger s_logger = new("vfs");

    private static readonly List<MountInfo> s_mounts = [];
    public static string CurrentDirectory { get; private set; } = "/";
    public static VfsCredentials CurrentCredentials { get; private set; } = VfsCredentials.Root;

    public static void SetCredentials(VfsCredentials credentials)
    {
        CurrentCredentials = credentials;
    }

    public static void Mount(string mountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        s_logger.Info($"Mounting {mountPoint} with {backend.GetType().Name}");

        for (int i = 0; i < s_mounts.Count; i++)
        {
            if (string.Equals(s_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                s_mounts[i] = new MountInfo
                {
                    MountPoint = mountPoint,
                    Backend = backend
                };
                s_logger.Info($"Remounting {mountPoint}");
                return;
            }
        }

        s_mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });

        s_mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        s_logger.Info($"Mounted {mountPoint}");
    }

    public static bool Unmount(string mountPoint)
    {
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        s_logger.Info($"Unmounting {mountPoint}");

        for (int i = 0; i < s_mounts.Count; i++)
        {
            if (string.Equals(s_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                s_mounts.RemoveAt(i);
                s_logger.Info($"Unmounted {mountPoint}");
                s_mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));
                return true;
            }
        }

        return false;
    }

    public static void Remount(string mountPoint, string oldMountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);
        oldMountPoint = FsPath.NormalizeAbsolute(oldMountPoint);

        s_logger.Info($"Remounting {mountPoint} -> {oldMountPoint} with {backend.GetType().Name}");

        MountInfo? oldMount = null;

        for (int i = 0; i < s_mounts.Count; i++)
        {
            if (string.Equals(s_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                oldMount = s_mounts[i];
                s_mounts.RemoveAt(i);
                break;
            }
        }

        for (int i = 0; i < s_mounts.Count; i++)
        {
            if (string.Equals(s_mounts[i].MountPoint, oldMountPoint, StringComparison.Ordinal))
            {
                s_mounts.RemoveAt(i);
                break;
            }
        }

        s_mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });

        if (oldMount is not null)
        {
            s_mounts.Add(new MountInfo
            {
                MountPoint = oldMountPoint,
                Backend = oldMount.Backend
            });

            s_logger.Info($"Moved old mount {mountPoint} -> {oldMountPoint}");
        }

        s_mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        s_logger.Info($"Remounted {mountPoint}");
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
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: false);
        return !resolved.IsFailure && resolved.Value.Kind != FsNodeKind.Missing;
    }

    public static bool FileExists(string path)
    {
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: true);
        return resolved.IsSuccess && resolved.Value.Kind == FsNodeKind.File;
    }

    public static bool DirectoryExists(string path)
    {
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: true);
        return resolved.IsSuccess && resolved.Value.Kind == FsNodeKind.Directory;
    }

    public static FsNodeKind GetKind(string path)
    {
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: false);
        return resolved.IsFailure ? FsNodeKind.Missing : resolved.Value.Kind;
    }

    public static bool IsDirectory(string path) => GetKind(path) == FsNodeKind.Directory;
    public static bool IsSymbolicLink(string path) => GetKind(path) == FsNodeKind.SymbolicLink;

}
