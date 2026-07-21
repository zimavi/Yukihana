// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.Core.IO;
using Yukihana.Debug.Interfaces;

namespace Yukihana.Debug.Sinks;

internal sealed class SerialSink() : ILogSink
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    public void Write(ReadOnlySpan<char> text)
    {
        Serial.WriteString(new string(text) + '\n');
    }
}