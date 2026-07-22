// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Boot;

public readonly struct BootArgument(string name, Option<string> value)
{
    public string Name { get; } = name;

    public Option<string> Value { get; } = value;

    public bool IsFlag => Value.IsNone;

    public override string ToString()
    {
        BootArgument self = this;
        return Value.Map(
            value => $"{self.Name}={self.Value}",
            () => self.Name);
    }
}
