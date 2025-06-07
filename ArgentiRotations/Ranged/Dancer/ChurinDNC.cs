using System.ComponentModel;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2.5", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed partial class ChurinDNC : DancerRotation
{
    #region Properties
    #region Boolean Properties

    #region Action Use Booleans
    private bool ShouldUseLastDance { get; set;}
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled;
    private bool ShouldUseStarfallDance { get; set; }
    private bool ShouldUseSaberDance { get; set; }
    private bool ShouldUseTillana { get; set; }
    private bool ShouldUseStandardStep { get; set;}
    private bool ShouldUseFlourish { get; set;}

    #endregion
    #region Status Booleans
    private static bool DanceDance => Player.HasStatus(true, StatusID.Devilment, StatusID.TechnicalFinish) ||
                                      !Player.WillStatusEnd(0,true, StatusID.Devilment, StatusID.TechnicalFinish);
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated);
    private static bool HasProcs => Player.HasStatus(true, StatusID.SilkenFlow,StatusID.SilkenSymmetry,StatusID.FlourishingFlow, StatusID.FlourishingSymmetry) ||
                                    !Player.WillStatusEnd(0, true, StatusID.SilkenFlow, StatusID.SilkenSymmetry, StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => Player.HasStatus(true, StatusID.FinishingMoveReady) ||
                                            !Player.WillStatusEnd(0,true, StatusID.FinishingMoveReady);
    private static bool HasTillana => Player.HasStatus(true, StatusID.FlourishingFinish) ||
                                      !Player.WillStatusEnd(0, true, StatusID.FlourishingFinish);
    private static bool HasStarfall => HasFlourishingStarfall || Player.HasStatus(true, StatusID.FlourishingStarfall);
    private static bool HasLastDanceReady => HasLastDance || Player.HasStatus(true, StatusID.LastDanceReady);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15) || CurrentTarget?.DistanceToPlayer() <= 15;
    private static bool ShouldSwapDancePartner => CurrentDancePartner != null && (CurrentDancePartner.HasStatus(true, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath,
                                            StatusID.DamageDown_2911) || CurrentDancePartner.IsDead);
    private bool HoldTechStepForTargets => HoldTechForTargets && AreDanceTargetsInRange;
    private bool HoldStandardStepForTargets => HoldStandardForTargets && AreDanceTargetsInRange;
    private bool ShouldHoldForTechStep => ShouldUseTechStep && (HoldTechStepForTargets || !HoldTechForTargets) && !IsDancing && !TillanaPvE.CanUse(out _) &&
                                          (TechnicalStepPvE.Cooldown.HasOneCharge || TechnicalStepIn(1.5f));
    private bool ShouldHoldForStandard => StandardStepPvE.IsEnabled && !HasLastDanceReady && (HoldStandardStepForTargets || !HoldStandardForTargets) && !IsDancing && !ShouldHoldForTechStep
                                          && (StandardStepPvE.Cooldown.HasOneCharge || StandardStepIn(1.5f) && (DanceDance && Esprit < 70 || !DanceDance && Esprit < 70));
    private bool StandardStepIn(float remainTime)
    {
        return StandardStepPvE.Cooldown is { HasOneCharge: false, IsCoolingDown: true } &&
               StandardStepPvE.Cooldown.RecastTimeElapsed >= 30 - remainTime &&
               StandardStepPvE.IsEnabled;
    }
    private bool TechnicalStepIn(float remainTime)
    {
        return TechnicalStepPvE.Cooldown is { HasOneCharge: false, IsCoolingDown: true } &&
               TechnicalStepPvE.Cooldown.RecastTimeElapsed >= 120 - remainTime &&
               ShouldUseTechStep;
    }
    #endregion
    #region Potion Booleans
    private bool FirstPotionUsed { get; set; }
    private bool SecondPotionUsed { get; set; }
    private bool ThirdPotionUsed { get; set; }

    private static bool OddMinutePotion(int potionTime) => potionTime % 2 == 1;
    private static bool EvenMinutePotion(int potionTime) => potionTime % 2 == 0;

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
    #endregion
    #endregion
    #region Other Properties

    private const float DefaultAnimationLock = 0.6f;

    private DateTime _lastPotionUsed = DateTime.MinValue;

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


    #endregion
    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    private bool HoldTechForTargets { get; set; } = true;
    [RotationConfig(CombatType.PvE, Name = "Holds Tech Finish if no targets in range (Warning, will drift)")]
    private bool HoldTechFinishForTargets { get; set; } = true;
    [RotationConfig(CombatType.PvE, Name = "Hold Standard Step if no targets in range (Warning, will drift)")]
    private bool HoldStandardForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold Standard Finish if no targets in range (Warning, will drift)")]
    private bool HoldStandardFinishForTargets { get; set; } = true;

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
    [Range(0,1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Finish?")]
    private float OpenerFinishTime { get; set; } = 0.5f;

    #endregion

    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime > 15) return base.CountDownAction(remainTime);
        if (TryUseClosedPosition(out var act) ||
            StandardStepPvE.CanUse(out act) ||
            ExecuteStepGCD(out act) ||
            remainTime <= 1 && EnableFirstPotion && FirstPotionTime == 0 && UseBurstMedicine(out act) ||
            remainTime <= OpenerFinishTime && DoubleStandardFinishPvE.CanUse(out act))
        {
            return act;
        }
        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (TryUsePots(out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (!ShouldHoldForStandard || !ShouldHoldForTechStep)
            return base.EmergencyAbility(nextGCD, out act);

        return SetActToNull(out act);
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsDancing || WeaponRemain <= DefaultAnimationLock) return SetActToNull(out act);
        return TryUseFlourish(out act) ||
               TryUseFeathers(out act) ||
               base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        UpdatePotionUse();
        if (IsDancing) return TryFinishTheDance(out act);
        if (TryHoldGCD(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (TryUseTechGCD(out act, HasDevilment || DanceDance)) return true;
        return TryUseFillerGCD(out act) ||
               base.GeneralGCD(out act);
    }

    #endregion

    #region Extra Methods

    #region Dance Partner Logic

    private bool TryUseClosedPosition(out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.ClosedPosition) || !PartyMembers.Any() || !ClosedPositionPvE.IsEnabled)
            return SetActToNull(out act);

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        if (!Player.HasStatus(true, StatusID.ClosedPosition) || !ShouldSwapDancePartner || !ClosedPositionPvE.IsEnabled)
            return SetActToNull(out act);

        // Check cooldown conditions
        if ((StandardStepIn(3) || TechnicalStepIn(3)) && ShouldSwapDancePartner)
        {
            return EndingPvE.CanUse(out act);
        }
        return SetActToNull(out act);
    }

    #endregion

    #region Dance Logic
    private bool TryHoldGCD(out IAction? act)
    {

        if (ShouldHoldForTechStep) return TryUseTechnicalStep(out act);
        if (ShouldHoldForStandard) return TryUseStandardStep(out act) ||
                                          TryUseFinishingMove(out act);
        return SetActToNull(out act);
    }
    private bool TryUseTechnicalStep(out IAction? act)
    {
        if (!InCombat || HoldTechStepForTargets || IsDancing || !ShouldUseTechStep)
        {
            return SetActToNull(out act);
        }

        return TechnicalStepPvE.CanUse(out act);
    }
    private bool TryFinishTheDance(out IAction? act)
    {
        if (!IsDancing) return SetActToNull(out act);

        if (HasStandardStep)
        {
            var shouldFinish = CompletedSteps == 2 && (!HoldStandardFinishForTargets || HoldStandardFinishForTargets && AreDanceTargetsInRange);
            var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

            if (shouldFinish || aboutToTimeOut)
            {
                return DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true);
            }
        }

        if (HasTechnicalStep)
        {
            var shouldFinish = CompletedSteps == 4 &&
                               (!HoldTechFinishForTargets || HoldTechFinishForTargets && AreDanceTargetsInRange);
            var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.TechnicalStep);

            if (shouldFinish || aboutToTimeOut)
            {
                return QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true);
            }
        }

        return ExecuteStepGCD(out act);
    }
    private bool TryUseStandardStep(out IAction? act)
    {
        if (IsDancing || HoldStandardStepForTargets || DanceDance || HasLastDanceReady) return SetActToNull(out act);

        if (HasFinishingMove) return TryUseFinishingMove(out act);

        if (InCombat && TechnicalStepIn(5))
        {
            ShouldUseStandardStep = false;
        }
        else if (!HasStandardFinish || StandardStepIn(5))
        {
            ShouldUseStandardStep = true;
        }

        return ShouldUseStandardStep ? StandardStepPvE.CanUse(out act) : SetActToNull(out act);
    }
    private bool TryUseFinishingMove(out IAction? act)
    {
        if (!HasFinishingMove || HoldStandardFinishForTargets && !AreDanceTargetsInRange ||
           HasLastDanceReady)
        {
            return SetActToNull(out act);
        }

        return FinishingMovePvE.CanUse(out act);
    }

    #endregion

    #region Burst Logic
    private bool TryUseTechGCD(out IAction? act, bool burst)
    {
        if (!burst || IsDancing) return SetActToNull(out act);
        if (ShouldHoldForStandard) return StandardStepPvE.CanUse(out act);

        return TryUseDanceOfTheDawn(out act) ||
               TryUseTillana(out act) ||
               TryUseLastDance(out act) ||
               TryUseFinishingMove(out act) ||
               TryUseStarfallDance(out act) ||
               TryUseSaberDance(out act) ||
               TryUseFillerGCD(out act) ||
               base.GeneralGCD(out act);
    }
    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        return Esprit < 50 ? SetActToNull(out act) : DanceOfTheDawnPvE.CanUse(out act);
    }
    private bool TryUseTillana(out IAction? act)
    {
        if (!HasTillana) return SetActToNull(out act);
        if (Esprit > 50 || HasLastDanceReady && StandardStepIn(5))
        {
            ShouldUseTillana = false;
        }
        else if (Esprit <= 30 && (!StandardStepIn(5) || !HasLastDanceReady))
        {
            ShouldUseTillana = true;
        }
        return ShouldUseTillana ? TillanaPvE.CanUse(out act) : SetActToNull(out act);
    }
    private bool TryUseLastDance(out IAction? act)
    {
        if (!HasLastDanceReady) return SetActToNull(out act);

        if (TechnicalStepIn(15) || DanceDance && Esprit >= 70 && !StandardStepIn(5))
        {
            ShouldUseLastDance = false;
        }
        else if (StandardStepIn(5) || HasLastDanceReady)
        {
            ShouldUseLastDance = true;
        }

        return ShouldUseLastDance ? LastDancePvE.CanUse(out act) : SetActToNull(out act);
    }
    private bool TryUseStarfallDance(out IAction? act)
    {
        if (!HasStarfall) return SetActToNull(out act);

        if (HasLastDanceReady && HasFinishingMove ||
            Esprit > 70 && DevilmentPvE.Cooldown.RecastTimeElapsed < 5)
        {
            ShouldUseStarfallDance = false;
        }
        else if ((!HasLastDanceReady || !HasFinishingMove) && Esprit < 70 || DevilmentPvE.Cooldown.RecastTimeElapsed > 5)
        {
            ShouldUseStarfallDance = true;
        }

        return ShouldUseStarfallDance ? StarfallDancePvE.CanUse(out act) : SetActToNull(out act);
    }
    #endregion

    #region GCD Skills
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (ShouldHoldForStandard) return StandardStepPvE.CanUse(out act);
        if (ShouldHoldForTechStep) return TechnicalStepPvE.CanUse(out act);

        return TryUseTillana(out act) ||
               TryUseProcs(out act) ||
               FeatherGCDHelper(out act) ||
               TryUseLastDance(out act) ||
               TryUseSaberDance(out act) ||
               TryUseBasicGCD(out act) ||
               base.GeneralGCD(out act);
    }
    private bool TryUseBasicGCD(out IAction? act)
    {
        if (ShouldHoldForStandard) return StandardStepPvE.CanUse(out act);
        if (ShouldHoldForTechStep) return TechnicalStepPvE.CanUse(out act);
        if (DanceDance && Esprit >= 50 && (!HasLastDanceReady || HasLastDanceReady && !HasFinishingMove)) return SaberDancePvE.CanUse(out act);

        return  BloodshowerPvE.CanUse(out act) ||
                FountainfallPvE.CanUse(out act) ||
                RisingWindmillPvE.CanUse(out act) ||
                ReverseCascadePvE.CanUse(out act) ||
                BladeshowerPvE.CanUse(out act) ||
                FountainPvE.CanUse(out act) ||
                WindmillPvE.CanUse(out act) ||
                CascadePvE.CanUse(out act) ||
                base.GeneralGCD(out act);
    }
    private bool FeatherGCDHelper(out IAction? act)
    {

        if (Feathers <= 3) return SetActToNull(out act);


        var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                         Player.HasStatus(true, StatusID.SilkenSymmetry);
        var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) ||
                              Player.HasStatus(true, StatusID.FlourishingSymmetry);

        if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < 50 && !DanceDance)
        {
            if (FountainPvE.CanUse(out act)) return true;

            if (CascadePvE.CanUse(out act)) return true;
        }

        if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > 50)
        {
            return SaberDancePvE.CanUse(out act);
        }

        return SetActToNull(out act);
    }
    private bool TryUseSaberDance(out IAction? act)
    {
        if (Esprit < 50) return SetActToNull(out act);

        if (DanceDance && HasLastDanceReady && StandardStepIn(5) && HasFinishingMove ||
            !DanceDance && Esprit < 70)
        {
            ShouldUseSaberDance = false;
        }
        else if (DanceDance && (!HasLastDanceReady || !HasFinishingMove) && Esprit >= 50|| IsMedicated ||
                 !DanceDance && Esprit >= 70)
        {
            ShouldUseSaberDance = true;
        }
        return ShouldUseSaberDance ? SaberDancePvE.CanUse(out act) : SetActToNull(out act);
    }

    private bool TryUseProcs(out IAction? act)
    {
        if (DanceDance || ShouldHoldForTechStep || !ShouldUseTechStep) return SetActToNull(out act);

        var gcdsUntilTechStep = 0;
        if (TechnicalStepPvE.Cooldown.IsCoolingDown)
        {
            if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(1, 0.5f))
            {
                gcdsUntilTechStep = 1;
            }
            else if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(2, 0.5f))
            {
                gcdsUntilTechStep = 2;
            }
            else if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(3, 0.5f))
            {
                gcdsUntilTechStep = 3;
            }
            else if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(4, 0.5f))
            {
                gcdsUntilTechStep = 4;
            }
            else if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(5,0.5f))
            {
                gcdsUntilTechStep = 5;
            }
            else
            {
                gcdsUntilTechStep = 0;
            }
        }

        if (gcdsUntilTechStep > 0)
        {
            switch (gcdsUntilTechStep)
            {
                case 5:
                case 4:
                    if (!HasProcs || HasProcs && Esprit < 90)
                        return TryUseBasicGCD(out act);
                    if (Esprit >= 90)
                        return SaberDancePvE.CanUse(out act);
                    break;
                case 3:
                    return (HasProcs && Esprit < 90) switch
                    {
                        false => FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act),
                        true => TryUseBasicGCD(out act)
                    };
                case 2:
                    if (Esprit >= 90)
                        return SaberDancePvE.CanUse(out act);
                    if (HasProcs && Esprit < 90)
                        return TryUseBasicGCD(out act);
                    if (FountainPvE.CanUse(out act) && Esprit < 90 && !HasProcs)
                        return true;
                    break;
                case 1:
                    switch (HasProcs)
                    {
                        case true when Esprit < 90:
                            return TryUseBasicGCD(out act);
                        case false when Esprit < 90 && FountainPvE.CanUse(out act):
                            return true;
                        case false when Esprit >= 50 && !FountainPvE.CanUse(out _):
                            return SaberDancePvE.CanUse(out act);
                        case false when Esprit < 50 && !FountainPvE.CanUse(out _):
                            return LastDancePvE.CanUse(out act);
                    }
                    break;
            }
        }
        return SetActToNull(out act);
    }

    #endregion

    #region OGCD Abilities

    /// <summary>
    ///     Determines whether the Devilment action can be used after the Technical Finish status is active.
    /// </summary>
    /// <param name="act">The action to be performed if Devilment can be used.</param>
    /// <returns>
    ///     <c>true</c> if the Devilment action can be used; otherwise, <c>false</c>.
    /// </returns>
    private bool TryUseDevilment(out IAction? act)
    {
        if (HasTechnicalFinish || IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE))
        {
            return DevilmentPvE.CanUse(out act);
        }
        return SetActToNull(out act);
    }

    /// <summary>
    ///     Handles the logic for using the Flourish action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
    private bool TryUseFlourish(out IAction? act)
    {
        if (!InCombat || HasThreefoldFanDance || !FlourishPvE.IsEnabled) return SetActToNull(out act);

        if (!DanceDance && TechnicalStepPvE.Cooldown.HasOneCharge ||
            !DanceDance && TechnicalStepIn(40) || ShouldHoldForTechStep)
        {
            ShouldUseFlourish = false;
        }
        else if (DanceDance || !DanceDance && (!ShouldUseTechStep && !TechnicalStepPvE.Cooldown.HasOneCharge ||
                 !ShouldUseTechStep && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(30)))
        {
            ShouldUseFlourish = true;
        }

        return ShouldUseFlourish ? FlourishPvE.CanUse(out act) : SetActToNull(out act);
    }

    /// <summary>
    /// Determines whether feathers should be used based on the next GCD action and current player status.
    /// </summary>
    /// <param name="act"> The action to be performed, if any.</param>
    /// <returns>True if a feather action was performed; otherwise, false.</returns>
    private bool TryUseFeathers(out IAction? act)
    {
        var hasEnoughFeathers = Feathers > 3;
        var hasThreefoldFanDance = Player.HasStatus(true, StatusID.ThreefoldFanDance);
        var hasFourfoldFanDance = Player.HasStatus(true, StatusID.FourfoldFanDance);

        if (Feathers == 4 && HasProcs)
        {
            if (hasThreefoldFanDance) return FanDanceIiiPvE.CanUse(out act);
            if (FanDanceIiPvE.CanUse(out act) || FanDancePvE.CanUse(out act)) return true;
        }

        if (hasFourfoldFanDance) return FanDanceIvPvE.CanUse(out act);
        if (hasThreefoldFanDance) return FanDanceIiiPvE.CanUse(out act);
        if (DanceDance || (hasEnoughFeathers && HasProcs && !ShouldHoldForTechStep) || IsMedicated)
        {
            return FanDanceIiPvE.CanUse(out act) ||
                   FanDancePvE.CanUse(out act);
        }
        return SetActToNull(out act);
    }

    private bool TryUsePots(out IAction? act)
    {
        if (HasTechnicalStep && IsDancing && CompletedSteps is 0 or 4)
        {
            if (EvenMinutePotion(FirstPotionTime)) return FirstPot(out act);
            if (EvenMinutePotion(SecondPotionTime)) return SecondPot(out act);
            if (EvenMinutePotion(ThirdPotionTime)) return ThirdPot(out act);
        }

        if (FlourishPvE.Cooldown is { IsCoolingDown: true, HasOneCharge: false } && FlourishPvE.Cooldown.WillHaveOneCharge(3) || StandardStepIn(10))
        {
            if (OddMinutePotion(FirstPotionTime)) return FirstPot(out act);
            if (OddMinutePotion(SecondPotionTime)) return SecondPot(out act);
            if (OddMinutePotion(ThirdPotionTime)) return ThirdPot(out act);
        }
        return SetActToNull(out act);
    }

    private bool FirstPot(out IAction? act)
    {
        if (FirstPotionUsed) return SetActToNull(out act);
        var firstPotionTime = FirstPotionTime * 60;

        switch (EnableFirstPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime >= firstPotionTime - 5 ||
                           firstPotionTime == 0:
                _lastPotionUsed = DateTime.Now;
                FirstPotionUsed = true;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }

    private bool SecondPot(out IAction? act)
    {
        if (SecondPotionUsed)  return SetActToNull(out act);
        var secondPotionTime = SecondPotionTime * 60;
        switch (EnableSecondPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime >= secondPotionTime - 5 &&
                           (DateTime.Now - _lastPotionUsed).TotalSeconds >= 270:
                _lastPotionUsed = DateTime.Now;
                SecondPotionUsed = true;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }

    private bool ThirdPot(out IAction? act)
    {
        if (ThirdPotionUsed) return SetActToNull(out act);
        var thirdPotionTime = ThirdPotionTime * 60;
        switch (EnableThirdPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime >= thirdPotionTime - 5 &&
                           (DateTime.Now - _lastPotionUsed).TotalSeconds >= 270:
                ThirdPotionUsed = true;
                _lastPotionUsed = DateTime.Now;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }

    private void UpdatePotionUse()
    {
        if (CombatTime == 0 || !InCombat)
        {
            FirstPotionUsed = false;
            SecondPotionUsed = false;
            ThirdPotionUsed = false;
            _lastPotionUsed = DateTime.MinValue;
        }
    }


    private static bool SetActToNull(out IAction? act)
    {
        act = null;
        return false;
    }

    #endregion

    #endregion
}
