// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders;

public sealed class VfsResourceProvider : IResourceProvider
{
    public Option<byte[]> TryLoad(string relativePath)
    {
        try
        {
            using FileStream fs = File.Open(relativePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using MemoryStream ms = new();

            fs.CopyTo(ms);

            return ms.ToArray();
        }
        catch
        {
            return Option<byte[]>.None();
        }
    }
}
