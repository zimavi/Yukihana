// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class TempFs : IVfsBackend
{
    private sealed class Node
    {
        public FsNodeKind Kind;
        public Dictionary<string, Node> Children = new(StringComparer.Ordinal);
        public byte[]? Data;
        public string? LinkTarget;
        public FsPermissions Permissions;
        public int UserId;
        public int GroupId;

        public Node(FsNodeKind kind)
        {
            Kind = kind;
            Permissions = kind switch
            {
                FsNodeKind.Directory => FsPermissionUtil.DefaultDirectory,
                FsNodeKind.SymbolicLink => FsPermissionUtil.DefaultSymbolic,
                _ => FsPermissionUtil.DefaultFile,
            };
        }
    }

    private sealed class NodeBacking : IRamFsStreamBacking
    {
        private readonly Node _node;

        public NodeBacking(Node node)
        {
            _node = node;
        }

        public long Length => _node.Data?.LongLength ?? 0;
        public bool CanRead => true;
        public bool CanWrite => true;
        public bool CanSeek => true;

        public int Read(long position, Span<byte> buffer)
        {
            byte[] data = _node.Data ?? Array.Empty<byte>();

            if (position < 0 || position > data.LongLength)
                throw new ArgumentOutOfRangeException(nameof(position));

            int remaining = data.Length - (int)position;
            if (remaining <= 0 || buffer.Length == 0)
                return 0;

            int toCopy = Math.Min(remaining, buffer.Length);
            new ReadOnlySpan<byte>(data, (int)position, toCopy).CopyTo(buffer);
            return toCopy;
        }

        public void Write(long position, ReadOnlySpan<byte> buffer)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            long required = position + buffer.Length;
            if (required > int.MaxValue)
                throw new IOException("File too large.");

            byte[] data = _node.Data ?? Array.Empty<byte>();
            if (required > data.Length)
            {
                var grown = new byte[(int)required];
                if (data.Length != 0)
                    Buffer.BlockCopy(data, 0, grown, 0, data.Length);
                data = grown;
            }

            buffer.CopyTo(new Span<byte>(data, (int)position, buffer.Length));
            _node.Data = data;
        }

        public void SetLength(long length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > int.MaxValue)
                throw new IOException("File too large.");

            byte[] data = _node.Data ?? Array.Empty<byte>();
            if (data.Length == length)
                return;

            var resized = new byte[(int)length];
            Buffer.BlockCopy(data, 0, resized, 0, Math.Min(data.Length, resized.Length));
            _node.Data = resized;
        }

        public void Flush() { }
    }


    private readonly Node _root = new(FsNodeKind.Directory);

    public bool Exists(string path)
    {
        path = FsPath.NormalizeRelative(path);
        return FindExactNode(path) is not null;
    }

    public FsNodeKind GetKind(string path)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        return node?.Kind ?? FsNodeKind.Missing;
    }

    public bool TryReadLink(string path, out string target)
    {
        path = FsPath.NormalizeRelative(path);
        target = string.Empty;

        var node = FindExactNode(path);
        if (node is null)
            return false;

        if (node.Kind != FsNodeKind.SymbolicLink || string.IsNullOrWhiteSpace(node.LinkTarget))
            return false;

        target = node.LinkTarget;

        return true;
    }

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        if (node is null)
        {
            metadata = default;
            return false;
        }

        metadata = new VfsMetadata(node.Kind, node.Permissions, node.UserId, node.GroupId);
        return true;
    }

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        if (node is null)
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.File)
            return Result<byte[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a regular file: {path}"));

        byte[] data = node.Data ?? Array.Empty<byte>();
        var copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);
        return copy;
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
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Result<Stream, KernelError>.Failure(KernelError.Corrupted("Cannot open the root directory as a file."));

        bool needsWrite = (access & FileAccess.Write) != 0;
        bool createMissing = mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate or FileMode.Append;
        bool truncateExisting = mode is FileMode.Create or FileMode.Truncate;
        bool append = mode == FileMode.Append;

        if (append && !needsWrite)
            return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Append requires write access: {path}"));

        var parentPath = FsPath.GetParent(path);
        if (createMissing || truncateExisting || append || mode == FileMode.CreateNew)
        {
            var ensure = TryEnsureDirectory(parentPath, recursive: true, out var dirError);
            if (!ensure)
                return Result<Stream, KernelError>.Failure(dirError);
        }

        var node = FindExactNode(path);

        switch (mode)
        {
            case FileMode.Open:
                if (node is null)
                    return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));
                break;

            case FileMode.CreateNew:
                if (node is not null)
                    return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"File already exists: {path}"));
                node = CreateFileNode(path);
                break;

            case FileMode.Create:
                if (node is null)
                    node = CreateFileNode(path);
                else
                {
                    if (node.Kind != FsNodeKind.File)
                        return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a file: {path}"));
                    node.Data = Array.Empty<byte>();
                }
                break;

            case FileMode.OpenOrCreate:
                if (node is null)
                    node = CreateFileNode(path);
                break;

            case FileMode.Truncate:
                if (node is null)
                    return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));
                if (node.Kind != FsNodeKind.File)
                    return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a file: {path}"));
                node.Data = Array.Empty<byte>();
                break;

            case FileMode.Append:
                if (node is null)
                    node = CreateFileNode(path);
                break;

            default:
                return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Unsupported file mode: {mode}"));
        }

        if (node is null)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.File)
            return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a regular file: {path}"));

        if (needsWrite && mode == FileMode.Open && node.Data is null)
        {
            // Still okay. Opening with write is allowed on writable backend.
        }

        long initialPosition = append ? (node.Data?.LongLength ?? 0) : 0;
        var stream = new RamFsStream(new NodeBacking(node), access, share, initialPosition);
        return stream;
    }

    private Node CreateFileNode(string path)
    {
        string parentPath = FsPath.GetParent(path);
        var parent = FindExactNode(parentPath);

        if (parent is null || parent.Kind != FsNodeKind.Directory)
            throw new DirectoryNotFoundException(parentPath);

        string name = FsPath.GetFileName(path);
        var node = new Node(FsNodeKind.File)
        {
            UserId = VFS.CurrentCredentials.UserId,
            GroupId = VFS.CurrentCredentials.GroupId,
        };
        parent.Children[name] = node;
        return node;
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot write to the root directory."));

        if (!TryEnsureDirectory(FsPath.GetParent(path), recursive: true, out var dirError))
            return Option<KernelError>.Some(dirError);

        string name = FsPath.GetFileName(path);
        var parent = FindExactNode(FsPath.GetParent(path));
        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(FsPath.GetParent(path)));

        if ((parent.Children.TryGetValue(name, out var existing) is bool exists) && existing!.Kind == FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.InvalidOp($"Cannot overwrite directory with file: {path}"));

        var copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);

        parent.Children[name] = new Node(FsNodeKind.File)
        {
            Data = copy,
            Permissions = exists ? existing!.Permissions : FsPermissionUtil.DefaultFile,
            UserId = exists ? existing!.UserId : VFS.CurrentCredentials.UserId,
            GroupId = exists ? existing!.GroupId : VFS.CurrentCredentials.GroupId,
        };

        return Option<KernelError>.None();
    }

    public Option<KernelError> CreateDirectory(string path, bool recursive)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.None();

        if (!TryEnsureDirectory(path, recursive, out var error))
            return Option<KernelError>.Some(error);

        return Option<KernelError>.None();
    }

    public Option<KernelError> CreateSymbolicLink(string path, string target)
    {
        path = FsPath.NormalizeRelative(path);
        target = FsPath.SanitizeSymlinkTarget(target);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot create a symlink at the root directory."));

        if (!TryEnsureDirectory(FsPath.GetParent(path), recursive: true, out var dirError))
            return Option<KernelError>.Some(dirError);

        string name = FsPath.GetFileName(path);
        var parent = FindExactNode(FsPath.GetParent(path));
        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(FsPath.GetParent(path)));

        parent.Children[name] = new Node(FsNodeKind.SymbolicLink)
        {
            LinkTarget = target,
            Permissions = FsPermissionUtil.DefaultSymbolic,
            UserId = VFS.CurrentCredentials.UserId,
            GroupId = VFS.CurrentCredentials.GroupId,
        };

        return Option<KernelError>.None();
    }

    public Option<KernelError> Delete(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot delete the root directory."));

        var parentPath = FsPath.GetParent(path);
        var parent = FindExactNode(parentPath);

        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(parentPath));

        string name = FsPath.GetFileName(path);
        if (!parent.Children.Remove(name))
            return Option<KernelError>.Some(KernelError.NotFound(path));

        return Option<KernelError>.None();
    }

    public Result<string[], KernelError> List(string path)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        if (node is null)
            return Result<string[], KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.Directory)
            return Result<string[], KernelError>.Failure(KernelError.Corrupted($"Path is not a directory: {path}"));

        var items = node.Children.Keys.ToArray();
        Array.Sort(items, StringComparer.Ordinal);
        return items;
    }

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        if (node is null)
            return Option<KernelError>.Some(KernelError.NotFound(path));

        node.Permissions = permissions;
        return Option<KernelError>.None();
    }

    private Node? FindExactNode(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return _root;

        var current = _root;

        foreach (var segment in FsPath.SplitRelative(path))
        {
            if (!current.Children.TryGetValue(segment, out var next))
                return null;

            current = next;
        }

        return current;
    }

    private bool TryEnsureDirectory(string path, bool recursive, out KernelError error)
    {
        error = KernelError.Corrupted("Unknown filesystem error.");

        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
        {
            _root.Kind = FsNodeKind.Directory;
            return true;
        }

        var parts = FsPath.SplitRelative(path);
        var current = _root;
        string currentPath = string.Empty;

        for (int i = 0; i < parts.Length; i++)
        {
            string segment = parts[i];
            string nextPath = string.IsNullOrEmpty(currentPath) ? segment : currentPath + "/" + segment;

            if (!current.Children.TryGetValue(segment, out var next))
            {
                if (!recursive && i != parts.Length - 1)
                {
                    error = KernelError.NotFound(currentPath);
                    return false;
                }

                next = new Node(FsNodeKind.Directory);
                current.Children[segment] = next;
            }
            else if (next.Kind != FsNodeKind.Directory)
            {
                error = KernelError.InvalidOp($"Path component is not a directory: {nextPath}");
                return false;
            }

            current = next;
            currentPath = nextPath;
        }

        return true;
    }
}