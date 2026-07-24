// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace Yukihana.BuildConfig;

internal class CustomHelpAction(HelpAction action) : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp = action;

    public override int Invoke(ParseResult parseResult)
    {
        int result = _defaultHelp.Invoke(parseResult);

        //Console.WriteLine("");

        return result;
    }
}
