// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

internal sealed class ResolvedPath
{
    public required MountInfo Mount { get; init; }
    public required string RelativePath { get; init; }
    public required string AbsolutePath { get; init; }
    public required FsNodeKind Kind { get; init; }
    public required VfsMetadata Metadata { get; init; }
}