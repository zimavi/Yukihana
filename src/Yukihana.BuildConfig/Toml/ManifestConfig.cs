// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Yukihana.BuildConfig.Toml;

internal sealed class ManifestConfig
{
    public int Version { get; set; } = 1;

    public MetadataConfig Metadata { get; set; } = new();

    public List<GroupConfig> Group { get; set; } = [];

    public List<FeatureConfig> Feature { get; set; } = [];

    internal sealed class MetadataConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    internal sealed class GroupConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
    }

    internal sealed class FeatureConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Define { get; set; } = string.Empty;
        public bool? EnabledByDefault { get; set; }
        public List<string> Depends { get; set; } = [];
        public List<string> Exclude { get; set; } = [];
    }
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace
)]
[TomlSerializable(typeof(ManifestConfig))]
[TomlSerializable(typeof(ManifestConfig.FeatureConfig))]
[TomlSerializable(typeof(ManifestConfig.GroupConfig))]
[TomlSerializable(typeof(ManifestConfig.MetadataConfig))]
internal partial class ManifestConfigContext : TomlSerializerContext
{
}
