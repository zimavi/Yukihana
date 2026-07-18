// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsInodeOperations : IInodeOperations
{
    public bool Lookup(IVfsInode dir, ReadOnlySpan<char> name, out IVfsInode? child)
    {
        if (dir is not InitfsInode dirInode)
        {
            child = null;
            return false;
        }

        string nameAsString = name.ToString();

        InitfsInode? found = dirInode.Children.FirstOrDefault(c => c.Name.AsSpan().SequenceEqual(nameAsString));
        child = found;
        return found != null;
    }

    public bool ReadDir(IVfsInode dir, out IReadOnlyList<IVfsInode> entries)
    {
        if (dir is not InitfsInode dirInode)
        {
            entries = Array.Empty<IVfsInode>();
            return false;
        }

        entries = dirInode.Children;
        return true;
    }

    public bool Create(IVfsInode dir, ReadOnlySpan<char> name, ModeEnum mode, out IVfsInode? inode)
    {
        // Read-only filesystem - no creation allowed
        inode = null;
        return false;
    }

    public bool Mkdir(IVfsInode dir, ReadOnlySpan<char> name, ModeEnum mode, out IVfsInode? inode)
    {
        // Read-only filesystem - no directory creation allowed
        inode = null;
        return false;
    }

    public bool Symlink(IVfsInode dir, ReadOnlySpan<char> name, ReadOnlySpan<char> target, out IVfsInode? inode)
    {
        // Read-only filesystem - no symlinks creation allowed
        inode = null;
        return false;
    }

    public bool Unlink(IVfsInode dir, ReadOnlySpan<char> name)
    {
        // Read-only filesystem - no deletion allowed
        return false;
    }

    public bool Rmdir(IVfsInode dir, ReadOnlySpan<char> name)
    {
        // Read-only filesystem - no directory deletion allowed
        return false;
    }

    public bool Rename(IVfsInode oldParent, ReadOnlySpan<char> oldName, IVfsInode newParent, ReadOnlySpan<char> newName)
    {
        // Read-only filesystem - no renames allowed
        return false;
    }

    public bool GetAttr(IVfsInode inode, out VfsStat stat)
    {
        if (inode is not InitfsInode initfsInode)
        {
            stat = default;
            return false;
        }

        stat = new VfsStat
        {
            Ino = initfsInode.InodeId,
            Mode = initfsInode.Mode,
            NLink = 1,
            Uid = (uint)initfsInode.UserId,
            Gid = (uint)initfsInode.GroupId,
            Rdev = 0,
            Size = (ulong)initfsInode.Size,
            BlkSize = initfsInode.BlockSize,
            Blocks = (ulong)(initfsInode.Size / 512 + 1),
            Atime = initfsInode.Timestamp,
            Mtime = initfsInode.Timestamp,
            Ctime = initfsInode.Timestamp
        };

        return true;
    }

    public bool SetAttr(IVfsInode inode, SetAttrFlags flags, in VfsStat attributes)
    {
        // Read-only filesystem - no attribute changes allowed
        return false;
    }
}
