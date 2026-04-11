// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression;

public interface IArchiveCompressor
{
    public bool IsSupportedFormat(byte[] data);
    public byte[] Decompress(byte[] data);
    public byte[] Compress(byte[] data, int level = 6);
}