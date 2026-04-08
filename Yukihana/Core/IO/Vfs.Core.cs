// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
    private static readonly List<MountInfo> _mounts = new();
    public static string CurrentDirectory { get; private set; } = "/";
    public static VfsCredentials CurrentCredentials { get; private set; } = VfsCredentials.Root;

    public static void SetCredentials(VfsCredentials credentials)
    {
        CurrentCredentials = credentials;
        ShellPrint.InfoK($"credentials changed: uid={credentials.GroupId}, gid={credentials.UserId}, root={credentials.IsRoot}", "vfs.auth");
    }

    public static void Mount(string mountPoint, IVfsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        ShellPrint.InfoK($"mounting {mountPoint} with {backend.GetType().Name}", "vfs.mount");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts[i] = new MountInfo
                {
                    MountPoint = mountPoint,
                    Backend = backend
                };
                ShellPrint.InfoK($"remounting {mountPoint}", "vfs.mount");
                return;
            }
        }

        _mounts.Add(new MountInfo
        {
            MountPoint = mountPoint,
            Backend = backend
        });
        _mounts.Sort((a, b) => b.MountPoint.Length.CompareTo(a.MountPoint.Length));

        ShellPrint.OkK($"mounted {mountPoint}", "vfs.mount");
    }

    public static bool Unmount(string mountPoint)
    {
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        ShellPrint.InfoK($"unmounting {mountPoint}", "vfs.mount");

        for (int i = 0; i < _mounts.Count; i++)
        {
            if (string.Equals(_mounts[i].MountPoint, mountPoint, StringComparison.Ordinal))
            {
                _mounts.RemoveAt(i);
                ShellPrint.WarnK($"unmounted {mountPoint}", "vfs.mount");
                return true;
            }
        }

        return false;
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
}