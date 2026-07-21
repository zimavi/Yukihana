// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Debug.Interfaces;
using Yukihana.IO;

namespace Yukihana.Debug.Formatters;

// This is Linux-like early boot delta formatter

internal sealed class DeltaAnsiFormatter : ILogFormatter
{
    private readonly AnsiStringBuilder _builder = new();

    public string Format(LogEvent log)
    {
        _builder.Clear();

        TimeSpan delta = log.Timestamp - Kernel.BootTime;

        _builder.AppendColored($"[{delta.TotalSeconds,10:0.000000}] [", AnsiColor.DarkGray)
        .AppendColored($"{log.Level.ToString().ToUpperInvariant(),-5}",
            log.Level == LogLevel.Crit ? AnsiColor.White :
            log.Level == LogLevel.Error ? AnsiColor.Red :
            log.Level == LogLevel.Warn ? AnsiColor.Yellow :
            log.Level == LogLevel.Info ? AnsiColor.Cyan :
            AnsiColor.DarkGray, log.Level == LogLevel.Crit ? AnsiColor.Red : null)
        .AppendColored("] ", AnsiColor.DarkGray);

        if (!string.IsNullOrEmpty(log.Source))
        {
            _builder.AppendColored(log.Source, AnsiColor.Gray);
            _builder.AppendColored(": ", AnsiColor.DarkGray);
        }

        _builder.AppendColored(log.Message, AnsiColor.Gray);

        return _builder.ToString();
    }
}
