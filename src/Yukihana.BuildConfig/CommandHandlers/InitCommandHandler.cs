// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Tomlyn;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class InitCommandHandler
{
    public static int Handle(ParseResult result)
    {
        string featuresPath = Path.Combine(Directory.GetCurrentDirectory(), Globals.FeaturesDirectoryPath);
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), Globals.OutputDirectoryPath);
        string configsPath = Path.Combine(Directory.GetCurrentDirectory(), Globals.ConfigsDirectoryPath);
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), Globals.ManifestTomlPath);

        Log.Verbose("Creating Features directory at -> {FeaturesPath}", featuresPath);
        Log.Verbose("Creating Configs directory at -> {ConfigsPath}", configsPath);
        Log.Verbose("Creating Generated directory at -> {OutputPath}", outputPath);

        if (!Directory.Exists(featuresPath))
        {
            Directory.CreateDirectory(featuresPath);
        }
        if (!Directory.Exists(configsPath))
        {
            Directory.CreateDirectory(configsPath);
        }
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Log.Verbose("Creating Manifest.toml at -> {ManifestPath}", manifestPath);

        File.Create(manifestPath);

        Log.Information("You would need to fill .toml files your self. Use --help to know more");

        return 0;
    }
}
