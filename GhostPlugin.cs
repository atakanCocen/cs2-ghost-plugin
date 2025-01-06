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
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.NextFrame(() =>
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                if (IsValidGhost(player))
                {
                    SetPlayerMoney(player, 0);
                    RemoveWeaponsFromPlayer(player);
                    SetPlayerVelocityMultiplier(player, 1.4f);
                    SetWeaponVisible(player, false);
                }
                else if (IsValidHuman(player))
                {
                    SetPlayerMoney(player, 10000);
                    SetAllowWeaponPickup(player, true);
                    SetPlayerAlpha(player, 255);
                    SetWeaponVisible(player, true);
                }
            });
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerTakeDamage(EventPlayerHurt @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (!IsValidHuman(victim) || !IsValidGhost(attacker))
            return HookResult.Continue;

        if (@event.Weapon == "weapon_knife")
        {
            @event.DmgHealth = 100;
            @event.Health = 0;
            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    private static void SetPlayerVelocityMultiplier(CCSPlayerController player, float multiplier)
    {
        if (!IsValidGhost(player))
            return;

        if (player.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null)
            return;

        player.PlayerPawn.Value.VelocityModifier = multiplier;
        Utilities.SetStateChanged(player, "CCSPlayerPawn", "m_flVelocityModifier");
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

        SetAllowWeaponPickup(player, true);

        player.RemoveWeapons();

        player.GiveNamedItem(CsItem.DefaultKnifeT);

        // SetAllowWeaponPickup(player, false);
    }

    private static void SetAllowWeaponPickup(CCSPlayerController player, bool allow)
    {
        if (player.PlayerPawn.Value?.WeaponServices == null)
            return;

        player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = !allow;
    }

    private static void SetPlayerAlphaBasedOnSpeed(CCSPlayerPawn pawn)
    {
        const int MAX_ALPHA = 85;
        int alpha = Math.Clamp((int)pawn.AbsVelocity.Length2D() - 5, 0, MAX_ALPHA);

        pawn.ShadowStrength = alpha / MAX_ALPHA;

        SetEntityAlpha(pawn, alpha);
    }

    private static void SetPlayerAlpha(CCSPlayerController player, int alpha)
    {
        if (player == null || !player.IsValid)
            return;

        if (player.PlayerPawn == null || !player.PlayerPawn.IsValid)
            return;

        SetEntityAlpha(player.PlayerPawn.Value!, alpha);
    }

    private static void SetWeaponVisible(CCSPlayerController player, bool visible)
    {
        if (player == null || !player.IsValid)
            return;

        if (player.PlayerPawn.Value?.WeaponServices == null)
            return;

        var weapon = player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;

        if (weapon == null || !weapon.IsValid)
            return;

        SetEntityAlpha(weapon, visible ? 255 : 0);
    }

    private static void SetEntityAlpha(CBaseModelEntity entity, int alpha)
    {
        if (entity == null || !entity.IsValid)
            return;

        entity.Render = Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
    }

    private static void SetPlayerMoney(CCSPlayerController player, int money)
    {
        var moneyServices = player.InGameMoneyServices;
        if (moneyServices == null) return;

        moneyServices.Account = money;

        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
    }
}
