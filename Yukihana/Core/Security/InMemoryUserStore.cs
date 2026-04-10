// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public sealed class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<int, User> _usersById = new();
    private readonly Dictionary<string, User> _usersByName = new(StringComparer.Ordinal);

    private readonly Dictionary<int, Group> _groupsById = new();
    private readonly Dictionary<string, Group> _groupsByName = new(StringComparer.Ordinal);

    public Option<User> GetUserById(int id)
    {
        if (_usersById.TryGetValue(id, out var user))
            return user;
        return Option<User>.None();
    }
    public Option<User> GetUserByName(string name)
    {
        if (_usersByName.TryGetValue(name, out var user))
            return user;
        return Option<User>.None();
    }

    public Option<Group> GetGroupById(int id)
    {
        if (_groupsById.TryGetValue(id, out var group))
            return group;
        return Option<Group>.None();
    }
    public Option<Group> GetGroupByName(string name)
    {
        if (_groupsByName.TryGetValue(name, out var group))
            return group;
        return Option<Group>.None();
    }

    public IEnumerable<User> GetAllUsers() => _usersById.Values;
    public IEnumerable<Group> GetAllGroups() => _groupsById.Values;

    public void AddUser(User user)
    {
        _usersById[user.Id] = user;
        _usersByName[user.Name] = user;

        if(_groupsById.TryGetValue(user.PrimaryGroupId, out var group))
            group.AddMember(user.Id);
    }

    public void AddGroup(Group group)
    {
        _groupsById[group.Id] = group;
        _groupsByName[group.Name] = group;
    }
}