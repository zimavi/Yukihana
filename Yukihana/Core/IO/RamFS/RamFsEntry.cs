// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.IO.Vfs;

namespace Yukihana.Core.IO.RamFS;

public sealed class RamFsEntry
{
    public FsNodeKind Kind { get; set; }
    public int Offset { get; set; }
    public int Length { get; set; }
    public string? LinkTarget { get; set; }
    public FsPermissions Permissions { get; set; }
    public int UserId { get; set; }
    public int GroupId { get; set; }

    public RamFsEntry(FsNodeKind kind)
    {
        Kind = kind;
        Permissions = kind switch
        {
            FsNodeKind.Directory => FsPermissionUtil.DefaultDirectory,
            FsNodeKind.SymbolicLink => FsPermissionUtil.DefaultSymbolic,
            _ => FsPermissionUtil.DefaultFile
        };
    }

    public RamFsEntry(int offset, int length) : this(FsNodeKind.File)
    {
        Offset = offset;
        Length = length;
    }

    public RamFsEntry(string linkTarget) : this(FsNodeKind.SymbolicLink)
    {
        LinkTarget = FsPath.SanitizeSymlinkTarget(linkTarget);
    }
}