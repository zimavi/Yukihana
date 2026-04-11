// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public sealed class Unit : IDisposable
{
    private readonly string _action;
    private readonly string _target;
    private readonly int _indent;

    private bool _done;

    internal Unit(string action, string target, int indent)
    {
        _action = action;
        _target = target;
        _indent = indent;

        PrintStart();
    }

    public void Ok()
    {
        PrintResult("OK", $"{Past(_action)} {_target}", ConsoleColor.Green);
        _done = true;
    }

    public void Fail()
    {
        PrintResult("FAILED", $"Failed to {Present(_action)} {_target}", ConsoleColor.Red);
        _done = true;
    }

    private void PrintStart()
    {
        Console.Write(new string(' ', _indent));
        Console.WriteLine($"{Present(_action)} {_target}");
    }

    private void PrintResult(string status, string text, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" [");
        Console.ForegroundColor = color;
        Console.Write($"{status.PadLeft(3 + status.Length / 2),-6}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("] ");
        Console.ResetColor();

        Console.WriteLine(text);
    }

    private static string Present(string verb) => verb + "ing";
    private static string Past(string verb) => verb + "ed";

    public void Dispose()
    {}
}