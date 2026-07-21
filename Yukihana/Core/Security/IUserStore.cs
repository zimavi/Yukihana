// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public interface IUserStore
{
    public Option<User> GetUserById(int id);
    public Option<User> GetUserByName(string name);

    public Option<Group> GetGroupById(int id);
    public Option<Group> GetGroupByName(string name);

    public IEnumerable<User> GetAllUsers();
    public IEnumerable<Group> GetAllGroups();

    public void AddUser(User user);
    public void AddGroup(Group group);
}
