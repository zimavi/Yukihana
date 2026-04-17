// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public static class FsPermissionUtil
{
    public static FsPermissions FromUnixMode(int unixMode) =>
        (FsPermissions)(unixMode & 0x1FFF);

    public static int ToUnixMode(FsPermissions permissions) =>
        (int)permissions & 0x1FFF;

    public static bool CanRead(
        FsPermissions permissions,
        VfsCredentials credentials,
        int ownerId,
        int groupId) =>
        credentials.IsRoot
            || (credentials.UserId == ownerId && (permissions & FsPermissions.UserRead) != 0)
            || (credentials.GroupId == groupId && (permissions & FsPermissions.GroupRead) != 0)
            || (permissions & FsPermissions.OtherRead) != 0;

    public static bool CanWrite(
        FsPermissions permissions,
        VfsCredentials credentials,
        int ownerId,
        int groupId) =>
        credentials.IsRoot
            || (credentials.UserId == ownerId && (permissions & FsPermissions.UserWrite) != 0)
            || (credentials.GroupId == groupId && (permissions & FsPermissions.GroupWrite) != 0)
            || (permissions & FsPermissions.OtherWrite) != 0;

    public static bool CanExecute(
        FsPermissions permissions,
        VfsCredentials credentials,
        int ownerId,
        int groupId) =>
        credentials.IsRoot
            || (credentials.UserId == ownerId && (permissions & FsPermissions.UserExecute) != 0)
            || (credentials.GroupId == groupId && (permissions & FsPermissions.GroupExecute) != 0)
            || (permissions & FsPermissions.OtherExecute) != 0;

    public static bool CanAccess(
        FsPermissions permissions,
        FsAccess access,
        VfsCredentials credentials,
        int ownerId,
        int groupId)
    {
        if (credentials.IsRoot)
            return true;

        if ((access & FsAccess.Read) != 0 && !CanRead(permissions, credentials, ownerId, groupId))
            return false;

        if ((access & FsAccess.Write) != 0 && !CanWrite(permissions, credentials, ownerId, groupId))
            return false;

        if ((access & FsAccess.Execute) != 0 && !CanExecute(permissions, credentials, ownerId, groupId))
            return false;

        return true;
    }

    public static string ToSymbolicString(FsPermissions permissions)
    {
        Span<char> str =
        [
            (permissions & FsPermissions.UserRead) != 0 ? 'r' : '-',
            (permissions & FsPermissions.UserWrite) != 0 ? 'w' : '-',
            (permissions & FsPermissions.UserExecute) != 0 ? 'x' : '-',
            (permissions & FsPermissions.GroupRead) != 0 ? 'r' : '-',
            (permissions & FsPermissions.GroupWrite) != 0 ? 'w' : '-',
            (permissions & FsPermissions.GroupExecute) != 0 ? 'x' : '-',
            (permissions & FsPermissions.OtherRead) != 0 ? 'r' : '-',
            (permissions & FsPermissions.OtherWrite) != 0 ? 'w' : '-',
            (permissions & FsPermissions.OtherExecute) != 0 ? 'x' : '-',
        ];
        return new string(str);
    }

    public static FsPermissions DefaultFile => FromUnixMode(644);
    public static FsPermissions DefaultDirectory => FromUnixMode(755);
    public static FsPermissions DefaultSymbolic => FromUnixMode(777);
}
