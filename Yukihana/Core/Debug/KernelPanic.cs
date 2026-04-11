// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cosmos.Kernel.Core.IO;

namespace Yukihana.Core.Debug;

public static class KernelPanic
{
    [DoesNotReturn]
    public static void Panic(
        string reason,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        ConsoleRenderer.Enabled = true;

        Print("*** KERNEL PANIC ***", ConsoleColor.Red);
        Print($"Reason: {reason}", ConsoleColor.White);
        Print($"At: {m} ({Path.GetFileName(f)}:{l})", ConsoleColor.Gray);

        Print("\n--- Last log entries ---", ConsoleColor.DarkGray);

        var logs = KernelLog.Entries;
        int start = Math.Max(0, logs.Count - 20);

        for (int i = start; i < logs.Count; i++)
        {
            var e = logs[i];
            Console.WriteLine($"[{e.Time:0.000}] {e.Source}: {e.Message}");
        }

        MirrorToSerial(reason, m, f, l);

        Print("\nSystem halted.", ConsoleColor.Red);

        for (;;);
    }

    private static void Print(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void MirrorToSerial(string reason, string m, string f, int l)
    {
        Serial.WriteString("\n*** KERNEL PANIC ***\n");
        Serial.WriteString($"Reason: {reason}\n");
        Serial.WriteString($"At: {m} ({Path.GetFileName(f)}:{l})\n");

        foreach (var e in KernelLog.Entries)
        {
            Serial.WriteString($"[{e.Time:0.000}] {e.Source}: {e.Message}\n");
        }
    }
}