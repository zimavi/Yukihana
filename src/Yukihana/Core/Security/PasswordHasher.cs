// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using acryptohashnet;

namespace Yukihana.Core.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var sha = new Sha2_256();
        return Convert.ToHexString(
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(password)
            )
        );
    }

    public static bool Verify(string password, string hash) => Hash(password) == hash;
}
