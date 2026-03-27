using System.Runtime.CompilerServices;
using Cosmos.Kernel.Core.IO;

namespace Yukihana.Core.Debug;

public enum LogOrigin : byte
{
    Unknown = 0,
    Bootstrap,
    Kernel,
    OS
}

public enum LogLevel : byte
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Critical
}

public struct LogEntry
{
    public LogLevel Level;
    public LogOrigin Origin;
    public string Log;
    //public DateTime Date;

    public string CallerMemberName;
    public string CallerFilePath;
    public int CallerLineNumber;

    public LogEntry(
        LogLevel level, 
        LogOrigin origin, 
        string log, 
        //DateTime date,
        string callerMemberName = "",
        string callerFilePath = "",
        int callerLineNumber = 0)
    {
        Level = level;
        Origin = origin;
        Log = log;
        //Date = date;
        CallerMemberName = callerMemberName ?? string.Empty;
        CallerFilePath = callerFilePath ?? string.Empty;
        CallerLineNumber = callerLineNumber;
    }

    public LogEntry WithLevel(LogLevel level)
    {
        Level = level;
        return this;
    }

    public LogEntry From(LogOrigin origin)
    {
        Origin = origin;
        return this;
    }

    public LogEntry WithLog(string log)
    {
        Log = log;
        return this;
    }

    //public LogEntry WithDate(DateTime date)
    //{
    //    Date = date;
    //    return this;
    //}

    public LogEntry WithCaller(string memberName, string filePath, int lineNumber)
    {
        CallerMemberName = memberName ?? string.Empty;
        CallerFilePath = filePath ?? string.Empty;
        CallerLineNumber = lineNumber;
        return this;
    }

    public bool HasCallerInfo =>
        !string.IsNullOrEmpty(CallerMemberName) ||
        !string.IsNullOrEmpty(CallerFilePath) ||
        CallerLineNumber != 0;

    public string CallerFileName =>
        string.IsNullOrEmpty(CallerFilePath) ? string.Empty : Path.GetFileName(CallerFilePath);

    public override string ToString()
    {
        //var timestamp = Date.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var caller = HasCallerInfo
            ? $" ({CallerMemberName}"
              + (string.IsNullOrEmpty(CallerFileName) ? "" : $" in {Path.GetFileName(CallerFileName)}")
              + (CallerLineNumber != 0 ? $":{CallerLineNumber}" : "")
              + ")"
            : string.Empty;
        
        //return $"[{timestamp}] [{Origin}] [{Level}] {Log}{caller}";
        return $"[{Origin}] [{Level}] {Log}{caller}";
    }
}


public static class Logger
{
    public static List<LogEntry> Logs = [];

    public static bool ReportLogsToDebugger = true;

    public static bool Enabled = true;

    public static LogLevel ReportLevel = LogLevel.Warn;

    public static void Log(
        LogLevel level,
        LogOrigin origin,
        string log,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if(!Enabled)
            return;

        var entry = new LogEntry(
            level,
            origin,
            log,
            //DateTime.Now,
            callerMemberName,
            callerFilePath,
            callerLineNumber);
        
        Logs.Add(entry);

        if (ReportLogsToDebugger && entry.Level >= ReportLevel)
            Serial.WriteString(entry.ToString() + "\n");
    }

    public static void Log(LogEntry entry)
    {
        if(!Enabled)
            return;

        Logs.Add(entry);

        if (ReportLogsToDebugger && entry.Level >= ReportLevel)
            Serial.WriteString(entry.ToString() + "\n");
    }

    public static void Trace(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Trace, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Debug(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Debug, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Info(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Info, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Warn(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Warn, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Error(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Error, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Critical(
        string log, 
        LogOrigin origin = LogOrigin.Unknown,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Log(LogLevel.Critical, origin, log, callerMemberName, callerFilePath, callerLineNumber);
    }
}
