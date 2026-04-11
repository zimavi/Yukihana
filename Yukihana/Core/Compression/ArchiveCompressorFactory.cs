// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Compression;

public static class ArchiveCompressorFactory
{
    public static Option<IArchiveCompressor> Detect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        IArchiveCompressor[] compressors =
        {
            new GzipArchiveCompressor(),
        };

        foreach (var compressor in compressors)
        {
            if (compressor.IsSupportedFormat(data))
                return Option<IArchiveCompressor>.Some(compressor);
        }

        return Option<IArchiveCompressor>.None();
    }
}