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

            var origColor = Kernel.IO.ForegroundColor;
            ClearLine();


            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("[" + _PRINT_PADDING + "] ");

            Kernel.IO.ForegroundColor = ConsoleColor.White;
            Kernel.IO.WriteLine(str);


            Kernel.IO.ForegroundColor = origColor;
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
            var origColor = Kernel.IO.ForegroundColor;
            ClearLine();


            if (printOnLineAbove)
            {
                Kernel.IO.SetCursorPosition(0, Kernel.IO.CursorTop - 1);
            }

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("[");

            Kernel.IO.ForegroundColor = ConsoleColor.Yellow;
            Kernel.IO.Write(_WARN_PADDING);

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("] ");

            Kernel.IO.ForegroundColor = ConsoleColor.White;
            Kernel.IO.WriteLine(str);


            Kernel.IO.ForegroundColor = origColor;
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
            var origColor = Kernel.IO.ForegroundColor;
            ClearLine();


            if (printOnLineAbove)
            {
                Kernel.IO.SetCursorPosition(0, Kernel.IO.CursorTop - 1);
            }

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("[");

            Kernel.IO.ForegroundColor = ConsoleColor.Red;
            Kernel.IO.Write(_ERROR_PADDING);

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("] ");

            Kernel.IO.ForegroundColor = ConsoleColor.White;
            Kernel.IO.WriteLine(str);


            Kernel.IO.ForegroundColor = origColor;
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
            var origColor = Kernel.IO.ForegroundColor;
            ClearLine();


            if (!printOnCurrentLine)
            {
                Kernel.IO.SetCursorPosition(0, Kernel.IO.CursorTop - 1);
            }

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("[");

            Kernel.IO.ForegroundColor = ConsoleColor.Green;
            Kernel.IO.Write(_OK_PADDING);

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("] ");

            Kernel.IO.ForegroundColor = ConsoleColor.White;
            Kernel.IO.WriteLine(str);


            Kernel.IO.ForegroundColor = origColor;
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
            var origColor = Kernel.IO.ForegroundColor;
            ClearLine();


            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("[");

            Kernel.IO.ForegroundColor = ConsoleColor.Red;
            Kernel.IO.Write(_WORKING_PADDING);

            Kernel.IO.ForegroundColor = ConsoleColor.Gray;
            Kernel.IO.Write("] ");

            Kernel.IO.ForegroundColor = ConsoleColor.White;
            Kernel.IO.WriteLine(str);


            Kernel.IO.ForegroundColor = origColor;
        }

        public static void ClearLine()
        {
            int currLineCursor = Kernel.IO.CursorTop;


            Kernel.IO.SetCursorPosition(0, Kernel.IO.CursorTop);
            Kernel.IO.Write(new string(ClearLineChar, Kernel.IO.WindowWidth - 1));


            Kernel.IO.SetCursorPosition(0, currLineCursor);
        }
    }
}
