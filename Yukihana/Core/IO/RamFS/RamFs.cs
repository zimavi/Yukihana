using System.Text;

namespace Yukihana.Core.IO.RamFS;

public class RamFs
{
    private readonly byte[] _blob;
    private readonly Dictionary<string, (int Offset, int Length)> _files;

    public RamFs(byte[] blob, Dictionary<string, (int Offset, int Length)> files)
    {
        _blob = blob;
        _files = files;
    }

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

    public string ReadAllText(string path, Encoding encoding = null)
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
        return path.Replace("\\", "/").TrimStart('/');
    }
}