// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public interface IUserStore
{
    Option<User> GetUserById(int id);
    Option<User> GetUserByName(string name);

    Option<Group> GetGroupById(int id);
    Option<Group> GetGroupByName(string name);

    IEnumerable<User> GetAllUsers();
    IEnumerable<Group> GetAllGroups();

    void AddUser(User user);
    void AddGroup(Group group);
}