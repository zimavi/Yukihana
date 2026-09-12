// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Commands;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public sealed class ShellCommandDispatcher(ShellCommandRegistry registry)
{
    private ShellCommandResult ExecuteInForegroundThread(IShellCommand command, ShellCommandContext context)
    {
        SecurityContext current = Kernel.SecurityManager.Current;

        ShellCommandResult result = new ShellCommandResult.Error("Command threw exception");

        Thread thread = new(() =>
        {
            try
            {
                Kernel.SecurityManager.Set(Thread.CurrentThread, current);

                result = command.Execute(context);
            }
            finally
            {
                Kernel.SecurityManager.Remove(Thread.CurrentThread);
            }
        });

        thread.Start();
        thread.Join();

        return result;
    }

    public ShellCommandResult Dispatch(
        ShellSession session,
        ShellCommandLine line)
    {
        IShellCommand? command = registry.Find(line.Command);

        if (command is null)
        {
            return new ShellCommandResult.NotFound(line.Command);
        }

        Capability required = command.RequiredCapabilities;

        if (required != Capability.None &&
            !Kernel.SecurityManager.Current.HasAllCapabilities(required))
        {
            return new ShellCommandResult.PermissionDenied(
                $"{line.Command}: permission denied");
        }

        ShellCancellation cancellation = new();

        ShellCommandContext context = new()
        {
            Session = session,
            Arguments = line.Arguments,
            In = Console.In,
            Out = Console.Out,
            Err = Console.Error,
            Cancellation = cancellation
        };

        return command.Flags.HasFlag(ShellCommandFlags.MutatesShellState) ? command.Execute(context) : ExecuteInForegroundThread(command, context);
    }
}