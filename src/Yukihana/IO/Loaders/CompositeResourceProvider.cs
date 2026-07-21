// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.IO.Loaders;

public sealed class CompositeResourceProvider(params IResourceProvider[] providers) : IResourceProvider
{
    private readonly List<IResourceProvider> _providers = [.. providers];

    public Option<byte[]> TryLoad(string relativePath)
    {
        foreach (IResourceProvider provider in _providers)
        {
            Option<byte[]> result = provider.TryLoad(relativePath);
            if (result.IsSome)
            {
                return result;
            }
        }

        return Option<byte[]>.None();
    }
}
