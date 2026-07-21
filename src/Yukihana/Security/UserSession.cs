// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Security;

public sealed class UserSession(User user)
{
    public User CurrentUser { get; private set; } = user;

    public void SwitchUser(User newUser)
    {
        CurrentUser = newUser;
    }

    public void Logout()
    {
        CurrentUser = User.None;
    }

    public bool IsRoot => CurrentUser.Id == 0;
}
