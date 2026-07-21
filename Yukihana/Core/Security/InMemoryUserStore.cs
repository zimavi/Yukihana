// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public sealed class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<int, User> _usersById = [];
    private readonly Dictionary<string, User> _usersByName = new(StringComparer.Ordinal);

    private readonly Dictionary<int, Group> _groupsById = [];
    private readonly Dictionary<string, Group> _groupsByName = new(StringComparer.Ordinal);

    public Option<User> GetUserById(int id)
    {
        if (_usersById.TryGetValue(id, out User? user))
        {
            return user;
        }

        return Option<User>.None();
    }
    public Option<User> GetUserByName(string name)
    {
        if (_usersByName.TryGetValue(name, out User? user))
        {
            return user;
        }

        return Option<User>.None();
    }

    public Option<Group> GetGroupById(int id)
    {
        if (_groupsById.TryGetValue(id, out Group? group))
        {
            return group;
        }

        return Option<Group>.None();
    }
    public Option<Group> GetGroupByName(string name)
    {
        if (_groupsByName.TryGetValue(name, out Group? group))
        {
            return group;
        }

        return Option<Group>.None();
    }

    public IEnumerable<User> GetAllUsers() => _usersById.Values.OrderBy(user => user.Id);
    public IEnumerable<Group> GetAllGroups() => _groupsById.Values.OrderBy(group => group.Id);

    public void AddUser(User user)
    {
        if (_usersById.TryGetValue(user.Id, out User? existing))
        {
            _usersByName.Remove(existing.Name);
            RemoveUserFromAllGroups(existing);
        }

        _usersById[user.Id] = user;
        _usersByName[user.Name] = user;

        AddUserToAllKnownGroups(user);
    }

    public void AddGroup(Group group)
    {
        if (_groupsById.TryGetValue(group.Id, out Group? existing))
        {
            _groupsByName.Remove(existing.Name);
        }

        _groupsById[group.Id] = group;
        _groupsByName[group.Name] = group;

        SyncGroupMembersFromUsers(group);
    }

    private void AddUserToAllKnownGroups(User user)
    {
        if (_groupsById.TryGetValue(user.PrimaryGroupId, out Group? primaryGroup))
        {
            primaryGroup.AddMember(user.Id);
        }

        foreach (int secondaryGroupId in user.SecondaryGroupIds.Distinct())
        {
            if (_groupsById.TryGetValue(secondaryGroupId, out Group? group))
            {
                group.AddMember(user.Id);
            }
        }
    }

    private void RemoveUserFromAllGroups(User user)
    {
        if (_groupsById.TryGetValue(user.PrimaryGroupId, out Group? primaryGroup))
        {
            primaryGroup.RemoveMember(user.Id);
        }

        foreach (int secondaryGroupId in user.SecondaryGroupIds.Distinct())
        {
            if (_groupsById.TryGetValue(secondaryGroupId, out Group? group))
            {
                group.RemoveMember(user.Id);
            }
        }
    }

    private void SyncGroupMembersFromUsers(Group group)
    {
        foreach (User user in _usersById.Values)
        {
            if (user.PrimaryGroupId == group.Id || user.SecondaryGroupIds.Contains(group.Id))
            {
                group.AddMember(user.Id);
            }
        }
    }
}
