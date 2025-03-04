// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using YukihanaOS.KernelRelated.Debug;

namespace YukihanaOS.KernelRelated.Utils
{
    internal static class ShellPrint
    {
        private const string _PRINT_PADDING = "       ";
        private const string _WARN_PADDING = "WARNING";
        private const string _ERROR_PADDING = " ERROR ";
        private const string _OK_PADDING = "   OK  ";
        private const string _WORKING_PADDING = "  ***  ";

        public static char ClearLineChar = ' ';
        public static bool KernelPrintEnabled = true;
        public static bool DoLogKernelPrint = true;     // only used when panicing so we don't break loop

        /// <summary>
        /// Prints kernel logs
        /// </summary>
        /// <param name="str">string to print</param>
        public static void PrintK(string str)
        {
            if (DoLogKernelPrint)
                Logger.DoKernelLog("[" + _PRINT_PADDING + "] " + str);
            if (!KernelPrintEnabled)
                return;

            var origColor = Console.ForegroundColor;
            ClearLine();


            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[" + _PRINT_PADDING + "] ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);


            Console.ForegroundColor = origColor;
        }

        /// <summary>
        /// Prints kernel warnings
        /// </summary>
        /// <param name="str">string to print</param>
        public static void WarnK(string str, bool printOnLineAbove = false)
        {
            if (DoLogKernelPrint)
                Logger.DoKernelLog("[" + _WARN_PADDING + "] " + str);
            if (!KernelPrintEnabled)
                return;
            var origColor = Console.ForegroundColor;
            ClearLine();


            if (printOnLineAbove)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(_WARN_PADDING);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);


            Console.ForegroundColor = origColor;
        }

        /// <summary>
        /// Prints kernel errors
        /// </summary>
        /// <param name="str">string to print</param>
        public static void ErrorK(string str, bool printOnLineAbove = false)
        {
            if (DoLogKernelPrint)
                Logger.DoKernelLog("[" + _ERROR_PADDING + "] " + str);
            if (!KernelPrintEnabled)
                return;
            var origColor = Console.ForegroundColor;
            ClearLine();


            if (printOnLineAbove)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(_ERROR_PADDING);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);


            Console.ForegroundColor = origColor;
        }

        /// <summary>
        /// Prints kernel task result "OK"
        /// </summary>
        /// <param name="str">string to print</param>
        public static void OkK(string str, bool printOnCurrentLine = false)
        {
            if (DoLogKernelPrint)
                Logger.DoKernelLog("[" + _OK_PADDING + "] " + str);
            if (!KernelPrintEnabled)
                return;
            var origColor = Console.ForegroundColor;
            ClearLine();


            if (!printOnCurrentLine)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_OK_PADDING);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);


            Console.ForegroundColor = origColor;
        }

        /// <summary>
        /// Prints kernel task result "OK"
        /// </summary>
        /// <param name="str">string to print</param>
        public static void WorkK(string str)
        {
            if (DoLogKernelPrint)
                Logger.DoKernelLog("[" + _WORKING_PADDING + "] " + str);
            if (!KernelPrintEnabled)
                return;
            var origColor = Console.ForegroundColor;
            ClearLine();


            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(_WORKING_PADDING);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);


            Console.ForegroundColor = origColor;
        }

        public static void ClearLine()
        {
            int currLineCursor = Console.CursorTop;


            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(ClearLineChar, Console.WindowWidth - 1));


            Console.SetCursorPosition(0, currLineCursor);
        }
    }
}
