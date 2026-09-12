// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell;

[Flags]
public enum ShellCommandFlags
{
    None = 0,
    Builtin = 1 << 0,
    MutatesShellState = 1 << 1,
    LongRunning = 1 << 2,
    RequiresInteractiveInput = 1 << 3,
}