// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class OverlayFs : IVfsBackend
{
    private readonly IVfsBackend _lower;
    private readonly IVfsBackend _upper;

    private readonly HashSet<string> _whiteouts = new(StringComparer.Ordinal);

    public OverlayFs(IVfsBackend lower, IVfsBackend upper)
    {
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(upper);

        _lower = lower;
        _upper = upper;
    }

    public bool Exists(string path)
    {
        path = Normalize(path);
        return GetKind(path) != FsNodeKind.Missing;
    }

    public FsNodeKind GetKind(string path)
    {
        path = Normalize(path);

        if (IsHiddenByWhiteout(path))
            return FsNodeKind.Missing;

        if (TryGetUpperMetadata(path, out var upperMeta))
            return upperMeta.Kind;

        if (TryGetLowerMetadata(path, out var lowerMeta))
            return lowerMeta.Kind;

        return FsNodeKind.Missing;
    }

    public bool TryReadLink(string path, out string target)
    {
        path = Normalize(path);
        target = string.Empty;

        if (IsHiddenByWhiteout(path))
            return false;

        if (TryGetUpperMetadata(path, out var upperMeta) && upperMeta.Kind == FsNodeKind.SymbolicLink)
            return _upper.TryReadLink(path, out target);

        if (TryGetLowerMetadata(path, out var lowerMeta) && lowerMeta.Kind == FsNodeKind.SymbolicLink)
            return _lower.TryReadLink(path, out target);

        return false;
    }

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        path = Normalize(path);

        if (IsHiddenByWhiteout(path))
        {
            metadata = default;
            return false;
        }

        if (TryGetUpperMetadata(path, out metadata))
            return true;

        if (TryGetLowerMetadata(path, out metadata))
            return true;

        metadata = default;
        return false;
    }

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        path = Normalize(path);

        if (!TryResolveEffectiveFile(path, out var backend, out var relativePath))
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        ShellPrint.InfoK($"read lower/upper file: {path}", "overlay.read");
        return backend.ReadAllBytes(relativePath);
    }

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return ReadAllBytes(path).Map(bytes => encoding.GetString(bytes));
    }

    public Result<Stream, KernelError> Open(
        string path,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.Read)
    {
        path = Normalize(path);

        ShellPrint.InfoK($"open request: {path} mode={mode} access={access}", "overlay.open");

        bool wantsWrite = (access & FileAccess.Write) != 0;
        bool createLike = mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate or FileMode.Append;
        bool truncateLike = mode == FileMode.Truncate;

        if (IsHiddenByWhiteout(path))
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (TryGetUpperMetadata(path, out var upperMeta))
        {
            if (upperMeta.Kind == FsNodeKind.Directory)
                return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is a directory: {path}"));

            return _upper.Open(path, mode, access, share);
        }

        if (TryGetLowerMetadata(path, out var lowerMeta))
        {
            if (lowerMeta.Kind == FsNodeKind.Directory)
                return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is a directory: {path}"));

            if (mode == FileMode.CreateNew)
                return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"File already exists: {path}"));

            if (wantsWrite || createLike || truncateLike)
            {
                var copyUp = CopyUpNode(path, lowerMeta.Kind);
                if (copyUp.IsSome)
                    return Result<Stream, KernelError>.Failure(copyUp.Value);

                if (lowerMeta.Kind == FsNodeKind.File && _lower.TryGetMetadata(path, out var meta))
                {
                    // preserve lower permissions on copy-up
                    var chmod = _upper.SetPermissions(path, meta.Permissions);
                    if (chmod.IsSome)
                        return Result<Stream, KernelError>.Failure(chmod.Value);
                }

                return _upper.Open(path, mode, access, share);
            }

            return _lower.Open(path, mode, access, share);
        }

        if (mode == FileMode.Open || mode == FileMode.Truncate)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        return _upper.Open(path, mode, access, share);
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        path = Normalize(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot write to the overlay root."));

        ShellPrint.InfoK($"write request: {path} ({data.Length} bytes)", "overlay.write");

        if (TryGetUpperMetadata(path, out var upperMeta))
        {
            if (upperMeta.Kind == FsNodeKind.Directory)
                return Option<KernelError>.Some(KernelError.InvalidOp($"Cannot overwrite directory with file: {path}"));

            if (upperMeta.Kind == FsNodeKind.SymbolicLink)
            {
                if (!TryResolveSymlinkTarget(path, out var resolvedTarget))
                    return Option<KernelError>.Some(KernelError.Corrupted($"Broken symlink: {path}"));

                return WriteAllBytes(resolvedTarget, data);
            }

            return _upper.WriteAllBytes(path, data);
        }

        if (TryGetLowerMetadata(path, out var lowerMeta))
        {
            if (lowerMeta.Kind == FsNodeKind.Directory)
                return Option<KernelError>.Some(KernelError.InvalidOp($"Cannot overwrite directory with file: {path}"));

            if (lowerMeta.Kind == FsNodeKind.SymbolicLink)
            {
                if (!TryResolveSymlinkTarget(path, out var resolvedTarget))
                    return Option<KernelError>.Some(KernelError.Corrupted($"Broken symlink: {path}"));

                return WriteAllBytes(resolvedTarget, data);
            }

            var copyUp = CopyUpFile(path);
            if (copyUp.IsSome)
                return copyUp;

            return _upper.WriteAllBytes(path, data);
        }

        var parentResult = EnsureWritableParentDirectory(path);
        if (parentResult.IsSome)
            return parentResult;

        ClearWhiteoutExact(path);

        return _upper.WriteAllBytes(path, data);
    }

    public Option<KernelError> CreateDirectory(string path, bool recursive)
    {
        path = Normalize(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.None();

        ShellPrint.InfoK($"mkdir request: {path}", "overlay.mkdir");

        if (TryGetUpperMetadata(path, out var upperMeta))
        {
            if (upperMeta.Kind == FsNodeKind.File)
                return Option<KernelError>.Some(KernelError.InvalidOp($"File already exists: {path}"));

            if (upperMeta.Kind == FsNodeKind.Directory)
                return Option<KernelError>.None();
        }

        if (TryGetLowerMetadata(path, out var lowerMeta) && lowerMeta.Kind == FsNodeKind.File)
            return Option<KernelError>.Some(KernelError.InvalidOp($"File already exists: {path}"));

        var parentResult = EnsureWritableParentDirectory(path);
        if (parentResult.IsSome)
            return Option<KernelError>.Some(parentResult.Value);

        ClearWhiteoutExact(path);

        var result = _upper.CreateDirectory(path, recursive: true);
        if (result.IsSome)
            return result;

        ShellPrint.OkK($"directory visible in upper: {path}", "overlay.mkdir");
        return Option<KernelError>.None();
    }

    public Option<KernelError> CreateSymbolicLink(string path, string target)
    {
        path = Normalize(path);
        target = target.Replace('\\', '/').Trim();

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot create a symlink at the root."));

        ShellPrint.InfoK($"symlink request: {path} -> {target}", "overlay.symlink");

        if (TryGetUpperMetadata(path, out var upperMeta) && upperMeta.Kind != FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Path already exists: {path}"));

        if (TryGetLowerMetadata(path, out var lowerMeta) && lowerMeta.Kind != FsNodeKind.Missing)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Path already exists: {path}"));

        var parentResult = EnsureWritableParentDirectory(path);
        if (parentResult.IsSome)
            return parentResult;

        ClearWhiteoutExact(path);

        var result = _upper.CreateSymbolicLink(path, target);
        if (result.IsSome)
            return result;

        ShellPrint.OkK($"symlink created in upper: {path}", "overlay.symlink");
        return Option<KernelError>.None();
    }

    public Option<KernelError> Delete(string path)
    {
        path = Normalize(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot delete the overlay root."));

        ShellPrint.InfoK($"delete request: {path}", "overlay.delete");

        bool upperExists = TryGetUpperMetadata(path, out var upperMeta);
        bool lowerExists = TryGetLowerMetadata(path, out var lowerMeta);

        if (!upperExists && !lowerExists)
            return Option<KernelError>.Some(KernelError.NotFound(path));

        if (upperExists)
        {
            var result = _upper.Delete(path);
            if (result.IsSome)
                return result;

            ShellPrint.WarnK($"removed upper entry: {path}", "overlay.delete");

            return Option<KernelError>.None();
        }

        _whiteouts.Add(path);
        ShellPrint.WarnK($"whiteout created: {path}", "overlay.whiteout");
        return Option<KernelError>.None();
    }

    public Result<string[], KernelError> List(string path)
    {
        path = Normalize(path);

        if (IsHiddenByWhiteout(path))
            return Result<string[], KernelError>.Failure(KernelError.NotFound(path));

        if (!TryGetMetadata(path, out var metadata) || metadata.Kind != FsNodeKind.Directory)
            return Result<string[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a directory: {path}"));

        var result = new HashSet<string>(StringComparer.Ordinal);

        if (TryGetUpperMetadata(path, out var upperMeta) && upperMeta.Kind == FsNodeKind.Directory)
        {
            var upperList = _upper.List(path);
            if (upperList.IsFailure)
                return Result<string[], KernelError>.Failure(upperList.Error);

            foreach (var name in upperList.Value)
                result.Add(name);
        }

        // Add lower layer entries unless hidden or overridden by upper.
        if (TryGetLowerMetadata(path, out var lowerMeta) && lowerMeta.Kind == FsNodeKind.Directory)
        {
            var lowerList = _lower.List(path);
            if (lowerList.IsFailure)
                return Result<string[], KernelError>.Failure(lowerList.Error);

            foreach (var name in lowerList.Value)
            {
                string childPath = Combine(path, name);

                if (result.Contains(name))
                    continue;

                if (IsHiddenByWhiteout(childPath))
                    continue;

                result.Add(name);
            }
        }

        var array = result.ToArray();
        Array.Sort(array, StringComparer.Ordinal);

        return array;
    }

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions)
    {
        path = Normalize(path);

        ShellPrint.InfoK($"chmod request: {path} -> {FsPermissionUtil.ToSymbolicString(permissions)}", "overlay.perm");

        if (TryGetUpperMetadata(path, out var upperMeta))
        {
            var result = _upper.SetPermissions(path, permissions);
            if (result.IsSome)
                return result;

            return Option<KernelError>.None();
        }

        if (TryGetLowerMetadata(path, out var lowerMeta))
        {
            var copyUp = CopyUpNode(path, lowerMeta.Kind);
            if (copyUp.IsSome)
                return copyUp;

            var result = _upper.SetPermissions(path, permissions);
            if (result.IsSome)
                return result;

            return Option<KernelError>.None();
        }

        return Option<KernelError>.Some(KernelError.NotFound(path));
    }

    private bool TryResolveEffectiveFile(string path, out IVfsBackend backend, out string relativePath)
    {
        backend = _upper;
        relativePath = path;

        if (IsHiddenByWhiteout(path))
            return false;

        if (TryGetUpperMetadata(path, out var upperMeta))
        {
            if (upperMeta.Kind == FsNodeKind.File)
            {
                backend = _upper;
                relativePath = path;
                return true;
            }

            if (upperMeta.Kind == FsNodeKind.SymbolicLink)
            {
                if (!TryResolveSymlinkTarget(path, out var resolvedTarget))
                    return false;

                return TryResolveEffectiveFile(resolvedTarget, out backend, out relativePath);
            }

            return false;
        }

        if (TryGetLowerMetadata(path, out var lowerMeta))
        {
            if (lowerMeta.Kind == FsNodeKind.File)
            {
                backend = _lower;
                relativePath = path;
                return true;
            }

            if (lowerMeta.Kind == FsNodeKind.SymbolicLink)
            {
                if (!TryResolveSymlinkTarget(path, out var resolvedTarget))
                    return false;

                return TryResolveEffectiveFile(resolvedTarget, out backend, out relativePath);
            }
        }

        return false;
    }

    private bool TryResolveSymlinkTarget(string path, out string resolvedTarget)
    {
        resolvedTarget = string.Empty;

        if (IsHiddenByWhiteout(path))
            return false;

        if (TryGetUpperMetadata(path, out var upperMeta) && upperMeta.Kind == FsNodeKind.SymbolicLink)
        {
            if (!_upper.TryReadLink(path, out var target))
                return false;

            resolvedTarget = target;
            return true;
        }

        if (TryGetLowerMetadata(path, out var lowerMeta) && lowerMeta.Kind == FsNodeKind.SymbolicLink)
        {
            if (!_lower.TryReadLink(path, out var target))
                return false;

            resolvedTarget = target;
            return true;
        }

        return false;
    }

    private Option<KernelError> CopyUpFile(string path)
    {
        path = Normalize(path);

        if (!TryGetLowerMetadata(path, out var lowerMeta))
            return Option<KernelError>.Some(KernelError.NotFound(path));

        if (lowerMeta.Kind == FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Cannot copy up directory as file: {path}"));

        var parentResult = EnsureWritableParentDirectory(path);
        if (parentResult.IsSome)
            return parentResult;

        ClearWhiteoutExact(path);

        if (lowerMeta.Kind == FsNodeKind.SymbolicLink)
        {
            if (!_lower.TryReadLink(path, out var target))
                return Option<KernelError>.Some(KernelError.Corrupted($"Broken symlink: {path}"));

            var createLink = _upper.CreateSymbolicLink(path, target);
            if (createLink.IsSome)
                return createLink;

            ShellPrint.InfoK($"copy-up symlink: {path}", "overlay.cow");
            return Option<KernelError>.None();
        }

        var bytes = _lower.ReadAllBytes(path);
        if (bytes.IsFailure)
            return Option<KernelError>.Some(bytes.Error);

        var write = _upper.WriteAllBytes(path, bytes.Value);
        if (write.IsSome)
            return write;

        ShellPrint.InfoK($"copy-up file: {path}", "overlay.cow");
        return Option<KernelError>.None();
    }

    private Option<KernelError> CopyUpNode(string path, FsNodeKind kind)
    {
        path = Normalize(path);

        if (kind == FsNodeKind.File)
            return CopyUpFile(path);

        if (kind == FsNodeKind.Directory)
        {
            var parent = FsPath.GetParent(path);
            var ensure = EnsureWritableParentDirectory(parent);
            if (ensure.IsSome)
                return ensure;

            ClearWhiteoutExact(path);

            var create = _upper.CreateDirectory(path, recursive: true);
            if (create.IsSome)
                return create;

            ShellPrint.InfoK($"copy-up directory: {path}", "overlay.cow");
            return Option<KernelError>.None();
        }

        if (kind == FsNodeKind.SymbolicLink)
        {
            if (!_lower.TryReadLink(path, out var target))
                return Option<KernelError>.Some(KernelError.Corrupted($"Broken symlink: {path}"));

            var parent = FsPath.GetParent(path);
            var ensure = EnsureWritableParentDirectory(parent);
            if (ensure.IsSome)
                return ensure;

            ClearWhiteoutExact(path);

            var createLink = _upper.CreateSymbolicLink(path, target);
            if (createLink.IsSome)
                return createLink;

            ShellPrint.InfoK($"copy-up symlink: {path}", "overlay.cow");
            return Option<KernelError>.None();
        }

        return Option<KernelError>.Some(KernelError.NotFound(path));
    }

    private Option<KernelError> EnsureWritableParentDirectory(string path)
    {
        path = Normalize(path);
        string parent = FsPath.GetParent(path);

        if (string.IsNullOrEmpty(parent))
            parent = string.Empty;

        ClearWhiteoutsAlongPath(parent);

        var create = _upper.CreateDirectory(parent, recursive: true);
        if (create.IsSome)
            return create;

        return Option<KernelError>.None();
    }

    private bool TryGetUpperMetadata(string path, out VfsMetadata metadata)
    {
        path = Normalize(path);

        if (IsHiddenByWhiteout(path))
        {
            metadata = default;
            return false;
        }

        return _upper.TryGetMetadata(path, out metadata);
    }

    private bool TryGetLowerMetadata(string path, out VfsMetadata metadata)
    {
        path = Normalize(path);

        if (IsHiddenByWhiteout(path))
        {
            metadata = default;
            return false;
        }

        return _lower.TryGetMetadata(path, out metadata);
    }

    private bool IsHiddenByWhiteout(string path)
    {
        path = Normalize(path);

        if (string.IsNullOrEmpty(path))
            return false;

        if (_whiteouts.Contains(path))
            return true;

        string current = string.Empty;
        foreach (var segment in FsPath.SplitRelative(path))
        {
            current = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;

            if (_whiteouts.Contains(current))
                return true;
        }

        return false;
    }

    private void ClearWhiteoutExact(string path)
    {
        path = Normalize(path);

        if (_whiteouts.Remove(path))
            ShellPrint.InfoK($"whiteout removed: {path}", "overlay.whiteout");
    }

    private void ClearWhiteoutsAlongPath(string path)
    {
        path = Normalize(path);

        if (string.IsNullOrEmpty(path))
            return;

        string current = string.Empty;

        foreach (var segment in FsPath.SplitRelative(path))
        {
            current = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;

            if (_whiteouts.Remove(current))
                ShellPrint.InfoK($"whiteout removed: {current}", "overlay.whiteout");
        }
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return FsPath.NormalizeRelative(path);
    }

    private static string Combine(string left, string right)
    {
        left = Normalize(left);
        right = Normalize(right);

        if (string.IsNullOrEmpty(left))
            return right;

        if (string.IsNullOrEmpty(right))
            return left;

        return left + "/" + right;
    }
}