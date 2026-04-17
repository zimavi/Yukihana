// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.IO.Vfs;
using Yukihana.Core.Security;

namespace Yukihana.Core.Extensions.Conversion;

public static class UserToVfs
{
    public static VfsCredentials ToVfsCredentials(this User user)
    {
        if (user.Id == User.None.Id)
            return VfsCredentials.None;

        return new VfsCredentials(user.Id, user.PrimaryGroupId, user.Id == 0);
    }
}
