// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Shell.Exceptions;

namespace Yukihana.Shell;


public sealed class ShellCancellation
{
    private volatile bool _isCancellationRequested;

    public bool IsCancellationRequested => _isCancellationRequested;

    public void Cancel()
    {
        _isCancellationRequested = true;
    }

    public void ThrowIfCancellationRequested()
    {
        if (_isCancellationRequested)
        {
            throw new ShellCommandCancelledException();
        }
    }
}