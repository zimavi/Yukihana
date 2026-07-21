// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core;

public sealed class KernelInfo
{
    public string Name { get; }
    public Version Version { get; }
    public DateTime Revision { get; }

    public KernelInfo(string name, Version version, DateTime revision)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        Name = name;
        Version = version;
        Revision = revision;
    }

    public string RevisionString => Revision.ToString("yyyyMMddHHmm");

    public override string ToString() => $"{Name} {Version} (rev {RevisionString})";
}
