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
    public IReadOnlyCollection<int> SecondaryGroupIds { get; init; } = [];

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

    public bool IsInGroup(int groupId) =>
        groupId == PrimaryGroupId || SecondaryGroupIds.Contains(groupId);

    public IEnumerable<int> GetAllGroupIds()
    {
        yield return PrimaryGroupId;

        foreach(var groupId in SecondaryGroupIds)
        {
            if (groupId != PrimaryGroupId)
                yield return groupId;
        }
    }

    public User WithSecondaryGroups(IEnumerable<int> groupIds) => this with
    {
        SecondaryGroupIds = [.. groupIds.Where(id => id >= 0).Distinct()]
    };
}