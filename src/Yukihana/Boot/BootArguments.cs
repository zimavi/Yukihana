// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Globalization;
using Yukihana.Core.Primitives;

namespace Yukihana.Boot;

public sealed class BootArguments
{
    private readonly BootArgument[] _arguments;

    internal BootArguments(BootArgument[] arguments)
    {
        _arguments = arguments;
    }

    public int Count => _arguments.Length;

    public ReadOnlySpan<BootArgument> AsSpan() => _arguments;

    public bool HasFlag(string name)
    {
        foreach (BootArgument argument in _arguments)
        {
            if (argument.Name == name && argument.IsFlag)
            {
                return true;
            }
        }

        return false;
    }

    public Option<BootArgument> Find(string name)
    {
        foreach (BootArgument argument in _arguments)
        {
            if (argument.Name == name)
            {
                return argument;
            }
        }

        return Option<BootArgument>.None();
    }

    public Option<string> GetValue(string name)
    {
        foreach (BootArgument argument in _arguments)
        {
            if (argument.Name == name)
            {
                return argument.Value;
            }
        }

        return Option<string>.None();
    }

    public Option<int> GetInt32(string name)
    {
        return GetValue(name).Bind(value =>
        {
            if (int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed))
            {
                return parsed;
            }

            return Option<int>.None();
        });
    }

    public Option<bool> GetBoolean(string name)
    {
        return GetValue(name).Bind(value =>
        {
            switch (value)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    return true;

                case "0":
                case "false":
                case "no":
                case "off":
                    return false;

                default:
                    return Option<bool>.None();
            }
        });
    }

    public IEnumerable<string> GetValues(string name)
    {
        foreach (BootArgument argument in _arguments)
        {
            if (argument.Name == name)
            {
                if (argument.Value.TryGetValue(out string? value))
                {
                    yield return value!;
                }
            }
        }
    }
}