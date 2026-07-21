// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public readonly struct KernelError(string code, string message)
{
    #region Properties

    public string Code { get; } = code;
    public string Message { get; } = message;

    #endregion

    #region Helpers

    public override string ToString() => $"{Code}: {Message}";

    #endregion

    #region Static errors

    public static class Codes
    {
        public const string UNKNOWN = "unknown";
        public const string NOT_FOUND = "not_found";
        public const string CORRUPTED_DATA = "corrupted_data";
        public const string PERMISSION_DENIED = "permission_denied";
        public const string INVALID_OPERATION = "invalid_operation";
        public const string OUT_OF_SPACE = "out_of_space";
    }

    #endregion

    #region Factory methods

    public static KernelError NotFound(string path) => new(Codes.NOT_FOUND, path);

    public static KernelError Corrupted(string description) => new(Codes.CORRUPTED_DATA, description);

    public static KernelError PermissionsDenied(string description) => new(Codes.PERMISSION_DENIED, description);

    public static KernelError InvalidOp(string description) => new(Codes.INVALID_OPERATION, description);

    public static KernelError NoSpaceLeft() => new(Codes.OUT_OF_SPACE, "No space left on device.");

    #endregion

}
