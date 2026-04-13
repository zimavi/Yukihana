// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using System.Text;
using Yukihana.Core.Compression;
using Yukihana.Core.Compression.Archives;
using Yukihana.Core.Debug;
using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public enum InitRamFsArchive
{
    Tar,
    Cpio
}

public sealed class InitRamFs : IVfsBackend
{
    private sealed class Inode
    {
        public FsNodeKind Kind;
        public FsPermissions Permissions;
        public int UserId;
        public int GroupId;
        public byte[]? Data;
        public string? LinkTarget;
        public long Size;
        public long SubtreeSize;
        public Dictionary<string, Inode>? Children = new(StringComparer.Ordinal);

        public Inode(FsNodeKind kind)
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

    private sealed class ReadOnlyNodeBacking : IRamFsStreamBacking
    {
        private readonly byte[] _data;

        public ReadOnlyNodeBacking(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            _data = data;
        }

        public long Length => _data.LongLength;
        public bool CanRead => true;
        public bool CanWrite => false;
        public bool CanSeek => true;

        public int Read(long position, Span<byte> buffer)
        {
            if (position < 0 || position >= _data.LongLength)
                throw new ArgumentOutOfRangeException(nameof(position));
            
            if (buffer.Length == 0 || position == _data.LongLength)
                return 0;
            
            int remaining = (int)Math.Min(_data.LongLength - position, int.MaxValue);
            int toCopy = Math.Min(remaining, buffer.Length);

            new ReadOnlySpan<byte>(_data, (int)position, toCopy).CopyTo(buffer);
            return toCopy;
        }

        public void Write(long position, ReadOnlySpan<byte> buffer) =>
            throw new InvalidOperationException("Read-only filesystem.");
        
        public void SetLength(long length) =>
            throw new InvalidOperationException("Read-only filesystem.");

        public void Flush() { /* no-op */ }
    }

    private readonly Inode _root = new(FsNodeKind.Directory);

    public InitRamFs(byte[] compressedArchive, InitRamFsArchive archiveType)
    {
        ArgumentNullException.ThrowIfNull(compressedArchive);

        byte[] archiveBytes = Decompress(compressedArchive);
        
        BuildFilesystemFromArchive(archiveBytes);

        ComputeSubtreeSizes(_root);
    }

    private static byte[] Decompress(byte[] data)
    {
        if (data.Length < 2)
            throw new ArgumentException("Archive data is too short.");
        
        var compressor = ArchiveCompressorFactory.Detect(data);

        if (compressor.IsSome)
            return compressor.Value.Decompress(data);
        else
            return data;
    }

    private void BuildFilesystemFromArchive(byte[] archiveBytes)
    {
        var archivator = ArchivatorFactory.Detect(archiveBytes);

        var archive = archivator.OrThrow("Unsupported initramfs format.").Read(archiveBytes);

        var createdNodes = new Dictionary<string, Inode>();

        foreach(var entry in archive.Entries)
        {
            string fullPath = NormalizePath(entry.Path);

            Inode inode;

            switch (entry.Kind)
            {
                case ArchiveEntryKind.Directory:
                {
                    inode = new Inode(FsNodeKind.Directory)
                    {
                        Permissions = FsPermissionUtil.FromUnixMode(entry.Mode & 0x1FF),
                        UserId = entry.UserId,
                        GroupId = entry.GroupId,
                        Size = 0,
                    };

                    AddNode(fullPath, inode);
                    createdNodes[fullPath] = inode;
                    break;
                }

                case ArchiveEntryKind.SymbolicLink:
                {
                    inode = new Inode(FsNodeKind.SymbolicLink)
                    {
                        LinkTarget = FsPath.SanitizeSymlinkTarget(entry.LinkTarget ?? ""),
                        Permissions = FsPermissionUtil.FromUnixMode(entry.Mode & 0x1FF),
                        UserId = entry.UserId,
                        GroupId = entry.GroupId,
                        Size = entry.LinkTarget?.Length ?? 0,
                    };

                    AddNode(fullPath, inode);
                    createdNodes[fullPath] = inode;
                    break;
                }

                case ArchiveEntryKind.HardLink:
                {
                    if (entry.LinkTarget == null)
                    continue;

                    string targetPath = NormalizePath(entry.LinkTarget);

                    if (!createdNodes.TryGetValue(targetPath, out var targetInode))
                    {
                        // fallback: create empty file (should not normally happen)
                        targetInode = new Inode(FsNodeKind.File)
                        {
                            Data = Array.Empty<byte>()
                        };
                    }

                    AddNode(fullPath, targetInode);
                    createdNodes[fullPath] = targetInode;
                    break;
                }

                case ArchiveEntryKind.File:
                default:
                {
                    inode = new Inode(FsNodeKind.File)
                    {
                        Data = entry.Data ?? [],
                        Permissions = FsPermissionUtil.FromUnixMode(entry.Mode & 0x1FF),
                        UserId = entry.UserId,
                        GroupId = entry.GroupId,
                        Size = entry.Size,
                    };

                    AddNode(fullPath, inode);
                    createdNodes[fullPath] = inode;
                    break;
                }
            }
        }
    }

    private void AddNode(string path, Inode node)
    {
        var parent = TraverseToParent(path, createMissing: true);
        string name = FsPath.GetFileName(path);
        parent.Children[name] = node;
    }

    private Inode TraverseToParent(string path, bool createMissing)
    {
        var current = _root;
        var segments = FsPath.SplitRelative(FsPath.GetParent(path));

        foreach (var segment in segments)
        {
            if(!current.Children.TryGetValue(segment, out var next))
            {
                if (createMissing)
                {
                    next = new Inode(FsNodeKind.Directory);
                    current.Children[segment] = next;
                }
                else throw new DirectoryNotFoundException(segment);
            }
            current = next;
        }

        return current;
    }

    private static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/').Trim();
        while (path.StartsWith("./")) path = path[2..];
        while (path.StartsWith("/")) path = path[1..];
        while (path.Contains("//")) path = path.Replace("//", "/");
        return path;
    }

    private static long ComputeSubtreeSizes(Inode root)
    {
        if (root is null)
            return 0;

        var stack = new Stack<(Inode node, bool visited)>(128);
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, visited) = stack.Pop();

            if (!visited)
            {
                // Visit later after children
                stack.Push((node, true));

                if (node.Kind == FsNodeKind.Directory && node.Children != null)
                {
                    foreach (var child in node.Children.Values)
                        stack.Push((child, false));
                }
            }
            else
            {
                long total = 0;

                switch (node.Kind)
                {
                    case FsNodeKind.Directory:
                    {
                        if (node.Children != null)
                        {
                            foreach (var child in node.Children.Values)
                                total += child.SubtreeSize;
                        }
                        break;
                    }

                    case FsNodeKind.SymbolicLink:
                    case FsNodeKind.File:
                    default:
                    {
                        total = node.Size;
                        break;
                    }
                }

                node.SubtreeSize = total;
            }
        }

        return root.SubtreeSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KernelError ReturnReadOnly() => KernelError.InvalidOp("Read-only filesystem.");

    #region Interface Implementation

    public bool Exists(string path) => FindNode(path) is not null;
    public FsNodeKind GetNodeKind(string path) => FindNode(path)?.Kind ?? FsNodeKind.Missing;

    public bool TryReadLink(string path, out string target)
    {
        var node = FindNode(path);
        if (node?.Kind == FsNodeKind.SymbolicLink)
        {
            target = node.LinkTarget!;
            return true;
        }
        target = string.Empty;
        return false;
    }

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        var node = FindNode(path);
        if (node is null)
        {
            metadata = default;
            return false;
        }
        metadata = new VfsMetadata(node.Kind, node.Permissions, node.UserId, node.GroupId, node.Size);
        return true;
    }

    public VfsSpaceInfo GetSpaceInfo()
    {
        ulong usedBytes = (ulong)_root.SubtreeSize;
        return new(usedBytes, usedBytes);
    }
    
    public Option<KernelError> ResizeSpace(ulong totalBytes) => ReturnReadOnly();

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        var node = FindNode(path);
        if (node is null) return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));
        if (node.Kind != FsNodeKind.File) return Result<byte[], KernelError>.Failure(KernelError.InvalidOp($"Not a file: {path}"));
        return node.Data ?? Array.Empty<byte>();
    }

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return ReadAllBytes(path).Map(b => encoding.GetString(b));
    }

    private Inode? FindNode(string path)
    {
        var current = _root;
        foreach (var seg in FsPath.SplitRelative(path))
            if (!current.Children.TryGetValue(seg, out current))
                return null;
        return current;
    }

    #region Write Operations (Read-Only)
    public Option<KernelError> WriteAllBytes(string path, byte[] data) => ReturnReadOnly();
    public Option<KernelError> CreateDirectory(string path, bool recursive) => ReturnReadOnly();
    public Option<KernelError> CreateSymbolicLink(string path, string target) => ReturnReadOnly();
    public Option<KernelError> Delete(string path) => ReturnReadOnly();
    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) => ReturnReadOnly();
    #endregion

    public Result<string[], KernelError> List(string path)
    {
        var node = FindNode(path);
        if (node is null) return Result<string[], KernelError>.Failure(KernelError.NotFound(path));
        if (node.Kind != FsNodeKind.Directory) return Result<string[], KernelError>.Failure(KernelError.InvalidOp($"Not a directory: {path}"));
        var items = node.Children.Keys.ToArray();
        Array.Sort(items, StringComparer.Ordinal);
        return items;
    }

    public FsNodeKind GetKind(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if(string.IsNullOrEmpty(path))
            return FsNodeKind.Directory;
        
        var node = FindNode(path);
        return node?.Kind ?? FsNodeKind.Missing;
    }

    public Result<Stream, KernelError> Open(
        string path, 
        FileMode mode = FileMode.Open, 
        FileAccess access = FileAccess.Read, 
        FileShare share = FileShare.Read)
    {
        path = FsPath.NormalizeRelative(path);

        if (mode != FileMode.Open)
            return Result<Stream, KernelError>.Failure(ReturnReadOnly());
        
        if ((access & FileAccess.Write) != 0)
            return Result<Stream, KernelError>.Failure(ReturnReadOnly());
        
        var node = FindNode(path);
        if (node is null)
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (node.Kind != FsNodeKind.File)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Not a regular file: {path}"));
        
        byte[] data = node.Data ?? Array.Empty<byte>();
        Stream stream = new RamFsStream(new ReadOnlyNodeBacking(data), access, share);
        return stream;
    }

    #endregion
}