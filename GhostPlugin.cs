using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
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
            MessageUtil.PrintToChatAll("Update deeetected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }

        RegisterListener<Listeners.OnTick>(OnTick);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamange, HookMode.Pre);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
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
                    // RemoveWeaponsFromPlayer(player);
                    DropDisallowedWeapons(player);
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

    private HookResult OnTakeDamange(DynamicHook hook)
    {
        var entity = hook.GetParam<CEntityInstance>(0);
        if (!entity.IsValid) { return HookResult.Continue; }
        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

        if (entity.DesignerName != "player") { return HookResult.Continue; }
        var victimPawn = entity.As<CCSPlayerPawn>();
        if (victimPawn == null || victimPawn is { IsValid: false }) { return HookResult.Continue; }

        var victim = victimPawn.OriginalController.Get();
        if (victim is null || victim is { IsValid: false }) return HookResult.Continue;

        var attackerHandle = damageInfo.Attacker;
        if (attackerHandle == null || attackerHandle is { IsValid: false }) return HookResult.Continue;

        if (attackerHandle.Value!.DesignerName != "player") return HookResult.Continue;

        var attackerPawn = attackerHandle.Value!.As<CCSPlayerPawn>();
        if (attackerPawn == null || attackerPawn is { IsValid: false }) return HookResult.Continue;

        var attacker = attackerPawn.OriginalController.Get();
        if (attacker is null || attacker is { IsValid: false }) return HookResult.Continue;

        var attackerWeaponName = attackerPawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName;

        if (damageInfo.Inflictor != null && damageInfo.Inflictor.IsValid)
        {
            MessageUtil.WriteLine($"Inflictor: {damageInfo.Inflictor.Value!.DesignerName}");
        }

        if (IsValidGhost(attacker) && IsValidHuman(victim))
        {
            if (attackerWeaponName == "weapon_knife")
            {
                damageInfo.Damage = 150;
                damageInfo.OriginalDamage = 150;
                return HookResult.Continue;
            }
            else
            {
                damageInfo.Damage = 0;
                damageInfo.OriginalDamage = 0;
                return HookResult.Continue;
            }
            
        }

        if (IsValidGhost(victim) && IsValidHuman(attacker))
        {
            PreventSlowdownOnPlayerHurt(victim);
        }

        return HookResult.Continue;
    }

    private static void SetPlayerVelocityMultiplier(CCSPlayerController player, float multiplier)
    {
        if (!IsValidGhost(player))
            return;

        if (player?.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null)
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
            if (!player.IsValid || player.Team != CsTeam.Terrorist)
                return;

            if (player?.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive)
                return;

            SetPlayerAlphaBasedOnSpeed(player.PlayerPawn.Value!);
        });
    }
    
    public static void RemoveWeaponsFromPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return;
        if (player?.PlayerPawn == null || !player.PlayerPawn.IsValid) return;
        if (!player.PawnIsAlive) return;

        MessageUtil.WriteLine($"Removing {player.PlayerName}'s weapons.");

        player.RemoveWeapons();

        player.GiveNamedItem(CsItem.Knife);
        player.GiveNamedItem(CsItem.DefaultKnifeT);
        
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
        
        SetEntityAlpha(pawn, alpha);
    }

    private static void SetPlayerAlpha(CCSPlayerController? player, int alpha)
    {
        if (player == null || !player.IsValid)
            return;

        if (player.PlayerPawn == null || !player.PlayerPawn.IsValid)
            return;
        
        SetEntityAlpha(player.PlayerPawn.Value!, alpha);
    }

    private static void SetWeaponVisible(CCSPlayerController? player, bool visible)
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

    private static void SetEntityAlpha(CBaseModelEntity? entity, int alpha)
    {
        if (entity == null || !entity.IsValid)
            return;

        entity.ShadowStrength = alpha == 255 ? 1 : 0;
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
    
    private static void OnPlayerGivenC4(CCSPlayerController? player, CsItem c4)
    {
        // Handle the event when a player is given the C4
        if (player == null || !player.IsValid)
            return;
        
        player.GiveNamedItem(c4);
        Console.WriteLine($"Player {player.PlayerName} has received the C4 bomb.");
    }
    
    private static void PreventSlowdownOnPlayerHurt(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return;
        
        if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid || !player.PlayerPawn.Value.IsValid)
            return;
        
        Vector playerSpeed = player!.PlayerPawn.Value!.AbsVelocity;

        player.PrintToChat($"OnTakeDamagePost VelocityModifier = {player.PlayerPawn.Value.VelocityModifier}");
        SetPlayerVelocityMultiplier(player, 1.4f);

        Server.NextFrame(() =>
        {
            SetPlayerVelocityMultiplier(player, 1.4f);
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.IsValid || !player.PlayerPawn.Value.IsValid)
                return;
            player!.PlayerPawn.Value!.AbsVelocity.X = playerSpeed.X;
            player!.PlayerPawn.Value!.AbsVelocity.Y = playerSpeed.Y;
            player!.PlayerPawn.Value!.AbsVelocity.Z = playerSpeed.Z;
        });
    }
    
    public static HookResult OnWeaponCanAcquire(DynamicHook hook)
    {

        if (hook.GetParam<CCSPlayer_ItemServices>(0).Pawn.Value?.Controller.Value?.As<CCSPlayerController>() is not CCSPlayerController player)
        {
            return HookResult.Continue;
        }

        if (!IsValidGhost(player))
        {
            return HookResult.Continue;
        }

        CCSWeaponBaseVData vdata = VirtualFunctions.GetCSWeaponDataFromKeyFunc
                                       .Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString())
                                   ?? throw new Exception("Failed to retrieve CCSWeaponBaseVData from ItemDefinitionIndex.");
        
        if (vdata.Name.Contains("knife") || vdata.Name == "weapon_c4")
        {
            return HookResult.Continue;
        }
        
        hook.SetReturn(AcquireResult.NotAllowedByProhibition);
        return HookResult.Stop;
    }

    public static void DropDisallowedWeapons(CCSPlayerController client)
    {
        if (client == null)
            return;

        List<string> allowedWeapons = new List<string>();
        allowedWeapons.Add("weapon_c4");
        allowedWeapons.Add("weapon_knife_t");

        foreach (var weapon in
                 client.PlayerPawn.Value?.WeaponServices?.MyWeapons.Where(
                     x => !allowedWeapons.Contains(x.Value?.DesignerName)))
        {
            if (weapon != null && weapon.IsValid)
            {
                client.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Raw = weapon.Raw;
                client.DropActiveWeapon();
            }
        }
        
    }
    

}
