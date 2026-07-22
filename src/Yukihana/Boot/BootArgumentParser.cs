// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Boot;

public static class BootArgumentParser
{
    public static BootArguments Parse(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return new BootArguments([]);
        }

        List<BootArgument> arguments = [];

        ReadOnlySpan<char> span = commandLine.AsSpan();

        while (!span.IsEmpty)
        {
            span = TrimStart(span);

            if (span.IsEmpty)
            {
                break;
            }

            int separator = span.IndexOf(' ');

            ReadOnlySpan<char> token;

            if (separator < 0)
            {
                token = span;
                span = [];
            }
            else
            {
                token = span[..separator];
                span = span[(separator + 1)..];
            }

            int equals = token.IndexOf('=');

            if (equals < 0)
            {
                arguments.Add(new BootArgument(
                    token.ToString(),
                    Option<string>.None()));
            }
            else
            {
                arguments.Add(new BootArgument(
                    token[..equals].ToString(),
                    token[(equals + 1)..].ToString()));
            }
        }

        return new BootArguments(arguments.ToArray());
    }

    private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
    {
        int i = 0;

        while (i < span.Length && char.IsWhiteSpace(span[i]))
        {
            i++;
        }

        return span[i..];
    }
}