// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public interface IShellCommand
{
    public string Name { get; }
    public IReadOnlyList<string> Aliases { get; }
    public string Summary { get; }
    public string Usage { get; }
    public Capability RequiredCapabilities { get; }
    public ShellCommandFlags Flags { get; }
    public ShellCommandResult Execute(ShellCommandContext context);
}