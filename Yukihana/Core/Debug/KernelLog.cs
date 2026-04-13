// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.Core.IO;

namespace Yukihana.Core.Debug;

public static class KernelLog
{
    private static readonly List<LogEntry> _entries = [];

    public static IReadOnlyList<LogEntry> Entries => _entries;
    public static LogLevel SerialLevel = LogLevel.Info;

    public static bool LogToScreen = false;
    public static bool LogToUart = true;

    public static void Write(
        LogLevel level,
        string source,
        string message,
        string member,
        string file,
        int line)
    {
        var entry = new LogEntry(
            DateTime.Now,
            level,
            source,
            message,
            member,
            file,
            line);

        _entries.Add(entry);

        if (level >= SerialLevel && LogToUart)
            Serial.WriteString(FormatForSerial(entry) + "\n");

        if (LogToScreen)
            ConsoleRenderer.Render(entry);
    }

    private static string FormatForSerial(LogEntry e)
    {
        var delta = e.Time - Kernel.BootTime;
        return string.IsNullOrWhiteSpace(e.Source)
            ? $"[{delta.TotalSeconds,10:0.000000}] [{e.Level}] {e.Message}"
            : $"[{delta.TotalSeconds,10:0.000000}] [{e.Level}] {e.Source}: {e.Message}";
    }
}