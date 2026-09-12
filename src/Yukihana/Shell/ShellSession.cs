// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public sealed class ShellSession
{
    public required User User { get; set; }
    public string CurrentDirectory { get; set; } = "/";
    //TODO
    //public ShellEnvironment Environment { get; } = new();
    //public ShellHistory History { get; } = new();
    public ShellJobManager Jobs { get; } = new();
    public bool ExitRequested { get; set; }
}