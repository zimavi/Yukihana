// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Serilog;
using Serilog.Sinks.Spectre;

namespace Yukihana.BuildConfig;

internal class Program
{
    private static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Spectre("[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        #region Global options

        Option<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "Enable verbose output."
        };
        Option<bool> quietOption = new("--quiet", "-q")
        {
            Description = "Suppress non-error output."
        };

        Option<bool> noColorOption = new("--no-color")
        {
            Description = "Disable ASNI colors."
        };
        Option<string> featuresPathOption = new("--features")
        {
            Description = "Path to Features directory",
            HelpName = "path"
        };
        Option<string> manifestPathOption = new("--manifest")
        {
            Description = "Path to Manifest.toml",
            HelpName = "path"
        };
        Option<string> generatedPathOption = new("--generated", "-o")
        {
            Description = "Path to generated output directory",
            HelpName = "path"
        };

        #endregion

        #region 'configure' command

        Argument<string?> presetArgument = new("preset");

        Option<bool> interactiveOption = new("--interactive", "-i")
        {
            Description = "Launch the interactive configurator."
        };
        Option<bool> cleanOption = new("--clean")
        {
            Description = "Remove generated files before generation."
        };
        Option<bool> noSaveOption = new("--no-save")
        {
            Description = "Do not update Current.toml."
        };

        Command configureCommand = new("configure", "Generated the build configuration.")
        {
            presetArgument,
            interactiveOption,
            cleanOption,
            noSaveOption
        };

        #endregion

        Command menuCommand = new("menu", "Launch the interactive configurator (alias to 'configure --interactive').");

        #region 'check' command

        Option<bool> fixOption = new("--fix")
        {
            Description = "Regenerate outdated files."
        };

        Command checkCommand = new("check", "Verify configuration.")
        {
            fixOption
        };

        #endregion

        Command validateCommand = new("validate", "Validate config files.");

        #region 'clean' command

        Option<bool> allOption = new("--all")
        {
            Description = "Remove Current.toml state information."
        };

        Command cleanCommand = new("clean", "Remove generated files.")
        {
            allOption
        };

        #endregion

        #region 'list' command

        Option<bool> featuresOption = new("--features");
        Option<bool> groupsOption = new("--groups");
        Option<bool> presetsOption = new("--presets");
        Option<bool> enabledOption = new("--enabled");
        Option<bool> disabledOption = new("--disabled");

        Command listCommand = new("list", "Show information.")
        {
            featuresOption,
            groupsOption,
            presetsOption,
            enabledOption,
            disabledOption
        };

        #endregion

        #region 'preset' command

        Argument<string> presetInteractionArgument = new("preset");

        Command listSubcommand = new("list", "List available presets.");

        Command showSubcommand = new("show", "Display specified preset.")
        {
            presetInteractionArgument
        };

        Command applySubcommand = new("apply", "Apply changes to specified preset.")
        {
            presetInteractionArgument
        };

        Command presetCommand = new("preset", "Manage presets.")
        {
            listSubcommand,
            showSubcommand,
            applySubcommand
        };

        #endregion

        #region 'feature' command

        Argument<string> featureArgument = new("feature");

        Command enableSubcommand = new("enable", "Enable the feature.")
        {
            featureArgument
        };

        Command disableSubcommand = new("disable", "Disable the feature.")
        {
            featureArgument
        };

        Command toggleSubcommand = new("toggle", "Toggle the feature on or off.")
        {
            featureArgument
        };

        Command featureCommand = new("feature", "Modify single feature.")
        {
            enableSubcommand,
            disableSubcommand,
            toggleSubcommand
        };

        #endregion

        Command graphCommand = new("graph", "Show included feature tree.");

        #region 'info' command

        Command infoCommand = new("info", "Show info about the feature.")
        {
            featureArgument
        };

        #endregion

        Command initCommand = new("init", "Creates \"Build\" directory at current location with tool's configs");

        RootCommand rootCmd = new("Yukihana build config tool")
        {
            // Global Options

            verboseOption,
            quietOption,
            noColorOption,
            featuresPathOption,
            manifestPathOption,
            generatedPathOption,

            // Commands
            configureCommand,
            menuCommand,
            checkCommand,
            validateCommand,
            cleanCommand,
            listCommand,
            presetCommand,
            featureCommand,
            graphCommand,
            infoCommand,
            initCommand
        };

        foreach (Option option in rootCmd.Options)
        {
            if (option is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new CustomHelpAction((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }

        // TODO: Set invocation target to command dispatcher

        ParseResult result = rootCmd.Parse(args);

        if (result.Errors.Count > 0)
        {
            string fullCmdLine = string.Join(" ", args);

            Log.Fatal("Syntax error parsing command line:");
            Log.Fatal($"> {fullCmdLine}");

            ParseError firstError = result.Errors[0];

            string? invalidToken = result.Tokens
                .Select(t => t.Value)
                .FirstOrDefault(v => firstError.Message.Contains($"'{v}'") || firstError.Message.Contains(v))
                ?? args.FirstOrDefault();

            if (string.IsNullOrEmpty(invalidToken))
            {
                Log.Fatal($"Error: {firstError.Message}");
                return 1;
            }

            int idx = fullCmdLine.IndexOf(invalidToken);
            if (idx >= 0)
            {
                string spaces = new(' ', idx + 2);
                string underline = '^' + new string('~', Math.Max(0, invalidToken.Length - 1));
                Log.Fatal($"{spaces}{underline}");
                Log.Fatal($"Error: {firstError.Message}");
            }

            return 1;
        }

        return result.Invoke();
    }
}
