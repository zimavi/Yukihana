// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using Cosmos.Kernel.Core.IO;
using Cosmos.Kernel.HAL;
using Yukihana.Core;

namespace Yukihana.Debug;

public static class KernelPanic
{
    private static int s_panicEntered;
    private static readonly Lock s_lock = new();

    private const int MAX_LOGS_DISPLAY = 30;
    private const bool IS_DEBUG = false;

    public static void Panic(
        string reason,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0)
    {
        if (Interlocked.Exchange(ref s_panicEntered, 1) != 0)
        {
            MinimalFailSafePanic("Recursive panic detected.");
        }

        PlatformHAL.CpuOps?.DisableInterrupts();

        LogDispatcher.RingBufferEnabled = false;

        lock (s_lock)
        {
            ThreadLockedPanic(reason, m, f, l);
        }

        PlatformHAL.CpuOps?.Halt();

        for (; ; )
        {
            ;
        }
    }

    private static void ThreadLockedPanic(string reason, string m, string f, int l)
    {
        Print("*** KERNEL PANIC ***", ConsoleColor.Red);
        Print($"Kernel: {KernelInfoString(VersionInfo.Kernel)}", ConsoleColor.Gray);
        Print($"Framework: {FrameworkInfoString(VersionInfo.Framework)}", ConsoleColor.Gray);
        Print($"Reason: {reason}", ConsoleColor.White);

#pragma warning disable CS0162
        if (IS_DEBUG)
        {
            Print($"At: {m} ({Path.GetFileName(f)}:{l})", ConsoleColor.Gray);
        }
#pragma warning restore

        Print("\n--- Last log entries ---", ConsoleColor.DarkGray);

        Span<LogEvent> logs = new LogEvent[LogDispatcher.RingBufferCount];
        LogDispatcher.Snapshot(logs);

        int start = Math.Max(0, logs.Length - MAX_LOGS_DISPLAY);

        Print($"Older logs are chopped. {logs.Length} > {MAX_LOGS_DISPLAY}", ConsoleColor.DarkGray);

        for (int i = start; i < logs.Length; i++)
        {
            LogEvent e = logs[i];
            TimeSpan delta = e.Timestamp - Kernel.BootTime;
            Console.WriteLine($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}");
        }

        MirrorToSerial(reason, m, f, l, logs);

        Print("\nSystem halted.", ConsoleColor.Red);
    }

    private static void MinimalFailSafePanic(string reason)
    {
        PlatformHAL.CpuOps?.DisableInterrupts();

        Serial.WriteString("\n*** FATAL RECURSIVE KERNEL PANIC ***\n");
        Serial.WriteString(reason);
        Serial.WriteString("\n");

        while (true)
        {
            PlatformHAL.CpuOps?.Halt();
        }
    }

    private static void Print(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void MirrorToSerial(string reason, string m, string f, int l, ReadOnlySpan<LogEvent> logs)
    {
        Serial.WriteString("\n*** KERNEL PANIC ***\n");
        Serial.WriteString($"Kernel: {KernelInfoString(VersionInfo.Kernel)}\n");
        Serial.WriteString($"Framework: {FrameworkInfoString(VersionInfo.Framework)}\n");
        Serial.WriteString($"Reason: {reason}\n");
        Serial.WriteString($"At: {m} ({Path.GetFileName(f)}:{l})\n");

        foreach (LogEvent e in logs)
        {
            TimeSpan delta = e.Timestamp - Kernel.BootTime;
            Serial.WriteString($"[{delta.TotalSeconds,10:0.000000}] {e.Source}: {e.Message}\n");
        }
    }

    // For some reason, using class' ToString causes kernel to freeze
    private static string KernelInfoString(KernelInfo info)
    {
        return info.Name + " " + info.Version.ToString() + " (rev " + info.RevisionString + ")";
    }

    private static string FrameworkInfoString(FrameworkInfo info)
    {
        return info.Name + " " + info.Version.ToString();
    }
}
