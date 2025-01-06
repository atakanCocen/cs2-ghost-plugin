using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
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

        RegisterListener<Listeners.OnTick>(OnTick);
    }

    [GameEventHandler]
    private void OnTick()
    {
        UpdateAllGhostAlphas();
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawned(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        Server.NextFrame(() =>
            {
                AddTimer(0.2f, () =>
                {
                    if (IsValidGhost(player))
                    {
                        RemoveWeaponsFromPlayer(player);
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        );


        return HookResult.Continue;
    }

    private static bool IsValidGhost(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.Team == CsTeam.Terrorist;
    }

    private static void UpdateAllGhostAlphas()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            if (player == null || !player.IsValid || player.Team != CsTeam.Terrorist)
                return;

            if (player.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive)
                return;

            SetPlayerAlphaBasedOnSpeed(player.PlayerPawn.Value!);
        });
    }

    public static void RemoveWeaponsFromPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return;
        if (player.PlayerPawn == null || !player.PlayerPawn.IsValid) return;
        if (!player.PawnIsAlive) return;

        MessageUtil.WriteLine($"Removing {player.PlayerName}'s weapons.");

        player.RemoveWeapons();
        player.GiveNamedItem(CsItem.DefaultKnifeT);
    }

    private static void SetPlayerAlphaBasedOnSpeed(CCSPlayerPawn pawn)
    {
        int alpha = Math.Clamp((int)pawn.AbsVelocity.Length2D() - 5, 0, 255);

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
