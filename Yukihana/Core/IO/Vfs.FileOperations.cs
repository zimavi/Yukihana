// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
    public static Result<byte[], KernelError> ReadAllBytes(string path)
    {
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Result<byte[], KernelError>.Failure(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.File)
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        VfsMetadata metadata = resolved.Value.Metadata;

        if (!FsPermissionUtil.CanRead(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            s_logger.Warn($"permission denied: read {resolved.Value.AbsolutePath}");
            return Result<byte[], KernelError>.Failure(KernelError.PermissionsDenied($"read {resolved.Value.AbsolutePath}"));
        }

        return resolved.Value.Mount.Backend.ReadAllBytes(resolved.Value.RelativePath);
    }

    public static Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return ReadAllBytes(path).Map(bytes => encoding.GetString(bytes));
    }

    public static Result<Stream, KernelError> Open(
        string path,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.Read)
    {
        string absolute = ToAbsolute(path);

        if (mode == FileMode.Append && (access & FileAccess.Write) == 0)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Append requires write access: {absolute}"));

        Result<ResolvedPath, KernelError> resolved = ResolvePath(absolute, followFinalSymlink: true);

        if (!resolved.IsFailure && resolved.Value.Kind == FsNodeKind.Directory)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Path is a directory: {absolute}"));

        if (!resolved.IsFailure && resolved.Value.Kind != FsNodeKind.Missing)
        {
            if (!CanOpenExisting(resolved.Value.Metadata, access, mode, resolved.Value.AbsolutePath))
                return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Permission denied: {resolved.Value.AbsolutePath}"));

            return resolved.Value.Mount.Backend.Open(resolved.Value.RelativePath, mode, access, share);
        }

        if (mode is FileMode.Open or FileMode.Truncate)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(absolute));

        string parentAbsolute = FsPath.GetParent(absolute);
        Result<ResolvedPath, KernelError> parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);

        if (parentResolved.IsFailure)
            return Result<Stream, KernelError>.Failure(parentResolved.Error);

        if (parentResolved.Value.Kind != FsNodeKind.Directory)
            return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Parent is not a directory: {parentAbsolute}"));

        if (!FsPermissionUtil.CanAccess(
                parentResolved.Value.Metadata.Permissions,
                FsAccess.Write | FsAccess.Execute,
                CurrentCredentials,
                parentResolved.Value.Metadata.UserId,
                parentResolved.Value.Metadata.GroupId))
        {
            s_logger.Warn($"permission denied: create in {parentResolved.Value.AbsolutePath}");
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Permission denied: {parentResolved.Value.AbsolutePath}"));
        }

        string leaf = FsPath.GetFileName(absolute);
        string rel = string.IsNullOrEmpty(parentResolved.Value.RelativePath)
            ? leaf
            : parentResolved.Value.RelativePath + "/" + leaf;

        return parentResolved.Value.Mount.Backend.Open(rel, mode, access, share);
    }

    public static Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        string absolute = ToAbsolute(path);

        Result<ResolvedPath, KernelError> resolved = ResolvePath(absolute, followFinalSymlink: true);
        VfsMetadata metadata;
        if (!resolved.IsFailure && resolved.Value.Kind == FsNodeKind.File)
        {
            metadata = resolved.Value.Metadata;
            if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
            {
                s_logger.Warn($"permission denied: write {resolved.Value.AbsolutePath}");
                return Option<KernelError>.Some(KernelError.PermissionsDenied($"write {resolved.Value.AbsolutePath}"));
            }

            return resolved.Value.Mount.Backend.WriteAllBytes(resolved.Value.RelativePath, data);
        }

        string parentAbsolute = FsPath.GetParent(absolute);
        Result<ResolvedPath, KernelError> parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        if (parentResolved.Value.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.Corrupted($"Parent is not a directory: {parentAbsolute}"));

        metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            s_logger.Warn($"permission denied: create/write in {parentResolved.Value.AbsolutePath}");
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

        Result<ResolvedPath, KernelError> resolved = ResolvePath(absolute, followFinalSymlink: true);
        if (!resolved.IsFailure && resolved.Value.Kind == FsNodeKind.Directory)
            return Option<KernelError>.None();

        string parentAbsolute = FsPath.GetParent(absolute);
        Result<ResolvedPath, KernelError> parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        VfsMetadata metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            s_logger.Warn($"permission denied: write in {parentResolved.Value.AbsolutePath}");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"write in {parentResolved.Value.AbsolutePath}"));
        }

        string leaf = FsPath.GetFileName(absolute);
        string rel = string.IsNullOrEmpty(parentResolved.Value.RelativePath)
            ? leaf
            : parentResolved.Value.RelativePath + "/" + leaf;

        s_logger.Warn($"mkdir: {absolute}");
        return parentResolved.Value.Mount.Backend.CreateDirectory(rel, recursive);
    }

    public static Option<KernelError> CreateSymbolicLink(string path, string target)
    {
        string absolute = ToAbsolute(path);
        target = FsPath.SanitizeSymlinkTarget(target);

        Result<ResolvedPath, KernelError> noFollowResolved = ResolvePath(absolute, followFinalSymlink: false);
        if (!noFollowResolved.IsFailure && noFollowResolved.Value.Kind != FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Path already exists: {absolute}"));

        Result<ResolvedPath, KernelError> parentResolved = ResolvePath(FsPath.GetParent(absolute), followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        VfsMetadata metadata = parentResolved.Value.Metadata;

        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            s_logger.Warn($"permission denied: write in {parentResolved.Value.AbsolutePath}");
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

        Result<ResolvedPath, KernelError> noFollowResolved = ResolvePath(absolute, followFinalSymlink: false);
        if (noFollowResolved.IsFailure || noFollowResolved.Value.Kind == FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.NotFound(absolute));

        string parentAbsolute = FsPath.GetParent(absolute);
        Result<ResolvedPath, KernelError> parentResolved = ResolvePath(parentAbsolute, followFinalSymlink: true);
        if (parentResolved.IsFailure)
            return Option<KernelError>.Some(parentResolved.Error);

        VfsMetadata metadata = parentResolved.Value.Metadata;
        if (!FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
        {
            s_logger.Warn($"permission denied: write in {parentResolved.Value.AbsolutePath}");
            return Option<KernelError>.Some(KernelError.PermissionsDenied($"write in {parentResolved.Value.AbsolutePath}"));
        }

        return noFollowResolved.Value.Mount.Backend.Delete(noFollowResolved.Value.RelativePath);
    }

    public static Result<string[], KernelError> List(string path)
    {
        Result<ResolvedPath, KernelError> resolved = ResolvePath(path, followFinalSymlink: true);
        if (resolved.IsFailure)
            return Result<string[], KernelError>.Failure(resolved.Error);

        if (resolved.Value.Kind != FsNodeKind.Directory)
            return Result<string[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a directory: {path}"));

        VfsMetadata metadata = resolved.Value.Metadata;
        if (!FsPermissionUtil.CanRead(
            resolved.Value.Metadata.Permissions,
            CurrentCredentials,
            metadata.UserId,
            metadata.GroupId))
        {
            s_logger.Warn($"permission denied: read {resolved.Value.AbsolutePath}");
            return Result<string[], KernelError>.Failure(KernelError.PermissionsDenied($"read {resolved.Value.AbsolutePath}"));
        }

        Result<string[], KernelError> backendResult = resolved.Value.Mount.Backend.List(resolved.Value.RelativePath);
        if (backendResult.IsFailure)
            return backendResult;

        var entries = new HashSet<string>(backendResult.Value, StringComparer.Ordinal);

        string currentAbs = resolved.Value.AbsolutePath;

        foreach (MountInfo mount in s_mounts)
        {
            string mountPoint = mount.MountPoint;

            if (mountPoint == currentAbs)
                continue;

            string parent = FsPath.GetParent(mountPoint);

            if (!string.Equals(parent, currentAbs, StringComparison.Ordinal))
                continue;

            string name = FsPath.GetFileName(mountPoint);

            if (!string.IsNullOrEmpty(name))
                entries.Add(name);
        }

        return Result<string[], KernelError>.Success([.. entries.OrderBy(e => e, StringComparer.Ordinal)]);
    }
}
