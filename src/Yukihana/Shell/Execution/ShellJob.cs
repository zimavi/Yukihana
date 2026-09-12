// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Execution;

public sealed class ShellJob
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public Thread? Thread { get; set; }

    public ShellCancellation Cancellation { get; } = new();

    public ShellJobState State { get; set; }

    public ShellCommandResult? Result { get; set; }

    public void Cancel()
    {
        State = ShellJobState.Cancelling;
        Cancellation.Cancel();
    }
}