// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Security;

public record User(
    int Id,
    string Name,
    int PrimaryGroupId,
    string HomeDirectory,
    string Shell,
    string PasswordHash
);