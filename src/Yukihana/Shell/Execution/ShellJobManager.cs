// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Execution;

public sealed class ShellJobManager
{
    private readonly List<ShellJob> _jobs = [];

    public ShellJob? Foreground { get; set; }

    public ShellJob CreateForeground(string name)
    {
        ShellJob job = new()
        {
            Id = _jobs.Count + 1,
            Name = name,
            State = ShellJobState.Created
        };

        _jobs.Add(job);
        Foreground = job;

        return job;
    }
}