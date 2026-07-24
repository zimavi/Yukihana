// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog.Core;

namespace Yukihana.BuildConfig;

internal static class Globals
{
    public static LoggingLevelSwitch LevelSwitch { get; set; } = new(Serilog.Events.LogEventLevel.Information);

    public static string FeaturesDirectoryPath { get; set; } = "./Build/Features/";
    public static string ConfigsDirectoryPath { get; set; } = "./Build/Configs/";
    public static string OutputDirectoryPath { get; set; } = "./Build/Generated/";
    public static string ManifestTomlPath { get; set; } = "./Build/Manifest.toml";

    public static class Args
    {
        #region Global options

        public static Option<bool> VerboseOption { get; set; } = new("--verbose", "-v")
        {
            Description = "Enable verbose output.",
        };
        public static Option<bool> QuietOption { get; set; } = new("--quiet", "-q")
        {
            Description = "Suppress non-error output."
        };

        public static Option<bool> NoColorOption { get; set; } = new("--no-color")
        {
            Description = "Disable ASNI colors."
        };
        public static Option<string> FeaturesPathOption { get; set; } = new("--features")
        {
            Description = "Path to Features directory",
            HelpName = "path",
        };
        public static Option<string> ManifestPathOption { get; set; } = new("--manifest")
        {
            Description = "Path to Manifest.toml",
            HelpName = "path"
        };
        public static Option<string> ConfigsPathOption { get; set; } = new("--configs")
        {
            Description = "Path to configs directory",
            HelpName = "path"
        };
        public static Option<string> GeneratedPathOption { get; set; } = new("--generated", "-o")
        {
            Description = "Path to generated output directory",
            HelpName = "path"
        };

        #endregion

        #region 'configure' command

        public static Argument<string?> PresetArgument { get; set; } = new("preset");

        public static Option<bool> InteractiveOption { get; set; } = new("--interactive", "-i")
        {
            Description = "Launch the interactive configurator."
        };
        public static Option<bool> CleanOption { get; set; } = new("--clean")
        {
            Description = "Remove generated files before generation."
        };
        public static Option<bool> NoSaveOption { get; set; } = new("--no-save")
        {
            Description = "Do not update Current.toml."
        };

        public static Command ConfigureCommand { get; set; } = new("configure", "Generated the build configuration.")
        {
            PresetArgument,
            InteractiveOption,
            CleanOption,
            NoSaveOption
        };

        #endregion

        public static Command MenuCommand { get; set; } = new("menu", "Launch the interactive configurator (alias to 'configure --interactive').");

        #region 'check' command

        public static Option<bool> FixOption { get; set; } = new("--fix")
        {
            Description = "Regenerate outdated files."
        };

        public static Command CheckCommand { get; set; } = new("check", "Verify configuration.")
        {
            FixOption
        };

        #endregion

        public static Command ValidateCommand { get; set; } = new("validate", "Validate config files.");

        #region 'clean' command

        public static Option<bool> AllOption { get; set; } = new("--all")
        {
            Description = "Remove Current.toml state information."
        };

        public static Command CleanCommand { get; set; } = new("clean", "Remove generated files.")
        {
            AllOption
        };

        #endregion

        #region 'list' command

        public static Option<bool> FeaturesOption { get; set; } = new("--features");
        public static Option<bool> GroupsOption { get; set; } = new("--groups");
        public static Option<bool> PresetsOption { get; set; } = new("--presets");
        public static Option<bool> EnabledOption { get; set; } = new("--enabled");
        public static Option<bool> FisabledOption { get; set; } = new("--disabled");

        public static Command ListCommand { get; set; } = new("list", "Show information.")
        {
            FeaturesOption,
            GroupsOption,
            PresetsOption,
            EnabledOption,
            FisabledOption
        };

        #endregion

        #region 'preset' command

        public static Argument<string> PresetInteractionArgument { get; set; } = new("preset");

        public static Command ListSubcommand { get; set; } = new("list", "List available presets.");

        public static Command ShowSubcommand { get; set; } = new("show", "Display specified preset.")
        {
            PresetInteractionArgument
        };

        public static Command ApplySubcommand { get; set; } = new("apply", "Apply changes to specified preset.")
        {
            PresetInteractionArgument
        };

        public static Command PresetCommand { get; set; } = new("preset", "Manage presets.")
        {
            ListSubcommand,
            ShowSubcommand,
            ApplySubcommand
        };

        #endregion

        #region 'feature' command

        public static Argument<string> FeatureArgument { get; set; } = new("feature");

        public static Command EnableSubcommand { get; set; } = new("enable", "Enable the feature.")
        {
            FeatureArgument
        };

        public static Command DisableSubcommand { get; set; } = new("disable", "Disable the feature.")
        {
            FeatureArgument
        };

        public static Command ToggleSubcommand { get; set; } = new("toggle", "Toggle the feature on or off.")
        {
            FeatureArgument
        };

        public static Command FeatureCommand { get; set; } = new("feature", "Modify single feature.")
        {
            EnableSubcommand,
            DisableSubcommand,
            ToggleSubcommand
        };

        #endregion

        public static Command GraphCommand { get; set; } = new("graph", "Show included feature tree.");

        #region 'info' command

        public static Command InfoCommand { get; set; } = new("info", "Show info about the feature.")
        {
            FeatureArgument
        };

        #endregion

        public static Command InitCommand { get; set; } = new("init", "Creates \"Build\" directory at current location with tool's configs");

        public static RootCommand RootCmd { get; set; } = new("Yukihana build config tool")
        {
            // Global Options

            VerboseOption,
            QuietOption,
            NoColorOption,
            FeaturesPathOption,
            ManifestPathOption,
            ConfigsPathOption,
            GeneratedPathOption,

            // Commands
            ConfigureCommand,
            MenuCommand,
            CheckCommand,
            ValidateCommand,
            CleanCommand,
            ListCommand,
            PresetCommand,
            FeatureCommand,
            GraphCommand,
            InfoCommand,
            InitCommand
        };
    }
}
