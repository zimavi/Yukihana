// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;
using Yukihana.Debug.Interfaces;
using Yukihana.IO;

namespace Yukihana.Debug.Formatters;

// This is Linux-like early boot delta formatter

internal sealed class DeltaFormatter : ILogFormatter
{
    private readonly StringBuilder _builder = new();

    public string Format(LogEvent log)
    {
        _builder.Clear();

        TimeSpan delta = log.Timestamp - Kernel.BootTime;

        _builder.Append($"[{delta.TotalSeconds,10:0.000000}] [{log.Level.ToString()}] ");

        if (!string.IsNullOrEmpty(log.Source))
        {
            _builder.Append(log.Source);
            _builder.Append(": ");
        }

        _builder.Append(log.Message);

        return _builder.ToString();
    }
}
