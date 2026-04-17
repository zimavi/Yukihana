// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

[Flags]
public enum FsAccess : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}
