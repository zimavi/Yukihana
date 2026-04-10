// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using Yukihana.Core.Debug;

namespace Yukihana.Core.IO;

public static class ShellPrint
{
    #region Constants

    private const string PRINT_PADDING   = "      ";
    private const string INFO_PADDING    = " INFO ";
    private const string WARN_PADDING    = " WARN ";
    private const string ERROR_PADDING   = " ERRO ";
    private const string OK_PADDING      = "  OK  ";

    private static readonly char[] _spinner = ['|', '/', '-', '\\'];

    private const char CLEAR_LINE_CHAR = ' ';

    #endregion

    #region Properties

    public static bool KernelPrintEnabled = true;
    public static bool DoLogKernelPrint = true;

    #endregion

    #region Public Logging API

    public static void PrintK(string message, string? source = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => CommonPrint(PRINT_PADDING, message, source, ConsoleColor.Gray, LogLevel.Debug, member, file, line);

    public static void InfoK(string message, string? source = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => CommonPrint(INFO_PADDING, message, source, ConsoleColor.Blue, LogLevel.Info, member, file, line);

    public static void WarnK(string message, string? source = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => CommonPrint(WARN_PADDING, message, source, ConsoleColor.Yellow, LogLevel.Warn, member, file, line);

    public static void ErrorK(string message, string? source = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => CommonPrint(ERROR_PADDING, message, source, ConsoleColor.Red, LogLevel.Error, member, file, line);

    public static void OkK(string message, string? source = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => CommonPrint(OK_PADDING, message, source, ConsoleColor.Green, LogLevel.Info, member, file, line);

    #endregion

    #region Task API

    public static ShellTask CreateTask(string text, string? source = null)
    {
        int row = Console.CursorTop;
        Console.WriteLine(); // reserve line
        return new ShellTask(text, source, row);
    }

    #endregion

    #region Core Print

    private static void CommonPrint(
        string prefix,
        string message,
        string? source,
        ConsoleColor color,
        LogLevel level,
        string member,
        string file,
        int line)
    {
        string finalMessage = FormatMessage(source, message);

        if (DoLogKernelPrint)
        {
            Logger.Log(level, LogOrigin.Kernel, $"[{prefix}] {finalMessage}", member, file, line);
        }

        if (!KernelPrintEnabled)
            return;

        var origColor = Console.ForegroundColor;

        ClearLine();
        PrintPrefix(prefix, color);

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(finalMessage);

        Console.ForegroundColor = origColor;
    }

    private static string FormatMessage(string? source, string message)
    {
        if (string.IsNullOrEmpty(source))
            return message;

        return $"{source}: {message}";
    }

    private static void PrintPrefix(string prefix, ConsoleColor color)
    {
        if (prefix == PRINT_PADDING)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"  {prefix}  ");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" [");

        Console.ForegroundColor = color;
        Console.Write(prefix);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("] ");
    }

    private static void ClearLine()
    {
        int top = Console.CursorTop;

        Console.CursorLeft = 0;
        Console.Write(new string(CLEAR_LINE_CHAR, Console.WindowWidth - 1));

        Console.CursorLeft = 0;
        Console.CursorTop = top;
    }

    #endregion

    #region Task Rendering

    private static void RenderTask(int row, ShellTask task)
    {
        int prevTop = Console.CursorTop;
        int prevLeft = Console.CursorLeft;

        Console.CursorTop = row;
        Console.CursorLeft = 0;

        ClearLine();

        string prefix = task.GetPrefix(out var color);

        PrintPrefix(prefix, color);

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(task.GetDisplayText());

        Console.CursorTop = prevTop;
        Console.CursorLeft = prevLeft;

        if(DoLogKernelPrint)
        {
            var level = task.GetState() switch
            {
                ShellPrintTaskState.Warn => LogLevel.Warn,
                ShellPrintTaskState.Error => LogLevel.Error,
                _ => LogLevel.Info,
            };

            Logger.Log(
                level,
                LogOrigin.Kernel,
                $" [{prefix}] {task.GetDisplayText()}",
                "",
                "",
                0);
        }
    }

    #endregion

    #region Task Types

    internal enum ShellPrintTaskState : byte
    {
        Working,
        Info,
        Warn,
        Error,
        Ok
    }

    public sealed class ShellTask
    {
        private readonly int _row;
        private ShellPrintTaskState _state;
        private int _progress = -1;
        private int _spinnerIndex;

        public string Text { get; private set; }
        public string? Source { get; }

        public ShellTask(string text, string? source, int row)
        {
            Text = text;
            Source = source;
            _row = row;
            _state = ShellPrintTaskState.Working;
        }

        #region Fluent API

        public ShellTask WithText(string text)
        {
            Text = text;
            return this;
        }

        public ShellTask Progress(int percent)
        {
            _progress = Math.Clamp(percent, 0, 100);
            return this;
        }

        public ShellTask Tick()
        {
            _spinnerIndex = (_spinnerIndex + 1) % _spinner.Length;
            return this;
        }

        public ShellTask Ok()    => SetState(ShellPrintTaskState.Ok);
        public ShellTask Warn()  => SetState(ShellPrintTaskState.Warn);
        public ShellTask Error() => SetState(ShellPrintTaskState.Error);
        public ShellTask Info()  => SetState(ShellPrintTaskState.Info);
        public ShellTask Work()  => SetState(ShellPrintTaskState.Working);

        public ShellTask Display()
        {
            RenderTask(_row, this);
            return this;
        }

        #endregion

        #region Internals

        private ShellTask SetState(ShellPrintTaskState state)
        {
            _state = state;
            return this;
        }

        internal ShellPrintTaskState GetState() => _state;

        public string GetPrefix(out ConsoleColor color)
        {
            color = _state switch
            {
                ShellPrintTaskState.Ok    => ConsoleColor.Green,
                ShellPrintTaskState.Warn  => ConsoleColor.Yellow,
                ShellPrintTaskState.Error => ConsoleColor.Red,
                ShellPrintTaskState.Info  => ConsoleColor.Blue,
                _                         => ConsoleColor.Gray
            };

            if (_state == ShellPrintTaskState.Working)
            {
                if (_progress >= 0)
                    return $"{_progress,3}% ";

                return $"  {_spinner[_spinnerIndex]}   ";
            }

            return _state switch
            {
                ShellPrintTaskState.Ok    => OK_PADDING,
                ShellPrintTaskState.Warn  => WARN_PADDING,
                ShellPrintTaskState.Error => ERROR_PADDING,
                ShellPrintTaskState.Info  => INFO_PADDING,
                _                         => PRINT_PADDING
            };
        }

        public string GetDisplayText()
        {
            if (string.IsNullOrEmpty(Source))
                return Text;

            return $"{Source}: {Text}";
        }

        #endregion
    }

    #endregion
}