// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public readonly struct VfsMetadata(FsNodeKind kind, FsPermissions permissions, int userId, int groupId, long size)
{
    public FsNodeKind Kind { get; } = kind;
    public FsPermissions Permissions { get; } = permissions;
    public int UserId { get; } = userId;
    public int GroupId { get; } = groupId;
    public long Size { get; } = size;
}
