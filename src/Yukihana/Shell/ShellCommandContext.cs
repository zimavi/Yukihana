// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell;

public sealed class ShellCommandContext
{
    public required ShellSession Session { get; init; }
    public required IReadOnlyList<string> Arguments { get; init; }
    public required TextReader In { get; init; }
    public required TextWriter Out { get; init; }
    public required TextWriter Err { get; init; }
    public required ShellCancellation Cancellation { get; init; }
}