// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Security;

public sealed class Group(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;

    private readonly HashSet<int> _members = [];
    public IReadOnlyCollection<int> Members => _members;

    public bool ContainsMember(int userId) => _members.Contains(userId);

    public void AddMember(int userId) => _members.Add(userId);

    public void AddMembers(IEnumerable<int> userIds)
    {
        foreach (int userId in userIds)
        {
            _members.Add(userId);
        }
    }

    public void RemoveMember(int userId) => _members.Remove(userId);

    public void ClearMembers() => _members.Clear();
}
