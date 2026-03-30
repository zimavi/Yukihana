// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.IO;

namespace Yukihana.Core.Debug;

public static class LogPrinter
{
    public static void PrintLogs(int maxEntries)
    {
        var logs = Logger.Logs;

        if (logs is null || logs.Count == 0)
        {
            ShellPrint.InfoK("Logs: <empty>");
            return;
        }

        int total = logs.Count;
        int start = total > maxEntries ? total - maxEntries : 0;

        if (start > 0)
            ShellPrint.InfoK($"Logs: showing last {maxEntries} of {total} (trimmed {start})");
        else
            ShellPrint.InfoK($"Logs: showing all {total}");

        for(int i = start; i < total; i++)
            ShellPrint.InfoK($"Logs: {logs[i]}");
    }
}