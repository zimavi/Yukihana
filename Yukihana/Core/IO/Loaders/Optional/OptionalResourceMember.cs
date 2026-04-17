// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Loaders.Optional;

public readonly struct OptionalResourceMember<TState>
{
    public readonly string Path;
    public readonly string Description;
    public readonly Action<TState, byte[]> Apply;

    public OptionalResourceMember(string path, string desc, Action<TState, byte[]> applyCallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(desc);
        ArgumentNullException.ThrowIfNull(applyCallback);

        Path = path;
        Description = desc;
        Apply = applyCallback;
    }
}
