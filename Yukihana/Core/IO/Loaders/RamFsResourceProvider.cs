using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders;

public sealed class RamFsResourceProvider : IResourceProvider
{
    private readonly RamFs _fs;

    public RamFsResourceProvider(RamFs fs)
    {
        _fs = fs;
    }

    public Option<byte[]> TryLoad(string relativePath)
    {


        if(!_fs.Exists(relativePath))
            return Option<byte[]>.None();
        
        return _fs.ReadAllBytes(relativePath);
    }
}