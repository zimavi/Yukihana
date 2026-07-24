// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Yukihana.BuildConfig.Toml;

internal sealed class PresetConfig
{
    public string Description { get; set; } = string.Empty;
    public List<string> Enabled { get; set; } = [];
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace
)]
[TomlSerializable(typeof(PresetConfig))]
internal partial class PresetConfigContext : TomlSerializerContext
{
}
