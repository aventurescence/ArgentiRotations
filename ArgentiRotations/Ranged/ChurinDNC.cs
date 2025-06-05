using System.ComponentModel;
using ArgentiRotations.Common;
using ArgentiRotations.Encounter.StateMachine;
using Dalamud.Interface.Colors;


namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2.5", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDNC : DancerRotation
{
    #region Properties

    #region Boolean Properties
    private bool ShouldUseLastDance { get; set; } = true;
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled;
    private bool ShouldUseStandardStep { get; set; }
    private static bool ShouldUseFlourish { get; set; }

    private static bool DanceDance => Player.HasStatus(true, StatusID.Devilment, StatusID.TechnicalFinish );
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15) || CurrentTarget?.DistanceToPlayer() <= 15;
    private static bool HasProcs => Player.HasStatus(true, StatusID.SilkenFlow,StatusID.SilkenSymmetry,StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);


    private bool FirstPotionUsed { get; set; } = false;
    private bool SecondPotionUsed { get; set; } = false;
    private bool ThirdPotionUsed { get; set; } = false;
    private static bool OddMinutePotion(int potionTime) => potionTime % 2 == 1;
    private static bool EvenMinutePotion(int potionTime) => potionTime % 2 == 0;

    private static bool IsWithinFirst15SecondsOfOddMinute()
    {
        if (CombatTime <= 0.0)
            return false;
        var num1 = (int) Math.Floor(CombatTime / 60.0);
        var num2 = (int) Math.Floor(CombatTime % 60.0);
        return num1 % 2 == 1 && num2 < 15;    }
    #endregion

    #region Other Properties

    private const float DefaultAnimationLock = 0.6f;
    private DateTime _lastPotionUsed = DateTime.MinValue;
    // StateMachine for encounter tracking
    private ArgentiStateMachine? StateMachine { get; set; }

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

    #endregion

    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        ShouldUseFlourish = false;

        if (remainTime >= 15) return base.CountDownAction(remainTime);
        if (TryUseClosedPosition(out var act) ||
            StandardStepPvE.CanUse(out act) ||
            ExecuteStepGCD(out act) || remainTime <= 1 && EnableFirstPotion && FirstPotionTime == 0 && UseBurstMedicine(out act)
            || remainTime <= 0 && DoubleStandardFinishPvE.CanUse(out act))
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
        CheckDancePartnerStatus();
        if (TryUsePots(out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (!IsDancing && !(StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118)))
            return base.EmergencyAbility(nextGCD, out act);

        return SetActToNull(out act);
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsDancing ||
            WeaponRemain <= DefaultAnimationLock ||
            ShouldHoldForStandardStep() ||
            ShouldHoldForTechnicalStep())
        {
            return SetActToNull(out act);
        }

        return TryUseFlourish(out act) ||
               TryUseFeathers(out act) ||
               base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        RotationDebugManager.CheckAndClearLogs();
        UpdatePotionUse();

        if (IsDancing)
        {
            RotationDebugManager.CurrentGCDEvaluation = "Dancing: TryFinishTheDance or ExecuteStepGCD";
            return TryFinishTheDance(out act) || ExecuteStepGCD(out act);
        }

        if (TryHoldGCD(out act))
        {
            RotationDebugManager.CurrentGCDEvaluation = "TryHoldGCD";
            return true;
        }

        if (TryUseProcs(out act))
        {
            RotationDebugManager.CurrentGCDEvaluation = "TryUseProcs";
            return true;
        }

        if (TryUseTechGCD(out act, Player.HasStatus(true, StatusID.Devilment)))
        {
            RotationDebugManager.CurrentGCDEvaluation = "TryUseTechGCD";
            return true;
        }

        if (TryUseFillerGCD(out act))
        {
            RotationDebugManager.CurrentGCDEvaluation = "TryUseFillerGCD";
            return true;
        }

        RotationDebugManager.CurrentGCDEvaluation = "No Valid GCD Found";
        return base.GeneralGCD(out act);
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
        if (!Player.HasStatus(true, StatusID.ClosedPosition) || !CheckDancePartnerStatus() || !ClosedPositionPvE.IsEnabled)
            return SetActToNull(out act);

        var standardOrFinishingCharge =
            (StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneChargeGCD(2, 0.5f)) ||
            (FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(2, 0.5f));

        // Check cooldown conditions
        if (standardOrFinishingCharge && CheckDancePartnerStatus())
        {
            return EndingPvE.CanUse(out act);
        }
        return SetActToNull(out act);
    }

    private static bool CheckDancePartnerStatus()
    {
        if (CurrentDancePartner != null)
        {
            return CurrentDancePartner.HasStatus(true, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath,
                       StatusID.DamageDown_2911) ||
                   CurrentDancePartner.IsDead;

        }
        return false;
    }

    #endregion

    #region Dance Logic

    private bool TryHoldGCD(out IAction? act)
    {

        if (ShouldHoldForTechnicalStep())
        {
            return TryUseTechnicalStep(out act);
        }

        if (ShouldHoldForStandardStep())
        {
                return TryUseStandardStep(out act) ||
                       TryUseFinishingMove(out act);
        }

        return SetActToNull(out act);
    }

    private bool ShouldHoldForTechnicalStep()
    {
        if (IsDancing || !ShouldUseTechStep)
        {
            return false;
        }

        var techWillHaveOneCharge = TechnicalStepPvE.Cooldown is { IsCoolingDown: true, RecastTimeRemainOneCharge: < 1 } ||
                                   TechnicalStepPvE.Cooldown.HasOneCharge;
        var holdForTarget = HoldTechForTargets && AreDanceTargetsInRange;

        var result = techWillHaveOneCharge && ShouldUseTechStep &&
                     (holdForTarget || !HoldTechForTargets) && (!HasTechnicalFinish || !HasTechnicalStep);

        return result;
    }

    private bool ShouldHoldForStandardStep()
    {
        var standardReady = !DanceDance && !Player.HasStatus(true, StatusID.FinishingMoveReady) &&
                                                (StandardStepPvE.Cooldown is { IsCoolingDown: true, RecastTimeRemainOneCharge: < 1 } ||
                                                StandardStepPvE.Cooldown.HasOneCharge);

        var finishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady) &&
                                    (FinishingMovePvE.Cooldown is { IsCoolingDown: true, RecastTimeRemainOneCharge: < 1  } ||
                                    FinishingMovePvE.Cooldown.HasOneCharge);

        var inBurstWithHighEsprit = DanceDance && Esprit >= 80;
        var holdForTarget = HoldStandardForTargets && AreDanceTargetsInRange;

        if (standardReady && !IsDancing && (holdForTarget || !HoldStandardForTargets)) return true;

        return finishingMoveReady && !inBurstWithHighEsprit && !IsDancing &&
               (holdForTarget || !HoldStandardForTargets) && (!ShouldHoldForTechnicalStep() || TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15)) ;
    }

    private bool TryUseTechnicalStep(out IAction? act)
    {
        if (!InCombat || (HoldTechForTargets && !AreDanceTargetsInRange) ||
        IsDancing || !ShouldUseTechStep)
        {
            return SetActToNull(out act);
        }

        return TechnicalStepPvE.CanUse(out act);
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        if (!Player.HasStatus(true, StatusID.StandardStep, StatusID.TechnicalStep) || !IsDancing)
        {
            return SetActToNull(out act);
        }

        if (IsDancing)
        {
            if (Player.HasStatus(true, StatusID.StandardStep))
            {
                var shouldFinish = CompletedSteps == 2 && (!HoldStandardFinishForTargets || HoldStandardFinishForTargets && AreDanceTargetsInRange);
                var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

                if (shouldFinish || aboutToTimeOut)
                {
                   return DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true);
                }
            }

            if (Player.HasStatus(true, StatusID.TechnicalStep))
            {
                var shouldFinish = CompletedSteps == 4 &&
                                   (!HoldTechFinishForTargets || HoldTechFinishForTargets &&AreDanceTargetsInRange);
                var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.TechnicalStep);

                if (shouldFinish || aboutToTimeOut)
                {
                    return QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true);
                }
            }
        }

        return ExecuteStepGCD(out act);
    }

    private bool TryUseStandardStep(out IAction? act)
    {
        var earlyExit = IsDancing || (HoldStandardForTargets && !AreDanceTargetsInRange) || DanceDance;

        if (earlyExit)
        {
            return SetActToNull(out act);
        }

        var techStepWillBeReadySoon = (TechnicalStepPvE.Cooldown.IsCoolingDown &&
                                       TechnicalStepPvE.Cooldown.WillHaveOneCharge(5) ||
                                       TechnicalStepPvE.Cooldown.HasOneCharge) && ShouldUseTechStep;

        if (techStepWillBeReadySoon)
        {
            ShouldUseStandardStep = false;
        }
        else if (!Player.WillStatusEnd(0, true, StatusID.StandardFinish) ||
                 !Player.HasStatus(true, StatusID.StandardFinish))
        {
            ShouldUseStandardStep = true;
        }

        return ShouldUseStandardStep ? StandardStepPvE.CanUse(out act) : SetActToNull(out act);

    }

    private bool TryUseFinishingMove(out IAction? act)
    {

        if (!Player.HasStatus(true, StatusID.FinishingMoveReady) ||
            (HoldStandardForTargets && !AreDanceTargetsInRange) ||
            Player.HasStatus(true, StatusID.LastDanceReady))
        {
            return SetActToNull(out act);
        }

        return FinishingMovePvE.CanUse(out act);
    }

    #endregion

    #region Burst Logic

   private bool TryUseTechGCD(out IAction? act, bool burst)
    {
        if (!burst || IsDancing)
        {
            return SetActToNull(out act);
        }

        if (ShouldHoldForStandardStep())
        {
            return TryHoldGCD(out act);
        }

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
    var hasFlourishingFinish = Player.HasStatus(true, StatusID.FlourishingFinish);

    if (!hasFlourishingFinish)
    {
        return SetActToNull(out act);
    }

    var tillanaEnding = Player.HasStatus(true, StatusID.FlourishingFinish) && Player.WillStatusEnd(3, true, StatusID.FlourishingFinish);
    var hasFinishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady);
    var finishingMoveWillBeReady = FinishingMovePvE.Cooldown is { IsCoolingDown: true, RecastTimeRemainOneCharge: < 3 };
    var burstEnding = DanceDance && Player.WillStatusEnd(3, true, StatusID.TechnicalFinish, StatusID.Devilment);
    var finishingMoveCooling = FinishingMovePvE.Cooldown.IsCoolingDown && !hasFinishingMoveReady;
    var starfallDanceEnding = Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall);

    if (TillanaPvE.CanUse(out act))
    {
        if (Esprit <= 30 && (hasFinishingMoveReady && !finishingMoveWillBeReady || !hasFinishingMoveReady)) return true;
        if (hasFinishingMoveReady && finishingMoveWillBeReady && Esprit >= 40 && !tillanaEnding && !burstEnding) return false;
        if (tillanaEnding) return true;
        if ((burstEnding || !DanceDance) && Esprit < 50) return true;
        if (finishingMoveCooling && Esprit < 50 && ((HasFlourishingStarfall && !starfallDanceEnding)|| !HasFlourishingStarfall)) return true;
    }
    return SetActToNull(out act);
}

    private bool TryUseLastDance(out IAction? act)
{
    var hasLastDanceReady = Player.HasStatus(true, StatusID.LastDanceReady);

    if (!hasLastDanceReady)
    {
        return SetActToNull(out act);
    }

    var techWillHaveCharge =
        TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(15) &&
        !TillanaPvE.CanUse(out act) || ShouldHoldForTechnicalStep() || TechnicalStepPvE.CanUse(out _);
    var standardOrFinishingMoveReady =
        (StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneCharge(5.5f) && ShouldUseStandardStep) ||
        (Player.HasStatus(true, StatusID.FinishingMoveReady) &&
        FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(5.5f)) ||
        StandardFinishPvE.Cooldown.HasOneCharge || FinishingMovePvE.Cooldown.HasOneCharge;
    var espritHighCondition = Player.HasStatus(true, StatusID.FinishingMoveReady) &&Esprit >= 70 ||
                              !Player.HasStatus(true, StatusID.FinishingMoveReady) && Esprit >= 50;

    if (techWillHaveCharge || DanceDance && espritHighCondition && !standardOrFinishingMoveReady)
    {
        ShouldUseLastDance = false;
    }
    else if (hasLastDanceReady || standardOrFinishingMoveReady)
    {
        ShouldUseLastDance = true;
    }

    return ShouldUseLastDance ? LastDancePvE.CanUse(out act) : SetActToNull(out act);
}

    private bool TryUseStarfallDance(out IAction? act)
{
    if (!HasFlourishingStarfall)
    {
        return SetActToNull(out act);
    }

    var willEndSoon = Player.HasStatus(true, StatusID.FlourishingStarfall) && Player.WillStatusEndGCD(2,0.5f, true, StatusID.FlourishingStarfall);
    var hasLowEsprit = Esprit < 80;
    var finishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady) && FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(1.5f) ;

    var shouldUse = false;
    if ((willEndSoon || hasLowEsprit) && !finishingMoveReady)
    {
        shouldUse = true;
    }
    else if (finishingMoveReady)
    {
        shouldUse = false;
    }

    return shouldUse ? StarfallDancePvE.CanUse(out act) : SetActToNull(out act);
}
    #endregion

    #region GCD Skills
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (ShouldHoldForStandardStep() || ShouldHoldForTechnicalStep())
        {
            return TryHoldGCD(out act);
        }
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
        if (ShouldHoldForStandardStep() || ShouldHoldForTechnicalStep())
        {
            return TryHoldGCD(out act);
        }

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

    if (Feathers <= 3)
    {
       return SetActToNull(out act);
    }

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

    if (Esprit < 50)
    {
        return SetActToNull(out act);
    }

    // Log status checks
    var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry,
        StatusID.FlourishingFlow, StatusID.FlourishingSymmetry) || CascadePvE.CanUse(out _);

    var shouldHoldForTech = ShouldHoldForTechnicalStep();
    var techStepSoon = TechnicalStepPvE.Cooldown.WillHaveOneCharge(5.5f) &&
                       !TechnicalFinishPvE.Cooldown.HasOneCharge;
    var espritOutOfBurst = Esprit >= 50 && techStepSoon && !hasProcs && !shouldHoldForTech;
    var hasTillana = Player.HasStatus(true, StatusID.FlourishingFinish);

    // Check for priority abilities
    var hasLastDanceReady = Player.HasStatus(true, StatusID.LastDanceReady);
    var hasStarfall = Player.HasStatus(true, StatusID.FlourishingStarfall);
    var starfallEnding = hasStarfall && Player.HasStatus(true, StatusID.FlourishingStarfall) &&
                         Player.WillStatusEndGCD(2, 0.5f, true, StatusID.FlourishingStarfall);
    var finishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady) &&
                             (FinishingMovePvE.Cooldown.IsCoolingDown || StandardStepPvE.Cooldown.IsCoolingDown) &&
                             FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(2, 0.5f);

    // Get initial result
    var saberDanceCanUse = SaberDancePvE.CanUse(out act);
    // Decision logic
    if (saberDanceCanUse)
    {
        bool shouldUse;
        switch (DanceDance)
        {
            case true when hasLastDanceReady && finishingMoveReady && Esprit < 70 || hasStarfall && starfallEnding:
                shouldUse = false;
                break;
            case true when Esprit >= 50:
            case false when espritOutOfBurst:
            case false when Esprit >= 70:
            case false when (hasTillana || IsMedicated) && Esprit >= 50:
                shouldUse = true;
                break;
            default:
                shouldUse = false;
                break;
        }

        return SaberDancePvE.CanUse(out act) && shouldUse;
    }

    return SetActToNull(out act);
}

    private bool TryUseProcs(out IAction? act)
    {
        if (DanceDance || ShouldHoldForTechnicalStep() || !ShouldUseTechStep) return SetActToNull(out act);

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
        if ((Player.HasStatus(true, StatusID.TechnicalFinish) ||
             IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE)) && DevilmentPvE.CanUse(out act))
            return true;

        return SetActToNull(out act);
    }

    /// <summary>
    ///     Handles the logic for using the Flourish action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
    private bool TryUseFlourish(out IAction? act)
    {
        if (!InCombat || Player.HasStatus(true, StatusID.ThreefoldFanDance))
        {
            return SetActToNull(out act);
        }

        // Check if Flourish can be used based on the current status and conditions
        if (DanceDance || !TechnicalStepPvE.IsEnabled || !DanceDance && TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(30))
        {
            ShouldUseFlourish = true;
        }
        else if (!DanceDance && FlourishPvE.CanUse(out _) && TechnicalStepPvE.IsEnabled && TechnicalStepPvE.Cooldown.HasOneCharge && !DevilmentPvE.Cooldown.IsCoolingDown)
        {
            ShouldUseFlourish = false;
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
        if (DanceDance || (hasEnoughFeathers && HasProcs && !ShouldHoldForTechnicalStep()) || IsMedicated)
        {
            return FanDanceIiPvE.CanUse(out act) ||
                   FanDancePvE.CanUse(out act);
        }
        return SetActToNull(out act);
    }

    private bool TryUsePots(out IAction? act)
    {
        if (HasTechnicalStep && IsDancing && (CompletedSteps == 0 || CompletedSteps == 4))
        {
            if (EvenMinutePotion(FirstPotionTime) && IsWithinFirst15SecondsOfEvenMinute()) return FirstPot(out act);
            if (EvenMinutePotion(SecondPotionTime) && IsWithinFirst15SecondsOfEvenMinute()) return SecondPot(out act);
            if (EvenMinutePotion(ThirdPotionTime) && IsWithinFirst15SecondsOfEvenMinute()) return ThirdPot(out act);
        }

        if (HasStandardStep && IsDancing || Player.HasStatus(true, StatusID.FinishingMoveReady) && FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(2.5f))
        {
            if (OddMinutePotion(FirstPotionTime) && IsWithinFirst15SecondsOfOddMinute()) return FirstPot(out act);
            if (OddMinutePotion(SecondPotionTime) && IsWithinFirst15SecondsOfOddMinute()) return SecondPot(out act);
            if (OddMinutePotion(ThirdPotionTime) && IsWithinFirst15SecondsOfOddMinute()) return ThirdPot(out act);
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
            case true when CombatTime >= firstPotionTime ||
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
            case true when CombatTime >= secondPotionTime &&
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
            case true when CombatTime >= thirdPotionTime &&
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

    #region Status Window Override
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
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawCombatStatusText();
        }
        ImGui.EndGroup();
    }
    private void DrawCombatStatusText()
    {
        try
        {
            ImGui.Columns(2, "CombatStatusColumns", false);

            // Column headers
            ImGui.Text("Status"); ImGui.NextColumn();
            ImGui.Text("Value"); ImGui.NextColumn();
            ImGui.Separator();

            ImGui.Text("Should Use Tech Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseTechStep.ToString()); ImGui.NextColumn();

            ImGui.Text("Should Use Flourish?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseFlourish.ToString()); ImGui.NextColumn();

            ImGui.Text("Should Use Standard Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseStandardStep.ToString()); ImGui.NextColumn();

            ImGui.Text("Should Use Last Dance?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseLastDance.ToString()); ImGui.NextColumn();

            ImGui.Text("In Burst:"); ImGui.NextColumn();
            ImGui.Text(DanceDance.ToString()); ImGui.NextColumn();

            ImGui.Text("Should Hold For Tech Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldHoldForTechnicalStep().ToString()); ImGui.NextColumn();

            ImGui.Text("Should Hold For Standard Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldHoldForStandardStep().ToString()); ImGui.NextColumn();

            ImGui.Text("Is Dancing:"); ImGui.NextColumn();
            ImGui.Text(IsDancing.ToString()); ImGui.NextColumn();

            // Reset columns
            ImGui.Columns(1);
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying combat status");
        }
    }
    private void DrawStateMachineStatus()
    {
        // Registry-based approach: check all available territory IDs to find current territory
        var availableTerritoryIds = new ushort[] { 1263, 1122, 1238 }; // M8S, FRU, M7S

        foreach (var territoryId in availableTerritoryIds)
        {
            if (IsInTerritory(territoryId))
            {
                StateMachineUI.DrawStateMachineStatus(StateMachine, territoryId);
                return;
            }
        }

        // Display "Territory not supported" message when not in any registered territory
        ImGui.TextColored(ImGuiColors.DalamudOrange, "Territory not supported");
    }

    public override void DisplayStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinDNC Status", true,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            var debugVisible = RotationDebugManager.IsDebugTableVisible;
            if (ImGui.Checkbox("Enable Debug Table", ref debugVisible))
                RotationDebugManager.IsDebugTableVisible = debugVisible;
            DrawRotationStatus();
            DrawCombatStatus();
            DrawStateMachineStatus();
            RotationDebugManager.DrawGCDMethodDebugTable();
            DisplayStatusHelper.EndPaddedChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying status: {ex.Message}");
        }
    }

    #endregion
}
