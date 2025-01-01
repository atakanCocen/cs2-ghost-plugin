using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;

namespace GhostPlugin;

public class GhostPlugin : BasePlugin
{
    public static readonly string Version = "0.0.1";

    public override string ModuleName => "GhostPlugin";

    public override string ModuleVersion => Version;

    public static readonly string LogPrefix = $"[GhostPlugin] {Version}";

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"{ModuleName} {ModuleVersion} loaded!");

        if (hotReload)
        {
            Server.PrintToChatAll($"{LogPrefix}Update detected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }

        AddTimer(0.1f, () => Utilities.GetPlayers().ForEach(SetPlayerAlphaBasedOnSpeed), CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    // private void RemoveGhostWeapons(CBasePlayerController player)
    // {
    //     if (player == null || !player.IsValid)
    //         return;

    //     var pawn = player.Pawn.Get();

    //     if (pawn == null || !pawn.IsValid)
    //         return;
    // }

    private void SetPlayerAlphaBasedOnSpeed(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.Pawn.Get();

        if (pawn == null || !pawn.IsValid)
            return;

        int alpha = 255;

        if (player.Team == CsTeam.CounterTerrorist)
        {
            alpha = Math.Min((int)pawn.AbsVelocity.Length(), 255);
        }

        Console.WriteLine($"Setting player {player.UserId} alpha to {alpha}");

        pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
