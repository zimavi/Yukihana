// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Text;
using YukihanaOS.KernelRelated.Managers.PipesIO;

namespace YukihanaOS.KernelRelated.Managers
{
#pragma warning disable CS8632
    public interface IPipeIO
    {
        public void Write(char value);
        public void Write(char[] buffer);
        public void Write(char[] buffer, int idx, int count);
        public void Write(bool value);
        public void Write(object value);
        public void Write(string? value);
        public void Write(StringBuilder? value);
        public void Write(string format, object? arg0);
        public void Write(string format, object? arg0, object? arg1);
        public void Write(string format, object? arg0, object? arg1, object? arg2);
        public void Write(string format, params object?[] arg);
        public void WriteLine(char value);
        public void WriteLine(char[] buffer);
        public void WriteLine(char[] buffer, int idx, int count);
        public void WriteLine(bool value);
        public void WriteLine(object value);
        public void WriteLine(string? value);
        public void WriteLine(StringBuilder? value);
        public void WriteLine(string format, object? arg0);
        public void WriteLine(string format, object? arg0, object? arg1);
        public void WriteLine(string format, object? arg0, object? arg1, object? arg2);
        public void WriteLine(string format, params object?[] arg);
        public void SetCursorPosition(int left, int top);

        public ConsoleColor BackgroundColor { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public int CursorLeft { get; }
        public int CursorTop { get; }
        public int WindowWidth { get; }
        public int WindowHeight { get; }

    }
#pragma warning restore CS8632

    public class IOManager : IManager
    {
        public string Name => nameof(IOManager);

        public string Description => "Manages pipe to I/O";

        public bool IsInitialized => _isInitialized;
        private bool _isInitialized;

        public IPipeIO Pipe;

        public bool Initialize(out string error)
        {
            Pipe = new ConsolePipe();
            _isInitialized = true;
            error = null;
            return true;
        }

        public void Shutdown()
        {
            _isInitialized = false;
        }


    }
}
