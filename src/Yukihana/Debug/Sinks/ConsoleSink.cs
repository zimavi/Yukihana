// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Debug.Interfaces;
using Yukihana.IO;

namespace Yukihana.Debug.Sinks;

internal sealed class ConsoleSink : ILogSink
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public void Write(ReadOnlySpan<char> text)
    {
        AnsiConsole.WriteLine(new string(text));
    }
}