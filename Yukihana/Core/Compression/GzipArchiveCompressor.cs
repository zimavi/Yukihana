// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using ICSharpCode.SharpZipLib.GZip;

namespace Yukihana.Core.Compression;

public sealed class GzipArchiveCompressor : IArchiveCompressor
{
    private const byte ID1 = 0x1F;
    private const byte ID2 = 0x8B;
    private const byte COMPRESSION_METHOD_DEFLATE = 0x08;

    public bool IsSupportedFormat(byte[] data)
    {
        return data is not null &&
                data.Length >= 18 &&
                data[0] == ID1 &&
                data[1] == ID2 &&
                data[2] == COMPRESSION_METHOD_DEFLATE;
    }

    public byte[] Compress(byte[] data, int level = 6)
    {
        using var srcStream = new MemoryStream(data, writable: false);
        using var dstStream = new MemoryStream();

        GZip.Compress(srcStream, dstStream, isStreamOwner: false, level);

        return dstStream.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var srcStream = new MemoryStream(data, writable: false);
        using var dstStream = new MemoryStream();

        GZip.Decompress(srcStream, dstStream, isStreamOwner: false);

        return dstStream.ToArray();
    }
}