// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.Storage;

namespace Yukihana.Core.IO.Vfs.Probe;

public interface IFilesystemProbe
{
    bool TryProbe(Partition device, out FilesystemProbeResult result);
}
