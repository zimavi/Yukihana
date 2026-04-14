// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Cosmos.Kernel.Core.IO;
using Cosmos.Kernel.HAL;
using Yukihana.Core.Generated;

namespace Yukihana.Core.Debug;

public static class KernelPanic
{
    private static int _panicEntered;

    public static void Panic(
        string reason,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        if (Interlocked.Exchange(ref _panicEntered, 1) != 0)
            MinimalFailSafePanic("Recursive panic detected.");

        PlatformHAL.CpuOps?.DisableInterrupts();

        ConsoleRenderer.Enabled = true;

        Print("*** KERNEL PANIC ***", ConsoleColor.Red);
        Print($"OS: {VersionInfo.OS_NAME} v{VersionInfo.OS_VERSION} ({VersionInfo.OS_REVISION})", ConsoleColor.Gray);
        Print($"Kernel: {VersionInfo.KERNEL_NAME} v{VersionInfo.KERNEL_VERSION}", ConsoleColor.Gray);
        Print($"Reason: {reason}", ConsoleColor.White);
        Print($"At: {m} ({Path.GetFileName(f)}:{l})", ConsoleColor.Gray);

        Print("\n--- Last log entries ---", ConsoleColor.DarkGray);

        var logs = SnapshotLogs();
        int start = Math.Max(0, logs.Length - 20);

        for (int i = start; i < logs.Length; i++)
        {
            var e = logs[i];
            var delta = e.Time - Kernel.BootTime;
            Console.WriteLine($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}");
        }

        MirrorToSerial(reason, m, f, l, logs);

        Print("\nSystem halted.", ConsoleColor.Red);

        PlatformHAL.CpuOps?.Halt();

        for (;;);
    }

    private static void MinimalFailSafePanic(string reason)
    {
        PlatformHAL.CpuOps?.DisableInterrupts();

        Serial.WriteString("\n*** FATAL RECURSIVE KERNEL PANIC ***\n");
        Serial.WriteString(reason);
        Serial.WriteString("\n");

        while (true)
            PlatformHAL.CpuOps?.Halt();
    }

    private static void Print(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void MirrorToSerial(string reason, string m, string f, int l, LogEntry[] logs)
    {
        Serial.WriteString("\n*** KERNEL PANIC ***\n");
        Serial.WriteString($"OS: {VersionInfo.OS_NAME} {VersionInfo.OS_VERSION} ({VersionInfo.OS_REVISION})\n");
        Serial.WriteString($"Kernel: {VersionInfo.KERNEL_NAME} {VersionInfo.KERNEL_VERSION}\n");
        Serial.WriteString($"Reason: {reason}\n");
        Serial.WriteString($"At: {m} ({Path.GetFileName(f)}:{l})\n");

        foreach (var e in logs)
        {
            var delta = e.Time - Kernel.BootTime;
            Serial.WriteString($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}\n");
        }
    }

    private static LogEntry[] SnapshotLogs() => [..KernelLog.Entries];
}