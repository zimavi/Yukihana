// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Debug;

public readonly record struct LogEvent(
    LogLevel Level,
    string Source,
    string Message,
    DateTime Timestamp,
    int CPU,
    string FilePath,
    string MememberName,
    int LineNumber
);