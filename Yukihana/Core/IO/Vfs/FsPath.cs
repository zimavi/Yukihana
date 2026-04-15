// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public static class FsPath
{
    public static bool IsAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Replace('\\', '/').Trim();
        return path.Length > 0 && path[0] == '/';
    }

    public static string NormalizeAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Replace('\\', '/').Trim();

        var parts = new List<string>(16);

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
                continue;
            }

            parts.Add(segment);
        }

        return parts.Count == 0 ? "/" : "/" + string.Join("/", parts);
    }

    public static string NormalizeRelative(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();
        bool absolute = path.StartsWith('/');

        var parts = new List<string>(16);

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (parts.Count > 0 && parts[^1] != "..")
                {
                    parts.RemoveAt(parts.Count - 1);
                }
                else if (!absolute)
                {
                    parts.Add("..");
                }

                continue;
            }

            parts.Add(segment);
        }

        return string.Join("/", parts);
    }

    public static string SanitizeSymlinkTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        return target.Replace('\\', '/').Trim();
    }

    public static string CombineAbsolute(string baseAbsolute, string addition)
    {
        baseAbsolute = NormalizeAbsolute(baseAbsolute);

        if (string.IsNullOrWhiteSpace(addition))
            return baseAbsolute;

        addition = addition.Replace('\\', '/').Trim();

        if (IsAbsolute(addition))
            return NormalizeAbsolute(addition);

        if (baseAbsolute == "/")
            return NormalizeAbsolute("/" + addition);

        return NormalizeAbsolute(baseAbsolute + "/" + addition);
    }

    public static string CombineRelative(string left, string right)
    {
        left = NormalizeRelative(left);
        right = NormalizeRelative(right);

        if (string.IsNullOrEmpty(left))
            return right;

        if (string.IsNullOrEmpty(right))
            return left;

        return NormalizeRelative(left + "/" + right);
    }

    public static string[] SplitRelative(string path)
    {
        path = NormalizeRelative(path);

        if (string.IsNullOrEmpty(path))
            return [];

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static string GetParent(string absolutePath)
    {
        absolutePath = NormalizeAbsolute(absolutePath);

        if (absolutePath == "/")
            return "/";

        int lastSlash = absolutePath.LastIndexOf('/');
        if (lastSlash <= 0)
            return "/";

        return absolutePath[..lastSlash];
    }

    public static string GetFileName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(path))
            return string.Empty;

        int lastSlash = path.LastIndexOf('/');
        return lastSlash < 0 ? path : path[(lastSlash + 1)..];
    }
}