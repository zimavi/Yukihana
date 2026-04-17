// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Compression.Archives;

public static class ArchivePath
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();

        while (path.StartsWith("./", StringComparison.Ordinal))
            path = path[2..];

        while (path.StartsWith('/'))
            path = path[1..];

        var parts = new List<string>();
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
                continue;
            }

            parts.Add(part);
        }

        return string.Join('/', parts);
    }

    public static string NormalizeLinkTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        return target.Replace('\\', '/');
    }

    public static int Align(int value, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);

        return (value + alignment - 1) & ~(alignment - 1);
    }

    public static byte[] CopyBytes(ReadOnlySpan<byte> source, int offset, int count)
    {
        byte[] result = new byte[count];
        source.Slice(offset, count).CopyTo(result);
        return result;
    }
}
