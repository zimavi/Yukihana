// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Vfs.Backends;

public sealed class DevFs : IVfsBackend
{
    private readonly Dictionary<string, Func<Result<Stream, KernelError>>> _devices;

    public DevFs()
    {
        _devices = new(StringComparer.Ordinal);

        _devices["null"] = () => new NullDeviceStream();
        _devices["zero"] = () => new ZeroDeviceStream();
        _devices["random"] = () => new RandomDeviceStream();
    }

    #region Interface Implementation

    public Result<byte[], KernelError> ReadAllBytes(string path)
    {
        var streamResult = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (streamResult.IsFailure)
            return Result<byte[], KernelError>.Failure(streamResult.Error);

        using var ms = new MemoryStream();
        streamResult.Value.CopyTo(ms);
        return Result<byte[], KernelError>.Success(ms.ToArray());
    }

    public Result<Stream, KernelError> Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        path = path.Trim('/');

        if (!_devices.TryGetValue(path, out var factory))
            return Result<Stream, KernelError>.Failure(KernelError.NotFound(path));

        if (mode != FileMode.Open)
            return Result<Stream, KernelError>.Failure(KernelError.InvalidOp("Device nodes only support FileMode.Open"));

        return factory();
    }

    public Option<KernelError> WriteAllBytes(string path, byte[] data)
    {
        using var result = Open(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        if (result.IsFailure)
            return result.Error;
        
        var stream = result.Value;
        stream.Write(data);
        stream.Flush();

        return Option<KernelError>.None();
    }

    public Option<KernelError> CreateDirectory(string path, bool recursive = true) => ReturnReadOnly();

    public Option<KernelError> CreateSymbolicLink(string path, string target) => ReturnReadOnly();

    public Option<KernelError> Delete(string path) => ReturnReadOnly();

    public Result<string[], KernelError> List(string path)
    {
        if (!string.IsNullOrEmpty(path.Trim('/')))
            return Result<string[], KernelError>.Failure(KernelError.NotFound(path));

        return Result<string[], KernelError>.Success([.. _devices.Keys]);
    }

    public Option<KernelError> SetPermissions(string path, FsPermissions permissions) => 
        ReturnReadOnly();

    public bool TryGetMetadata(string path, out VfsMetadata metadata)
    {
        path = path.Trim('/');
        if (string.IsNullOrEmpty(path))
        {
            metadata = new VfsMetadata(FsNodeKind.Directory, FsPermissionUtil.DefaultDirectory, 0, 0, 0);
            return true;
        }

        if (_devices.ContainsKey(path))
        {
            metadata = new VfsMetadata(FsNodeKind.File, FsPermissionUtil.DefaultFile, 0, 0, 0);
            return true;
        }

        metadata = default;
        return false;
    }

    public bool TryReadLink(string path, out string target)
    {
        target = string.Empty;
        return false;
    }

    public Result<string, KernelError> ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytesResult = ReadAllBytes(path);
        if (bytesResult.IsFailure)
            return Result<string, KernelError>.Failure(bytesResult.Error);

        return Result<string, KernelError>.Success(encoding.GetString(bytesResult.Value));
    }

    public bool Exists(string path)
    {
        path = path.Trim('/');
        return string.IsNullOrEmpty(path) || _devices.ContainsKey(path);
    }

    public FsNodeKind GetKind(string path)
    {
        path = path.Trim('/');
        if (string.IsNullOrEmpty(path))
            return FsNodeKind.Directory;

        return _devices.ContainsKey(path) ? FsNodeKind.File : FsNodeKind.Missing;
    }

    public VfsSpaceInfo GetSpaceInfo() => new(0, 0);

    public Option<KernelError> ResizeSpace(ulong totalBytes) => ReturnReadOnly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private KernelError ReturnReadOnly() => KernelError.InvalidOp("Read-only filesystem.");

    #endregion

    #region Device Streams

    public sealed class NullDeviceStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0; // EOF
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { /* discard */ }
    }

    private sealed class ZeroDeviceStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => long.MaxValue;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { /* discard */ }
    }

    private sealed class RandomDeviceStream : Stream
    {
        private readonly Random _rng = new();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => long.MaxValue;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            _rng.NextBytes(buffer.AsSpan(offset, count));
            return count;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { /* discard */ }
    }

    #endregion
}