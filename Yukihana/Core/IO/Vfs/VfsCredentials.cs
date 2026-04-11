// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

public readonly record struct VfsCredentials(int UserId, int GroupId, bool IsRoot)
{
    public static VfsCredentials Root => new(0, 0, true);

    public static VfsCredentials None => new(-1, -1, false);
}