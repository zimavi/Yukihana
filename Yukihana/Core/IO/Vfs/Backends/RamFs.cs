// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class RamFs : IVfsBackend
{
    private readonly byte[] _blob;
    private readonly Dictionary<string, RamFsEntry> _entries;
    private readonly Dictionary<string, (int Offset, int Length)> _files;
    private readonly Dictionary<string, HashSet<string>> _children;

    public IReadOnlyDictionary<string, (int Offset, int Length)> Files => _files;
    public IReadOnlyDictionary<string, RamFsEntry> Entries => _entries;

    public RamFs(byte[] blob, Dictionary<string, (int Offset, int Length)> files)
        : this(blob, ConvertOldFiles(files))
    {
    }

    public RamFs(byte[] blob, Dictionary<string, RamFsEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(blob);
        ArgumentNullException.ThrowIfNull(entries);
        
        _blob = blob;
        _entries = new Dictionary<string, RamFsEntry>(StringComparer.Ordinal);
        _files = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);
        _children = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        EnsureDirectory(string.Empty);

        foreach (var pair in entries)
            AddEntry(pair.Key, pair.Value);
    }

    public static Result<RamFs, KernelError> FromArchive(byte[] archiveBytes) => RamFsArchive.LoadFromArchive(archiveBytes);

    private static Dictionary<string, RamFsEntry> ConvertOldFiles(Dictionary<string, (int Offset, int Length)> files)
    {
        var result = new Dictionary<string, RamFsEntry>(StringComparer.Ordinal);

        foreach (var pair in files)
            result[pair.Key] = new RamFsEntry(pair.Value.Offset, pair.Value.Length);

        return result;
    }

    public bool Exists(string path)
    {
        path = FsPath.NormalizeRelative(path);
        return _entries.ContainsKey(path);
    }

    public FsNodeKind GetKind(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (!_entries.TryGetValue(path, out var entry))
            return FsNodeKind.Missing;

        return entry.Kind;
    }

    public bool TryReadLink(string path, out string target)
    {
        path = FsPath.NormalizeRelative(path);
        target = string.Empty;

        if (!_entries.TryGetValue(path, out var entry))
            return false;

        if (entry.Kind != FsNodeKind.SymbolicLink || string.IsNullOrWhiteSpace(entry.LinkTarget))
            return false;

        target = entry.LinkTarget;

        ShellPrint.InfoK($"follow link {path} -> {entry.LinkTarget}", "fs.ramfs");

        return true;
    }

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        path = FsPath.NormalizeRelative(path);

        if (!_entries.TryGetValue(path, out var entry))
        {
            metadata = default;
            return false;
        }

        metadata = new VfsMetadata(entry.Kind, entry.Permissions, entry.UserId, entry.GroupId);
        return true;
    }

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (!_entries.TryGetValue(path, out var entry))
            return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));

        if (entry.Kind != FsNodeKind.File)
            return Result<byte[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a regular file: {path}"));

        var result = new byte[entry.Length];
        Buffer.BlockCopy(_blob, entry.Offset, result, 0, entry.Length);
        return result;
    }

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return ReadAllBytes(path).Map(bytes => encoding.GetString(bytes));
    }

    public Result<Stream, KernelError> Open(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (!_entries.TryGetValue(path, out var entry))
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (entry.Kind != FsNodeKind.File)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp($"Path is not a regular file: {path}"));

        return new RamFsStream(_blob, entry.Offset, entry.Length);
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data)
        => Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {FsPath.NormalizeRelative(path)}"));

    public Option<KernelError> CreateDirectory(string path, bool recursive)
        => Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {FsPath.NormalizeRelative(path)}"));

    public Option<KernelError> CreateSymbolicLink(string path, string target)
        => Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {FsPath.NormalizeRelative(path)}"));

    public Option<KernelError> Delete(string path)
        => Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {FsPath.NormalizeRelative(path)}"));

    public Result<string[], KernelError> List(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (!_entries.TryGetValue(path, out var entry))
            return Result<string[], KernelError>.Failure(KernelError.NotFound(path));

        if (entry.Kind != FsNodeKind.Directory)
            return Result<string[], KernelError>.Failure(KernelError.InvalidOp($"Path is not a directory: {path}"));

        if (!_children.TryGetValue(path, out var set))
            return Array.Empty<string>();

        var result = set.ToArray();
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) =>
        Option<KernelError>.Some(KernelError.InvalidOp($"Read-only filesystem: {FsPath.NormalizeRelative(path)}"));

    private void AddEntry(string path, RamFsEntry entry)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
        {
            _entries[string.Empty] = new RamFsEntry(FsNodeKind.Directory);
            return;
        }

        EnsureDirectory(FsPath.GetParent(path));
        _entries[path] = entry;

        string parent = FsPath.GetParent(path);
        string name = FsPath.GetFileName(path);
        AddChild(parent, name);

        if (entry.Kind == FsNodeKind.File)
            _files[path] = (entry.Offset, entry.Length);
    }

    private void EnsureDirectory(string path)
    {
        path = FsPath.NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
        {
            _entries[string.Empty] = new RamFsEntry(FsNodeKind.Directory);
            return;
        }

        string current = string.Empty;

        foreach (var segment in FsPath.SplitRelative(path))
        {
            string next = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;

            if (!_entries.TryGetValue(next, out var existing))
            {
                _entries[next] = new RamFsEntry(FsNodeKind.Directory);
            }
            else if (existing.Kind != FsNodeKind.Directory)
            {
                throw new InvalidDataException($"Non-directory path component exists while building RAM FS: {next}");
            }

            AddChild(current, segment);
            current = next;
        }
    }

    private void AddChild(string parent, string child)
    {
        parent = FsPath.NormalizeRelative(parent);

        if (!_children.TryGetValue(parent, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _children[parent] = set;
        }

        set.Add(child);
    }
}