// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using System.Text;
using Cosmos.Kernel.Core.Memory;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class ProcFs : IVfsBackend
{
    private readonly Dictionary<string, Func<Result<byte[], KernelError>>> _files;

    public ProcFs()
    {
        _files = new Dictionary<string, Func<Result<byte[], KernelError>>>(StringComparer.Ordinal)
        {
            ["meminfo"] = () => Encoding.UTF8.GetBytes(GetMemoryInfo())
        };
    }

    private static string GetMemoryInfo()
    {
        ulong totalPages = PageAllocator.TotalPageCount;
        ulong freePages = PageAllocator.FreePageCount;
        ulong usedPages = totalPages - freePages;
        ulong pageSize = PageAllocator.PageSize;

        ulong totalBytes = totalPages * pageSize;
        ulong freeBytes = freePages * pageSize;
        ulong usedBytes = usedPages * pageSize;

        return
            $"page_size\t: {pageSize}" +
            $"pages_total\t: {totalPages}" +
            $"pages_used\t: {usedPages}" +
            $"pages_free\t: {freePages}" +

            $"memory_total\t: {totalBytes}" +
            $"memory_used\t: {usedBytes}" +
            $"memory_free\t: {freeBytes}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KernelError ReturnReadOnly() =>
        KernelError.InvalidOp("Read-only filesystem.");

    #region Interface Implementation

    public Option<KernelError> CreateDirectory(string path, bool recursive) => ReturnReadOnly();

    public Option<KernelError> CreateSymbolicLink(string path, string target) => ReturnReadOnly();

    public Option<KernelError> Delete(string path) => ReturnReadOnly();

    public bool Exists(string path)
    {
        path = path.Trim('/');
        return _files.ContainsKey(path);
    }

    public FsNodeKind GetKind(string path)
    {
        path = path.Trim('/');

        if (_files.ContainsKey(path))
            return FsNodeKind.File;

        return FsNodeKind.Missing;
    }

    public VfsSpaceInfo GetSpaceInfo() => new(0, 0);

    public Option<KernelError> ResizeSpace(ulong totalBytes) => ReturnReadOnly();

    public Result<string[], KernelError> List(string path)
    {
        if (!string.IsNullOrEmpty(path.Trim('/')))
            return Result<string[], KernelError>.Failure(KernelError.NotFound(path));

        return Result<string[], KernelError>.Success([.. _files.Keys]);
    }

    public Result<Stream, KernelError> Open(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
    {
        if (mode != FileMode.Open || (access & FileAccess.Write) != 0)
            return Result<Stream, KernelError>.Failure(ReturnReadOnly());

        var bytesResult = ReadAllBytes(path);
        if (bytesResult.IsFailure) return Result<Stream, KernelError>.Failure(bytesResult.Error);

        return Result<Stream, KernelError>.Success(new MemoryStream(bytesResult.Value, writable: false));
    }

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        path = path.Trim('/');

        if (_files.TryGetValue(path, out var generator))
        {
            return generator();
        }

        return Result<byte[], KernelError>.Failure(KernelError.NotFound(path));
    }

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        return ReadAllBytes(path).Map(bytes =>
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(bytes);
        });
    }

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) => ReturnReadOnly();

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        path = path.Trim('/');

        if (string.IsNullOrEmpty(path))
        {
            metadata = new VfsMetadata(FsNodeKind.Directory, FsPermissionUtil.DefaultDirectory, 0, 0, 0);
            return true;
        }

        if (_files.ContainsKey(path))
        {
            metadata = new VfsMetadata(FsNodeKind.File, FsPermissionUtil.DefaultFile, 0, 0, 0);
            return true;
        }

        metadata = default;
        return false;
    }

    public bool TryReadLink(string path, out string target)
    {
        target = string.Empty;
        return false;
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data) => ReturnReadOnly();

    #endregion
}
