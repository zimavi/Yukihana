// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public sealed class AuthService(IUserStore store)
{
    private readonly IUserStore _store = store;

    public Option<User> Login(string username, string password)
    {
        var user = _store.GetUserByName(username);

        if (user.IsNone)
            return Option<User>.None();
        
        if (!PasswordHasher.Verify(password, user.Value.PasswordHash))
            return Option<User>.None();
        
        return user.Value;
    }
}