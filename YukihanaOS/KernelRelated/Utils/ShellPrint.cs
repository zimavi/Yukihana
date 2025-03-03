﻿using System;

namespace YukihanaOS.KernelRelated.Utils
{
    internal static class ShellPrint
    {
        private const string _PRINT_PADDING = "       ";
        private const string _WARN_PADDING = "WARNING";
        private const string _ERROR_PADDING = " ERROR ";
        private const string _OK_PADDING = "   OK  ";
        private const string _WORKING_PADDING = "  ***  ";

        /// <summary>
        /// Prints kernel logs
        /// </summary>
        /// <param name="str">string to print</param>
        public static void PrintK(string str)
        {
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
            var origColor = Console.ForegroundColor;
            ClearLine();


            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Yellow;
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
            Console.Write(new string('\0', Console.WindowWidth - 1));


            Console.SetCursorPosition(0, currLineCursor);
        }
    }
}
