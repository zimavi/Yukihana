// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;

namespace Yukihana.Vfs.Device;

internal sealed class MemoryBlockDevice(string name, ulong blockSize, ulong blockCount) : IBlockDevice
{
    private readonly byte[] _storage = new byte[blockSize * blockCount];

    public ulong BlockCount { get; } = blockCount;

    public ulong BlockSize { get; } = blockSize;

    public string Name { get; } = name;

    public void Flush() { }
    public void ReadBlock(ulong blockNo, ulong blockCount, Span<byte> data)
        => _storage.AsSpan((int)(blockNo * BlockSize), (int)(blockCount * BlockSize)).CopyTo(data);
    public void WriteBlock(ulong blockNo, ulong blockCount, ReadOnlySpan<byte> data)
        => data.Slice(0, (int)(blockCount * BlockSize)).CopyTo(_storage.AsSpan((int)(blockNo * BlockSize)));
}
