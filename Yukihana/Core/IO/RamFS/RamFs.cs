// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.IO.RamFS;

public class RamFs
{
    private readonly byte[] _blob;
    private readonly Dictionary<string, (int Offset, int Length)> _files;

    public IReadOnlyDictionary<string, (int Offset, int Length)> Files => _files;

    public RamFs(byte[] blob, Dictionary<string, (int Offset, int Length)> files)
    {
        ArgumentNullException.ThrowIfNull(blob);
        ArgumentNullException.ThrowIfNull(files);

        _blob = blob;
        _files = new Dictionary<string, (int Offset, int Length)>(files, StringComparer.Ordinal);
    }

    public static RamFs FromArchive(byte[] archiveBytes) => RamFsArchive.LoadFromArchive(archiveBytes);

    public bool Exists(string path)
    {
        path = Normalize(path);
        return _files.ContainsKey(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        path = Normalize(path);

        if (!_files.TryGetValue(path, out var entry))
            throw new FileNotFoundException(path);

        var result = new byte[entry.Length];
        Buffer.BlockCopy(_blob, entry.Offset, result, 0, entry.Length);
        return result;
    }

    public string ReadAllText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetString(ReadAllBytes(path));
    }

    public Stream Open(string path)
    {
        path = Normalize(path);

        if (!_files.TryGetValue(path, out var entry))
            throw new FileNotFoundException(path);

        return new RamFsStream(_blob, entry.Offset, entry.Length);
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();

        while (path.StartsWith("./", StringComparison.Ordinal))
            path = path[2..];

        while (path.StartsWith("/", StringComparison.Ordinal))
            path = path[1..];

        while (path.Contains("//", StringComparison.Ordinal))
            path = path.Replace("//", "/");

        return path;
    }
}