// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Execution;

public enum ShellJobState
{
    Created,
    Running,
    Cancelling,
    Completed,
    Failed,
    Cancelled
}