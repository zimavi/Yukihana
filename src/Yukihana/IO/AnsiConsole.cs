// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.RegularExpressions;

namespace Yukihana.IO;

public static partial class AnsiConsole
{
    private static readonly Regex AnsiRegex =
        AnsiRegexG();

    private static readonly Dictionary<int, ConsoleColor> ForegroundColors = new()
    {
        [30] = ConsoleColor.Black,
        [31] = ConsoleColor.DarkRed,
        [32] = ConsoleColor.DarkGreen,
        [33] = ConsoleColor.DarkYellow,
        [34] = ConsoleColor.DarkBlue,
        [35] = ConsoleColor.DarkMagenta,
        [36] = ConsoleColor.DarkCyan,
        [37] = ConsoleColor.Gray,

        [90] = ConsoleColor.DarkGray,
        [91] = ConsoleColor.Red,
        [92] = ConsoleColor.Green,
        [93] = ConsoleColor.Yellow,
        [94] = ConsoleColor.Blue,
        [95] = ConsoleColor.Magenta,
        [96] = ConsoleColor.Cyan,
        [97] = ConsoleColor.White,
    };

    private static readonly Dictionary<int, ConsoleColor> BackgroundColors = new()
    {
        [40] = ConsoleColor.Black,
        [41] = ConsoleColor.DarkRed,
        [42] = ConsoleColor.DarkGreen,
        [43] = ConsoleColor.DarkYellow,
        [44] = ConsoleColor.DarkBlue,
        [45] = ConsoleColor.DarkMagenta,
        [46] = ConsoleColor.DarkCyan,
        [47] = ConsoleColor.Gray,

        [100] = ConsoleColor.DarkGray,
        [101] = ConsoleColor.Red,
        [102] = ConsoleColor.Green,
        [103] = ConsoleColor.Yellow,
        [104] = ConsoleColor.Blue,
        [105] = ConsoleColor.Magenta,
        [106] = ConsoleColor.Cyan,
        [107] = ConsoleColor.White,
    };

    public static void Write(string text)
    {
        var defaultForeground = Console.ForegroundColor;
        var defaultBackground = Console.BackgroundColor;

        bool bold = false;
        int lastIndex = 0;

        foreach (Match match in AnsiRegex.Matches(text))
        {
            // Write text preceding ANSI sequence
            if (match.Index > lastIndex)
            {
                Console.Write(text.Substring(lastIndex, match.Index - lastIndex));
            }

            var codes = match.Groups[1].Value;

            if (string.IsNullOrEmpty(codes))
            {
                Reset(defaultForeground, defaultBackground);
            }
            else
            {
                foreach (var part in codes.Split(';'))
                {
                    if (!int.TryParse(part, out int code))
                    {
                        continue;
                    }

                    switch (code)
                    {
                        case 0:
                            bold = false;
                            Reset(defaultForeground, defaultBackground);
                            break;

                        case 1:
                            bold = true;
                            break;

                        case 22:
                            bold = false;
                            break;

                        case 39:
                            Console.ForegroundColor = defaultForeground;
                            break;

                        case 49:
                            Console.BackgroundColor = defaultBackground;
                            break;

                        default:
                            if (ForegroundColors.TryGetValue(code, out var fg))
                            {
                                Console.ForegroundColor = bold
                                    ? BrightEquivalent(fg)
                                    : fg;
                            }
                            else if (BackgroundColors.TryGetValue(code, out var bg))
                            {
                                Console.BackgroundColor = bg;
                            }
                            break;
                    }
                }
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            Console.Write(text.AsSpan(lastIndex));
        }

        Console.ForegroundColor = defaultForeground;
        Console.BackgroundColor = defaultBackground;
    }

    public static void WriteLine(string text)
    {
        Write(text);
        Console.WriteLine();
    }

    private static void Reset(ConsoleColor fg, ConsoleColor bg)
    {
        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;
    }

    private static ConsoleColor BrightEquivalent(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => ConsoleColor.DarkGray,
            ConsoleColor.DarkRed => ConsoleColor.Red,
            ConsoleColor.DarkGreen => ConsoleColor.Green,
            ConsoleColor.DarkYellow => ConsoleColor.Yellow,
            ConsoleColor.DarkBlue => ConsoleColor.Blue,
            ConsoleColor.DarkMagenta => ConsoleColor.Magenta,
            ConsoleColor.DarkCyan => ConsoleColor.Cyan,
            ConsoleColor.Gray => ConsoleColor.White,
            _ => color
        };
    }

    [GeneratedRegex(@"\x1B\[([0-9;]*)m")]
    private static partial Regex AnsiRegexG();
}
