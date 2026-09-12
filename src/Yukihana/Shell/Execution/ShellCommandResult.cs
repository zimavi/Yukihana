// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Execution;

public abstract record ShellCommandResult
{
    private ShellCommandResult() { }

    public abstract int ExitCode { get; }

    public sealed record Success : ShellCommandResult
    {
        public override int ExitCode => 0;
    }

    public sealed record Cancelled : ShellCommandResult
    {
        public override int ExitCode => 130;
    }

    public sealed record PermissionDenied(string Message) : ShellCommandResult
    {
        public override int ExitCode => 126;
    }

    public sealed record NotFound(string Command) : ShellCommandResult
    {
        public override int ExitCode => 127;
        public string Message => $"{Command}: command not found";
    }

    public sealed record Error(string Message) : ShellCommandResult
    {
        public override int ExitCode => 1;
    }

    public static readonly Success Ok = new();
    public static readonly Cancelled CancelledInstance = new();

}