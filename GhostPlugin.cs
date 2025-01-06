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
        // TODO prevent ghosts from picking up weapons and buying (maybe set money to 0?)
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

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.NextFrame(() =>
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                if (IsValidGhost(player))
                {
                    SetPlayerMoney(player, 0);
                }
                else if (IsValidHuman(player))
                {
                    SetPlayerMoney(player, 10000);
                }
            });
        });

        return HookResult.Continue;
    }

    private static bool IsValidGhost(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.Team == CsTeam.Terrorist;
    }

    private static bool IsValidHuman(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.Team == CsTeam.CounterTerrorist;
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

        if (player.PlayerPawn.Value?.WeaponServices != null)
        {
            player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
        }
    }

    private static void SetPlayerAlphaBasedOnSpeed(CCSPlayerPawn pawn)
    {
        int alpha = Math.Clamp((int)pawn.AbsVelocity.Length2D() - 5, 0, 150);

        SetEntityAlpha(pawn, alpha);

        var weapon = pawn.WeaponServices?.ActiveWeapon?.Value;

        if (weapon != null && weapon.IsValid)
        {
            SetEntityAlpha(weapon, alpha);
        }
    }

    private static void SetEntityAlpha(CBaseModelEntity entity, int alpha)
    {
        if (entity == null || !entity.IsValid)
            return;

        entity.Render = Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
    }

    private static void SetPlayerMoney(CCSPlayerController controller, int money)
    {
        var moneyServices = controller.InGameMoneyServices;
        if (moneyServices == null) return;

        moneyServices.Account = money;

        Utilities.SetStateChanged(controller, "CCSPlayerController", "m_pInGameMoneyServices");
    }
}
