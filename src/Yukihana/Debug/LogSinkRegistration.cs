// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Debug.Interfaces;

namespace Yukihana.Debug;

internal sealed class LogSinkRegistration
{
    public required ILogSink Sink { get; set; }
    public required ILogFormatter Formatter { get; set; }
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
    public bool Enabled { get; set; } = true;
}
