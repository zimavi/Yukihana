// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Debug.Interfaces;

public interface ILogFormatter
{
    public string Format(LogEvent log);
}
