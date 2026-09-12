// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Exceptions;

public class ShellCommandCancelledException : Exception
{
    public ShellCommandCancelledException()
    {
    }

    public ShellCommandCancelledException(string message) : base(message)
    {
    }

    public ShellCommandCancelledException(string message, Exception inner) : base(message, inner)
    {
    }
}