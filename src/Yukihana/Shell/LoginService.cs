// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Extensions.System;
using Yukihana.Security;

namespace Yukihana.Shell;

public sealed class LoginService
{
    public User LoginInteractive()
    {
        while (true)
        {
            Console.Write("login: ");
            string? username = Console.ReadLine();

            User? user = UserManager.GetUser(username ?? "");

            if (user is null)
            {
                Console.WriteLine("unknown user");
                continue;
            }

            Console.Write("password: ");
            string? password = Console.ReadLineHidden();

            if (!AccountManager.Authenticate(user.Id, password ?? ""))
            {
                Console.WriteLine("incorrect password");
                continue;
            }

            return user;
        }
    }
}