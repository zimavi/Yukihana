// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;

namespace Yukihana.Debug;

public sealed class Logger(string source)
{
    private readonly string _source = source;

    public Logger() : this(string.Empty)
    { }

    public void Trace(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Trace, _source, msg, DateTime.Now, 0, f, m, l));
    }
    public void Debug(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Debug, _source, msg, DateTime.Now, 0, f, m, l));
    }

    public void Info(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Info, _source, msg, DateTime.Now, 0, f, m, l));
    }

    public void Warn(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Warn, _source, msg, DateTime.Now, 0, f, m, l));
    }

    public void Error(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Error, _source, msg, DateTime.Now, 0, f, m, l));
    }

    public void Critical(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        LogDispatcher.Dispatch(new(LogLevel.Crit, _source, msg, DateTime.Now, 0, f, m, l));
    }
}
