// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Vfs.Probe;

public readonly struct FilesystemProbeResult
{
    public required bool Success { get; init; }
    public required string Filesystem { get; init; }
    public Guid? Uuid { get; init; }
    public string? Label { get; init; }
}
