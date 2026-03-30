// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;
using Cosmos.Kernel.Core.IO;
using Yukihana.Core.Generated;
using Yukihana.Core.IO;

namespace Yukihana.Core.Debug;

public static class KernelPanic
{
    private const int MAX_ENTRIES = 20;

    public static void Panic(
        string panicReason,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        ShellPrint.KernelPrintEnabled = true;
        ShellPrint.DoLogKernelPrint = false;
        Logger.Enabled = false;

        ShellPrint.PrintK("=== KERNEL PANIC ===", "panic");
        ShellPrint.PrintK($"Kernel: {VersionInfo.KERNEL_NAME} v{VersionInfo.KERNEL_VERSION}", "panic");
        ShellPrint.PrintK($"OS: {VersionInfo.OS_NAME} v{VersionInfo.OS_VERSION} ({VersionInfo.OS_REVISION})", "panic");
        ShellPrint.PrintK($"Reason: {panicReason}", "panic");
        ShellPrint.PrintK($"Location: {callerMemberName} in {Path.GetFileName(callerFilePath)}:{callerLineNumber}", "panic");
        ShellPrint.PrintK("====================", "panic");

        //LogPrinter.PrintLogs(MAX_ENTRIES);

        var logs = Logger.Logs;
        if (logs is not null && logs.Count > 0)
        {
            int total = logs.Count; 
            int start = total > MAX_ENTRIES ? total - MAX_ENTRIES : 0;

            if (start > 0)
                ShellPrint.InfoK($"Showing last {MAX_ENTRIES} of {total} log entries (older logs trimmed)", "panic");

            for(int i = start; i < total; i++)
                ShellPrint.InfoK(logs[i].ToString(), "panic");
        }
        else
        {
            ShellPrint.InfoK("No logs available", "panic");
        }

        MirrorPanicToSerial(panicReason, callerMemberName, callerFilePath, callerLineNumber);

        ShellPrint.InfoK("System halted due to kernel panic.", "panic");

        Kernel.SpeedrunShutdown = true;
        
        Kernel.Instance.Stop();
    }

    private static void MirrorPanicToSerial(
        string panicReason, 
        string callerMemberName, 
        string callerFilePath, 
        int callerLineNumber)
    {
        Serial.WriteString("\n\n=== KERNEL PANIC ===\n");
        Serial.WriteString($"Kernel: {VersionInfo.KERNEL_NAME} v{VersionInfo.KERNEL_VERSION}\n");
        Serial.WriteString($"OS: {VersionInfo.OS_NAME} v{VersionInfo.OS_VERSION} ({VersionInfo.OS_REVISION})\n");
        Serial.WriteString($"Reason: {panicReason}\n");
        Serial.WriteString($"Location: {callerMemberName} in {Path.GetFileName(callerFilePath)}:{callerLineNumber}\n");
        Serial.WriteString("====================\n");

        var logs = Logger.Logs;

        Serial.WriteString("Full log list:\n");

        if (logs is not null && logs.Count > 0)
        {
            for(int i = 0; i < logs.Count; i++)
                Serial.WriteString(logs[i].ToString() + "\n");
        }
        else
        {
            Serial.WriteString("No logs available\n");
        }

        Serial.WriteString("Kernel panid end.\n\n");
    }
}