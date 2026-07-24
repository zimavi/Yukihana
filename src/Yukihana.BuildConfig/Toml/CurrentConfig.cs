// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Yukihana.BuildConfig.Toml;

// This class is used to keep track of current selected options
internal sealed class CurrentConfig
{
    public int Version { get; set; } = 1;
    public List<string>? Enabled { get; set; } = [];
    public List<string>? Disabled { get; set; } = [];
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace
)]
[TomlSerializable(typeof(CurrentConfig))]
internal partial class CurrentConfigContext : TomlSerializerContext
{
}
