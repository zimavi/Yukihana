// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Spectre;
using Yukihana.BuildConfig.CommandHandlers;

namespace Yukihana.BuildConfig;

internal class Program
{
    private const string MUTUAL_EXCLUSIVE_MSG = "Mutually exclusive options used. Use either '{0}' or '{1}'";

    private static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(Globals.LevelSwitch)
            .WriteTo.Spectre("[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        foreach (Option option in Globals.Args.RootCmd.Options)
        {
            if (option is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new CustomHelpAction((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }

        Globals.Args.FeaturesPathOption.DefaultValueFactory = _ => "./Build/Features/";
        Globals.Args.GeneratedPathOption.DefaultValueFactory = _ => "./Build/Generated/";
        Globals.Args.ConfigsPathOption.DefaultValueFactory = _ => "./Build/Configs/";
        Globals.Args.ManifestPathOption.DefaultValueFactory = _ => "./Build/Manifest.toml";

        Globals.Args.RootCmd.SetAction(RootCommandHandler.Handle);
        // configure handler
        // menu handler
        // check handler
        // validate handler
        // clean handler
        // list handler
        // preset handler
        // feature handler
        // info handler
        // graph handler
        Globals.Args.InitCommand.SetAction(InitCommandHandler.Handle);

        Globals.Args.RootCmd.Validators.Add(result =>
        {
            if (result.Children
                .OfType<OptionResult>()
                .Count(or => or.Option == Globals.Args.VerboseOption
                    || or.Option == Globals.Args.QuietOption) != 1)
            {
                OptionResult verbose = result.Children.OfType<OptionResult>().First(or => or.Option == Globals.Args.VerboseOption);
                OptionResult quiet = result.Children.OfType<OptionResult>().First(or => or.Option == Globals.Args.QuietOption);
                result.AddError(string.Format(MUTUAL_EXCLUSIVE_MSG, verbose.IdentifierToken, quiet.IdentifierToken));
            }
        });

        ParseResult result = Globals.Args.RootCmd.Parse(args);

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
