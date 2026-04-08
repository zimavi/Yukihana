// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO;

public static partial class VFS
{
    private static bool CanOpenExisting(VfsMetadata metadata, FileAccess access, FileMode mode, string path)
    {
        if (metadata.Kind == FsNodeKind.Missing)
            return false;

        if ((access & FileAccess.Read) != 0 &&
            !FsPermissionUtil.CanRead(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
            return false;

        if ((access & FileAccess.Write) != 0 &&
            !FsPermissionUtil.CanWrite(metadata.Permissions, CurrentCredentials, metadata.UserId, metadata.GroupId))
            return false;

        if (mode == FileMode.Truncate && (access & FileAccess.Write) == 0)
            return false;

        if (mode == FileMode.Append && (access & FileAccess.Write) == 0)
            return false;

        return true;
    }

    public static Option<KernelError> ChangeMode(string path, FsPermissions permissions)
    {
        var resolved = ResolvePath(path, followFinalSymlink: false);

        if(resolved.IsFailure)
            return Option<KernelError>.Some(resolved.Error);
        
        ShellPrint.InfoK($"chmod: {resolved.Value.AbsolutePath} -> {FsPermissionUtil.ToSymbolicString(permissions)}", "vfs.perm");
        return resolved.Value.Mount.Backend.SetPermissions(resolved.Value.AbsolutePath, permissions);
    }
}