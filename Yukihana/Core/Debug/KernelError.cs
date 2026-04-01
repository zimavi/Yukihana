// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;

namespace Yukihana.Core.Debug;

public readonly struct KernelError
{
#region Properties

    public string Code { get; }
    public string Message { get; }

#endregion

#region Constructor

    public KernelError(string code, string message)
    {
        Code = code;
        Message = message;
    }

#endregion

#region Helpers

    public override string ToString() => $"{Code}: {Message}";
    
#endregion

#region Static errors

    public static class Codes
    {
        public const string UNKNOWN         = "unknown";
        public const string NOT_FOUND       = "not_found";
        public const string CORRUPTED_DATA  = "corrupted_data";
    }

#endregion

#region Factory methods

    public static KernelError NotFound(string path) => new(Codes.NOT_FOUND, $"Not found: {path}");

    public static KernelError Corrupted(string description) => new(Codes.CORRUPTED_DATA, description);

#endregion

}