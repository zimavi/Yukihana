// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class SubtreeFs : IVfsBackend
{
    private readonly IVfsBackend _inner;
    private readonly string _prefix;

    public SubtreeFs(IVfsBackend inner, string prefix)
    {
        _inner = inner;
        _prefix = Normalize(prefix);
    }

    private string Map(string path)
    {
        path = Normalize(path);

        if (string.IsNullOrEmpty(_prefix))
            return path;

        if (string.IsNullOrEmpty(path))
            return _prefix;

        return _prefix + "/" + path;
    }

    public bool Exists(string path) => 
        _inner.Exists(Map(path));

    public FsNodeKind GetKind(string path) => 
        _inner.GetKind(Map(path));

    public bool TryReadLink(string path, out string target) => 
        _inner.TryReadLink(Map(path), out target);

    public bool TryGetMetadata(string path, out VfsMetadata metadata) => 
        _inner.TryGetMetadata(Map(path), out metadata);

    public Result<byte[], KernelError> ReadAllBytes(string path) =>
        _inner.ReadAllBytes(Map(path));

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null) => 
        _inner.ReadAllText(Map(path), encoding);

    public Result<Stream, KernelError> Open(
        string path,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.Read)
        => _inner.Open(Map(path), mode, access, share);

    public Option<KernelError> WriteAllBytes(string path, byte[] data) => 
        _inner.WriteAllBytes(Map(path), data);

    public Option<KernelError> CreateDirectory(string path, bool recursive) => 
        _inner.CreateDirectory(Map(path), recursive);

    public Option<KernelError> CreateSymbolicLink(string path, string target) => 
        _inner.CreateSymbolicLink(Map(path), target);

    public Option<KernelError> Delete(string path) => 
        _inner.Delete(Map(path));

    public Result<string[], KernelError> List(string path) => 
        _inner.List(Map(path));

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) => 
        _inner.SetPermissions(Map(path), permissions);

    public VfsSpaceInfo GetSpaceInfo() => _inner.GetSpaceInfo();

    public Option<KernelError> ResizeSpace(ulong totalBytes) => 
        _inner.ResizeSpace(totalBytes);

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return FsPath.NormalizeRelative(path);
    }
}