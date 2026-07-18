// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.HAL.Interfaces.Devices;

namespace Yukihana.Core.IO.Vfs.Device;

internal sealed class MemoryBlockDevice : IBlockDevice
{
    private readonly byte[] _storage;

    public MemoryBlockDevice(string name, ulong blockSize, ulong blockCount)
    {
        Name = name;
        BlockSize = blockSize;
        BlockCount = blockCount;
        _storage = new byte[blockSize * blockCount];
    }

    public ulong BlockCount { get; }

    public ulong BlockSize { get; }

    public string Name { get; }

    public void Flush() {}
    public void ReadBlock(ulong blockNo, ulong blockCount, Span<byte> data) 
        => _storage.AsSpan((int)(blockNo * BlockSize), (int)(blockCount * BlockSize)).CopyTo(data);
    public void WriteBlock(ulong blockNo, ulong blockCount, ReadOnlySpan<byte> data) 
        => data.Slice(0, (int)(blockCount * BlockSize)).CopyTo(_storage.AsSpan((int)(blockNo * BlockSize)));
}