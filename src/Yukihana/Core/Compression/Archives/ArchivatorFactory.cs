// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.Compression.Archives;

public static class ArchivatorFactory
{
    private static readonly IArchivator[] s_archivators =
    [
        new CpioArchivator(),
        new TarArchivator(),
    ];

    public static Option<IArchivator> Detect(ReadOnlySpan<byte> data)
    {
        foreach (IArchivator archivator in s_archivators)
        {
            if (archivator.CanRead(data))
            {
                return Option<IArchivator>.Some(archivator);
            }
        }

        return Option<IArchivator>.None();
    }

    public static ArchiveImage Open(ReadOnlySpan<byte> data) =>
        Detect(data).OrThrow("Unknown archive format.").Read(data);
}
