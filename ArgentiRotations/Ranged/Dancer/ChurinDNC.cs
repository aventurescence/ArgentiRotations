using System.ComponentModel;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.3", Description = "Candles lit, runes drawn upon the floor, sacrifice prepared. Everything is ready for the summoning. I begin the incantation: \"Shakira, Shakira!\"")]
[SourceCode(Path = "main/ArgentiRotations/Ranged/Dancer/ChurinDNC.cs")]
[Api(6)]
public sealed partial class ChurinDNC : DancerRotation
{
    #region Properties

    #region Constants
    private const float DefaultAnimationLock = 0.7f;
    private const float TechnicalStepCooldown = 120f;
    private const float StandardStepCooldown = 30f;
    private const int FlourishCooldown = 60;
    private const int SaberDanceEspritCost = 50;
    private const int HighEspritThreshold = 90;
    private const int BurstEspritThreshold = 70;
    private const int MidEspritThreshold = 70;
    private const int LowEspritThreshold = 30;
    private const int DanceTargetRange = 15;
    #endregion

    #region Status Booleans
    private static bool InTwoMinuteWindow => HasTechnicalStep || HasTechnicalStep && CompletedSteps == 4 || IsLastGCD(ActionID.TechnicalStepPvE);
    private bool InOddMinuteWindow => TechnicalStepPvE.Cooldown.IsCoolingDown && FlourishPvE.Cooldown.IsCoolingDown && HasStandardStep;
    private static bool HasTillana => Player.HasStatus(true, StatusID.FlourishingFinish) && !Player.WillStatusEnd(0, true, StatusID.FlourishingFinish);
    private static bool IsBurstPhase => HasDevilment && HasTechnicalFinish;
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated) && !Player.WillStatusEnd(0, true, StatusID.Medicated);
    private static bool HasAnyProc => Player.HasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry, StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => Player.HasStatus(true, StatusID.FinishingMoveReady) && !Player.WillStatusEnd(0, true, StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasFlourishingStarfall && !Player.WillStatusEnd(0, true, StatusID.FlourishingStarfall);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= DanceTargetRange) || CurrentTarget?.DistanceToPlayer() <= DanceTargetRange;
    private static bool ShouldSwapDancePartner => CurrentDancePartner != null && (CurrentDancePartner.HasStatus(false, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911) || CurrentDancePartner.IsDead);
    private bool ShouldSwapBackToPartner => CurrentDancePartner != null && ClosedPositionPvE.Target.Target !=null && ClosedPositionPvE.Target.Target != CurrentDancePartner;
    #endregion

    #region Conditionals
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled;
    private bool ShouldUseStandardStep => StandardStepPvE.IsEnabled && !HasLastDance;
    private static bool CanWeave => WeaponRemain >= DefaultAnimationLock;

    private bool CanUseTechnicalStep
    {
        get
        {
            if (ShouldUseTechStep && !HasTillana && !IsDancing && TechnicalStepIn(1.5f))
            {
                switch (TechHoldStrategy)
                {
                    case HoldTechStrategy.HoldTechStepOnly when AreDanceTargetsInRange:
                    case HoldTechStrategy.DontHoldTechStepAndFinish:
                    case HoldTechStrategy.HoldTechFinishOnly:
                    case HoldTechStrategy.HoldTechStepAndFinish when AreDanceTargetsInRange:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }
    }

    private bool CanUseStandardStep
    {
        get
        {
            if (ShouldUseStandardStep && (!CanUseTechnicalStep || !TechnicalStepIn(7)) && !IsDancing)
            {
                if (StandardStepIn(0.5f) && (FlourishPvE.Cooldown is { IsCoolingDown:true,HasOneCharge: false } && ShouldUseTechStep || !ShouldUseTechStep ))
                {
                    if ((IsBurstPhase && !DisableStandardInBurst ||
                        IsBurstPhase && DisableStandardInBurst && HasFinishingMove) && Esprit <= BurstEspritThreshold ||
                        !IsBurstPhase && Esprit <= HighEspritThreshold)
                    {
                        switch (StandardHoldStrategy)
                        {
                            case HoldStandardStrategy.HoldStandardStepOnly when AreDanceTargetsInRange:
                            case HoldStandardStrategy.DontHoldStandardStepAndFinish:
                            case HoldStandardStrategy.HoldStandardFinishOnly:
                            case HoldStandardStrategy.HoldStandardStepAndFinish when AreDanceTargetsInRange:
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            return false;
        }
    }

    private bool StandardStepIn(float remainTime) => ShouldUseStandardStep && ((StandardStepPvE.Cooldown is { IsCoolingDown: true } &&
                                                                               (StandardStepCooldown - StandardStepPvE.Cooldown.RecastTimeElapsed <= remainTime ||
                                                                               StandardStepPvE.Cooldown.WillHaveOneCharge(remainTime))) ||
                                                                               StandardStepPvE.Cooldown.HasOneCharge);
    private bool TechnicalStepIn(float remainTime) => ShouldUseTechStep && ((TechnicalStepPvE.Cooldown is { IsCoolingDown: true } &&
                                                      (TechnicalStepCooldown - TechnicalStepPvE.Cooldown.RecastTimeElapsed <= remainTime ||
                                                      TechnicalStepPvE.Cooldown.WillHaveOneCharge(remainTime))) ||
                                                      TechnicalStepPvE.Cooldown.HasOneCharge);


    private bool HoldTechStepOnly => TechHoldStrategy == HoldTechStrategy.HoldTechStepOnly;
    private bool HoldTechFinishOnly => TechHoldStrategy == HoldTechStrategy.HoldTechFinishOnly;
    private bool HoldTechStepAndFinish => TechHoldStrategy == HoldTechStrategy.HoldTechStepAndFinish;
    private bool DontHoldTech => TechHoldStrategy == HoldTechStrategy.DontHoldTechStepAndFinish;
    private bool HoldStandardStepOnly => StandardHoldStrategy == HoldStandardStrategy.HoldStandardStepOnly && !HasFinishingMove;
    private bool HoldStandardFinishOnly => StandardHoldStrategy == HoldStandardStrategy.HoldStandardFinishOnly;
    private bool HoldStandardStepAndFinish => StandardHoldStrategy == HoldStandardStrategy.HoldStandardStepAndFinish;

    private bool DontHoldStandard => StandardHoldStrategy == HoldStandardStrategy.DontHoldStandardStepAndFinish;

    #endregion

    #endregion

    #region Other Properties
    private enum PotionTimings
    {
        [Description("None")] None,
        [Description("Opener and Six Minutes")] ZeroSix,
        [Description("Two Minutes and Eight Minutes")] TwoEight,
        [Description("Opener, Five Minutes and Ten Minutes")] ZeroFiveTen,
    }

    private enum HoldTechStrategy
    {
        [Description("Hold Technical Step only if no targets in range")] HoldTechStepOnly,
        [Description("Hold Technical Finish and Tillana only if no targets in range")] HoldTechFinishOnly,
        [Description("Hold Technical Step, Technical Finish & Tillana if no targets in range")] HoldTechStepAndFinish,
        [Description("Don't hold Technical Step, Technical Finish & Tillana if no targets in range")] DontHoldTechStepAndFinish
    }

    private enum HoldStandardStrategy
    {
        [Description("Hold Standard Step only if no targets in range")] HoldStandardStepOnly,
        [Description("Hold Standard Finish & Finishing Move only if no targets in range")] HoldStandardFinishOnly,
        [Description("Hold Standard Step, Standard Finish & Finishing Move if no targets in range")] HoldStandardStepAndFinish,
        [Description("Don't hold Standard Step, Standard Finish & Finishing Move if no targets in range")] DontHoldStandardStepAndFinish,
    }
    #endregion

    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Technical Step, Technical Finish & Tillana Hold Strategy")]
    private HoldTechStrategy TechHoldStrategy  { get; set; } = HoldTechStrategy.HoldTechStepAndFinish;

    [RotationConfig(CombatType.PvE, Name = "Standard Step, Standard Finish & Finishing Move Hold Strategy")]
    private HoldStandardStrategy StandardHoldStrategy { get; set; } = HoldStandardStrategy.HoldStandardStepAndFinish;

    [Range(0, 20, ConfigUnitType.Seconds, 0.5f)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus time in seconds")]
    private float OpenerPotionTime { get; set; } = 1f;

    [RotationConfig(CombatType.PvE, Name = "Potion Presets")]
    private static PotionTimings PotionTiming { get; set; } = PotionTimings.None;

    [RotationConfig(CombatType.PvE, Name = "Use Custom Potion Timing")]
    private static bool CustomPotionTiming { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Custom Potions - Enable First Potion")]
    private bool CustomEnableFirstPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Potions - First Potion(time in minutes)")]
    private int CustomFirstPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Custom Potions - Enable Second Potion")]
    private bool CustomEnableSecondPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Potions - Second Potion(time in minutes)")]
    private int CustomSecondPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Custom Potions - Enable Third Potion")]
    private bool CustomEnableThirdPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Potions - Third Potion(time in minutes)")]
    private int CustomThirdPotionTime { get; set; } = 0;
    [Range(0,1, ConfigUnitType.Seconds, 0)]

    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Finish?")]
    private float OpenerFinishTime { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Disable Standard Step in Burst")]
    private bool DisableStandardInBurst { get; set; } = true;

    #endregion

    #region Main Combat Logic
    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        InitializePotions();
        UpdatePotions();
        if (remainTime > 15) return base.CountDownAction(remainTime);
        if (TryUseClosedPosition(out var act) ||
            StandardStepPvE.CanUse(out act) ||
            ExecuteStepGCD(out act) ||
            TryUsePotion(out act)
            || remainTime <= OpenerFinishTime && DoubleStandardFinishPvE.CanUse(out act)) return act;

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        UpdatePotions();
        if (TryUsePotion(out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (!CanUseStandardStep || !CanUseTechnicalStep || IsDancing)
            return base.EmergencyAbility(nextGCD, out act);

        act = null;
        return false;
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (IsDancing || !CanWeave) return false;
        if (TryUseFlourish(out act)) return true;
        return TryUseFeathers(out act) || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (IsDancing) return TryFinishTheDance(out act);
        if (TryUseDance(out act)) return true;
        if (InCombat && !IsDancing)
        {
            if (CanUseTechnicalStep || CanUseStandardStep)
            {
                return base.GeneralGCD(out act) ||
                       SetActToNull(out act);
            }
        }
        if (TryUseDance(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (IsBurstPhase && TryUseBurstGCD(out act)) return true;

        return TryUseFillerGCD(out act) || base.GeneralGCD(out act);
    }

    #endregion

    #endregion

    #region Extra Methods

    #region Dance Partner Logic
    private bool TryUseClosedPosition(out IAction? act)
    {
        act = null;
        if (Player.HasStatus(true, StatusID.ClosedPosition) || !PartyMembers.Any() || !ClosedPositionPvE.IsEnabled) return false;

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        act = null;
        if (!Player.HasStatus(true, StatusID.ClosedPosition) || !ShouldSwapDancePartner || !ShouldSwapBackToPartner|| !ClosedPositionPvE.IsEnabled) return false;

        if ((StandardStepIn(5f) || FinishingMovePvE.Cooldown.WillHaveOneCharge(5) ||TechnicalStepIn(5f)) && (ShouldSwapDancePartner|| ShouldSwapBackToPartner))
        {
            return EndingPvE.CanUse(out act);
        }
        return false;
    }

    #endregion
    #region Dance Logic

    private bool TryUseDance(out IAction? act)
    {
        if (CanUseTechnicalStep && TechnicalStepPvE.CanUse(out act)) return true;
        if (CanUseStandardStep && StandardStepPvE.CanUse(out act)) return true;
        if (TryUseFinishingMove(out act)) return true;

        act = null;
        return false;
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        act = null;
        if (!IsDancing) return false;

        if (HasStandardStep)
        {
            var shouldFinish = CompletedSteps == 2 && ((HoldStandardFinishOnly || HoldStandardStepAndFinish) && AreDanceTargetsInRange ||
                                                       DontHoldStandard || HoldStandardStepOnly);
            var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

            if ((shouldFinish || aboutToTimeOut) && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (HasTechnicalStep)
        {
            var shouldFinish = CompletedSteps == 4 && ((HoldTechFinishOnly || HoldTechStepAndFinish) && AreDanceTargetsInRange || DontHoldTech || HoldTechStepOnly);
            var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.TechnicalStep);

            if ((shouldFinish || aboutToTimeOut) && QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        return ExecuteStepGCD(out act);
    }

    private bool TryUseFinishingMove(out IAction? act)
    {
        act = null;
        if (!HasFinishingMove || (HoldStandardFinishOnly || HoldStandardStepAndFinish) && !AreDanceTargetsInRange ||
            HasLastDance)
        {
            return false;
        }

        return FinishingMovePvE.CanUse(out act);
    }

    #endregion
    #region Burst Logic
    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;

        if (IsDancing) return false;

        if (TryUseDance(out act)) return true;

        if (TryUseTillana(out act)) return true;

        if (TryUseDanceOfTheDawn(out act)) return true;

        if (TryUseStarfallDance(out act)) return true;

        if (TryUseLastDance(out act)) return true;

        return TryUseSaberDance(out act) || TryUseFillerGCD(out act);
    }

    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        act = null;
        return Esprit >= SaberDanceEspritCost && DanceOfTheDawnPvE.CanUse(out act);
    }

    private bool TryUseTillana(out IAction? act)
    {
        act = null;
        if (!HasTillana || HasTillana && (HoldTechFinishOnly || HoldTechStepAndFinish) && !AreDanceTargetsInRange ||
            Esprit > SaberDanceEspritCost || StandardStepIn(5)) return false;

        if (TillanaPvE.CanUse(out act))
        {
            return IsBurstPhase switch
            {
                true when Esprit <= LowEspritThreshold && !StandardStepIn(5) => true,
                true when Esprit < SaberDanceEspritCost && !HasLastDance && !StandardStepIn(5) => true,
                true when !HasStarfall && !HasLastDance && !HasFinishingMove && Esprit < SaberDanceEspritCost => true,
                false when Esprit < SaberDanceEspritCost => true,
                false when Player.WillStatusEnd(2.5f, true, StatusID.FlourishingFinish) => true,
                _ => false
            };
        }
        return false;
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance) return false;

        if (LastDancePvE.CanUse(out act))
        {
            return IsBurstPhase switch
            {
                true when !HasFinishingMove && Esprit > SaberDanceEspritCost => false,
                true when HasLastDance && Player.WillStatusEnd(3, true, StatusID.LastDanceReady) => true,
                true when HasFinishingMove && StandardStepIn(5) => true,
                true when HasFinishingMove && Esprit < MidEspritThreshold => true,
                true when Esprit < SaberDanceEspritCost && (!HasFinishingMove || !HasStarfall) => true,
                true when !HasStarfall && Esprit < MidEspritThreshold => true,
                false when StandardStepIn(5) && !TechnicalStepIn(15) => true,
                false when Esprit < MidEspritThreshold && !TechnicalStepIn(15) => true,
                _ => false
            };
        }
        return false;
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall) return false;

        if (StarfallDancePvE.CanUse(out act))
        {
            return Esprit switch
            {
                >= 0 when HasStarfall && (Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall) || DevilmentPvE.Cooldown.RecastTimeElapsed > 5 && !HasFinishingMove) => true,
                < MidEspritThreshold when !HasFinishingMove || DevilmentPvE.Cooldown.RecastTimeElapsed > 7 => true,
                > MidEspritThreshold when DevilmentPvE.Cooldown.RecastTimeElapsed > 15 => true,
                _ => false
            };
        }
        return false;
    }
    #endregion
    #region GCD Skills
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (TryUseDance(out act)) return true;
        if (TryUseTillana(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        if (TryUseLastDance(out act)) return true;
        return TryUseSaberDance(out act) || TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        if (TryUseDance(out act)) return true;
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        if (WindmillPvE.CanUse(out act)) return true;
        if (CascadePvE.CanUse(out act)) return true;

        act = null;
        return false;
    }

    private bool TryUseFeatherGCD(out IAction? act)
    {
        act = null;
        if (Feathers <= 3) return false;

        var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) || Player.HasStatus(true, StatusID.SilkenSymmetry);
        var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) || Player.HasStatus(true, StatusID.FlourishingSymmetry);

        if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < SaberDanceEspritCost && !IsBurstPhase)
        {
            if (FountainPvE.CanUse(out act)) return true;
            if (CascadePvE.CanUse(out act)) return true;
        }

        if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > SaberDanceEspritCost)
        {
            return SaberDancePvE.CanUse(out act);
        }

        return false;
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        act = null;
        if (Esprit < SaberDanceEspritCost) return false;

        if (SaberDancePvE.CanUse(out act))
        {
            return IsBurstPhase switch
            {
                true when Esprit >= HighEspritThreshold && !FinishingMovePvE.CanUse(out _) => true,
                true when (HasLastDance || HasFinishingMove || HasStarfall) && !StandardStepIn(5) &&
                          Esprit >= MidEspritThreshold => true,
                true when (!HasLastDance || !HasFinishingMove || !HasStarfall) && Esprit >= SaberDanceEspritCost => true,
                true when !HasLastDance && HasFinishingMove && !StandardStepIn(3) &&
                          Esprit >= SaberDanceEspritCost => true,
                true when HasLastDance && !HasFinishingMove && Esprit >= SaberDanceEspritCost => true,
                true when HostileTarget?.GetHealthRatio() < 0.05f && HostileTarget.IsBossFromNameplateIcon() &&
                          Esprit >= SaberDanceEspritCost => true,
                false when Esprit >= MidEspritThreshold => true,
                false when Esprit >= SaberDanceEspritCost && Feathers > 3 && HasAnyProc => true,
                false when HostileTarget?.GetHealthRatio() < 0.05f && HostileTarget.IsBossFromNameplateIcon() && Esprit >= SaberDanceEspritCost => true,
                _ => false
            };
        }

        return false;
    }

    private bool TryUseProcs(out IAction? act)
    {
        act = null;
        if (IsBurstPhase || CanUseTechnicalStep || !ShouldUseTechStep) return false;

        var gcdsUntilTech = 0;
        for (uint i = 1; i <= 5; i++)
        {
            if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
            {
                gcdsUntilTech = (int)i;
                break;
            }
        }

        if (gcdsUntilTech == 0) return false;

        switch (gcdsUntilTech)
        {
            case 5:
            case 4:
                if (!HasAnyProc || (HasAnyProc && Esprit < HighEspritThreshold)) return TryUseBasicGCD(out act);
                if (Esprit >= HighEspritThreshold) return SaberDancePvE.CanUse(out act);
                break;
            case 3:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                return FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act);
            case 2:
                if (Esprit >= HighEspritThreshold) return SaberDancePvE.CanUse(out act);
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (FountainPvE.CanUse(out act) && Esprit < HighEspritThreshold && !HasAnyProc) return true;
                break;
            case 1:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (!HasAnyProc && Esprit < HighEspritThreshold && FountainPvE.CanUse(out act)) return true;
                if (!HasAnyProc && Esprit >= SaberDanceEspritCost && !FountainPvE.CanUse(out _)) return SaberDancePvE.CanUse(out act);
                if (!HasAnyProc && Esprit < SaberDanceEspritCost && !FountainPvE.CanUse(out _)) return LastDancePvE.CanUse(out act);
                break;
        }
        return false;
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
        act = null;
        if (HasTechnicalFinish || IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE))
        {
            return DevilmentPvE.CanUse(out act);
        }
        return false;
    }

    /// <summary>
    ///     Handles the logic for using the Flourish action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
    private bool TryUseFlourish(out IAction? act)
    {
        act = null;
        if (!InCombat || HasThreefoldFanDance || !FlourishPvE.IsEnabled) return false;

        var useFlourish = IsBurstPhase || ShouldUseTechStep &&!IsBurstPhase && (TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(25) || !ShouldUseTechStep);

        if (ShouldUseTechStep && !IsBurstPhase && (TechnicalStepPvE.Cooldown is { IsCoolingDown: false, HasOneCharge: true } && !HasTillana ||
                                  TechnicalStepPvE.Cooldown.WillHaveOneCharge(15)))
        {
            useFlourish = false;
        }

        return useFlourish && FlourishPvE.CanUse(out act);
    }

    /// <summary>
    /// Determines whether feathers should be used based on the next GCD action and current player status.
    /// </summary>
    /// <param name="act"> The action to be performed, if any.</param>
    /// <returns>True if a feather action was performed; otherwise, false.</returns>
    private bool TryUseFeathers(out IAction? act)
    {
        act = null;
        var hasEnoughFeathers = Feathers > 3;

        if (Feathers == 4 && HasAnyProc)
        {
            if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        if (HasFourfoldFanDance && FanDanceIvPvE.CanUse(out act)) return true;
        if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;

        if (IsBurstPhase || (hasEnoughFeathers && HasAnyProc && !CanUseTechnicalStep) || IsMedicated || HostileTarget?.GetHealthRatio() < 0.05f && HostileTarget.IsBossFromNameplateIcon())
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        return false;
    }
    #endregion
    #region Potions

    private readonly List<(int Time, bool Enabled, bool Used)> _potions = [];
    private void InitializePotions()
    {
        _potions.Clear();
        switch (PotionTiming, CustomPotionTiming)
        {
            case (PotionTimings.None, false):
                break;
            case (PotionTimings.ZeroSix, false):
                _potions.Add((0, true, false));
                _potions.Add((6, true, false));
                break;
            case (PotionTimings.TwoEight, false):
                _potions.Add((2, true, false));
                _potions.Add((8, true, false));
                break;
            case (PotionTimings.ZeroFiveTen, false):
                _potions.Add((0, true, false));
                _potions.Add((5, true, false));
                _potions.Add((10, true, false));
                break;
        }

        if (CustomPotionTiming)
        {
            if (CustomEnableFirstPotion)
            {
                _potions.Add((CustomFirstPotionTime, true, false));
            }

            if (CustomEnableSecondPotion)
            {
                _potions.Add((CustomSecondPotionTime, true, false));
            }

            if (CustomEnableThirdPotion)
            {
                _potions.Add((CustomThirdPotionTime, true, false));
            }
        }

    }
    private bool TryUsePotion(out IAction? act)
    {
        act = null;

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
                canUse = !InCombat && Countdown.TimeRemaining <= OpenerPotionTime;
            }
            else
            {
                canUse = InCombat && CombatTime >= potionTimeInSeconds && CombatTime <= potionTimeInSeconds + 59;
            }

            if (IsMedicated && canUse)
            {
                _potions[i] = (time, enabled, true);
                continue;
            }

            if (!canUse) continue;

            var condition = (isEvenMinutePotion ? InTwoMinuteWindow : InOddMinuteWindow) || isOpenerPotion || InTwoMinuteWindow;

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
        if ( !CustomPotionTiming && _lastPotionTiming != PotionTiming ||
             CustomPotionTiming &&
             (_lastFirst != CustomFirstPotionTime ||
            _lastSecond != CustomSecondPotionTime ||
            _lastThird != CustomThirdPotionTime))
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

            if (!CustomPotionTiming)
            {
                _lastPotionTiming = PotionTiming;
            }
            else
            {
                _lastFirst = CustomFirstPotionTime;
                _lastSecond = CustomSecondPotionTime;
                _lastThird = CustomThirdPotionTime;
            }

        }
    }

    #endregion
    #region Miscellaneous
    private static bool SetActToNull(out IAction? act)
    {
        act = null;
        return false;
    }
    #endregion
    #endregion

}
