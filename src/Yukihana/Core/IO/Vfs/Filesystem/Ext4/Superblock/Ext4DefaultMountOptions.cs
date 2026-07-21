// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

[Flags]
internal enum Ext4DefaultMountOptions : uint
{
    // Print debug info upon (re)mount
    Debug = 0x0001,

    // New files take the gid of the containing directory 
    // (instead of the fsgid of the current process).
    BsdGroups = 0x0002,
    XAttrUser = 0x0004,

    // POSIX acces control list.
    Acl = 0x0008,

    // Do not support 32-bit UIDs.
    UID16 = 0x0010,

    // All data and metadata are commited to the journal.
    JModeData = 0x0020,

    // All data are flushed to the disk before metadata are committed to the journal.
    JModeOrdered = 0x0040,

    // Data ordering is not preserved; data may be written after the metadata has been written.
    JModeWriteBack = 0x0060,

    // Disable write flushes
    NoBarrier = 0x0100,

    // Track which blocks in a filesystem are metadata and therefore should not be used as data blocks. 
    // This option will be enabled by default on 3.18, hopefully.
    BlockValidity = 0x0200,

    // Enable DISCARD support, where the storage device is told about blocks becoming unused.
    Discard = 0x0400,

    // Disable delayed allocation.
    NoDeAlloc = 0x0800
}
