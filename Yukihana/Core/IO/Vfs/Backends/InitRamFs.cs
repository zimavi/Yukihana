// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Compression;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

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

        public long Length => _data.Length;
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

    public InitRamFs(byte[] compressedArchive)
    {
        ArgumentNullException.ThrowIfNull(compressedArchive);

        byte[] tarBytes = Decompress(compressedArchive);
        BuildFilesystemFromTar(tarBytes);
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

    private void BuildFilesystemFromTar(byte[] tarBytes)
    {
        int offset = 0;
        while (offset + 512 <= tarBytes.Length)
        {
            if(IsAllZero(tarBytes, offset, 512))
                break;
            
            string name = ReadAscii(tarBytes, offset, 100);
            int mode = (int)ReadOctal(tarBytes, offset + 100, 8);
            int uid = (int)ReadOctal(tarBytes, offset + 108, 8);
            int gid = (int)ReadOctal(tarBytes, offset + 116, 8);
            long size = ReadOctal(tarBytes, offset + 124, 12);
            char typeFlag = (char)tarBytes[offset + 156];
            string linkName = ReadAscii(tarBytes, offset + 157, 100);
            string prefix = ReadAscii(tarBytes, offset + 345, 155);

            string fullPath = NormalizePath(string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}");
            offset += 512;

            Inode inode;
            switch (typeFlag)
            {
                case '5':
                    inode = new Inode(FsNodeKind.Directory)
                    {
                        Permissions = FsPermissionUtil.FromUnixMode(mode & 0x1FF),
                        UserId = uid,
                        GroupId = gid,
                    };
                    AddNode(fullPath, inode);
                    offset = Align512(offset + (int)size);
                    continue;
                
                case '2':
                case '1':
                    inode = new Inode(FsNodeKind.SymbolicLink)
                    {
                        LinkTarget = FsPath.SanitizeSymlinkTarget(linkName),
                        Permissions = FsPermissionUtil.FromUnixMode(mode & 0x1FF),
                        UserId = uid,
                        GroupId = gid,
                    };
                    AddNode(fullPath, inode);
                    offset = Align512(offset + (int)size);
                    continue;
                
                case '0':
                case '\0':
                    inode = new Inode(FsNodeKind.File)
                    {
                        Data = tarBytes[offset..(offset + (int)size)],
                        Permissions = FsPermissionUtil.FromUnixMode(mode & 0x1FF),
                        UserId = uid,
                        GroupId = gid,
                    };
                    AddNode(fullPath, inode);
                    offset = Align512(offset + (int)size);
                    continue;
                default:
                    ShellPrint.WarnK($"Unknown TAR type '{typeFlag}' for {fullPath}", "ramfs");
                    offset = Align512(offset + (int)size);
                    continue;
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

    private void EnsureParentDirectories(string path)
        => TraverseToParent(path, createMissing: true);

    private static int Align512(int value) => ((value + 511) / 512) * 512;

    private static bool IsAllZero(byte[] data, int offset, int count)
    {
        for (int i = 0; i < count; i++)
            if (data[offset + i] != 0) return false;
        return true;
    }

    private static string ReadAscii(byte[] data, int offset, int length)
    {
        return Encoding.ASCII.GetString(data, offset, length).TrimEnd('\0');
    }

    private static long ReadOctal(byte[] data, int offset, int length)
    {
        string str = ReadAscii(data, offset, length).Trim();
        return str.Length == 0 ? 0 : Convert.ToInt64(str, 8);
    }

    private static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/').Trim();
        while (path.StartsWith("./")) path = path[2..];
        while (path.StartsWith("/")) path = path[1..];
        while (path.Contains("//")) path = path.Replace("//", "/");
        return path;
    }

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
        metadata = new VfsMetadata(node.Kind, node.Permissions, node.UserId, node.GroupId);
        return true;
    }

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
    public Option<KernelError> WriteAllBytes(string path, byte[] data) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {path}"));
    public Option<KernelError> CreateDirectory(string path, bool recursive) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {path}"));
    public Option<KernelError> CreateSymbolicLink(string path, string target) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {path}"));
    public Option<KernelError> Delete(string path) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {path}"));
    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {path}"));
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
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Read-only filesystem: {path}"));
        
        if ((access & FileAccess.Write) != 0)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Read-only filesystem: {path}"));
        
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