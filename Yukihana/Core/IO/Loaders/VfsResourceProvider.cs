// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders;

public sealed class VfsResourceProvider : IResourceProvider
{
    public Option<byte[]> TryLoad(string relativePath)
    {
        if(!VFS.Exists(relativePath))
            return Option<byte[]>.None();
        
        return VFS.ReadAllBytes(relativePath).ToOption();
    }
}