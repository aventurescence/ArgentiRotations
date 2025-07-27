using System.ComponentModel;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ArgentiRotations.Ranged;

[Rotation("Churin BRD", CombatType.PvE, GameVersion = "7.2.5",
    Description = "I sing the body electric. I gasp the body organic. I miss the body remembered.")]
[SourceCode(Path = "main/ArgentiRotations/Ranged/Bard/ChurinBRD.cs")]
[Api(5)]
public sealed partial class ChurinBRD : BardRotation
{
    #region Properties

    private enum SongTiming
    {
        [Description("Standard 3-3-12 Cycle")] Standard,

        [Description("Adjusted Standard Cycle - 2.48 GCD ideal")]
        AdjustedStandard,

        [Description("3-6-9 Cycle - 2.49 or 2.5 GCD ideal")]
        Cycle369,
        [Description("Custom")] Custom
    }

    private enum WandererWeave
    {
        [Description("Early")] Early,
        [Description("Late")] Late
    }

    private enum PotionTimings
    {
        [Description("None")] None,

        [Description("Opener and Six Minutes")]
        ZeroSix,

        [Description("Two Minutes and Eight Minutes")]
        TwoEight,

        [Description("Opener, Five Minutes and Ten Minutes")]
        ZeroFiveTen,

        [Description("Custom - set values below")]
        Custom
    }

    private float WandTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 42,
        SongTiming.Cycle369 => 42,
        SongTiming.Custom => CustomWandTime,
        _ => 0
    };

    private float MageTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 42,
        SongTiming.Cycle369 => 39,
        SongTiming.Custom => CustomMageTime,
        _ => 0
    };

    private float ArmyTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 33,
        SongTiming.Cycle369 => 36,
        SongTiming.Custom => CustomArmyTime,
        _ => 0
    };
    private float WandRemainTime => 45 - WandTime;
    private float MageRemainTime => 45 - MageTime;
    private float ArmyRemainTime => 45 - ArmyTime;

    #region Potions
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


    private static double RecastTime => ActionManager.GetAdjustedRecastTime(ActionType.Action, 16495U) / 1000.00;
    private static bool CanLateWeave => WeaponRemain < LateWeaveWindow && EnoughWeaveTime;
    private static bool CanEarlyWeave => WeaponRemain > LateWeaveWindow;
    private static float LateWeaveWindow => (float)(RecastTime * 0.45f);
    private static bool EvenMinutePots => !IsFirstCycle && HasRadiantFinale && HasBattleVoice;
    private static bool OddMinutePots => InMages && SoulVoice >= 70;

    private static bool TargetHasDoTs =>
        CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true &&
        CurrentTarget.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite);

    private static bool DoTsEnding => CurrentTarget?.WillStatusEndGCD(1, 0.5f, true, StatusID.Windbite,
        StatusID.Stormbite,
        StatusID.VenomousBite, StatusID.CausticBite) ?? false;

    private static bool InWanderers => Song == Song.Wanderer;
    private static bool InMages => Song == Song.Mage;
    private static bool InArmys => Song == Song.Army;
    private static bool NoSong => Song == Song.None;
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated) && !Player.WillStatusEnd(0,true, StatusID.Medicated);
    private static bool HasResonantArrow => Player.HasStatus(true, StatusID.ResonantArrowReady);

    private static bool EnoughWeaveTime => WeaponRemain > DefaultAnimationLock;

    private const float DefaultAnimationLock = 0.6f;

    private bool InBurst => (!BattleVoicePvE.EnoughLevel && !RadiantFinalePvE.EnoughLevel && HasRagingStrikes) ||
                            (!RadiantFinalePvE.EnoughLevel && HasRagingStrikes && HasBattleVoice) ||
                            (HasRagingStrikes && HasBattleVoice && HasRadiantFinale);

    private static bool IsFirstCycle { get; set; }

    #endregion
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Only use DOTs on targets with Boss Icon")]
    private bool DoTsBoss { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Choose Bard Song Timing Preset")]
    private SongTiming SongTimings { get; set; } = SongTiming.Standard;

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Wanderer's Minuet Uptime - if Using Custom Song Timings")]
    private float CustomWandTime { get; set; } = 45;

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Mage's Ballad Uptime - if Using Custom Song Timings")]
    private float CustomMageTime { get; set; } = 45;

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Army's Paeon Uptime - if Using Custom Song Timings")]
    private float CustomArmyTime { get; set; } = 45;

    [RotationConfig(CombatType.PvE, Name = "Opener Wanderer's Minuet Weave Slot? - if Using Custom Song Timings")]
    private WandererWeave WanderersWeave { get; set; } = WandererWeave.Early;

    [RotationConfig(CombatType.PvE, Name = "Enable PrepullHeartbreak Shot? - Use with BMR Auto Attack Manager")]
    private bool EnablePrepullHeartbreakShot { get; set; } = true;
    [RotationConfig(CombatType.PvE, Name = "Potion Presets")]
    private PotionTimings PotionTiming { get; set; } = PotionTimings.None;

    [RotationConfig(CombatType.PvE, Name = "Enable First Potion for Custom Potion Timings?")]
    private bool CustomEnableFirstPotion { get; set; } = true;
    [Range(0,20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "First Potion Usage for custom timings - enter time in minutes")]
    private int CustomFirstPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Second Potion?")]
    private bool CustomEnableSecondPotion { get; set; } = true;
    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Second Potion Usage for custom timings - enter time in minutes")]
    private int CustomSecondPotionTime { get; set; } = 0;
    [RotationConfig(CombatType.PvE, Name = "Enable Third Potion?")]
    private bool CustomEnableThirdPotion { get; set; } = true;
    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Third Potion Usage for custom timings - enter time in minutes")]
    private int CustomThirdPotionTime { get; set; } = 0;
    [RotationConfig(CombatType.PvE, Name = "Enable Sandbag Mode?")]
    private static bool EnableSandbagMode { get; set; } = false;

    #endregion
    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        IsFirstCycle = true;
        return SongTimings switch
        {
            SongTiming.AdjustedStandard when remainTime <= 0 && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Cycle369 when EnablePrepullHeartbreakShot && remainTime <= 1.6f && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Cycle369 when remainTime <= 0 && StormbitePvE.CanUse(out var act) => act,
            _ => base.CountDownAction(remainTime)
        };    }

#endregion
    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        UpdatePotions();
        if (IsFirstCycle && InArmys && !RadiantFinalePvE.Cooldown.IsCoolingDown)
        {
            IsFirstCycle = false;
        }

        return TryUsePots(out act) ||
               TryUseEmpyrealArrow(out act) ||
               TryUsePitchPerfect(out act) ||
               base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        return TryUseWanderers(out act) ||
               TryUseMages(out act) ||
               TryUseArmys(out act) ||
               base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGcd, out IAction? act)
        {
            return TryUseRadiantFinale(out act) ||
                   TryUseBattleVoice(out act) ||
                   TryUseRagingStrikes(out act) ||
                   TryUseBarrage(out act)||
                   TryUseHeartBreakShot(out act) ||
                   TryUseSideWinder(out act) ||
                   base.AttackAbility(nextGcd, out act);
        }

    #endregion
    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (TryUseIronJaws(out act)) return true;
        if (TryUseDoTs(out act)) return true;

        return InBurst &&RadiantEncorePvE.CanUse(out act, skipComboCheck: true) ||
               TryUseApexArrow(out act) ||
               TryUseBlastArrow(out act) ||
               HasResonantArrow && ResonantArrowPvE.CanUse(out act) ||
               TryUseAoE(out act) ||
               TryUseFiller(out act) ||
               base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods

    #region GCD Skills

    private bool TryUseIronJaws(out IAction? act)
    {
        if (IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true) && (IronJawsPvE.Target.Target?.WillStatusEnd(30, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) ?? false))
        {
            if (InBurst && Player.WillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes) && !BlastArrowPvE.CanUse(out _))
            {
                return true;
            }
        }
        return IronJawsPvE.CanUse(out act);
    }

    private bool TryUseDoTs(out IAction? act)
    {
        if (IronJawsPvE.CanUse(out act) || TargetHasDoTs) return false;

        if (StormbitePvE.EnoughLevel)
        {
            if (StormbitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
                (!DoTsBoss || StormbitePvE.Target.Target.IsBossFromIcon()) &&
                !StormbitePvE.Target.Target.HasStatus(true, StatusID.Stormbite))
            {
                return true;
            }
        }

        if (CausticBitePvE.EnoughLevel)
        {
            if (CausticBitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
                (!DoTsBoss || CausticBitePvE.Target.Target.IsBossFromIcon()) &&
                !CausticBitePvE.Target.Target.HasStatus(true, StatusID.VenomousBite))
            {
                return true;
            }
        }

        if (!StormbitePvE.EnoughLevel && WindbitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
            (!DoTsBoss || WindbitePvE.Target.Target.IsBossFromIcon()))
        {
            if (!IronJawsPvE.EnoughLevel ||
                (IronJawsPvE.EnoughLevel && !WindbitePvE.Target.Target.HasStatus(true, StatusID.Windbite)))
            {
                return true;
            }
        }

        if (!CausticBitePvE.EnoughLevel && VenomousBitePvE.CanUse(out act, skipStatusProvideCheck: true) &&
            (!DoTsBoss || VenomousBitePvE.Target.Target.IsBossFromIcon()))
        {
            if (!IronJawsPvE.EnoughLevel ||
                (IronJawsPvE.EnoughLevel && !VenomousBitePvE.Target.Target.HasStatus(true, StatusID.CausticBite)))
            {
                return true;
            }
        }

        act = null;
        return false;
    }

    private bool TryUseApexArrow(out IAction? act)
        {
            if (ShouldEnterSandbagMode() || InBurst && HasBarrage || SoulVoice < 80) return SetActToNull(out act);

            var hasFullSoul = SoulVoice == 100;
            var hasRagingStrikes = Player.HasStatus(true, StatusID.RagingStrikes);
            var hasBattleVoice = Player.HasStatus(true, StatusID.BattleVoice);

            return ApexArrowPvE.CanUse(out act) switch
            {
                true when (QuickNockPvE.CanUse(out _) || LadonsbitePvE.CanUse(out _)) && hasFullSoul => true,
                true when CurrentTarget?.WillStatusEndGCD(1, 1, true, StatusID.Windbite, StatusID.Stormbite,
                    StatusID.VenomousBite, StatusID.CausticBite) ?? false => false,
                true when hasFullSoul && BattleVoicePvE.Cooldown.WillHaveOneCharge(25) => false,
                true when InWanderers && SoulVoice >= 80 && !hasRagingStrikes => false,
                true when hasRagingStrikes && Player.WillStatusEnd(10, true, StatusID.RagingStrikes) &&
                          (hasFullSoul || SoulVoice >= 80) => true,
                true when hasFullSoul && hasRagingStrikes && hasBattleVoice => true,
                true when InMages && SoulVoice >= 80 && SongEndAfter(22) && SongEndAfter(18) => true,
                true when hasFullSoul && !hasRagingStrikes => true,
                _ => SetActToNull(out act)
            };
        }

    private bool TryUseBlastArrow(out IAction? act)
    {
        return (HasRagingStrikes, DoTsEnding) switch
        {
            (true, false) when BarragePvE.Cooldown.IsCoolingDown || IsLastGCD(ActionID.ApexArrowPvE) => BlastArrowPvE.CanUse(out act),
            (false, false) when InMages => BlastArrowPvE.CanUse(out act),
            _ => SetActToNull(out act)
        };
    }

    private bool TryUseAoE(out IAction? act)
    {
        if (ShouldEnterSandbagMode()) return SetActToNull(out act);

        return ShadowbitePvE.CanUse(out act) ||
               WideVolleyPvE.CanUse(out act) ||
               QuickNockPvE.CanUse(out act);
    }

    private bool TryUseFiller(out IAction? act)
    {
        if (ShouldEnterSandbagMode()) return SetActToNull(out act);

        return (RefulgentArrowPvE.CanUse(out act, skipComboCheck: true) && TargetHasDoTs) ||
               StraightShotPvE.CanUse(out act) ||
               (BurstShotPvE.CanUse(out act) && !HasHawksEye && !HasResonantArrow && !HasBarrage);
    }

        #endregion

    #region oGCD Abilities

        #region Emergency Abilities

        private bool TryUseBarrage(out IAction? act)
        {
            var empyrealArrowReady = EmpyrealArrowPvE.EnoughLevel && Repertoire == 3;

            if (!HasRagingStrikes || empyrealArrowReady || HasHawksEye || ShouldEnterSandbagMode())
                return SetActToNull(out act);

            return BarragePvE.CanUse(out act) && EnoughWeaveTime ;
        }

        private bool TryUseEmpyrealArrow(out IAction? act)
        {
            if (ShouldEnterSandbagMode()) return SetActToNull(out act);

            if (EmpyrealArrowPvE.CanUse(out act))
            {
                return (SongTimings, Song) switch
                {
                    (SongTiming.Standard or SongTiming.Custom, _) => IsFirstCycle
                        ? (InWanderers || !NoSong) && CanLateWeave
                        : EnoughWeaveTime,
                    (SongTiming.AdjustedStandard, _) => InWanderers && CanLateWeave ||
                                                        (InMages || InArmys) && EnoughWeaveTime,
                    (SongTiming.Cycle369, Song.Wanderer) =>  HasRagingStrikes && EnoughWeaveTime ||
                                                             RagingStrikesPvE.Cooldown.IsCoolingDown &&
                                                             !RagingStrikesPvE.Cooldown.WillHaveOneCharge(1) &&
                                                             EnoughWeaveTime,
                    (SongTiming.Cycle369, Song.Mage) => IsFirstCycle ? EnoughWeaveTime : !SongEndAfter(MageRemainTime),
                    (SongTiming.Cycle369, Song.Army) => EnoughWeaveTime,
                    _ => false,

                };
            }
            return false;
        }

        #endregion

        #region Songs

        private bool TryUseWanderers(out IAction? act)
        {
            if (!TheWanderersMinuetPvE.EnoughLevel ||
                !EnableSandbagMode && (IsLastAbility(ActionID.ArmysPaeonPvE) ||
                                       IsLastAbility(ActionID.MagesBalladPvE)))
            {
                return SetActToNull(out act);
            }

            if (NoSong && IsFirstCycle && TheWanderersMinuetPvE.CanUse(out act))
                return SongTimings switch
                {
                    SongTiming.Standard or SongTiming.AdjustedStandard => true,
                    SongTiming.Cycle369 => CanLateWeave,
                    SongTiming.Custom => WanderersWeave == WandererWeave.Early && CanEarlyWeave ||
                                         WanderersWeave == WandererWeave.Late && CanLateWeave,
                                         _ => false
                };
            if (!IsFirstCycle &&
                (InArmys && SongEndAfter(ArmyRemainTime) ||
                 (NoSong && (ArmysPaeonPvE.Cooldown.IsCoolingDown ||
                             MagesBalladPvE.Cooldown.IsCoolingDown))) &&
                CanLateWeave)
            {
                return TheWanderersMinuetPvE.CanUse(out act);
            }
            return SetActToNull(out act);
        }

        private bool TryUseMages(out IAction? act)
        {
            if (!MagesBalladPvE.EnoughLevel ||
                !EnableSandbagMode && (IsLastAbility(ActionID.ArmysPaeonPvE) ||
                                       IsLastAbility(ActionID.TheWanderersMinuetPvE)))
            {
                return SetActToNull(out act);
            }

            if (MagesBalladPvE.CanUse(out act) && CanLateWeave)
            {
                if (InWanderers && SongEndAfter(WandRemainTime) && (Repertoire == 0 || !HasHostilesInMaxRange) ||
                    InArmys && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown ||
                    NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown) ||
                    EnableSandbagMode && SongEndAfter(WandRemainTime))
                {
                    return true;
                }

            }

            return false;
        }

        private bool TryUseArmys(out IAction? act)
        {
            if (!ArmysPaeonPvE.EnoughLevel ||
                !EnableSandbagMode && (IsLastAbility(ActionID.TheWanderersMinuetPvE) ||
                                       IsLastAbility(ActionID.MagesBalladPvE)))
            {
                return SetActToNull(out act);
            }

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if (((InMages && SongEndAfter(MageRemainTime)) ||
                         (InWanderers && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown) ||
                         (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)) ||
                         (NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown ||
                                     MagesBalladPvE.Cooldown.IsCoolingDown))) && CanLateWeave || EnableSandbagMode && InMages && SongEndAfter(MageRemainTime))
                        return ArmysPaeonPvE.CanUse(out act);
                    break;
                case SongTiming.Cycle369:
                    if (!EnableSandbagMode && (InMages && SongEndAfter(MageRemainTime) ||
                                               (InWanderers && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown) ||
                                               (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2))))
                        switch (IsFirstCycle)
                        {
                            case true:
                                if (CanLateWeave && ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                            case false:
                                if (ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                        }
                    if (EnableSandbagMode && InMages && SongEndAfter(MageRemainTime))
                        return ArmysPaeonPvE.CanUse(out act);

                    break;
            }

            return SetActToNull(out act);
        }

        #endregion

        #region Buffs

        private bool TryUseRadiantFinale(out IAction? act)
        {
            if (!RadiantFinalePvE.EnoughLevel || !RadiantFinalePvE.IsEnabled) return SetActToNull(out act);

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if ((IsFirstCycle && HasBattleVoice && InWanderers) ||
                        (!IsFirstCycle && InWanderers && BattleVoicePvE.Cooldown.WillHaveOneCharge(0.5f)))
                        return RadiantFinalePvE.CanUse(out act);

                    break;
                case SongTiming.Cycle369:
                {
                    if (IsFirstCycle && InWanderers && TargetHasDoTs)
                        return RadiantFinalePvE.CanUse(out act) && CanLateWeave;
                }
                    if (!IsFirstCycle && InWanderers && BattleVoicePvE.Cooldown.WillHaveOneCharge(0.5f))
                        return RadiantFinalePvE.CanUse(out act) && CanEarlyWeave;
                    break;
            }

            return SetActToNull(out act);
        }

        private bool TryUseBattleVoice(out IAction? act)
        {
            if (!BattleVoicePvE.EnoughLevel || !BattleVoicePvE.IsEnabled) return SetActToNull(out act);

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if (InWanderers)
                        if (IsFirstCycle && CanLateWeave && !HasRadiantFinale ||
                            !IsFirstCycle && HasRadiantFinale ||
                            IsLastAbility(ActionID.RadiantFinalePvE))
                            return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                    break;
                case SongTiming.Cycle369:
                    if (InWanderers)
                    {
                        if (IsFirstCycle && TargetHasDoTs && HasRadiantFinale)
                        {
                            return BattleVoicePvE.CanUse(out act) && CanEarlyWeave;
                        }

                        if (!IsFirstCycle && HasRadiantFinale)
                        {
                            return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                        }
                    }

                    break;
            }

            return SetActToNull(out act);
        }

        private bool TryUseRagingStrikes(out IAction? act)
        {
            if ((HasBattleVoice && HasRadiantFinale ||
                !RadiantFinalePvE.EnoughLevel ||
                !BattleVoicePvE.EnoughLevel) && CanLateWeave)
            {
                return RagingStrikesPvE.CanUse(out act);
            }

            return SetActToNull(out act);
        }

        #endregion

        #region Attack Abilities

        private bool TryUseHeartBreakShot(out IAction? act)
        {
            if (ShouldEnterSandbagMode()) return SetActToNull(out act);

            var willHaveMaxCharges = BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 3);
            var willHave1ChargeInMages = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InMages;
            var willHave1ChargeInArmys = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InArmys;

            if ((InWanderers && !HasRagingStrikes && BloodletterPvE.Cooldown.CurrentCharges < 3 && !willHaveMaxCharges) ||
                (InArmys && SongTime <= 30 && BloodletterPvE.Cooldown.CurrentCharges < 3 && !willHaveMaxCharges) ||
                (InMages && SongEndAfter((float)(MageRemainTime + RecastTime * 0.9))) ||
                (!NoSong && (EmpyrealArrowPvE.CanUse(out _) || EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(0.5f))))
            {
                return SetActToNull(out act);
            }

            if (SongTimings == SongTiming.Cycle369 && NoSong &&
                HeartbreakShotPvE.CanUse(out act, usedUp: false)) return true;

            if ((InBurst || BloodletterPvE.Cooldown.CurrentCharges < 3 &&
                    (willHaveMaxCharges || willHave1ChargeInMages || (willHave1ChargeInArmys && SongTime > 30)) ||
                    IsMedicated) && EnoughWeaveTime)
            {
                return RainOfDeathPvE.CanUse(out act, usedUp: true) ||
                    HeartbreakShotPvE.CanUse(out act, usedUp: true) ||
                    BloodletterPvE.CanUse(out act, usedUp: true);
            }

            return SetActToNull(out act);
        }

        private bool TryUseSideWinder(out IAction? act)
        {
            if (ShouldEnterSandbagMode()) return SetActToNull(out act);

            var rFWillHaveCharge = RadiantFinalePvE.Cooldown.IsCoolingDown &&RadiantFinalePvE.Cooldown.WillHaveOneCharge(10);
            var bVWillHaveCharge = BattleVoicePvE.Cooldown.IsCoolingDown && BattleVoicePvE.Cooldown.WillHaveOneCharge(10);

            if  (InBurst || !RadiantFinalePvE.EnoughLevel ||
                (!rFWillHaveCharge && !bVWillHaveCharge && RagingStrikesPvE.Cooldown.IsCoolingDown) ||
                (RagingStrikesPvE.Cooldown.IsCoolingDown && !HasRagingStrikes))
                return SidewinderPvE.CanUse(out act) && EnoughWeaveTime;

            return SetActToNull(out act);
        }

        private bool TryUsePitchPerfect(out IAction? act)
        {
            if (!InBurst && !RagingStrikesPvE.Cooldown.IsCoolingDown || ShouldEnterSandbagMode() || Song != Song.Wanderer) return SetActToNull(out act);

            return Repertoire switch
            {
                3 when PitchPerfectPvE.CanUse(out act) => true,
                2 when EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) && PitchPerfectPvE.CanUse(out act) => true,
                > 0 when SongEndAfter(WandRemainTime) && SongTime <= WandRemainTime - 0.6f  && PitchPerfectPvE.CanUse(out act) => true,
                _ => SetActToNull(out act)
            };
        }

        #endregion

        #endregion

    #region Miscellaneous

        private static bool SetActToNull(out IAction? act)
        {
            act = null;
            return false;
        }

        private bool ShouldEnterSandbagMode()
        {
            return EnableSandbagMode && (!InBurst || Song != Song.Wanderer) &&
                   (IsFirstCycle && !RadiantFinalePvE.Cooldown.HasOneCharge && !BattleVoicePvE.Cooldown.HasOneCharge && !RagingStrikesPvE.Cooldown.HasOneCharge &&
                    RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.Cooldown.IsCoolingDown && RagingStrikesPvE.Cooldown.IsCoolingDown ||
                    !IsFirstCycle && !BattleVoicePvE.Cooldown.HasOneCharge && !RagingStrikesPvE.Cooldown.HasOneCharge);
        }

        private bool TryUsePots(out IAction? act)
        {
            for (var i = 0; i < _potions.Count; i++)
            {
                var (time, enabled, used) = _potions[i];
                if (!enabled || used) continue;

                var potionTimeInSeconds = time * 60;
                var openerCondition = IsFirstCycle && Song == Song.Wanderer && TargetHasDoTs;
                var isOpenerPotion = potionTimeInSeconds == 0;
                var isEvenMinutePotion = time % 2 == 0;

                var canUse = (isOpenerPotion && openerCondition) ||
                             (!isOpenerPotion && CombatTime >= potionTimeInSeconds &&
                              CombatTime < potionTimeInSeconds + 59);

                if (!canUse) continue;

                var condition = (isEvenMinutePotion ? EvenMinutePots : OddMinutePots) ||
                                (isOpenerPotion && openerCondition);

                if (condition && UseBurstMedicine(out act, false))
                {
                    _potions[i] = (time, enabled, true);
                    return true;
                }
            }
            act = null;
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

    #endregion

    #endregion
}


