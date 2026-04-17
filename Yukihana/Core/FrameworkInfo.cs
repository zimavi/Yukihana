// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core;

public sealed class FrameworkInfo
{
    public string Name { get; }
    public Version Version { get; }

    public FrameworkInfo(string name, Version version)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        Name = name;
        Version = version;
    }

    public override string ToString() => $"{Name} {Version}";
}
