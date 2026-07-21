// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.Security;

public static class IdentitySerializer
{
    private static readonly Logger s_logger = new("identity.serializer");

    public static (string Passwd, string Shadow, string Group) Serialize(IUserStore store)
    {
        s_logger.Info("Attempting to serialize users");
        return (
            SerializePasswd(store.GetAllUsers()),
            SerializeShadow(store.GetAllUsers()),
            SerializeGroup(store.GetAllGroups(), store.GetAllUsers())
        );
    }

    public static string SerializePasswd(IEnumerable<User> users)
    {
        var sb = new StringBuilder();

        foreach (User? user in users.OrderBy(user => user.Id))
        {
            s_logger.Info($"Serializing user \"{user.Name}\"");

            ValidateName(user.Name);
            ValidatePath(user.HomeDirectory);
            ValidateField(user.Shell, nameof(User.Shell));

            sb.Append(user.Name);
            sb.Append(":x:");
            sb.Append(user.Id);
            sb.Append(':');
            sb.Append(user.PrimaryGroupId);
            sb.Append(':');
            sb.Append(user.HomeDirectory);
            sb.Append(':');
            sb.Append(user.Shell);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string SerializeShadow(IEnumerable<User> users)
    {
        var sb = new StringBuilder();

        foreach (User? user in users.OrderBy(user => user.Id))
        {
            s_logger.Info($"Serializing password for \"{user.Name}\"");

            ValidateName(user.Name);
            ValidateField(user.PasswordHash, nameof(user.PasswordHash), allowEmpty: true);

            sb.Append(user.Name);
            sb.Append(':');
            sb.Append(user.PasswordHash);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string SerializeGroup(IEnumerable<Group> groups, IEnumerable<User> users)
    {
        var usersById = users.ToDictionary(user => user.Id, user => user.Name);
        var sb = new StringBuilder();

        foreach (Group? group in groups.OrderBy(group => group.Id))
        {
            s_logger.Info($"Serializing group \"{group.Name}\"");

            ValidateName(group.Name);

            ICollection<int> members = group.Members as ICollection<int> ?? group.Members.ToList();

            var namesSet = new HashSet<string>(StringComparer.Ordinal);

            foreach (int userId in members.OrderBy(id => id))
            {
                if (!usersById.TryGetValue(userId, out string? userName))
                {
                    throw new InvalidOperationException(
                        $"Group '{group.Name}' references unknown user id '{userId}'.");
                }

                namesSet.Add(userName);
            }

            sb.Append(group.Name);
            sb.Append(":x:");
            sb.Append(group.Id);
            sb.Append(':');
            sb.Append(string.Join(',', namesSet));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static InMemoryUserStore Deserialize(string passwdText, string shadowText, string groupText)
    {
        var store = new InMemoryUserStore();
        LoadInto(store, passwdText, shadowText, groupText);
        return store;
    }

    public static void LoadInto(IUserStore store, string passwdText, string shadowText, string groupText)
    {
        ArgumentNullException.ThrowIfNull(store);

        Dictionary<string, string> passwordHashes = ParseShadow(shadowText);

        foreach (var line in ReadMeaningfulLines(passwdText))
        {
            string[] parts = SplitFields(line, 6, "passwd");

            string name = parts[0];
            string passwordField = parts[1];
            int userId = ParseInt(parts[2], "uid", line);
            int primaryGroupId = ParseInt(parts[3], "gid", line);
            string homeDirectory = parts[4];
            string shell = parts[5];

            string passwordHash = passwordHashes.TryGetValue(name, out string? hash)
                ? hash
                : passwordField is "x" or "*" ? string.Empty : passwordField;

            var user = new User(
                Id: userId,
                Name: name,
                PrimaryGroupId: primaryGroupId,
                HomeDirectory: homeDirectory,
                Shell: shell,
                PasswordHash: passwordHash);

            store.AddUser(user);
        }

        foreach (string line in ReadMeaningfulLines(groupText))
        {
            string[] parts = SplitFields(line, 4, "group");

            string name = parts[0];
            int groupId = ParseInt(parts[2], "gid", line);
            string membersField = parts[3];

            var group = new Group(groupId, name);
            store.AddGroup(group);

            if (string.IsNullOrWhiteSpace(membersField))
            {
                continue;
            }

            foreach (string memberName in membersField.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                Option<User> maybeUser = store.GetUserByName(memberName);
                if (maybeUser.IsNone)
                {
                    throw new FormatException(
                        $"Group '{name}' references unknown user '{memberName}'.");
                }

                group.AddMember(maybeUser.Value.Id);
            }
        }

        ApplySecondaryGroupIds(store);
    }

    private static void ApplySecondaryGroupIds(IUserStore store)
    {
        foreach (User? user in store.GetAllUsers().ToList())
        {
            int[] secondaryGroupIds = store.GetAllGroups()
                .Where(group => group.Id != user.PrimaryGroupId && group.ContainsMember(user.Id))
                .Select(group => group.Id)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            store.AddUser(user.WithSecondaryGroups(secondaryGroupIds));
        }
    }

    private static Dictionary<string, string> ParseShadow(string shadowText)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string line in ReadMeaningfulLines(shadowText))
        {
            string[] parts = SplitFields(line, 2, "shadow");

            string name = parts[0];
            string hash = parts[1];

            result[name] = hash;
        }

        return result;
    }

    private static IEnumerable<string> ReadMeaningfulLines(string text)
    {
        using var reader = new StringReader(text ?? string.Empty);

        while (reader.ReadLine() is { } rawLine)
        {
            string line = rawLine.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            yield return line;
        }
    }

    private static string[] SplitFields(string line, int expectedFieldCount, string fileKind)
    {
        string[] parts = line.Split(':');

        if (parts.Length != expectedFieldCount)
        {
            throw new FormatException(
                $"Invalid {fileKind} entry. Expected {expectedFieldCount} fields, got {parts.Length}: '{line}'.");
        }

        return parts;
    }

    private static int ParseInt(string value, string fieldName, string sourceLine)
    {
        if (!int.TryParse(value, out var result))
        {
            throw new FormatException($"Invalid {fieldName} value '{value}' in line: '{sourceLine}'.");
        }

        return result;
    }

    private static void ValidateName(string value)
    {
        ValidateField(value, "name", allowEmpty: false, ':', ',', '\n', '\r');
    }

    private static void ValidatePath(string value)
    {
        ValidateField(value, "path", allowEmpty: false, ':', '\n', '\r');
    }

    private static void ValidateField(string value, string fieldName, bool allowEmpty = false, params char[] forbidden)
    {
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be empty.", fieldName);
        }

        if (value.Any(ch => forbidden.Contains(ch)))
        {
            throw new ArgumentException($"{fieldName} contains invalid characters.", fieldName);
        }
    }
}
