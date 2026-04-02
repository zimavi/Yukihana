// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static class VFS
{
    private sealed class MountInfo
    {
        public required string MountPoint { get; init; }
        public required IVfsBackend Backend { get; init; }
    }

    private sealed class ResolvedPath
    {
        public required MountInfo Mount { get; init; }
        public required string RelativePath { get; init; }
        public required string AbsolutePath { get; init; }
        public required FsNodeKind Kind { get; init; }
        public required VfsMetadata Metadata { get; init; }
    }

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

    public static Result<byte[], KernelError> ReadAllBytes(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Result<byte[], KernelError>.Failure(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.File)
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        var metadata = resolved.Value.Metadata;

        if (!FsPermissionUtil.CanRead(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: read {resolved.Value.AbsolutePath}", "vfs.perm");
            return Result<byte[], KernelError>.Failure(KernelError.PermissionsDenied($"read {resolved.Value.AbsolutePath}"));
        }

        return resolved.Value.Mount.Backend.ReadAllBytes(resolved.Value.RelativePath);
    }

    public static Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return ReadAllBytes(path).Map(bytes => encoding.GetString(bytes));
    }

    public static Result<Stream, KernelError> Open(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Result<Stream, KernelError>.Failure(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.File)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));
        
        var metadata = resolved.Value.Metadata;

        if (!FsPermissionUtil.CanRead(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission_denied: open {resolved.Value.AbsolutePath}", "vfs.perm");
            return Result<Stream, KernelError>.Failure(KernelError.PermissionsDenied($"open {resolved.Value.AbsolutePath}"));
        }

        return resolved.Value.Mount.Backend.Open(resolved.Value.RelativePath);
    }

    public static Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        string absolute = ToAbsolute(path);

        var resolved = ResolvePath(absolute, followFinalSymlink: true);
        VfsMetadata metadata;
        if (!resolved.IsFailure && resolved.Value.Kind == FsNodeKind.File)
        {
            metadata = resolved.Value.Metadata;
            if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
            {
                ShellPrint.WarnK($"permission denied: write {resolved.Value.AbsolutePath}", "vfs.perm");
                return Option<KernelError>.Some(KernelError.PermissionsDenied($"write {resolved.Value.AbsolutePath}"));
            }

            return resolved.Value.Mount.Backend.WriteAllBytes(resolved.Value.RelativePath, data);
        }

        string parentAbsolute = FsPath.GetParent(absolute);
        var parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        if (parentResolved.Value.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.Corrupted($"Parent is not a directory: {parentAbsolute}"));

        metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: create/write in {parentResolved.Value.AbsolutePath}", "vfs.perm");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"create/write in {parentResolved.Value.AbsolutePath}"));
        }

        string leaf = FsPath.GetFileName(absolute);
        string rel = string.IsNullOrEmpty(parentResolved.Value.RelativePath)
            ? leaf
            : parentResolved.Value.RelativePath + "/" + leaf;

        return parentResolved.Value.Mount.Backend.WriteAllBytes(rel, data);
    }

    public static Option<KernelError> WriteAllText(string path, string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return WriteAllBytes(path, encoding.GetBytes(text));
    }

    public static Option<KernelError> CreateDirectory(string path, bool recursive = true)
    {
        string absolute = ToAbsolute(path);

        var resolved = ResolvePath(absolute, followFinalSymlink: true);
        if (!resolved.IsFailure && resolved.Value.Kind == FsNodeKind.Directory)
            return Option<KernelError>.None();

        string parentAbsolute = FsPath.GetParent(absolute);
        var parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        var metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: write in {parentResolved.Value.AbsolutePath}", "vfs.perm");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"write in {parentResolved.Value.AbsolutePath}"));
        }

        string leaf = FsPath.GetFileName(absolute);
        string rel = string.IsNullOrEmpty(parentResolved.Value.RelativePath)
            ? leaf
            : parentResolved.Value.RelativePath + "/" + leaf;

        ShellPrint.InfoK($"mkdir: {absolute}", "vfs.fs");
        return parentResolved.Value.Mount.Backend.CreateDirectory(rel, recursive);
    }

    public static Option<KernelError> CreateSymlink(string path, string target)
    {
        string absolute = ToAbsolute(path);
        target = FsPath.SanitizeSymlinkTarget(target);

        var noFollowResolved = ResolvePath(absolute, followFinalSymlink: false);
        if (!noFollowResolved.IsFailure && noFollowResolved.Value.Kind != FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Path already exists: {absolute}"));

        var parentResolved = ResolvePath(FsPath.GetParent(absolute), followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);
        
        var metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: write in {parentResolved.Value.AbsolutePath}", "vfs.perm");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"write in {parentResolved.Value.AbsolutePath}"));
        }

        string leaf = FsPath.GetFileName(absolute);
        string rel = string.IsNullOrEmpty(parentResolved.Value.RelativePath)
            ? leaf
            : parentResolved.Value.RelativePath + "/" + leaf;

        return parentResolved.Value.Mount.Backend.CreateSymbolicLink(rel, target);
    }

    public static Option<KernelError> Delete(string path)
    {
        string absolute = ToAbsolute(path);

        var noFollowResolved = ResolvePath(absolute, followFinalSymlink: false);
        if (noFollowResolved.IsFailure || noFollowResolved.Value.Kind == FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.NotFound(absolute));
        
        string parentAbsolute = FsPath.GetParent(absolute);
        var parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);
        
        var metadata = parentResolved.Value.Metadata;
        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: write in {parentResolved.Value.AbsolutePath}", "vfs.perm");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"write in {parentResolved.Value.AbsolutePath}"));
        }

        return noFollowResolved.Value.Mount.Backend.Delete(noFollowResolved.Value.RelativePath);
    }

    public static Result<string[], KernelError> List(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Result<string[], KernelError>.Failure(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.Directory)
            return Result<string[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a directory: {path}"));
        
        var metadata = resolved.Value.Metadata;
        if (!FsPermissionUtil.CanRead(resolved.Value.Metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            ShellPrint.WarnK($"permission denied: read {resolved.Value.AbsolutePath}", "vfs.perm");
            return Result<string[], KernelError>.Failure(KernelError.PermissionsDenied($"read {resolved.Value.AbsolutePath}"));
        }

        return resolved.Value.Mount.Backend.List(resolved.Value.RelativePath);
    }

    public static Option<KernelError> ChangeMode(string path, FsPermissions permissions)
    {
        var resolved = ResolvePath(path, followFinalSymlink: false);

        if(resolved.IsFailure)
            return Option<KernelError>.Some(resolved.Error);
        
        ShellPrint.InfoK($"chmod: {resolved.Value.AbsolutePath} -> {FsPermissionUtil.ToSymbolicString(permissions)}", "vfs.perm");
        return resolved.Value.Mount.Backend.SetPermissions(resolved.Value.AbsolutePath, permissions);
    }

    public static Result<VfsMetadata, KernelError> Stat(string path)
    {
        var resolved = ResolvePath(path, followFinalSymlink: false);
        if (resolved.IsFailure)
            return Result<VfsMetadata, KernelError>.Failure(resolved.Error);

        return resolved.Value.Metadata;
    }

    private static Result<ResolvedPath, KernelError> ResolvePath(string path, bool followFinalSymlink)
    {
        string absolute = ToAbsolute(path);

        for(int hop = 0; hop < 32; hop++)
        {
            var mount = FindBestMount(absolute);
            if (mount is null)
                return Result<ResolvedPath, KernelError>.Failure(KernelError.NotFound(absolute));
            
            string relative = GetRelativePath(absolute, mount.MountPoint);
            string[] segments = FsPath.SplitRelative(relative);

            if (segments.Length == 0)
            {
                if (!mount.Backend.TryGetMetadata(string.Empty, out var rootMeta))
                    rootMeta = new VfsMetadata(
                        FsNodeKind.Directory, 
                        FsPermissionUtil.DefaultDirectory, 
                        CurrentCredentials.UserId, 
                        CurrentCredentials.GroupId);
                
                if (rootMeta.Kind == FsNodeKind.SymbolicLink && followFinalSymlink)
                {
                    if (!mount.Backend.TryReadLink(string.Empty, out var target))
                        return Result<ResolvedPath, KernelError>.Failure(KernelError.Corrupted($"Broken link: {absolute}"));

                    absolute = ExpandSymbolicLinkTarget(mount.MountPoint, target, string.Empty);
                    continue;
                }

                return new ResolvedPath
                {
                    Mount = mount,
                    RelativePath = string.Empty,
                    AbsolutePath = absolute,
                    Kind = rootMeta.Kind,
                    Metadata = rootMeta
                };
            }

            string currentRel = string.Empty;

            for(int i = 0; i < segments.Length; i++)
            {
                currentRel = string.IsNullOrEmpty(currentRel) ? segments[i] : currentRel + "/" + segments[i];

                if (!mount.Backend.TryGetMetadata(currentRel, out var meta))
                {
                    return new ResolvedPath
                    {
                        Mount = mount,
                        RelativePath = currentRel,
                        AbsolutePath = FsPath.CombineAbsolute(mount.MountPoint, currentRel),
                        Kind = FsNodeKind.Missing,
                        Metadata = default
                    };
                }

                if (meta.Kind == FsNodeKind.SymbolicLink)
                {
                    if (i == segments.Length - 1 && !followFinalSymlink)
                    {
                        return new ResolvedPath
                        {
                            Mount = mount,
                            RelativePath = currentRel,
                            AbsolutePath = FsPath.CombineAbsolute(mount.MountPoint, currentRel),
                            Kind = FsNodeKind.SymbolicLink,
                            Metadata = meta
                        };
                    }

                    if (!mount.Backend.TryReadLink(currentRel, out var target))
                        return Result<ResolvedPath, KernelError>.Failure(KernelError.Corrupted($"Broken link: {absolute}"));
                    
                    string remainder = JoinSegments(segments, i + 1);
                    absolute = ExpandSymbolicLinkTarget(FsPath.CombineAbsolute(mount.MountPoint, currentRel), target, remainder);
                    goto nextHop;
                }
                
                if (meta.Kind == FsNodeKind.Directory && i < segments.Length - 1)
                {
                    if (!FsPermissionUtil.CanRead(meta.Permissions, CurrentCredentials, meta.UserId, meta.GroupId))
                    {
                        string deniedPath = FsPath.CombineAbsolute(mount.MountPoint, currentRel);

                        ShellPrint.WarnK($"permission denied: read {deniedPath}", "vfs.perm");
                        return Result<ResolvedPath, KernelError>.Failure(KernelError.PermissionsDenied($"read {deniedPath}"));
                    }
                }

                if (i == segments.Length - 1)
                {
                    return new ResolvedPath
                    {
                        Mount = mount,
                        RelativePath = currentRel,
                        AbsolutePath = FsPath.CombineAbsolute(mount.MountPoint, currentRel),
                        Kind = meta.Kind,
                        Metadata = meta
                    };
                }
            }

        nextHop:
            continue;
        }

        return Result<ResolvedPath, KernelError>.Failure(KernelError.Corrupted($"Symlink loop detected while resolving: {path}"));
    }

    private static MountInfo? FindBestMount(string absolutePath)
    {
        absolutePath = FsPath.NormalizeAbsolute(absolutePath);

        MountInfo? best = null;
        int bestLength = -1;

        foreach (var mount in _mounts)
        {
            if (!IsUnderMount(absolutePath, mount.MountPoint))
                continue;

            if (mount.MountPoint.Length > bestLength)
            {
                best = mount;
                bestLength = mount.MountPoint.Length;
            }
        }

        return best;
    }

    private static bool IsUnderMount(string absolutePath, string mountPoint)
    {
        absolutePath = FsPath.NormalizeAbsolute(absolutePath);
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        if (mountPoint == "/")
            return true;

        if (absolutePath == mountPoint)
            return true;

        return absolutePath.StartsWith(mountPoint + "/", StringComparison.Ordinal);
    }

    private static string GetRelativePath(string absolutePath, string mountPoint)
    {
        absolutePath = FsPath.NormalizeAbsolute(absolutePath);
        mountPoint = FsPath.NormalizeAbsolute(mountPoint);

        if (mountPoint == "/")
            return absolutePath.Length <= 1 ? string.Empty : absolutePath[1..];

        if (absolutePath == mountPoint)
            return string.Empty;

        if (!absolutePath.StartsWith(mountPoint + "/", StringComparison.Ordinal))
            return string.Empty;

        return absolutePath[(mountPoint.Length + 1)..];
    }

    private static string ToAbsolute(string path)
    {
        if (FsPath.IsAbsolute(path))
            return FsPath.NormalizeAbsolute(path);

        return FsPath.NormalizeAbsolute(FsPath.CombineAbsolute(CurrentDirectory, path));
    }

    private static string ExpandSymbolicLinkTarget(string symlinkAbsolutePath, string symlinkTarget, string remainder)
    {
        symlinkTarget = FsPath.SanitizeSymlinkTarget(symlinkTarget);
        remainder = FsPath.NormalizeRelative(remainder);

        if (FsPath.IsAbsolute(symlinkTarget))
            return FsPath.CombineAbsolute(symlinkTarget, remainder);

        string parent = FsPath.GetParent(symlinkAbsolutePath);
        string combined = FsPath.CombineRelative(symlinkTarget, remainder);
        return FsPath.CombineAbsolute(parent, combined);
    }

    private static string JoinSegments(string[] segments, int startIndex)
    {
        if (startIndex < 0 || startIndex >= segments.Length)
            return string.Empty;

        if (startIndex == segments.Length - 1)
            return segments[startIndex];

        var list = new List<string>(segments.Length - startIndex);
        for (int i = startIndex; i < segments.Length; i++)
            list.Add(segments[i]);

        return string.Join("/", list);
    }
}