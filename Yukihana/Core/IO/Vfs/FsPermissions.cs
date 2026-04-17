// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs;

[Flags]
public enum FsPermissions : ushort
{
    None = 0,

    OtherExecute = 0x001,
    OtherWrite = 0x002,
    OtherRead = 0x004,

    GroupExecute = 0x008,
    GroupRead = 0x010,
    GroupWrite = 0x020,

    UserExecute = 0x040,
    UserRead = 0x080,
    UserWrite = 0x100,
}
