// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public static class UnitManager
{
    private static int _indent = 10;

    public static Unit Start(string action, string target) =>
        new (action, target, _indent);

    public static void Target(string name)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" [");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  OK  ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("] ");
        Console.ResetColor();

        Console.WriteLine($"Reached target {name}");
    }
}