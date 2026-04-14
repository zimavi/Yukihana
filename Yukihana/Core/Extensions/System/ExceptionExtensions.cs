// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Cosmos.Kernel.Core.IO;
using Yukihana.Core.Debug;

namespace Yukihana.Core.Extensions.System;

public static partial class ExceptionExtensions
{
    extension (Exception ex)
    {
        public void Panic(
            string? message = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            KernelPanic.Panic(
                BuildReason(ex, message),
                //$"{message}: {ex.Message}",
                member,
                file,
                line);

            throw new UnreachableException();
        }

        // This avoid allocations
        public void PanicUnsafe(
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string reason;

            try
            {
                reason = ex.GetType().FullName + ": " + ex.Message;
                //reason = $"<exception>: {ex.Message}";
            }
            catch
            {
                reason = "Unknown exception (failed to read exception data)";
            }

            KernelPanic.Panic(reason, member, file, line);

            throw new UnreachableException();
        }
    }

    public static string BuildReason(Exception ex, string? message)
    {
        try
        {
            Serial.WriteString("Trying to build reason from exception\n");
            var sb = new StringBuilder(512);

            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.Append(message);
                sb.Append(": ");
            }

            AppendException(sb, ex, 0);

            return sb.ToString();
        }
        catch
        {
            try
            {
                Serial.WriteString("Unable to format exception. Fallback to `type: msg`\n");
                return ex.GetType().FullName + ": " + ex.Message;
            }
            catch
            {
                Serial.WriteString("Skipping exception extraction\n");
                return "Fatal exception (unprintable)";
            }
        }
    }

    private static void AppendException(StringBuilder sb, Exception ex, int depth)
    {
        Serial.WriteString("Extracting exception data\n");
        if (depth > 8)
        {
            sb.Append("\n[Truncated exception chain]");
            return;
        }

        Serial.WriteString("Extracting type\n");

        var type = ex.GetType();

        Serial.WriteString("Appending type name\n");
        sb.Append(type.FullName);
        sb.Append(": ");
        sb.Append(ex.Message);

        Serial.WriteString("Reading stack trace\n");

        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            sb.Append('\n');
            sb.Append(ex.StackTrace);
        }

        if (ex.InnerException != null)
        {
            sb.Append("\n---> ");
            AppendException(sb, ex.InnerException, depth + 1);
        }
    }
}