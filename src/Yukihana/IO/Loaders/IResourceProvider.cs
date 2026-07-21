// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.IO.Loaders;

public interface IResourceProvider
{
    public Option<byte[]> TryLoad(string relativePath);
}
