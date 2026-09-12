// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Execution;

public sealed record ShellCommandLine(string Command, IReadOnlyList<string> Arguments);