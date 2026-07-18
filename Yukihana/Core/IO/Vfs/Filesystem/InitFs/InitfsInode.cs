// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Core.Compression.Archives;

namespace Yukihana.Core.IO.Vfs.Filesystem.InitFs;

internal sealed class InitfsInode : IVfsInode
{
    private static ulong s_nextInodeId = 1;
    
    public InitfsInode(string name, ArchiveEntryKind kind, string path)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Kind = kind;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        InodeId = s_nextInodeId++;
        
        Mode = kind switch
        {
            ArchiveEntryKind.Directory => ModeEnum.Directory,
            ArchiveEntryKind.SymbolicLink => ModeEnum.SymbolicLink,
            _ => ModeEnum.RegularFile
        };

        UserId = 0;
        GroupId = 0;
        Timestamp = new VfsTimespec(0, 0);
        
        if (kind == ArchiveEntryKind.Directory)
        {
            BlockOffset = 0;
        }
    }

    public string Name { get; set; }
    
    public ulong InodeId { get; }
    
    public ArchiveEntryKind Kind { get; }
    
    public string Path { get; }
    
    public ModeEnum Mode { get; set; }
    
    public int UserId { get; set; }
    
    public int GroupId { get; set; }
    
    public VfsTimespec Timestamp { get; set; }
    
    public ulong BlockOffset { get; set; }
    
    public long Size { get; set; }
    
    public long BlockSize => 512;

    public IInodeOperations InodeOperations { get; } = new InitfsInodeOperations();
    
    public IFileOperations? FileOperations { get; set; }

    public List<InitfsInode> Children { get; } = [];
    
    public InitfsInode? Parent { get; set; }
}