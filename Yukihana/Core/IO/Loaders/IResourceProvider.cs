using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders;

public interface IResourceProvider
{
    Option<byte[]> TryLoad(string relativePath);
}