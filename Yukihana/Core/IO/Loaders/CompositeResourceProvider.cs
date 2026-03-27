using Yukihana.Core.Primitives;
using static Yukihana.Core.IO.ShellPrint;

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