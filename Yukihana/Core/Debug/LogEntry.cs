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

public readonly struct LogEntry
{
    public readonly DateTime Time;
    public readonly LogLevel Level;
    public readonly string Source;
    public readonly string Message;

    public readonly string Member;
    public readonly string File;
    public readonly int Line;

    public LogEntry(
        DateTime time,
        LogLevel level,
        string source,
        string message,
        string member,
        string file,
        int line)
    {
        Time = time;
        Level = level;
        Source = source;
        Message = message;
        Member = member;
        File = file;
        Line = line;
    }
}