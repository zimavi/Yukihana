// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public enum LogLevel : byte
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Critical
}

public readonly struct LogEntry(
    DateTime time,
    LogLevel level,
    string source,
    string message,
    string member,
    string file,
    int line)
{
    public readonly DateTime Time = time;
    public readonly LogLevel Level = level;
    public readonly string Source = source;
    public readonly string Message = message;

    public readonly string Member = member;
    public readonly string File = file;
    public readonly int Line = line;
}
