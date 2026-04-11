// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public readonly struct VfsMetadata
{
    public FsNodeKind Kind { get; }
    public FsPermissions Permissions { get; }
    public int UserId { get; }
    public int GroupId { get; }
    public long Size { get; }

    public VfsMetadata(FsNodeKind kind, FsPermissions permissions, int userId, int groupId, long size) 
    {
        Kind = kind;
        Permissions = permissions;
        UserId = userId;
        GroupId = groupId;
        Size = size;
    }
}