using System.ComponentModel;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ArgentiRotations.Tank;

[Rotation("ChurinDRK", CombatType.PvE, GameVersion = "7.3", Description = "Find it in your heart. You'll need to break past the ribs and then scoop it out, but it's in there, and you need to find it. Quickly.")]
[SourceCode(Path = "main/ArgentiRotations/Tank/ChurinDRK.cs")]
[Api(6)]
public sealed partial class ChurinDRK : DarkKnightRotation
{
    #region Properties
    private static bool HasBuffs => Common.PartyComposition.HasBuffs;
    private static bool HasDisesteem => Player.HasStatus(true, StatusID.Scorn);
    private bool InBurstWindow => DeliriumPvE.Cooldown.IsCoolingDown && !DeliriumPvE.Cooldown.ElapsedAfter(20) && (LivingShadowPvE.EnoughLevel && ShadowTime is > 0 and < 15 || !LivingShadowPvE.EnoughLevel) || HasBuffs;
    private static bool InOddWindow(IBaseAction action) => action.Cooldown.IsCoolingDown && action.Cooldown.ElapsedAfter(30) && !action.Cooldown.ElapsedAfter(90);
    private static bool CanFitSksGCD(float duration, int extraGCDs = 0) => WeaponRemain + ActionManager.GetAdjustedRecastTime(ActionType.Action, 3617U) * extraGCDs < duration;
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated) && !Player.WillStatusEnd(0, true, StatusID.Medicated);

    #region Enums
    private enum MpStrategy
    {
        [Description("Optimal")] Optimal,
        [Description("Auto at 3000+ MP")] Auto3K,
        [Description("Auto at 6000+ MP")] Auto6K,
        [Description("Auto at 9000+ MP")] Auto9K,
        [Description("Auto when about to cap")] AutoRefresh,
        [Description("Force Edge of Darkness")] ForceEdge,
        [Description("Force Flood of Darkness")] ForceFlood
    }

    private enum BloodStrategy
    {
        [Description("Automatic")] Automatic,
        [Description("Use ASAP")] Asap,
        [Description("Conserve for burst")] Conserve,
        [Description("Only Bloodspiller")] OnlyBloodspiller,
        [Description("Only Quietus")] OnlyQuietus
    }

    #endregion


    #region Potions
    private enum PotionTimings
    {
        [Description("None")] None,
        [Description("Opener and Six Minutes")] ZeroSix,
        [Description("Two Minutes and Eight Minutes")] TwoEight,
        [Description("Opener, Five Minutes and Ten Minutes")] ZeroFiveTen,
        [Description("Custom - set values below")] Custom
    }
    private readonly List<(int Time, bool Enabled, bool Used)> _potions = [];

    private void InitializePotions()
    {
        _potions.Clear();
        switch (PotionTiming)
        {
            case PotionTimings.ZeroSix:
                _potions.Add((0, true, false));
                _potions.Add((6, true, false));
                break;
            case PotionTimings.TwoEight:
                _potions.Add((2, true, false));
                _potions.Add((8, true, false));
                break;
            case PotionTimings.ZeroFiveTen:
                _potions.Add((0, true, false));
                _potions.Add((5, true, false));
                _potions.Add((10, true, false));
                break;
            case PotionTimings.Custom:
                if (CustomEnableFirstPotion) _potions.Add((CustomFirstPotionTime, true, false));
                if (CustomEnableSecondPotion) _potions.Add((CustomSecondPotionTime, true, false));
                if (CustomEnableThirdPotion) _potions.Add((CustomThirdPotionTime, true, false));
                break;
        }
    }



    #endregion
    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "MP Spending Strategy")]
    private MpStrategy MpSpendingStrategy { get; set; } = MpStrategy.Optimal;

    [RotationConfig(CombatType.PvE, Name = "Blood Gauge Strategy")]
    private BloodStrategy BloodSpendingStrategy { get; set; } = BloodStrategy.Automatic;

    [RotationConfig(CombatType.PvE, Name = "Use The Blackest Night on lowest HP party member during AOE scenarios")]
    private bool BlackLantern { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Blackest Night with above option")]
    private float BlackLanternRatio { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Potion Presets")]
    private PotionTimings PotionTiming { get; set; } = PotionTimings.None;

    [RotationConfig(CombatType.PvE, Name = "Enable First Potion for Custom Potion Timings?")]
    private bool CustomEnableFirstPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "First Potion Usage for custom timings - enter time in minutes")]
    private int CustomFirstPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Second Potion for Custom Potion Timings?")]
    private bool CustomEnableSecondPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Second Potion Usage for custom timings - enter time in minutes")]
    private int CustomSecondPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Third Potion for Custom Potion Timings?")]
    private bool CustomEnableThirdPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Third Potion Usage for custom timings - enter time in minutes")]
    private int CustomThirdPotionTime { get; set; } = 0;

    #endregion

    #region Countdown Logic
    // Countdown logic to prepare for combat.
    // Includes logic for using Provoke, tank stances, and burst medicines.
    protected override IAction? CountDownAction(float remainTime)
    {
        InitializePotions();
        UpdatePotions();
        if (remainTime <= 3 && HasTankStance && TheBlackestNightPvE.CanUse(out var act) ||
            remainTime <= 1 && UnmendPvE.CanUse(out act) || remainTime <= 1 && !HasWeaved() && TryUsePotion(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.ShadowstridePvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return ShadowstridePvE.CanUse(out act) || base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        switch (InBurstWindow)
        {
            case false when DarkMissionaryPvE.CanUse(out act):
            case false when ReprisalPvE.CanUse(out act, skipAoeCheck: true):
            case false when ShouldUseMp(MpSpendingStrategy) && BlackLantern && TheBlackestNightPvE.CanUse(out act) && TheBlackestNightPvE.Target.Target == LowestHealthPartyMember &&
                            TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio:
                return true;
            default:
                return base.DefenseAreaAbility(nextGCD, out act);
        }
    }

    [RotationDesc(ActionID.OblationPvE, ActionID.TheBlackestNightPvE, ActionID.DarkMindPvE, ActionID.ShadowWallPvE, ActionID.ShadowedVigilPvE, ActionID.RampartPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        //10
        if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false))
        {
            return true;
        }

        if (ShouldUseMp(MpSpendingStrategy) && TheBlackestNightPvE.CanUse(out act))
        {
            return true;
        }

        if (ShouldUseMp(MpSpendingStrategy) &&
            TheBlackestNightPvE.CanUse(out act) && TheBlackestNightPvE.Target.Target == Player)
        {
            return true;
        }
        //20
        if (DarkMindPvE.CanUse(out act))
        {
            return true;
        }

        //30
        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act))
        {
            return true;
        }

        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act))
        {
            return true;
        }

        //20
        if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        return ReprisalPvE.CanUse(out act) || base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return  TryUseEdgeOfShadow(out act) ||
                TryUseLivingShadow(out act) ||
                TryUseDelirium(out act) ||
                TryUseSaltedEarth(out act) ||
                TryUseShadowbringer(out act) ||
                TryUseCarveAndSpit(out act) ||
                base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        return TryUseDisesteem(out act) ||
               TryUseDeliriumCombo(out act) ||
               TryUseBlood(out act) ||
               TryUseFiller(out act) ||
               base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    #region GCD Skills
    private bool TryUseDisesteem(out IAction? act)
    {
        act = null;
        if (!HasDisesteem) return false;

        if ((LivingShadowPvE.Cooldown.ElapsedAfterGCD(3) && CurrentTarget?.DistanceToPlayer() > 3 || LivingShadowPvE.Cooldown.ElapsedAfterGCD(3) || HasBuffs) && LiveComboTime <= 0)
        {
            return DisesteemPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true);
        }

        return false;
    }
    private bool TryUseBlood(out IAction? act)
    {
        act = null;
        if (ShouldUseBlood(BloodSpendingStrategy, CurrentTarget))
        {
            return QuietusPvE.CanUse(out act) ||
                   BloodspillerPvE.CanUse(out act, skipComboCheck: true);
        }
        return false;
    }
    private bool TryUseDeliriumCombo(out IAction? act)
    {
        act = null;
        if ((CurrentMp >= 9600 || !HasDelirium ) && DeliriumPvE.EnoughLevel) return false;

        if (!DeliriumPvE.EnoughLevel && BloodWeaponPvE.EnoughLevel)
        {
            if (BloodWeaponStacks > 0 && BloodspillerPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (HasDelirium && LiveComboTime <= 0)
        {
            return ImpalementReady && ImpalementPvE.CanUse(out act) ||
                   TorcleaverReady && TorcleaverPvE.CanUse(out act, skipComboCheck: true) ||
                   ComeuppanceReady && ComeuppancePvE.CanUse(out act, skipComboCheck: true) ||
                   ScarletDeliriumReady && ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true);
        }

        return false;
    }
    private bool TryUseFiller(out IAction? act)
    {
        act = null;
        if (HasDelirium) return false;

        return StalwartSoulPvE.CanUse(out act) ||
               UnleashPvE.CanUse(out act) ||
               SouleaterPvE.CanUse(out act) ||
               SyphonStrikePvE.CanUse(out act) ||
               HardSlashPvE.CanUse(out act) ||
               UnmendPvE.CanUse(out act);
    }

    #endregion
    #region oGCD Skills
    private bool TryUseEdgeOfShadow(out IAction? act)
    {
        act = null;
        if (ShouldUseMp(MpSpendingStrategy))
        {
            return FloodOfDarknessPvE.CanUse(out act) ||
                   EdgeOfDarknessPvE.CanUse(out act);
        }
        return false;
    }
    private bool TryUseLivingShadow(out IAction? act)
    {
        if (InCombat && DarkSideTime > 0)
        {
            return LivingShadowPvE.CanUse(out act);
        }
        act = null;
        return false;
    }
    private bool TryUseShadowbringer(out IAction? act)
    {
        if (HasBuffs || InBurstWindow && ShadowbringerPvE.Cooldown.CurrentCharges > 0)
        {
            return ShadowbringerPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (ShadowbringerPvE.Cooldown.CurrentCharges == 1 && ShadowbringerPvE.Cooldown.WillHaveXChargesGCD(2, 1))
        {
            return ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true);
        }
        act = null;
        return false;
    }
    private bool TryUseSaltedEarth(out IAction? act)
    {
        if (HasBuffs || !CombatElapsedLessGCD(3) || InBurstWindow)
        {
            return IsInHighEndDuty && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true) ||
                   !IsInHighEndDuty && !IsMoving && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true) ||
                   SaltAndDarknessPvE.CanUse(out act);
        }
        act = null;
        return false;
    }
    private bool TryUseDelirium (out IAction? act)
    {
        act = null;
        if (!DeliriumPvE.EnoughLevel && BloodWeaponPvE.EnoughLevel && BloodWeaponPvE.CanUse(out act))
        {
            return true;
        }

        return !CombatElapsedLessGCD(3) && DeliriumPvE.CanUse(out act, skipComboCheck: true);
    }
    private bool TryUseCarveAndSpit(out IAction? act)
    {
        act = null;
        if (InBurstWindow || DeliriumPvE.Cooldown.IsCoolingDown && !DeliriumPvE.Cooldown.WillHaveOneCharge(20))
        {
            return AbyssalDrainPvE.CanUse(out act) ||
                   CarveAndSpitPvE.CanUse(out act);
        }

        return false;
    }

    #endregion
    #region Miscellaneous Methods
    private bool TryUsePotion(out IAction? act)
    {
        act = null;
        if (IsMedicated) return false;

        for (var i = 0; i < _potions.Count; i++)
        {
            var (time, enabled, used) = _potions[i];
            if (!enabled || used) continue;

            var potionTimeInSeconds = time * 60;
            var isOpenerPotion = potionTimeInSeconds == 0;
            var isEvenMinutePotion = time % 2 == 0;

            bool canUse;
            if (isOpenerPotion)
            {
                canUse = InCombat && IsLastGCD(ActionID.UnmendPvE) && HasWeaved();
            }
            else
            {
                canUse = InCombat && CombatTime >= potionTimeInSeconds && CombatTime <= potionTimeInSeconds + 59;
            }

            if (!canUse) continue;

            var condition = (isEvenMinutePotion ? InBurstWindow : InOddWindow(LivingShadowPvE)) || isOpenerPotion || InBurstWindow;

            if (condition && UseBurstMedicine(out act, false))
            {
                _potions[i] = (time, enabled, true);
                return true;
            }
        }
        return false;
    }

    private PotionTimings _lastPotionTiming;
    private int _lastFirst, _lastSecond, _lastThird;

    private void UpdatePotions()
    {
        if (_lastPotionTiming != PotionTiming ||
            _lastFirst != CustomFirstPotionTime ||
            _lastSecond != CustomSecondPotionTime ||
            _lastThird != CustomThirdPotionTime)
        {
            var oldPotions = new List<(int Time, bool Enabled, bool Used)>(_potions);

            InitializePotions();

            // Merge used state if in combat
            if (InCombat)
                for (var i = 0; i < _potions.Count; i++)
                {
                    var (time, enabled, _) = _potions[i];
                    var old = oldPotions.FirstOrDefault(p => p.Time == time);
                    if (old.Time == time)
                        _potions[i] = (time, enabled, old.Used);
                }

            _lastPotionTiming = PotionTiming;
            _lastFirst = CustomFirstPotionTime;
            _lastSecond = CustomSecondPotionTime;
            _lastThird = CustomThirdPotionTime;
        }
    }
    private bool ShouldUseMp(MpStrategy strategy)
{
    var riskingMp = CurrentMp >= 8500;

    if (!FloodOfShadowPvE.EnoughLevel)
        return false;

    if (strategy == MpStrategy.Optimal)
    {
        if (riskingMp)
            return CurrentMp >= 3000 || HasDarkArts;

        if (DeliriumPvE.EnoughLevel)
        {
            // For Dark Arts
            if (TheBlackestNightPvE.EnoughLevel && HasDarkArts)
            {
                if (HasDelirium || !DeliriumPvE.Cooldown.WillHaveOneCharge(DarkSideTime + WeaponTotal))
                    return CurrentMp >= 3000;
            }

            // 1m window - 2 uses expected
            if (!DeliriumPvE.Cooldown.WillHaveOneCharge(40) && InOddWindow(LivingShadowPvE))
                return CurrentMp >= 6000;

            // 2m window - 4 uses expected; 5 with Dark Arts
            if (!DeliriumPvE.Cooldown.WillHaveOneCharge(40) && !InOddWindow(LivingShadowPvE))
                return CurrentMp >= 3000;
        }

        // If no Delirium, just use it whenever we have more than 3000 MP
        if (!DeliriumPvE.EnoughLevel)
            return CurrentMp >= 3000;
    }

    return strategy switch
    {
        MpStrategy.Auto3K => CurrentMp >= 3000,
        MpStrategy.Auto6K => CurrentMp >= 6000,
        MpStrategy.Auto9K => CurrentMp >= 9000,
        MpStrategy.AutoRefresh => riskingMp,
        MpStrategy.ForceEdge => EdgeOfDarknessPvE.EnoughLevel && (CurrentMp >= 3000 || HasDarkArts),
        MpStrategy.ForceFlood => FloodOfDarknessPvE.EnoughLevel && (CurrentMp >= 3000 || HasDarkArts),
        _ => false
    };
}

    private bool ShouldUseBlood(BloodStrategy strategy, IBattleChara? target)
    {
        var riskingBlood = Blood >= 90;
        var minimum = (BloodspillerPvE.EnoughLevel || QuietusPvE.EnoughLevel) && (Blood >= 50 || HasDelirium);
        var inMeleeRange = target != null && target.DistanceToPlayer() <= 3;

        // Basic condition for using blood
        var condition = InCombat && target != null && inMeleeRange && minimum &&
                   DarkSideTime > 0 && (riskingBlood || !InOddWindow(LivingShadowPvE) ?
                       !CanFitSksGCD(Player.StatusTime(true, StatusID.Delirium_3836), 3) ||
                       HasBuffs : minimum);

        return strategy switch
        {
            BloodStrategy.Automatic => condition,
            BloodStrategy.OnlyBloodspiller => condition && target!.DistanceToPlayer() <= 3,
            BloodStrategy.OnlyQuietus => condition && NumberOfAllHostilesInRange > 2,
            BloodStrategy.Asap => minimum,
            BloodStrategy.Conserve => riskingBlood || (HasDelirium && Player.StatusTime(true, StatusID.Delirium_3836) > WeaponTotal),
            _ => false
        };
    }

    private List<IBattleChara> _currentParty = [];

    public List<IBattleChara> CurrentParty
    {
        get => _currentParty;
        set
        {
            _currentParty = value;
            var newParty = PartyMembers ?? [Player];
            IEnumerable<IBattleChara> battleCharas = newParty.ToList();
            var hasChanged = !_currentParty.ToHashSet().SetEquals(battleCharas);

            if (hasChanged)
            {
                _currentParty.Clear();
                _currentParty.AddRange(battleCharas);
            }

            if (_currentParty.Count == 0)
            {
                _currentParty.Add(Player);
            }
        }
    }


    #endregion
    #endregion
    }