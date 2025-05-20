using System.ComponentModel;
using System.Globalization;
using ArgentiRotations.Common;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ArgentiRotations.Ranged;

[Rotation("Churin BRD", CombatType.PvE, GameVersion = "7.2.1",
    Description = "For max level high-end content use only :3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinBRD.cs")]
[Api(4)]
public sealed class ChurinBRD : BardRotation
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

    private DateTime _lastPotionUsed = DateTime.MinValue;
    private float WandRemainTime => 45 - WandTime;
    private float MageRemainTime => 45 - MageTime;
    private float ArmyRemainTime => 45 - ArmyTime;



    private int FirstPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 0,
        PotionTimings.TwoEight => 2,
        PotionTimings.ZeroFiveTen => 0,
        PotionTimings.Custom => CustomFirstPotionTime,
        _ => 9999
    };

    private int SecondPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 6,
        PotionTimings.TwoEight => 8,
        PotionTimings.ZeroFiveTen => 5,
        PotionTimings.Custom => CustomSecondPotionTime,
        _ => 9999
    };

    private int ThirdPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 9999,
        PotionTimings.TwoEight => 9999,
        PotionTimings.ZeroFiveTen => 10,
        PotionTimings.Custom => CustomThirdPotionTime,
        _ => 9999
    };

    private bool EnableFirstPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => true,
        PotionTimings.TwoEight => true,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableFirstPotion,
        _ => false
    };

    private bool EnableSecondPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => true,
        PotionTimings.TwoEight => true,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableSecondPotion,
        _ => false
    };

    private bool EnableThirdPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => false,
        PotionTimings.TwoEight => false,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableThirdPotion,
        _ => false
    };

    private static double RecastTime => ActionManager.GetAdjustedRecastTime(ActionType.Action, 16495U) / 1000.00;

    private static bool CanLateWeave => WeaponRemain < LateWeaveWindow && EnoughWeaveTime;

    private static bool CanEarlyWeave => WeaponRemain > LateWeaveWindow;

    private static float LateWeaveWindow => (float)(RecastTime * 0.45f);

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
    private static bool EnoughWeaveTime => WeaponRemain > DefaultAnimationLock;

    private const float DefaultAnimationLock = 0.6f;

    private bool InBurst => (!BattleVoicePvE.EnoughLevel && !RadiantFinalePvE.EnoughLevel && HasRagingStrikes) ||
                            (!RadiantFinalePvE.EnoughLevel && HasRagingStrikes && HasBattleVoice) ||
                            (HasRagingStrikes && HasBattleVoice && HasRadiantFinale);

    private static bool IsFirstCycle { get; set; }

    #endregion
    #region Config Options

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
    [RotationConfig(CombatType.PvE, Name = "Potion Presets")]
    private PotionTimings PotionTiming { get; set; } = PotionTimings.None;
    [RotationConfig(CombatType.PvE,Name = "Enable First Potion for Custom Potion Timings?")]
    private bool CustomEnableFirstPotion { get; set; }
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

    #endregion
    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        IsFirstCycle = true;
        return SongTimings switch
        {
            SongTiming.AdjustedStandard when remainTime <= 0 && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Standard or SongTiming.Custom or SongTiming.Cycle369 when remainTime <= 0.01 && WindbitePvE.CanUse(out var act) => act,
            _ => base.CountDownAction(remainTime)
        };
    }

    #endregion
    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction(ActionID.WindbitePvE) && NoSong && SongTimings == SongTiming.Cycle369) return HeartbreakShotPvE.CanUse(out act);
        if (TryUsePots(out act))
            switch (IsFirstCycle)
            {
                case true:
                if (IsLastGCD(ActionID.CausticBitePvE)) return true;
                break;
                case false:
                    switch (SongTimings)
                    {
                        case SongTiming.Standard or SongTiming.Custom:
                            if (!NoSong && SongTime < 45 - RecastTime * 0.5) return true;
                            break;
                        case SongTiming.AdjustedStandard or SongTiming.Cycle369:
                            if (HasRadiantFinale && HasBattleVoice) return true;
                            break;
                    }
                    break;
            }

        return TryUseEmpyrealArrow(out act) ||
               TryUseBarrage(out act)||
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
            if (IsFirstCycle && InArmys && !RadiantFinalePvE.Cooldown.IsCoolingDown)
            {
                IsFirstCycle = false;
            }

            if (IsLastAbility(ActionID.BattleVoicePvE) && !IsFirstCycle)
            {
                return TryUseHeartBreakShot(out act) || TryUseRagingStrikes(out act);
            }

            return TryUseRadiantFinale(out act) ||
                   TryUseBattleVoice(out act) ||
                   TryUseRagingStrikes(out act) ||
                   TryUsePitchPerfect(out act) ||
                   TryUseSideWinder(out act) ||
                   TryUseHeartBreakShot(out act) ||
                   base.AttackAbility(nextGcd, out act);
        }

    #endregion
    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (Player.HasStatus(true,StatusID.Barrage)) return RefulgentArrowPvE.CanUse(out act);

        return TryUseDoTs(out act) ||
               TryUseIronJaws(out act) ||
               TryUseApexArrow(out act) ||
               TryUseBlastArrow(out act) ||
               (InBurst && RadiantEncorePvE.CanUse(out act, skipComboCheck: true)) ||
               ResonantArrowPvE.CanUse(out act) ||
               TryUseAoE(out act) ||
               TryUseFiller(out act) ||
               base.GeneralGCD(out act);
    }
    #endregion
    #region Extra Methods
        #region GCD Skills
        private bool TryUseIronJaws(out IAction? act)
        {
            var target = CurrentTarget;
            if (target?.StatusList == null || !target.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) &&
                !target.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite))
            {
                return SetActToNull(out act);
            }

            if (IronJawsPvE.Target.Target?.WillStatusEnd(30, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) == true)
            {
                if (InBurst &&
                    Player.WillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes) &&
                    !BlastArrowPvE.CanUse(out _))
                {
                    return IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true);
                }
            }

            return IronJawsPvE.CanUse(out act) || SetActToNull(out act);
        }

        private bool TryUseDoTs(out IAction? act)
        {
            if (CurrentTarget?.StatusList == null) return SetActToNull(out act);

        if (IronJawsPvE.EnoughLevel && CurrentTarget.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) &&
            CurrentTarget.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite))
        {
            // Do not use WindbitePvE or VenomousBitePvE if both statuses are present and IronJawsPvE has enough level
        }
        else
        {
            if (WindbitePvE.CanUse(out act, skipTTKCheck: true, skipAoeCheck: true)) return true;
            if (VenomousBitePvE.CanUse(out act, skipTTKCheck: true, skipAoeCheck: true)) return true;
        }

        return SetActToNull(out act);
    }

        private bool TryUseApexArrow(out IAction? act)
            {
                if (!ApexArrowPvE.CanUse(out act, skipAoeCheck: true))
                    return false;

                var hasFullSoul = SoulVoice == 100;
                var hasRagingStrikes = Player.HasStatus(true, StatusID.RagingStrikes);
                var hasBattleVoice = Player.HasStatus(true, StatusID.BattleVoice);

                return ApexArrowPvE.CanUse(out act) switch
                {
                    true when (QuickNockPvE.CanUse(out _) || LadonsbitePvE.CanUse(out _)) && hasFullSoul => true,
                    false when CurrentTarget?.WillStatusEndGCD(1,1, true, StatusID.Windbite, StatusID.Stormbite,
                        StatusID.VenomousBite, StatusID.CausticBite) ?? false => false,
                    true when hasFullSoul && BattleVoicePvE.Cooldown.WillHaveOneCharge(25) => false,
                    true when InWanderers && SoulVoice >= 80 && !hasRagingStrikes => false,
                    true when hasRagingStrikes && Player.WillStatusEnd(10, true, StatusID.RagingStrikes) && (hasFullSoul || SoulVoice >= 80) => true,
                    true when hasFullSoul && hasRagingStrikes && hasBattleVoice => true,
                    true when InMages && SoulVoice >= 80 && SongEndAfter(22) && SongEndAfter(18) => true,
                    true when hasFullSoul && !hasRagingStrikes => true,
                    _ => SetActToNull(out act)
                };
            }

        private bool TryUseBlastArrow(out IAction? act)
            {
                var hasRagingStrikes = Player.HasStatus(true, StatusID.RagingStrikes);

                return (hasRagingStrikes, DoTsEnding) switch
                {
                    (true, false) when BarragePvE.Cooldown.IsCoolingDown => BlastArrowPvE.CanUse(out act),
                    (false,false) when InMages => BlastArrowPvE.CanUse(out act),
                    _ => SetActToNull(out act)
                };
            }

        private bool TryUseAoE(out IAction? act)
        {
            return ShadowbitePvE.CanUse(out act) ||
                   WideVolleyPvE.CanUse(out act)||
                   QuickNockPvE.CanUse(out act)||
                   SetActToNull(out act);
        }

        private bool TryUseFiller(out IAction? act)
        {
            if (RefulgentArrowPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (StraightShotPvE.CanUse(out act)) return true;
            if (BurstShotPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.HawksEye_3861) || Player.HasStatus(true, StatusID.ResonantArrowReady)) return true;
            if (HeavyShotPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.HawksEye_3861) || Player. HasStatus(true, StatusID.ResonantArrowReady)) return true;

            return SetActToNull(out act);
        }

        #endregion
        #region oGCD Abilities
            #region Emergency Abilities
            private bool TryUseBarrage(out IAction? act)
                {
                    var hasRagingStrikes = Player.HasStatus(true, StatusID.RagingStrikes);
                    var hasHawksEye = Player.HasStatus(true, StatusID.HawksEye_3861);
                    var empyrealArrowReady = EmpyrealArrowPvE.EnoughLevel && Repertoire == 3;

                    if (!hasRagingStrikes || empyrealArrowReady || hasHawksEye)
                        return SetActToNull(out act);

                    return BarragePvE.CanUse(out act) && EnoughWeaveTime || SetActToNull(out act);
                }

            private bool TryUseEmpyrealArrow(out IAction? act)
                         {
                             switch (SongTimings)
                             {
                                 case SongTiming.Standard or SongTiming.Custom:
                                     switch (IsFirstCycle)
                                     {
                                         case true when TheWanderersMinuetPvE.Use() || !NoSong:
                                             return EmpyrealArrowPvE.CanUse(out act) && CanLateWeave;
                                         case false:
                                             return EmpyrealArrowPvE.CanUse(out act) && EnoughWeaveTime;
                                     }
                                     break;
                                 case SongTiming.AdjustedStandard:
                                     switch (Song)
                                     {
                                         case Song.Wanderer:
                                         {
                                             if (CanLateWeave)
                                                 return EmpyrealArrowPvE.CanUse(out act);
                                             break;
                                         }
                                         case Song.Mage or Song.Army:
                                         {
                                             return EmpyrealArrowPvE.CanUse(out act) && EnoughWeaveTime;
                                         }
                                     }
                                     break;
                                 case SongTiming.Cycle369:
                                     switch (Song)
                                     {
                                         case Song.Wanderer:
                                             switch (IsFirstCycle)
                                             {
                                                case true:
                                                 if (Player.HasStatus(true, StatusID.RagingStrikes) ||
                                                     IsLastAbility(ActionID.RagingStrikesPvE) ||
                                                     RagingStrikesPvE.Cooldown.IsCoolingDown)
                                                     return EmpyrealArrowPvE.CanUse(out act) && EnoughWeaveTime;
                                                 break;
                                                case false:
                                                    if (EnoughWeaveTime) return EmpyrealArrowPvE.CanUse(out act);
                                                    break;
                                             }
                                             break;
                                         case Song.Mage:
                                             if (!IsFirstCycle && SongEndAfter(MageRemainTime))
                                             {
                                                 return ArmysPaeonPvE.CanUse(out act);
                                             }
                                             if (EmpyrealArrowPvE.CanUse(out act) && EnoughWeaveTime)
                                             {
                                                 return true;
                                             }
                                             break;
                                         case Song.Army:
                                             if (EmpyrealArrowPvE.Cooldown.IsCoolingDown &&
                                                 EmpyrealArrowPvE.Cooldown.RecastTimeRemainOneCharge > WeaponRemain - 0.6)
                                                 return SetActToNull(out act);
                                             return EmpyrealArrowPvE.CanUse(out act) && EnoughWeaveTime;
                                     }
                                     break;
                             }

                             return SetActToNull(out act);
                         }
            #endregion
            #region Songs
            private bool TryUseWanderers(out IAction? act)
            {
                if (!TheWanderersMinuetPvE.EnoughLevel || IsLastAbility(ActionID.ArmysPaeonPvE) || IsLastAbility(ActionID.MagesBalladPvE)) return SetActToNull(out act);

                if (NoSong && IsFirstCycle)
                {
                    switch (SongTimings)
                    {
                        case SongTiming.Standard or SongTiming.AdjustedStandard: if (TheWanderersMinuetPvE.CanUse(out act)) return true;
                            break;
                        case SongTiming.Cycle369: if (CanLateWeave) return TheWanderersMinuetPvE.CanUse(out act);
                            break;
                        case SongTiming.Custom:
                            switch (WanderersWeave)
                            {
                                case WandererWeave.Early:
                                    if (TheWanderersMinuetPvE.CanUse(out act) && CanEarlyWeave) return true;
                                    break;
                                case WandererWeave.Late:
                                    if (TheWanderersMinuetPvE.CanUse(out act) && CanLateWeave) return true;
                                    break;
                                default:
                                    if (TheWanderersMinuetPvE.CanUse(out act) && CanEarlyWeave) return true;
                                    break;
                            }

                            break;
                    }
                }

                if ((!IsFirstCycle && InArmys && SongEndAfter(ArmyRemainTime) || NoSong &&
                    (ArmysPaeonPvE.Cooldown.IsCoolingDown || MagesBalladPvE.Cooldown.IsCoolingDown)) && CanLateWeave)
                {
                    return TheWanderersMinuetPvE.CanUse(out act);
                }
                return SetActToNull(out act);
            }
            private bool TryUseMages(out IAction? act)
            {
                if (!InCombat || IsLastAbility(ActionID.ArmysPaeonPvE) || IsLastAbility(ActionID.TheWanderersMinuetPvE))
                {
                    return SetActToNull(out act);
                }

                switch (SongTimings)
                {
                    case SongTiming.Cycle369:
                    if (InWanderers && SongEndAfter((float)(WandRemainTime - RecastTime * 0.4)) &&
                        (Repertoire == 0 || !HasHostilesInMaxRange) ||
                        (InArmys && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown) || NoSong &&
                        (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown))
                    {
                        return MagesBalladPvE.CanUse(out act);
                    }
                    break;
                    case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                        if (InWanderers && SongEndAfter(WandRemainTime + LateWeaveWindow) &&
                            (Repertoire == 0 || !HasHostilesInMaxRange) ||
                            (InArmys && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown) || NoSong &&
                            (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown))
                        {
                            return MagesBalladPvE.CanUse(out act);
                        }
                        break;
                }

                return SetActToNull(out act);
            }
            private bool TryUseArmys(out IAction? act)
            {
                if (!ArmysPaeonPvE.EnoughLevel ||
                    IsLastAbility(ActionID.TheWanderersMinuetPvE) ||
                    IsLastAbility(ActionID.MagesBalladPvE))
                {
                    return SetActToNull(out act);
                }

                switch (SongTimings)
                {
                    case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                        if (((InMages && SongEndAfter(MageRemainTime)) ||
                            (InWanderers && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown) ||
                            (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)) || NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || MagesBalladPvE.Cooldown.IsCoolingDown)) && CanLateWeave)
                        {
                            return ArmysPaeonPvE.CanUse(out act);
                        }
                        break;
                    case SongTiming.Cycle369:
                        if (InMages && SongEndAfter(MageRemainTime) ||
                        (InWanderers && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown) ||
                            (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)))
                    {
                        switch (IsFirstCycle)
                        {
                            case true:
                                if (CanLateWeave && ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                            case false:
                                if (ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                        }
                    } break;
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
                        if (IsFirstCycle && Player.HasStatus(true, StatusID.BattleVoice) && InWanderers ||
                            !IsFirstCycle && InWanderers && SongTime < 45 - RecastTime - RecastTime * 0.5)
                        {
                            return RadiantFinalePvE.CanUse(out act);
                        }

                        break;
                    case SongTiming.Cycle369:
                    {
                        if (IsFirstCycle && InWanderers && TargetHasDoTs)
                            return RadiantFinalePvE.CanUse(out act) && CanLateWeave;
                    }
                        if (!IsFirstCycle && InWanderers && SongTime < 45 - RecastTime - RecastTime * 0.5)
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
                        {
                            if (IsFirstCycle && CanLateWeave && !Player.HasStatus(true, StatusID.RadiantFinale) ||
                                !IsFirstCycle && (Player.HasStatus(true, StatusID.RadiantFinale)|| IsLastAbility(ActionID.RadiantFinalePvE)))
                                return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                        }
                        break;
                    case SongTiming.Cycle369:
                        if (InWanderers)
                        {
                            if (IsFirstCycle && TargetHasDoTs && (Player.HasStatus(true, StatusID.RadiantFinale) || RadiantFinalePvE.Cooldown.IsCoolingDown))
                                return BattleVoicePvE.CanUse(out act) && CanEarlyWeave;

                            if (!IsFirstCycle && (IsLastAbility(ActionID.RadiantFinalePvE) || Player.HasStatus(true, StatusID.RadiantFinale)))
                                return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                        }
                        break;
                }

                return SetActToNull(out act);
            }

            private bool TryUseRagingStrikes(out IAction? act)
            {
                if (Player.HasStatus(true, StatusID.BattleVoice, StatusID.RadiantFinale) ||
                    !RadiantFinalePvE.EnoughLevel || !BattleVoicePvE.EnoughLevel)
                    return RagingStrikesPvE.CanUse(out act) && CanLateWeave;

                return SetActToNull(out act);
            }


            #endregion
            #region Attack Abilities
            private bool TryUseHeartBreakShot(out IAction? act)
            {
                var willHaveMaxCharges = BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax,3);
                var willHave1ChargeInMages = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InMages;
                var willHave1ChargeInArmys = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InArmys;

                if (InWanderers && !Player.HasStatus(true, StatusID.RagingStrikes) && !willHaveMaxCharges ||
                    InArmys && SongTime <= 35 && !willHaveMaxCharges ||
                    InMages && SongEndAfter((float)(MageRemainTime + RecastTime * 0.9))
                    ||!NoSong && (EmpyrealArrowPvE.CanUse(out _) || EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(0.5f))) return SetActToNull(out act);

                if ((InBurst || willHaveMaxCharges || willHave1ChargeInMages ||
                     willHave1ChargeInArmys && SongTime > 35 || Player.HasStatus(true, StatusID.Medicated)) &&
                    EnoughWeaveTime)

                    return HeartbreakShotPvE.CanUse(out act, usedUp: true) ||
                           RainOfDeathPvE.CanUse(out act, usedUp: true) ||
                           BloodletterPvE.CanUse(out act, usedUp: true);

                return SetActToNull(out act);
            }
            private bool TryUseSideWinder(out IAction? act)
            {
                var rFWillHaveCharge = RadiantFinalePvE.Cooldown.WillHaveOneCharge(10);
                var bVWillHaveCharge = BattleVoicePvE.Cooldown.WillHaveOneCharge(10);

               if (InBurst || !RadiantFinalePvE.EnoughLevel ||
                   !rFWillHaveCharge && !bVWillHaveCharge && RagingStrikesPvE.Cooldown.IsCoolingDown ||
                   RagingStrikesPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.RagingStrikes))
                   return SidewinderPvE.CanUse(out act) && EnoughWeaveTime;

               return SetActToNull(out act);
            }
            private bool TryUsePitchPerfect(out IAction? act)
            {
                if (Song != Song.Wanderer) return SetActToNull(out act);

                if (SongEndAfter(WandRemainTime) && Repertoire > 0 && WeaponRemain > RecastTime * 0.45 && WeaponRemain < RecastTime * 0.55||
                    ((Repertoire == 3 || Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1, 0.5f)) && EnoughWeaveTime))
                    return PitchPerfectPvE.CanUse(out act) ;

                return SetActToNull(out act);
            }
            #endregion

        #endregion
        #region Miscellaneous
        private static bool SetActToNull(out IAction? act)
        {
            act = null;
            return false;
        }

        private bool TryUsePots(out IAction? act)
        {
            if (CombatTime <= 0) return SetActToNull(out act);

            var canUsePotion = (EnableFirstPotion && CombatTime >= FirstPotionTime * 60 &&
                                FirstPotionTime < SecondPotionTime && FirstPotionTime < ThirdPotionTime) ||
                               (EnableSecondPotion && CombatTime >= SecondPotionTime * 60 &&
                                SecondPotionTime > FirstPotionTime && SecondPotionTime < ThirdPotionTime &&
                                (DateTime.Now - _lastPotionUsed).TotalSeconds >= 270) ||
                               (EnableThirdPotion && CombatTime >= ThirdPotionTime * 60 &&
                                ThirdPotionTime > FirstPotionTime && ThirdPotionTime > SecondPotionTime &&
                                (DateTime.Now - _lastPotionUsed).TotalSeconds >= 270);

            if (canUsePotion)
            {
                _lastPotionUsed = DateTime.Now;
                return UseBurstMedicine(out act);
            }

            return SetActToNull(out act);
        }
        #endregion
    #endregion
    #region Status Window Override

    public override void DisplayStatus()
    {
        DisplayStatusHelper.BeginPaddedChild("The CustomRotation's status window", true,
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        DrawRotationStatus();
        DrawCombatStatus();
        ImGui.Separator();
        DisplayStatusHelper.EndPaddedChild();
    }
    private void DrawRotationStatus()
    {
        var text = "Rotation: " + Name;
        var textSize = ImGui.CalcTextSize(text).X;
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, text);
            DisplayStatusHelper.HoveredTooltip(Description);
        }, ImGui.GetWindowWidth(), textSize);
    }
    private void DrawCombatStatus()
    {
        var tableSize = ImGui.GetContentRegionAvail().X;
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.BeginGroup();
            DrawCombatStatusText();
            ImGui.EndGroup();
        }, ImGui.GetWindowWidth(), tableSize);
    }
    private void DrawCombatStatusText()
    {
        if (ImGui.BeginTable("CombatStatusTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Label");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            // Row 1: Current Preset
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Song Timing Preset");
            ImGui.TableNextColumn();
            ImGui.Text(SongTimings.ToString());

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Song");
            ImGui.TableNextColumn();
            ImGui.Text(Song.ToString());

            // Row 2: Wanderer's Minuet Uptime
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Wanderer's Minuet Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {WandTime} seconds");
            ImGui.Text($"Remaining Time: {WandRemainTime} seconds");

            // Row 3: Mage's Ballad Uptime
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Mage's Ballad Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {MageTime} seconds");
            ImGui.Text($"Remaining Time: {MageRemainTime} seconds");

            // Row 4: Army's Paeon Uptime
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Army's Paeon Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {ArmyTime} seconds");
            ImGui.Text($"Remaining Time: {ArmyRemainTime} seconds");

            // Row 5: Is First Cycle?
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Is First Cycle?");
            ImGui.TableNextColumn();
            ImGui.Text(IsFirstCycle ? "Yes" : "No");

            // Row 6: Target Has DoTs?
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Target Has DoTs?");
            ImGui.TableNextColumn();
            ImGui.Text(TargetHasDoTs ? "Yes" : "No");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Adjusted Recast Time");
            ImGui.TableNextColumn();
            ImGui.Text(RecastTime.ToString(CultureInfo.CurrentCulture));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Time until the next GCD");
            ImGui.TableNextColumn();
            ImGui.Text(NextAbilityToNextGCD.ToString(CultureInfo.CurrentCulture));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Recast Time remaining for Empyreal Arrow");
            ImGui.TableNextColumn();
            ImGui.Text(EmpyrealArrowPvE.Cooldown.RecastTimeRemainOneCharge.ToString(CultureInfo.CurrentCulture));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enough Weave Time");
            ImGui.Text("Can Early Weave");
            ImGui.Text("Can Late Weave");
            ImGui.TableNextColumn();
            ImGui.Text(EnoughWeaveTime ? "Yes " : "No ");
            ImGui.Text(CanEarlyWeave ? "Yes " : "No ");
            ImGui.Text(CanLateWeave ? "Yes " : "No ");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Potion Usage");
            ImGui.TableNextColumn();
            ImGui.Text($"First: {EnableFirstPotion} at {FirstPotionTime} minutes");
            ImGui.Text($"Second: {EnableSecondPotion} at {SecondPotionTime} minutes");
            ImGui.Text($"Third: {EnableThirdPotion} at {ThirdPotionTime} minutes");
            ImGui.Text($"Last potion used at: {_lastPotionUsed}");


            ImGui.EndTable();
        }
    }

    #endregion
}