// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Yukihana.BuildConfig.Toml;

internal sealed class StateConfig
{
    [JsonIgnore]
    public Version GeneratorVersion { get; set; } = new();

    [JsonPropertyName("generator")]
    public string GeneratorVersionString
    {
        get => GeneratorVersion.ToString();
        set => GeneratorVersion = Version.TryParse(value, out Version? v) ? v : new Version();
    }

    public string ManifestHash { get; set; } = string.Empty;
    public string ConfigurationHash { get; set; } = string.Empty;
    public string Preset { get; set; } = string.Empty;

    [JsonPropertyName("generated")]
    public DateTime GeneratedTime { get; set; } = new();
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace
)]
[TomlSerializable(typeof(StateConfig))]
internal partial class StateConfigContext : TomlSerializerContext
{
}
