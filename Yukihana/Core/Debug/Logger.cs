// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;

namespace Yukihana.Core.Debug;

public sealed class Logger
{
    private readonly string _source;

    public Logger(string source)
    {
        _source = source;
    }

    public void Info(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) =>
        KernelLog.Write(LogLevel.Info, _source, msg, m, f, l);
    
    public void Warn(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
        => KernelLog.Write(LogLevel.Warn, _source, msg, m, f, l);

    public void Error(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
        => KernelLog.Write(LogLevel.Error, _source, msg, m, f, l);
}