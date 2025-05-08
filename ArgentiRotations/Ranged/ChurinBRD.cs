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
            [Description("Adjusted Standard Cycle")] AdjustedStandard,
            [Description("3-6-9 Cycle")] Cycle369,
            [Description("Custom")] Custom
        }
        private enum WandererWeave
        {
            [Description("Early")] Early,
            [Description("Late")] Late
        }
        private enum PotionTimingOption
        {
            [Description("None")]None,
            [Description("Opener and 6 Minutes")]ZeroSix,
            [Description("2 and 8 minutes")]TwoEight,
            [Description("Opener, 5 and 10 minutes")]ZeroFiveTen
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

        private static double RecastTime => ActionManager.GetAdjustedRecastTime(ActionType.Action, 16495U) / 1000.00;

        private static bool TargetHasDoTs => CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true &&
                                                 CurrentTarget.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite);
        private static bool DoTsEnding => CurrentTarget?.WillStatusEndGCD(1, 0.5f, true, StatusID.Windbite, StatusID.Stormbite,
            StatusID.VenomousBite, StatusID.CausticBite) ?? false;

        private static bool InWanderers => Song == Song.Wanderer;
        private static bool InMages => Song == Song.Mage;
        private static bool InArmys => Song == Song.Army;
        private static bool NoSong => Song == Song.None;

        private static bool HasRagingStrikes => !Player.WillStatusEnd(0, true, StatusID.RagingStrikes);
        private static bool HasBattleVoice =>  !Player.WillStatusEnd(0, true, StatusID.BattleVoice);
        private static bool HasRadiantFinale => !Player.WillStatusEnd(0, true, StatusID.RadiantFinale);
        private bool InBurst =>  !BattleVoicePvE.EnoughLevel && !RadiantFinalePvE.EnoughLevel && HasRagingStrikes ||
                                 !RadiantFinalePvE.EnoughLevel && HasRagingStrikes && HasBattleVoice ||
                                  HasRagingStrikes && HasBattleVoice && HasRadiantFinale;
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

    [RotationConfig(CombatType.PvE, Name = "Potion Timings")]
    private PotionTimingOption PotionTimings { get; set; } = PotionTimingOption.None;

    #endregion
    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        IsFirstCycle = true;
        return SongTimings switch
        {
            SongTiming.AdjustedStandard when remainTime <= 0 && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Standard or SongTiming.Custom or SongTiming.Cycle369 when remainTime <= 0 && WindbitePvE.CanUse(out var act) => act,
            _ => base.CountDownAction(remainTime)
        };
    }

    #endregion
    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return TryUseBarrage(out act)||
               TryUseEmpyrealArrow(out act) ||
               TryUseEmergencyHeartbreakShot(out act) ||
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
            if (IsFirstCycle && InArmys && SongEndAfter((float)(ArmyRemainTime + RecastTime)))
            {
                IsFirstCycle = false;
            }
            return TryUseOpener(out act) ||
                   TryUseRadiantFinale(out act) ||
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
        if (CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == false &&
            CurrentTarget.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == false)
        {
            return SetActToNull(out act);
        }

        if (IronJawsPvE.Target.Target?.WillStatusEnd(30, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) ?? false)
        {
            if (InBurst && Player.WillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale,
                    StatusID.RagingStrikes))
            {
                return IronJawsPvE.CanUse(out act, skipStatusProvideCheck:true);
            }
        }

        return IronJawsPvE.CanUse(out act) || SetActToNull(out act);
    }

        private bool TryUseDoTs(out IAction? act)
    {
        if (IronJawsPvE.EnoughLevel && CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true &&
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
            if (BurstShotPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.HawksEye_3861)) return true;
            if (HeavyShotPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.HawksEye_3861)) return true;

            return SetActToNull(out act);
        }

        #endregion
        #region oGCD Abilities
            #region Emergency Abilities
            private bool TryUseBarrage(out IAction? act)
                {
                    var hasRagingStrikes = Player.HasStatus(true, StatusID.RagingStrikes);
                    var willHaveEmpyrealArrowSoon = EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1);
                    var hasHawksEye = Player.HasStatus(true, StatusID.HawksEye_3861);
                    var empyrealArrowReady = EmpyrealArrowPvE.EnoughLevel && Repertoire == 3;

                    if (!hasRagingStrikes || willHaveEmpyrealArrowSoon || empyrealArrowReady || hasHawksEye)
                        return SetActToNull(out act);

                    return BarragePvE.CanUse(out act) || SetActToNull(out act);
                }
            private bool TryUseEmergencyHeartbreakShot(out IAction? act)
            {
                if (IsLastGCD(ActionID.WindbitePvE) && SongTimings == SongTiming.Cycle369) return HeartbreakShotPvE.CanUse(out act);

                if (!Player.HasStatus(true, StatusID.RagingStrikes))
                return SetActToNull(out act);

                return HeartbreakShotPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.35 || SetActToNull(out act);
            }
            private bool TryUseEmpyrealArrow(out IAction? act)
                         {
                             switch (SongTimings)
                             {
                                 case SongTiming.Standard or SongTiming.Custom:
                                     switch (IsFirstCycle)
                                     {
                                         case true when (TheWanderersMinuetPvE.Use() || !NoSong) && WeaponRemain <= RecastTime * 0.5 && WeaponRemain > RecastTime * 0.25:
                                             return EmpyrealArrowPvE.CanUse(out act, isFirstAbility: true);
                                         case false:
                                             return EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.25;
                                     }
                                     break;
                                 case SongTiming.AdjustedStandard:
                                     switch (Song)
                                     {
                                         case Song.Wanderer:
                                         {
                                             if (WeaponRemain < RecastTime * 0.5)
                                                 return EmpyrealArrowPvE.CanUse(out act);
                                             break;
                                         }
                                         case Song.Mage or Song.Army:
                                         {
                                             return EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.25;
                                         }
                                     }
                                     break;
                                 case SongTiming.Cycle369:
                                     switch (Song)
                                     {
                                         case Song.Wanderer:
                                             if (Player.HasStatus(true, StatusID.RagingStrikes) ||
                                                 IsLastAbility(ActionID.RagingStrikesPvE) || RagingStrikesPvE.Cooldown.IsCoolingDown)
                                                 return EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.5;
                                             break;
                                         case Song.Mage:
                                             if (SongEndAfter(MageRemainTime) && !IsFirstCycle)
                                             {
                                                 return ArmysPaeonPvE.CanUse(out act);
                                             }
                                             if (EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > RecastTime *0.25)
                                             {
                                                 return true;
                                             }
                                             break;
                                         case Song.Army:
                                             return EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.25;
                                     }
                                     break;
                             }

                             return SetActToNull(out act);
                         }
            #endregion
            #region Songs
            private bool TryUseWanderers(out IAction? act)
            {
                if (!TheWanderersMinuetPvE.EnoughLevel || IsLastAbility(ActionID.ArmysPaeonPvE) || IsLastAbility(ActionID.MagesBalladPvE) || InWanderers) return SetActToNull(out act);

                if (NoSong && IsFirstCycle)
                {
                    switch (SongTimings)
                    {
                        case SongTiming.Standard or SongTiming.AdjustedStandard: if (TheWanderersMinuetPvE.CanUse(out act)) return true;
                            break;
                        case SongTiming.Cycle369: if (WeaponRemain < RecastTime * 0.5) return TheWanderersMinuetPvE.CanUse(out act);
                            break;
                        case SongTiming.Custom:
                            switch (WanderersWeave)
                            {
                                case WandererWeave.Early:
                                    if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.5) return true;
                                    break;
                                case WandererWeave.Late:
                                    if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < RecastTime * 0.5) return true;
                                    break;
                            }

                            break;
                    }
                }
                if (!IsFirstCycle && InArmys && SongEndAfter(ArmyRemainTime) && (Song != Song.None || Player.HasStatus(true, StatusID.ArmysEthos)))
                    return TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain > RecastTime * 0.5;

                return SetActToNull(out act);
            }
            private bool TryUseMages(out IAction? act)
            {
                if (!InCombat || IsLastAbility(ActionID.ArmysPaeonPvE) || IsLastAbility(ActionID.TheWanderersMinuetPvE))
                {
                    return SetActToNull(out act);
                }

                if ((InWanderers && SongEndAfter(WandRemainTime) && (Repertoire == 0 || !HasHostilesInMaxRange)) ||
                    (InArmys && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown))
                {
                    return MagesBalladPvE.CanUse(out act) && WeaponRemain <= RecastTime * 0.35;
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
                        if ((InMages && SongEndAfter(MageRemainTime)) ||
                            (InWanderers && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown) ||
                            (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)))
                        {
                            return ArmysPaeonPvE.CanUse(out act) && WeaponRemain <= RecastTime * 0.5;
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
                                if (WeaponRemain < RecastTime * 0.5 && ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                            case false:
                                if (WeaponRemain > RecastTime * 0.5 && ArmysPaeonPvE.CanUse(out act)) return true;
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
                            !IsFirstCycle && InWanderers && RagingStrikesPvE.Cooldown.WillHaveOneChargeGCD(1))
                        {
                            return RadiantFinalePvE.CanUse(out act);
                        }

                        break;
                    case SongTiming.Cycle369:
                    {
                        if (IsFirstCycle && InWanderers && TargetHasDoTs && WeaponRemain < RecastTime * 0.5)
                            return RadiantFinalePvE.CanUse(out act);
                    }
                        if (!IsFirstCycle && InWanderers && SongTime < 45 - (2 * RecastTime + RecastTime * 0.5))
                            return RadiantFinalePvE.CanUse(out act);
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
                            if (IsFirstCycle && WeaponRemain <= RecastTime * 0.5 && !Player.HasStatus(true, StatusID.RadiantFinale) ||
                                !IsFirstCycle && (Player.HasStatus(true, StatusID.RadiantFinale)|| IsLastAbility(ActionID.RadiantFinalePvE)))
                                return BattleVoicePvE.CanUse(out act, isLastAbility: true);
                        }
                        break;
                    case SongTiming.Cycle369:
                        if (InWanderers)
                        {
                            if (IsFirstCycle && TargetHasDoTs && (Player.HasStatus(true, StatusID.RadiantFinale) || RadiantFinalePvE.Cooldown.IsCoolingDown))
                                return BattleVoicePvE.CanUse(out act);

                            if (!IsFirstCycle && (IsLastAbility(ActionID.RadiantFinalePvE) || Player.HasStatus(true, StatusID.RadiantFinale)))
                                return BattleVoicePvE.CanUse(out act) && WeaponRemain < RecastTime * 0.5;
                        }
                        break;
                }

                return SetActToNull(out act);
            }

            private bool TryUseRagingStrikes(out IAction? act)
            {
                if (Player.HasStatus(true, StatusID.BattleVoice, StatusID.RadiantFinale) && WeaponRemain < RecastTime * 0.45 ||
                    !RadiantFinalePvE.EnoughLevel || !BattleVoicePvE.EnoughLevel)
                    return RagingStrikesPvE.CanUse(out act);

                return SetActToNull(out act);
            }


            #endregion
            #region Attack Abilities
            private bool TryUseHeartBreakShot(out IAction? act)
            {
                var willHaveMaxCharges = BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 3);
                var willHave1ChargeInMages = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InMages;
                var willHave1ChargeInArmys = BloodletterPvE.Cooldown.WillHaveXCharges(1, 7.5f) && InArmys;

                if (NoSong && IsFirstCycle && EmpyrealArrowPvE.CanUse(out _) ||
                    InArmys && SongTime <= 35 && !willHaveMaxCharges ||
                    InMages && SongEndAfter((float)(MageRemainTime + RecastTime * 0.5))) return SetActToNull(out act);

                if (InBurst || willHaveMaxCharges || willHave1ChargeInMages|| willHave1ChargeInArmys && SongTime > 35 || Player.HasStatus(true,StatusID.Medicated) && WeaponRemain > RecastTime * 0.25)
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
                   return SidewinderPvE.CanUse(out act);

               return SetActToNull(out act);
            }
            private bool TryUsePitchPerfect(out IAction? act)
            {
                if (Song != Song.Wanderer) return SetActToNull(out act);

                if (SongEndAfter(WandRemainTime) && Repertoire > 0 && WeaponRemain > RecastTime * 0.45 && WeaponRemain < RecastTime * 0.55 || Repertoire == 3 || Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) && RadiantFinalePvE.Cooldown.IsCoolingDown)
                    return PitchPerfectPvE.CanUse(out act, skipAoeCheck:true, skipCastingCheck: true,skipComboCheck:true);

                return SetActToNull(out act);
            }
            #endregion
            #region Openers
            private bool TryUseOpener(out IAction? act)
            {
                if (!IsFirstCycle) return SetActToNull(out act);

                return SongTimings switch
                {
                    SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom =>
                        TryUseEmpyrealArrow(out act) || TryUseBattleVoice(out act) || TryUseRadiantFinale(out act) ||
                        TryUseRagingStrikes(out act) || TryUsePitchPerfect(out act) || TryUseHeartBreakShot(out act) ||
                        TryUseSideWinder(out act),
                    SongTiming.Cycle369 => TryUseHeartBreakShot(out act) || TryUseRadiantFinale(out act) ||
                                           TryUseBattleVoice(out act) || TryUseRagingStrikes(out act) ||
                                           TryUsePitchPerfect(out act) || TryUseEmpyrealArrow(out act) ||
                                           TryUseSideWinder(out act),
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


            ImGui.EndTable();
        }
    }

    #endregion
}