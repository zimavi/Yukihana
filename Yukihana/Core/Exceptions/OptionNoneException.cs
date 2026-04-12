// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Exceptions;

[Serializable]
public class OptionNoneException : InvalidOperationException
{
    public OptionNoneException() { }
    public OptionNoneException(string message) : base(message) { }
    public OptionNoneException(string message, Exception inner) : base(message, inner) { }
}