// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Security.Cryptography;

namespace Yukihana.Core.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return Sha256.ComputeHashString(password);
    }

    public static bool Verify(string password, string hash) => Hash(password) == hash;
}