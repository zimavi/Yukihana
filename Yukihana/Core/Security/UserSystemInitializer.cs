// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Security;

public static class UserSystemInitializer
{
    public static IUserStore CreateDefault()
    {
        var store = new InMemoryUserStore();

        var rootGroup = new Group(0, "root");
        var usersGroup = new Group(100, "users");


        store.AddGroup(rootGroup);
        store.AddGroup(usersGroup);

        var root = new User(
            Id: 0,
            Name: "root",
            PrimaryGroupId: rootGroup.Id,
            HomeDirectory: "/root",
            Shell: "yukihana-shell",
            PasswordHash: PasswordHasher.Hash("root")
        );

        var yuki = new User(
            Id: 1000,
            Name: "yuki",
            PrimaryGroupId: usersGroup.Id,
            HomeDirectory: "/home/yuki",
            Shell: "yukihana-shell",
            PasswordHash: PasswordHasher.Hash("yuki-yuki")
        );

        store.AddUser(root);
        store.AddUser(yuki);

        return store;
    }
}