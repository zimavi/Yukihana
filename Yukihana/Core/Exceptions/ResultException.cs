// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Exceptions;

[Serializable]
public class ResultException<TError> : InvalidOperationException
{
    public TError Error { get; }

    public ResultException(TError error)
    {
        Error = error;
    }

    public ResultException(string message, TError error) : base(message)
    {
        Error = error;
    }

    public ResultException(string message, TError error, Exception inner) : base(message, inner)
    {
        Error = error;
    }
}