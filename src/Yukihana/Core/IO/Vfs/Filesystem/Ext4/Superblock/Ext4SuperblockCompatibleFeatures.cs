// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4SuperblockCompatibleFeatures : uint
{
    // Directory preallocation
    DirPrealloc = 0x1,

    // "imagic inodes". Not clear from the code what this does
    ImagicInodes = 0x2,
    HasJournal = 0x4,

    // Has reserved GDT blocks for filesystem expansion. Requires CompactSparseSuper
    ResizeInode = 0x10,
    DirIndex = 0x20,

    // "Lazy BG". Not in Linux kernel, seems to have been uninitialized block groups?
    LazyBg = 0x40,

    // Not used
    ExcludeInode = 0x80,

    // Seems to be used to indicate the presence of snapshot-related exclude bit-maps? 
    // Not defined in kernel or used in e2fsprogs
    ExcludeBitmap = 0x100,

    // Sparse Super Block, v2. If flag is set, the SB field BakcupBgs points to the 
    // two block groups that conain backup superblocks
    SparseSuper2 = 0x200,

    // Fast commits supported. Although fast commits blocks are backward incopatible,
    // fast commit blocks are not always present in the journal. If fast commit blocks
    // are present in the journal, JBD2 incompact feature JbdIncompactFastCommit gets set 
    FastCommit = 0x400,

    // Orphan file allocated. This is special file for more efficient tracking of
    // unlinked but still open inodes. When there may be any entries in the file,
    // we additionally set proper rocompat feature
    RoOrphanPresent = 0x1000
}
