// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Spectre.Console;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class RootCommandHandler
{
    public static int Handle(ParseResult result)
    {
        bool isVerbose = result.GetValue(Globals.Args.VerboseOption);
        bool isQuiet = result.GetValue(Globals.Args.QuietOption);
        bool noColor = result.GetValue(Globals.Args.NoColorOption);
        string featuresPath = result.GetValue(Globals.Args.FeaturesPathOption)!;
        string outputPath = result.GetValue(Globals.Args.GeneratedPathOption)!;
        string configsPath = result.GetValue(Globals.Args.ConfigsPathOption)!;
        string manifestPath = result.GetValue(Globals.Args.ManifestPathOption)!;

        if (isVerbose)
        {
            Globals.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
        }
        else if (isQuiet)
        {
            Globals.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
        }

        if (noColor)
        {
            AnsiConsole.Profile.Capabilities.Ansi = false;
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        }

        Globals.FeaturesDirectoryPath = featuresPath;
        Globals.OutputDirectoryPath = outputPath;
        Globals.ConfigsDirectoryPath = configsPath;
        Globals.ManifestTomlPath = manifestPath;

        return 0;
    }
}
