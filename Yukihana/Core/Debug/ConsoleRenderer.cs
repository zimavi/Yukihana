// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public static class ConsoleRenderer
{
    public static bool Enabled { get; set; } = true;

    public static void Render(LogEntry entry)
    {
        if (!Enabled) return;

        if (BootEnvironment.Stage == BootStage.CoreInit)
            RenderSystemd(entry);
        else
            RenderKernel(entry);
    }

    private static void RenderKernel(LogEntry e)
    {
        TimeSpan delta = e.Time - Kernel.BootTime;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{delta.TotalSeconds,10:0.000000}] ");

        if (!string.IsNullOrWhiteSpace(e.Source))
        {
            Console.ForegroundColor = GetColor(e.Level);
            Console.Write($"{e.Source}: ");
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(e.Message);

        Console.ResetColor();
    }

    private static void RenderSystemd(LogEntry e)
    {
        Console.ForegroundColor = ConsoleColor.White;
        if (string.IsNullOrWhiteSpace(e.Source))
            Console.WriteLine($"          {e.Message}");
        else
            Console.WriteLine($"          {e.Source}: {e.Message}");
        Console.ResetColor();
    }

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Warn => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };
}
