// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
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
                        CurrentCredentials.GroupId,
                        0);
                
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

                        s_logger.Warn($"permission denied: read {deniedPath}");
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

    internal static MountInfo? FindBestMount(string absolutePath)
    {
        absolutePath = FsPath.NormalizeAbsolute(absolutePath);

        MountInfo? best = null;
        int bestLength = -1;

        foreach (var mount in s_mounts)
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