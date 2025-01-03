using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace GhostPlugin;

public class GhostPlugin : BasePlugin
{
    public static readonly string Version = "0.0.1";

    public override string ModuleName => "GhostPlugin";

    public override string ModuleVersion => Version;

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"{ModuleName} {ModuleVersion} loaded!");

        if (hotReload)
        {
            MessageUtil.PrintToChatAll("Update detected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }

        AddTimer(0.1f, () => Utilities.GetPlayers().ForEach(SetPlayerAlphaBasedOnSpeed), CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        if (player.Team == CsTeam.Terrorist)
        {
            AddTimer(1.0f, () => RemoveGhostWeapons(player));
        }

        return HookResult.Continue;
    }

    private static void RemoveGhostWeapons(CCSPlayerController player)
    {
        MessageUtil.WriteLine($"Removing {player.PlayerName}'s weapons.");

        if (player == null || !player.IsValid)
            return;

        player.RemoveWeapons();
        player.GiveNamedItem(CsItem.DefaultKnifeT);
    }

    private static void SetPlayerAlphaBasedOnSpeed(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.Pawn.Get();

        if (pawn == null || !pawn.IsValid)
            return;

        int alpha = 255;

        if (player.Team == CsTeam.Terrorist)
        {
            MessageUtil.WriteLine($"${player.PlayerName}'s speed: {pawn.Speed} - velocity: {pawn.AbsVelocity.Length2D()}");
            alpha = Math.Clamp((int)pawn.AbsVelocity.Length2D(), 0, 255);
        }

        MessageUtil.WriteLine($"Setting {player.PlayerName}'s alpha to {alpha}");

        SetEntityAlpha(pawn, alpha);
    }

    private static void SetEntityAlpha(CBaseModelEntity entity, int alpha)
    {
        if (entity == null || !entity.IsValid)
            return;

        entity.Render = Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
    }
}
