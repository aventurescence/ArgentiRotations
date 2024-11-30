using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.ComponentModel;

namespace DefaultRotations.Ranged;
/// <summary>
/// The level of the LB.
/// </summary>

[Rotation("Lelia's PvP", CombatType.PvP, GameVersion = "6.58")]
[SourceCode(Path = "main/DefaultRotations/Ranged/MCH_Default.PvP.cs")]
[Api(4)]


public sealed class MCH_LeliaDefaultPvP : MachinistRotation
{

    public static IBaseAction MarksmansSpitePvP => new BaseAction((ActionID)29415);
    //public static IBaseAction BishopAutoturretPvP => new BaseAction((ActionID)29412);
    
    [RotationConfig(CombatType.PvP, Name = "LBを使用します。\nUse Limit Break (Note: RSR cannot predict the future, and this has a cast time.")]
    public bool LBInPvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "LB:魔弾の射手を行うために必要なターゲットのHP%%は？\nThe target HP%% required to perform LB:Marksman's Spite is")]
    public int MSValue { get; set; } = 45;

    [RotationConfig(CombatType.PvP, Name = "スプリントを使います。\nSprint")]
    public bool UseSprintPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "快気を使います。\nRecuperate")]
    public bool UseRecuperatePvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "快気を使うプレイヤーのHP%%は？\nRecuperateHP%%?")]
    public int RCValue { get; set; } = 75;

    [RotationConfig(CombatType.PvP, Name = "浄化を使います。\nUse Purify")]
    public bool UsePurifyPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "スタン\nUse Purify on Stun")]
    public bool Use1343PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "氷結\nUse Purify on DeepFreeze")]
    public bool Use3219PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "徐々に睡眠\nUse Purify on HalfAsleep")]
    public bool Use3022PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "睡眠\nUse Purify on Sleep")]
    public bool Use1348PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "バインド\nUse Purify on Bind")]
    public bool Use1345PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "ヘヴィ\nUse Purify on Heavy")]
    public bool Use1344PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "沈黙\nUse Purify on Silence")]
    public bool Use1347PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "自分が防御中は攻撃を中止します。\nStop attacking while in Guard.")]
    public bool GuardCancel { get; set; } = false;


    private bool TryPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP) return false;

        var purifyStatuses = new Dictionary<int, bool>
        {
            { 1343, Use1343PvP },
            { 3219, Use3219PvP },
            { 3022, Use3022PvP },
            { 1348, Use1348PvP },
            { 1345, Use1345PvP },
            { 1344, Use1344PvP },
            { 1347, Use1347PvP }
        };

        foreach (var status in purifyStatuses)
        {
            if (status.Value && Player.HasStatus(true, (StatusID)status.Key))
            {
                return PurifyPvP.CanUse(out action);
            }
        }

        return false;
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;

        //if ((((sbyte)LimitBreakLevel>=1) && SprintPvP.CanUse(out act))) return true;
        if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && (LimitBreakLevel == 1) &&
            LBInPvP && HostileTarget?.GetHealthRatio() * 100 <= MSValue &&
            MarksmansSpitePvP.CanUse(out act)) return true;
        //if(LBInPvP && (LimitBreakLevel >= 1))
        //{
        //    if ((HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && 
        //        (HostileTarget?.GetHealthRatio() * 100 < MSValue) &&
        //        (MarksmansSpitePvP.CanUse(out act))) return true;
        //}

        if (Player.HasStatus(true, StatusID.Overheated_3149) && (!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false))
        {
            if (HeatBlastPvP.CanUse(out act, skipComboCheck: true)) return true;
        }
        else if ((Player.HasStatus(true, StatusID.BioblasterPrimed) && HostileTarget?.DistanceToPlayer() <= 12 && BioblasterPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) ||
                (Player.HasStatus(true, StatusID.AirAnchorPrimed) && AirAnchorPvP.CanUse(out act, usedUp: true)) ||
                (Player.HasStatus(true, StatusID.ChainSawPrimed) && ChainSawPvP.CanUse(out act, usedUp: true, skipAoeCheck: true))) return true;
        else
        { 
            if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false))
            {
                if (Player.HasStatus(true, StatusID.Overheated_3149)) return false;

                if (HostileTarget?.DistanceToPlayer() <= 12 && ScattergunPvP.CanUse(out act, skipAoeCheck: true, skipComboCheck: true)) return true;
            }
        }


		if (!Player.HasStatus(true, StatusID.Overheated_3149))
		{
        	if (Player.HasStatus(true, StatusID.DrillPrimed) && DrillPvP.CanUse(out act, usedUp: true)) return true;
        	if (BlastChargePvP.CanUse(out act, skipCastingCheck: true)) return true;
		}

        if (!Player.HasStatus(true, StatusID.Guard) && UseSprintPvP && !Player.HasStatus(true, StatusID.Sprint) && SprintPvP.CanUse(out act, skipComboCheck: true)) return true;

        return base.GeneralGCD(out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (UseRecuperatePvP && Player.GetHealthRatio() * 100 < RCValue && RecuperatePvP.CanUse(out act)) return true;

        if (TryPurify(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;

        if (BishopAutoturretPvP.CanUse(out act, skipAoeCheck: true)) return true;

        // Use WildfirePvP if Overheated
        //if (Player.HasStatus(true, StatusID.Overheated_3149) && WildfirePvP.CanUse(out act, skipAoeCheck: true, skipComboCheck: true, skipClippingCheck: true)) return true;
        if (Player.HasStatus(true, StatusID.Overheated_3149) && WildfirePvP.CanUse(out act, skipAoeCheck: true, skipComboCheck: true)) return true;

        // Check if BioblasterPvP, AirAnchorPvP, or ChainSawPvP can be used
        if (InCombat && !Player.HasStatus(true, StatusID.Analysis) && !Player.HasStatus(true, StatusID.Overheated_3149) &&
            (BioblasterPvP.Cooldown.CurrentCharges>0 || AirAnchorPvP.Cooldown.CurrentCharges > 0 || ChainSawPvP.Cooldown.CurrentCharges > 0) &&
            AnalysisPvP.CanUse(out act, usedUp: true)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
}