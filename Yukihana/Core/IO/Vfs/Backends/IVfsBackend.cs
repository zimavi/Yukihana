// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs;

public interface IVfsBackend
{
    bool Exists(string path);
    FsNodeKind GetKind(string path);
    bool TryReadLink(string path, out string target);
    bool TryGetMetadata(string path, out VfsMetadata metadata);

    Result<byte[], KernelError> ReadAllBytes(string path);
    Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null);

    Result<Stream, KernelError> Open(
        string path,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.Read);


    Option<KernelError> WriteAllBytes(string path, byte[] data);
    Option<KernelError> CreateDirectory(string path, bool recursive);
    Option<KernelError> CreateSymbolicLink(string path, string target);
    Option<KernelError> Delete(string path);
    Result<string[], KernelError> List(string path);

    Option<KernelError> SetPermissions(string path, FsPermissions permissions);
}