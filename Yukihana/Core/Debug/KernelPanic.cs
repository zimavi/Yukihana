// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cosmos.Kernel.Core.IO;
using Cosmos.Kernel.HAL;

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
            var delta = e.Time - Kernel.BootTime;
            Console.WriteLine($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}");
        }

        MirrorToSerial(reason, m, f, l);

        Print("\nSystem halted.", ConsoleColor.Red);


        if (PlatformHAL.CpuOps != null)
        {
            PlatformHAL.CpuOps.DisableInterrupts();
            PlatformHAL.CpuOps.Halt();
        }

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
            var delta = e.Time - Kernel.BootTime;
            Serial.WriteString($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}\n");
        }
    }
}