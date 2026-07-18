// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.Storage;
using Yukihana.Core.IO.Vfs.Probe.Filesystem;

namespace Yukihana.Core.IO.Vfs.Probe;

internal sealed class FilesystemProber
{
    private static readonly IFilesystemProbe[] s_filesystemProbes = [
        new FatProbe()
    ];

    public static bool ProbeFilesystem(Partition device, out FilesystemProbeResult? result)
    {
        foreach(IFilesystemProbe probe in s_filesystemProbes)
        {
            bool isSuccess = probe.TryProbe(device, out var res);

            if (isSuccess)
            {
                result = res;
                return true;
            }
        }
        // We couldn't identify filesystem
        result = null;
        return false;
    }
}