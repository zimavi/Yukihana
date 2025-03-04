// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Text;

namespace YukihanaOS.KernelRelated.Managers.PipesIO
{
    internal class ConsolePipe : IPipeIO
    {
        public void Write(char value)
        {
            Console.Write(value);
        }

        public void Write(char[] buffer)
        {
            Console.Write(buffer);
        }

        public void Write(char[] buffer, int idx, int count)
        {
            Console.Write(buffer, idx, count);
        }

        public void Write(bool value)
        {
            Console.Write(value ? "true" : "false");
        }

        public void Write(object value)
        {
            Console.Write(value);
        }

        public void Write(string value)
        {
            Console.Write(value);
        }

        public void Write(StringBuilder value)
        {
            Console.Write(value);
        }

        public void Write(string format, object arg0)
        {
            Console.Write(format, arg0);
        }

        public void Write(string format, object arg0, object arg1)
        {
            Console.Write(format, arg0, arg1);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            Console.Write(format, arg0, arg1, arg2);
        }

        public void Write(string format, params object[] arg)
        {
            Console.Write(format, arg);
        }

        public void WriteLine(char value)
        {
            Console.WriteLine(value);
        }

        public void WriteLine(char[] buffer)
        {
            Console.WriteLine(buffer);
        }

        public void WriteLine(char[] buffer, int idx, int count)
        {
            Console.WriteLine(buffer, idx, count);
        }

        public void WriteLine(bool value)
        {
            Console.WriteLine(value ? "true" : "false");
        }

        public void WriteLine(object value)
        {
            Console.WriteLine(value);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        public void WriteLine(StringBuilder value)
        {
            Console.WriteLine(value);
        }

        public void WriteLine(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public void WriteLine(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }
}
