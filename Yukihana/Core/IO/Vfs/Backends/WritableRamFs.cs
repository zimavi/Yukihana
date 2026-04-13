// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class TempFs(ulong totalBytes) : IVfsBackend
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
        public Node? Parent;

        public long OwnSize;
        public long SubtreeSize;

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
        private readonly TempFs _fs;
        private readonly Node _node;

        public NodeBacking(TempFs fs, Node node)
        {
            _fs = fs;
            _node = node;
        }

        public long Length => _node.OwnSize;
        public bool CanRead => true;
        public bool CanWrite => true;
        public bool CanSeek => true;

        public int Read(long position, Span<byte> buffer)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (buffer.IsEmpty)
                return 0;

            byte[] data = _node.Data ?? Array.Empty<byte>();

            if (position > int.MaxValue || position >= data.LongLength)
                return 0;

            int offset = (int)position;
            int remaining = data.Length - offset;
            if (remaining <= 0)
                return 0;

            int toCopy = Math.Min(remaining, buffer.Length);
            data.AsSpan(offset, toCopy).CopyTo(buffer);
            return toCopy;
        }

        public void Write(long position, ReadOnlySpan<byte> buffer)
        {
            _fs.WriteToFileNode(_node, position, buffer);
        }

        public void SetLength(long length)
        {
            _fs.SetFileLength(_node, length);
        }

        public void Flush() { }
    }

    private const ulong DEFAULT_CAPACITY_BYTES = 64 * 1024 * 1024; // 64 MiB

    private readonly Node _root = new(FsNodeKind.Directory);
    private ulong _totalBytes = totalBytes;
    private long _usedBytes;

    public TempFs() : this(DEFAULT_CAPACITY_BYTES)
    { }

    public bool Exists(string path) =>
        FindExactNode(FsPath.NormalizeRelative(path)) is not null;

    public FsNodeKind GetKind(string path)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        return node?.Kind ?? FsNodeKind.Missing;
    }

    public bool TryReadLink(string path, out string target)
    {
        target = string.Empty;

        var node = FindExactNode(FsPath.NormalizeRelative(path));
        if (node is null)
            return false;

        if (node.Kind != FsNodeKind.SymbolicLink || string.IsNullOrWhiteSpace(node.LinkTarget))
            return false;

        target = node.LinkTarget;
        return true;
    }

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        var node = FindExactNode(FsPath.NormalizeRelative(path));
        if (node is null)
        {
            metadata = default;
            return false;
        }

        long size = node.Kind == FsNodeKind.Directory ? node.SubtreeSize : node.OwnSize;
        metadata = new VfsMetadata(node.Kind, node.Permissions, node.UserId, node.GroupId, size);
        return true;
    }

    public VfsSpaceInfo GetSpaceInfo()
    {
        ulong used = _usedBytes <= 0 ? 0UL : (ulong)_usedBytes;
        return new VfsSpaceInfo(_totalBytes, used);
    }

    public Option<KernelError> ResizeSpace(ulong totalBytes)
    {
        ulong used = _usedBytes <= 0 ? 0UL : (ulong)_usedBytes;

        if (totalBytes < used)
            return Option<KernelError>.Some(KernelError.InvalidOp(
                $"Cannot resize TempFs below currently used space. Used={used}, requested={totalBytes}"));
        
        _totalBytes = totalBytes;
        return Option<KernelError>.None();
    }

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        path = FsPath.NormalizeRelative(path);

        var node = FindExactNode(path);
        if (node is null)
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.File)
            return Result<byte[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a regular file: {path}"));

        byte[] data = node.Data ?? [];
        byte[] copy = new byte[data.Length];
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

        if (createMissing || truncateExisting || append)
        {
            string parentPath = FsPath.GetParent(path);
            if (!TryEnsureDirectory(parentPath, recursive: true, out var dirError))
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

                node = CreateEmptyFileNode(path);
                break;

            case FileMode.Create:
                if (node is null)
                {
                    node = CreateEmptyFileNode(path);
                }
                else
                {
                    if (node.Kind != FsNodeKind.File)
                        return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a file: {path}"));

                    if (!TryReplaceLeaf(node, FsNodeKind.File, Array.Empty<byte>(), null, 0, out var fileError))
                        return Result<Stream, KernelError>.Failure(fileError);
                }
                break;

            case FileMode.OpenOrCreate:
                if (node is null)
                    node = CreateEmptyFileNode(path);
                break;

            case FileMode.Truncate:
                if (node is null)
                    return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

                if (node.Kind != FsNodeKind.File)
                    return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a file: {path}"));

                if (!TryReplaceLeaf(node, FsNodeKind.File, Array.Empty<byte>(), null, 0, out var truncateError))
                    return Result<Stream, KernelError>.Failure(truncateError);
                break;

            case FileMode.Append:
                if (node is null)
                    node = CreateEmptyFileNode(path);
                break;

            default:
                return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Unsupported file mode: {mode}"));
        }

        if (node is null)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.File)
            return Result<Stream, KernelError>.Failure(KernelError.Corrupted($"Path is not a regular file: {path}"));

        long initialPosition = append ? node.OwnSize : 0;
        return new RamFsStream(new NodeBacking(this, node), access, share, initialPosition);
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot write to the root directory."));

        if (!TryEnsureDirectory(FsPath.GetParent(path), recursive: true, out var dirError))
            return Option<KernelError>.Some(dirError);

        string parentPath = FsPath.GetParent(path);
        string name = FsPath.GetFileName(path);

        var parent = FindExactNode(parentPath);
        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(parentPath));

        byte[] copy = CloneBytes(data);

        if (parent.Children.TryGetValue(name, out var existing))
        {
            if (existing.Kind == FsNodeKind.Directory)
            {
                return Option<KernelError>.Some(
                    KernelError.InvalidOp($"Cannot overwrite directory with file: {path}"));
            }

            if (!TryReplaceLeaf(existing, FsNodeKind.File, copy, null, copy.Length, out var error))
                return Option<KernelError>.Some(error);

            return Option<KernelError>.None();
        }

        if (!TryCreateLeaf(parent, name, CreateFileLeaf(copy), out var createError))
            return Option<KernelError>.Some(createError);

        return Option<KernelError>.None();
    }

    public Option<KernelError> CreateDirectory(string path, bool recursive = false)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.None();

        return TryEnsureDirectory(path, recursive, out var error)
            ? Option<KernelError>.None()
            : Option<KernelError>.Some(error);
    }

    public Option<KernelError> CreateSymbolicLink(string path, string target)
    {
        path = FsPath.NormalizeRelative(path);
        target = FsPath.SanitizeSymlinkTarget(target);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot create a symlink at the root directory."));

        if (!TryEnsureDirectory(FsPath.GetParent(path), recursive: true, out var dirError))
            return Option<KernelError>.Some(dirError);

        string parentPath = FsPath.GetParent(path);
        string name = FsPath.GetFileName(path);

        var parent = FindExactNode(parentPath);
        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(parentPath));

        long targetSize = Encoding.UTF8.GetByteCount(target);

        if (parent.Children.TryGetValue(name, out var existing))
        {
            if (existing.Kind == FsNodeKind.Directory)
            {
                return Option<KernelError>.Some(
                    KernelError.InvalidOp($"Cannot overwrite directory with symlink: {path}"));
            }

            if (!TryReplaceLeaf(existing, FsNodeKind.SymbolicLink, null, target, targetSize, out var error))
                return Option<KernelError>.Some(error);

            return Option<KernelError>.None();
        }

        if (targetSize > (long)FreeBytes)
            return Option<KernelError>.Some(KernelError.NoSpaceLeft());

        var node = CreateSymlinkLeaf(target, targetSize);
        if (!TryCreateLeaf(parent, name, node, out var createError))
            return Option<KernelError>.Some(createError);

        return Option<KernelError>.None();
    }

    public Option<KernelError> Delete(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return Option<KernelError>.Some(KernelError.InvalidOp("Cannot delete the root directory."));

        string parentPath = FsPath.GetParent(path);
        var parent = FindExactNode(parentPath);

        if (parent is null || parent.Kind != FsNodeKind.Directory)
            return Option<KernelError>.Some(KernelError.NotFound(parentPath));

        string name = FsPath.GetFileName(path);
        if (!parent.Children.TryGetValue(name, out var node))
            return Option<KernelError>.Some(KernelError.NotFound(path));

        BubbleSubtreeDelta(parent, -node.SubtreeSize);
        parent.Children.Remove(name);
        node.Parent = null;

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

    private Node CreateEmptyFileNode(string path)
    {
        string parentPath = FsPath.GetParent(path);
        var parent = FindExactNode(parentPath);

        if (parent is null || parent.Kind != FsNodeKind.Directory)
            throw new DirectoryNotFoundException(parentPath);

        string name = FsPath.GetFileName(path);
        var node = CreateFileLeaf(Array.Empty<byte>());
        AttachChild(parent, name, node);
        return node;
    }

    private static byte[] CloneBytes(byte[] data)
    {
        byte[] copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);
        return copy;
    }

    private static Node CreateFileLeaf(byte[] data)
    {
        return new Node(FsNodeKind.File)
        {
            Data = data,
            OwnSize = data.Length,
            SubtreeSize = data.Length,
            UserId = VFS.CurrentCredentials.UserId,
            GroupId = VFS.CurrentCredentials.GroupId,
        };
    }

    private static Node CreateSymlinkLeaf(string target, long targetSize)
    {
        return new Node(FsNodeKind.SymbolicLink)
        {
            LinkTarget = target,
            OwnSize = targetSize,
            SubtreeSize = targetSize,
            UserId = VFS.CurrentCredentials.UserId,
            GroupId = VFS.CurrentCredentials.GroupId,
        };
    }

    private void WriteToFileNode(Node node, long position, ReadOnlySpan<byte> buffer)
    {
        if (node.Kind != FsNodeKind.File)
            throw new IOException("Path is not a regular file.");

        ArgumentOutOfRangeException.ThrowIfNegative(position);

        if (buffer.IsEmpty)
            return;

        long required = checked(position + buffer.Length);
        if (required > int.MaxValue)
            throw new IOException("File too large.");

        int oldLength = node.Data?.Length ?? 0;
        int newLength = (int)required;

        if (newLength > oldLength)
        {
            long delta = newLength - oldLength;
            EnsureCanAllocate(delta);

            byte[] grown = new byte[newLength];
            if (oldLength != 0 && node.Data is not null)
                Buffer.BlockCopy(node.Data, 0, grown, 0, oldLength);

            buffer.CopyTo(grown.AsSpan((int)position, buffer.Length));
            node.Data = grown;
            node.OwnSize = newLength;
            BubbleSubtreeDelta(node, delta);
            return;
        }

        byte[] data = node.Data ?? [];
        buffer.CopyTo(data.AsSpan((int)position, buffer.Length));
        node.Data ??= data;
    }

    private void SetFileLength(Node node, long length)
    {
        if (node.Kind != FsNodeKind.File)
            throw new IOException("Path is not a regular file.");

        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (length > int.MaxValue)
            throw new IOException("File too large.");

        int oldLength = node.Data?.Length ?? 0;
        int newLength = (int)length;

        if (newLength == oldLength)
            return;

        long delta = newLength - oldLength;
        if (delta > 0)
            EnsureCanAllocate(delta);

        byte[] resized = newLength == 0 ? [] : new byte[newLength];
        if (oldLength != 0 && node.Data is not null)
            Buffer.BlockCopy(node.Data, 0, resized, 0, Math.Min(oldLength, newLength));

        node.Data = resized;
        node.OwnSize = newLength;
        BubbleSubtreeDelta(node, delta);
    }

    private bool TryReplaceLeaf(
        Node node,
        FsNodeKind newKind,
        byte[]? newData,
        string? newLinkTarget,
        long newSize,
        out KernelError error)
    {
        error = KernelError.Corrupted("Unknown filesystem error.");

        if (node.Kind == FsNodeKind.Directory)
        {
            error = KernelError.InvalidOp("Path component is a directory.");
            return false;
        }

        long delta = checked(newSize - node.OwnSize);
        if (delta > 0 && (ulong)delta > FreeBytes)
        {
            error = KernelError.NoSpaceLeft();
            return false;
        }

        node.Kind = newKind;
        node.Data = newData;
        node.LinkTarget = newLinkTarget;
        node.OwnSize = newSize;
        BubbleSubtreeDelta(node, delta);
        return true;
    }

    private bool TryCreateLeaf(Node parent, string name, Node child, out KernelError error)
    {
        error = KernelError.Corrupted("Unknown filesystem error.");

        if (parent.Kind != FsNodeKind.Directory)
        {
            error = KernelError.InvalidOp("Parent is not a directory.");
            return false;
        }

        if ((ulong)child.SubtreeSize > FreeBytes)
        {
            error = KernelError.NoSpaceLeft();
            return false;
        }

        AttachChild(parent, name, child);
        return true;
    }

    private void AttachChild(Node parent, string name, Node child)
    {
        child.Parent = parent;
        parent.Children.Add(name, child);
        BubbleSubtreeDelta(parent, child.SubtreeSize);
    }

    private void BubbleSubtreeDelta(Node start, long delta)
    {
        if (delta == 0)
            return;

        for (Node? current = start; current is not null; current = current.Parent)
            current.SubtreeSize = checked(current.SubtreeSize + delta);

        _usedBytes = checked(_usedBytes + delta);
    }

    private void EnsureCanAllocate(long additionalBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(additionalBytes);

        if (additionalBytes == 0)
            return;

        if ((ulong)additionalBytes > FreeBytes)
            throw new IOException("No space left on device.");
    }

    private ulong FreeBytes
    {
        get
        {
            ulong used = _usedBytes <= 0 ? 0UL : (ulong)_usedBytes;
            return _totalBytes > used ? _totalBytes - used : 0UL;
        }
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

        string[] parts = FsPath.SplitRelative(path);
        Node current = _root;
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

                next = new Node(FsNodeKind.Directory)
                {
                    Parent = current
                };

                current.Children.Add(segment, next);
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