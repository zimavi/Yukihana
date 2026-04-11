// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Security;

public record User(
    int Id,
    string Name,
    int PrimaryGroupId,
    string HomeDirectory,
    string Shell,
    string PasswordHash)
{
    public static readonly User None = new(
        Id: -1,
        Name: "nobody",
        PrimaryGroupId: -1,
        HomeDirectory: "/nonexistent",
        Shell: "nologin",
        PasswordHash: string.Empty);
    
    public static readonly User Guest = new(
        Id: 65534,
        Name: "guest",
        PrimaryGroupId: 65534,
        HomeDirectory: "/tmp",
        Shell: "restricted-shell",
        PasswordHash: string.Empty);
}