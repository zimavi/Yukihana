// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Extensions.Conversion;
using Yukihana.Core.IO;
using Yukihana.Core.IO.Vfs;

namespace Yukihana.Core.Security;

public sealed class UserSession
{
    public User CurrentUser { get; private set; }

    public VfsCredentials Credentials => CurrentUser.ToVfsCredentials();

    public UserSession(User user)
    {
        CurrentUser = user;
        VFS.SetCredentials(Credentials);
    }

    public void SwitchUser(User newUser)
    {
        CurrentUser = newUser;
        VFS.SetCredentials(Credentials);
    }

    public void Logout()
    {
        CurrentUser = User.None;
        VFS.SetCredentials(VfsCredentials.None);
    }

    public bool IsRoot => CurrentUser.Id == 0;
}