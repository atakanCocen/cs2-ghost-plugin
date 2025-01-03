using CounterStrikeSharp.API;

namespace GhostPlugin;

public static class MessageUtil
{
    public static readonly string LogPrefix = $"[GhostPlugin {GhostPlugin.Version}]";


    public static void WriteLine(String message)
    {
        Console.WriteLine($"{LogPrefix} {message}");
    }

    public static void PrintToChatAll(String message)
    {
        Server.PrintToChatAll($"{LogPrefix} {message}");
    }
}
