// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;

namespace Yukihana.IO;

public enum AnsiColor
{
    Black = ConsoleColor.Black,
    DarkBlue = ConsoleColor.DarkBlue,
    DarkGreen = ConsoleColor.DarkGreen,
    DarkCyan = ConsoleColor.DarkCyan,
    DarkRed = ConsoleColor.DarkRed,
    DarkMagenta = ConsoleColor.DarkMagenta,
    DarkYellow = ConsoleColor.DarkYellow,
    Gray = ConsoleColor.Gray,

    DarkGray = ConsoleColor.DarkGray,
    Blue = ConsoleColor.Blue,
    Green = ConsoleColor.Green,
    Cyan = ConsoleColor.Cyan,
    Red = ConsoleColor.Red,
    Magenta = ConsoleColor.Magenta,
    Yellow = ConsoleColor.Yellow,
    White = ConsoleColor.White
}

public sealed class AnsiStringBuilder
{
    private readonly StringBuilder _builder = new();

    public const string RESET = "\u001b[0m";

    public AnsiStringBuilder Append(string text)
    {
        _builder.Append(text);
        return this;
    }

    public AnsiStringBuilder AppendLine()
    {
        _builder.AppendLine();
        return this;
    }

    public AnsiStringBuilder AppendLine(string text)
    {
        _builder.AppendLine(text);
        return this;
    }

    public AnsiStringBuilder AppendColored(
        string text,
        AnsiColor? foreground = null,
        AnsiColor? background = null,
        bool bold = false,
        bool italic = false,
        bool underline = false)
    {
        bool first = true;

        void AddCode(int code)
        {
            _builder.Append(first ? "\u001b[" : ";");
            _builder.Append(code);
            first = false;
        }

        if (bold)
        {
            AddCode(1);
        }
        if (italic)
        {
            AddCode(3);
        }
        if (underline)
        {
            AddCode(4);
        }
        if (foreground.HasValue)
        {
            AddCode(GetForegroundCode(foreground.Value));
        }

        if (background.HasValue)
        {
            AddCode(GetBackgroundCode(background.Value));
        }
        if (!first)
        {
            _builder.Append('m');
        }

        _builder.Append(text);
        if (!first)
        {
            _builder.Append(RESET);
        }

        return this;
    }

    public AnsiStringBuilder AppendRgb(
        string text,
        byte r,
        byte g,
        byte b)
    {
        _builder.Append($"\u001b[38;2;{r};{g};{b}m");
        _builder.Append(text);
        _builder.Append(RESET);
        return this;
    }

    public AnsiStringBuilder AppendRgbBackground(
        string text,
        byte r,
        byte g,
        byte b)
    {
        _builder.Append($"\u001b[48;2;{r};{g};{b}m");
        _builder.Append(text);
        _builder.Append(RESET);
        return this;
    }

    public AnsiStringBuilder Red(string text)
    => AppendColored(text, foreground: AnsiColor.Red);

    public AnsiStringBuilder Green(string text)
        => AppendColored(text, foreground: AnsiColor.Green);

    public AnsiStringBuilder Yellow(string text)
        => AppendColored(text, foreground: AnsiColor.Yellow);

    public AnsiStringBuilder Blue(string text)
        => AppendColored(text, foreground: AnsiColor.Blue);

    public AnsiStringBuilder Bold(string text)
        => AppendColored(text, bold: true);


    public AnsiStringBuilder Clear()
    {
        _builder.Clear();
        return this;
    }

    public override string ToString() => _builder.ToString();

    private static int GetForegroundCode(AnsiColor color)
    {
        return color switch
        {
            AnsiColor.Black => 30,
            AnsiColor.DarkRed => 31,
            AnsiColor.DarkGreen => 32,
            AnsiColor.DarkYellow => 33,
            AnsiColor.DarkBlue => 34,
            AnsiColor.DarkMagenta => 35,
            AnsiColor.DarkCyan => 36,
            AnsiColor.Gray => 37,

            AnsiColor.DarkGray => 90,
            AnsiColor.Red => 91,
            AnsiColor.Green => 92,
            AnsiColor.Yellow => 93,
            AnsiColor.Blue => 94,
            AnsiColor.Magenta => 95,
            AnsiColor.Cyan => 96,
            AnsiColor.White => 97,

            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };
    }

    private static int GetBackgroundCode(AnsiColor color)
    {
        return color switch
        {
            AnsiColor.Black => 40,
            AnsiColor.DarkRed => 41,
            AnsiColor.DarkGreen => 42,
            AnsiColor.DarkYellow => 43,
            AnsiColor.DarkBlue => 44,
            AnsiColor.DarkMagenta => 45,
            AnsiColor.DarkCyan => 46,
            AnsiColor.Gray => 47,

            AnsiColor.DarkGray => 100,
            AnsiColor.Red => 101,
            AnsiColor.Green => 102,
            AnsiColor.Yellow => 103,
            AnsiColor.Blue => 104,
            AnsiColor.Magenta => 105,
            AnsiColor.Cyan => 106,
            AnsiColor.White => 107,

            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };
    }
}
