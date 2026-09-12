// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Commands;

public sealed class ShellCommandRegistry
{
    private readonly Dictionary<string, IShellCommand> _commands = new(StringComparer.Ordinal);

    public void Register(IShellCommand command)
    {
        _commands[command.Name] = command;

        foreach (string alias in command.Aliases)
        {
            _commands[alias] = command;
        }
    }

    public IShellCommand? Find(string name)
        => _commands.TryGetValue(name, out IShellCommand? command)
            ? command
            : null;

    public IReadOnlyCollection<IShellCommand> All
        => _commands.Values.Distinct().ToArray();
}