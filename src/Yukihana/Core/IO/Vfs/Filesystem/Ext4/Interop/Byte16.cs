// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using System.Text;

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Interop;

[InlineArray(16)]
public struct Byte16
{
    private byte _element0;

    public readonly Guid ToGuid()
    {
        ReadOnlySpan<byte> span = this;
        return new Guid(span, bigEndian: true);
    }

    public void Set(Guid value)
    {
        Span<byte> span = this;
        value.TryWriteBytes(span, bigEndian: true, out _);
    }

    public readonly string GetAsciiString()
    {
        ReadOnlySpan<byte> span = this;

        int len = span.IndexOf((byte)0);
        if (len < 0)
        {
            len = span.Length;
        }

        return Encoding.ASCII.GetString(span[..len]);
    }

    public void SetAsciiString(ReadOnlySpan<char> text)
    {
        Span<byte> span = this;

        span.Clear();

        int written = Encoding.ASCII.GetBytes(
            text,
            span);

        if (written < span.Length)
        {
            span[written] = 0;
        }
    }
}
