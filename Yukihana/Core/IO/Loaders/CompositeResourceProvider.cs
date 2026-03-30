// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders;

public sealed class CompositeResourceProvider : IResourceProvider
{
    private readonly List<IResourceProvider> _providers;

    public CompositeResourceProvider(params IResourceProvider[] providers)
    {
        _providers = [.. providers];
    }

    public Option<byte[]> TryLoad(string relativePath)
    {
        foreach (var provider in _providers)
        {
            var result = provider.TryLoad(relativePath);
            if (result.IsSome)
                return result;
        }

        return Option<byte[]>.None();
    }
}