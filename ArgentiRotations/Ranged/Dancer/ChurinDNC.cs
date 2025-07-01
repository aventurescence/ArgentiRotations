using System.ComponentModel;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2.5", Description = "Candles lit, runes drawn upon the floor, sacrifice prepared. Everything is ready for the summoning. I begin the incantation: \"Shakira, Shakira!\"")]
[SourceCode(Path = "ArgentiRotations/Ranged/Dancer/ChurinDNC.cs")]
[Api(5)]
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
    private bool InOddMinuteWindow => !InTwoMinuteWindow && FlourishPvE.Cooldown.IsCoolingDown && FlourishCooldown - FlourishPvE.Cooldown.RecastTimeElapsed < 10;
    private new static bool IsDancing => Player.HasStatus(true, StatusID.StandardStep, StatusID.TechnicalStep) && !Player.WillStatusEnd(0, true, StatusID.StandardStep, StatusID.TechnicalStep);
    private new static bool HasStandardStep => Player.HasStatus(true, StatusID.StandardStep) && !Player.WillStatusEnd(0, true, StatusID.StandardStep);
    private new static bool HasTechnicalStep => Player.HasStatus(true, StatusID.TechnicalStep) && !Player.WillStatusEnd(0, true, StatusID.TechnicalStep);
    private static bool HasTillana => Player.HasStatus(true, StatusID.FlourishingFinish) && !Player.WillStatusEnd(0, true, StatusID.FlourishingFinish);
    private new static bool HasTechnicalFinish => Player.HasStatus(true, StatusID.TechnicalFinish);
    private new static bool HasDevilment => Player.HasStatus(true, StatusID.Devilment);
    private new static bool HasLastDance => Player.HasStatus(true, StatusID.LastDanceReady) && !Player.WillStatusEnd(0, true, StatusID.LastDanceReady);
    private static bool IsBurstPhase => HasDevilment && HasTechnicalFinish;
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated) && !Player.WillStatusEnd(0, true, StatusID.Medicated);

    private static bool HasAnyProc => Player.HasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry,
        StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => Player.HasStatus(true, StatusID.FinishingMoveReady) && !Player.WillStatusEnd(0, true, StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasFlourishingStarfall && !Player.WillStatusEnd(0, true, StatusID.FlourishingStarfall);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= DanceTargetRange) || CurrentTarget?.DistanceToPlayer() <= DanceTargetRange;
    private static bool ShouldSwapDancePartner => CurrentDancePartner != null && (CurrentDancePartner.HasStatus(true, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911) || CurrentDancePartner.IsDead);
    private bool ShouldSwapBackToPartner => CurrentDancePartner != null && ClosedPositionPvE.Target.Target !=null && ClosedPositionPvE.Target.Target != CurrentDancePartner;
    #endregion

    #region Conditionals
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled;
    private bool ShouldUseStandardStep => StandardStepPvE.IsEnabled && !HasLastDance;
    private static bool CanWeave => WeaponRemain >= DefaultAnimationLock;

    private bool CanUseTechnicalStep => ShouldUseTechStep && !HasTillana && !IsDancing &&
                                        ((!HoldTechForTargets || AreDanceTargetsInRange) &&
                                         TechnicalStepIn(0.5f) ||  TechnicalStepPvE.Cooldown.WillHaveOneCharge(0.5f) || TechnicalStepPvE.CanUse(out _));
    private bool CanUseStandardStep => ShouldUseStandardStep && !CanUseTechnicalStep && !IsDancing &&
                                       (!HoldStandardForTargets || AreDanceTargetsInRange) && (StandardStepIn(0.5f) || StandardStepPvE.Cooldown.WillHaveOneCharge(0.5f) || StandardStepPvE.CanUse(out _)) &&
                                       (IsBurstPhase && Esprit < BurstEspritThreshold || !IsBurstPhase && Esprit < HighEspritThreshold ||
                                       IsBurstPhase && FlourishPvE.Cooldown is { HasOneCharge: false, IsCoolingDown: true } && !FlourishPvE.Cooldown.WillHaveOneCharge(10));
    private bool StandardStepIn(float remainTime) => StandardStepPvE.Cooldown is { IsCoolingDown: true } &&
                                                     StandardStepCooldown - StandardStepPvE.Cooldown.RecastTimeElapsed + WeaponRemain <= remainTime &&
                                                     ShouldUseStandardStep & !IsDancing || StandardStepPvE.CanUse(out _);
    private bool TechnicalStepIn(float remainTime) => TechnicalStepPvE.Cooldown is { IsCoolingDown: true } &&
                                                      TechnicalStepCooldown - TechnicalStepPvE.Cooldown.RecastTimeElapsed + WeaponRemain <= remainTime &&
                                                      ShouldUseTechStep || TechnicalStepPvE.CanUse(out _);
    #endregion

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

    #endregion

    #region Other Properties
    private enum PotionTimings
    {
        [Description("None")] None,
        [Description("Opener and Six Minutes")] ZeroSix,
        [Description("Two Minutes and Eight Minutes")] TwoEight,
        [Description("Opener, Five Minutes and Ten Minutes")] ZeroFiveTen,
        [Description("Custom - set values below")] Custom
    }
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
        //ResetPotions();
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
        if (!CanUseStandardStep || !CanUseTechnicalStep)
            return base.EmergencyAbility(nextGCD, out act);

        act = null;
        return false;
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (IsDancing || !CanWeave || CanUseTechnicalStep || CanUseStandardStep) return false;
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
            if (CanUseTechnicalStep && !TechnicalStepPvE.CanUse(out act) && !HasTillana || CanUseStandardStep && !StandardStepPvE.CanUse(out act))
            {
                act = null;
                return false;
            }
        }
        if (TryUseDance(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (IsBurstPhase && TryUseBurstGCD(out act)) return true;

        return TryUseFillerGCD(out act) || base.GeneralGCD(out act);
    }

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
        if (CanUseTechnicalStep && TechnicalStepPvE.CanUse(out act) || TechnicalStepPvE.CanUse(out act)) return true;
        if (CanUseStandardStep && StandardStepPvE.CanUse(out act) || StandardStepPvE.CanUse(out act)) return true;
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
            var shouldFinish = CompletedSteps == 2 && (!HoldStandardFinishForTargets || AreDanceTargetsInRange);
            var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

            if ((shouldFinish || aboutToTimeOut) && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (HasTechnicalStep)
        {
            var shouldFinish = CompletedSteps == 4 && (!HoldTechFinishForTargets || AreDanceTargetsInRange);
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
        if (!HasFinishingMove || (HoldStandardFinishForTargets && !AreDanceTargetsInRange) || HasLastDance) return false;

        return FinishingMovePvE.CanUse(out act);
    }

    #endregion

    #region Burst Logic
    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;

        if (IsDancing) return false;

        if (TryUseTillana(out act)) return true;

        if (TryUseDanceOfTheDawn(out act)) return true;

        if (TryUseLastDance(out act)) return true;

        if (TryUseFinishingMove(out act)) return true;

        if (TryUseStarfallDance(out act)) return true;

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
        if (!HasTillana) return false;

        var useTillana = Esprit <= LowEspritThreshold && (!HasFinishingMove || HasFinishingMove && !StandardStepIn(5));

        if (Esprit >= SaberDanceEspritCost)
        {
            useTillana = false;
        }

        return useTillana && TillanaPvE.CanUse(out act);
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance) return false;

        var useLastDance = StandardStepIn(5) || HasLastDance || FinishingMovePvE.Cooldown.HasOneCharge || StandardStepPvE.Cooldown.HasOneCharge;

        if (TechnicalStepPvE.Cooldown.IsCoolingDown && (TechnicalStepPvE.Cooldown.ElapsedAfter(105) ||
            TechnicalStepPvE.Cooldown.WillHaveOneCharge(15)) || TechnicalStepPvE.CanUse(out _ ) && !HasTillana ||
            IsBurstPhase && Esprit >= MidEspritThreshold ||
            (HasStarfall && !HasFinishingMove) || IsBurstPhase && !HasFinishingMove && HasStarfall ||
            IsBurstPhase && !HasFinishingMove && Esprit >= 50)
        {
            useLastDance = false;
        }

        return useLastDance && LastDancePvE.CanUse(out act);
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall) return false;

        var useStarfall = (!HasLastDance || !HasFinishingMove || DevilmentPvE.Cooldown.RecastTimeElapsed > 7) && Esprit <= MidEspritThreshold;
        if ((HasLastDance && HasFinishingMove) || (Esprit > MidEspritThreshold && DevilmentPvE.Cooldown.RecastTimeElapsed < 5))
        {
            useStarfall = false;
        }

        return useStarfall && StarfallDancePvE.CanUse(out act);
    }
    #endregion

    #region GCD Skills
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (CanUseTechnicalStep) return TechnicalStepPvE.CanUse(out act);
        if (CanUseStandardStep) return StandardStepPvE.CanUse(out act);
        if (TryUseTillana(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        if (TryUseLastDance(out act)) return true;
        return TryUseSaberDance(out act) || TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        if (CanUseTechnicalStep) return TechnicalStepPvE.CanUse(out act);
        if (CanUseStandardStep) return StandardStepPvE.CanUse(out act);
        if (IsBurstPhase && Esprit >= SaberDanceEspritCost &&
            (!HasFinishingMove || (HasLastDance && !HasFinishingMove)) &&
            !CanUseTechnicalStep && SaberDancePvE.CanUse(out act)) return true;
        if (IsBurstPhase && IsLastGCD(ActionID.FinishingMovePvE, ActionID.StandardFinishPvE) &&
            Esprit < SaberDanceEspritCost && (!HasStarfall || StarfallDancePvE.CanUse(out _)) &&
            LastDancePvE.CanUse(out act)) return true;

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

        var useSaberDance = IsBurstPhase && (HasLastDance && HasFinishingMove || HasStarfall) &&
                            Esprit >= MidEspritThreshold || (IsBurstPhase && HasLastDance && !HasFinishingMove && Esprit >= SaberDanceEspritCost) ||
                               !IsBurstPhase && (Esprit >= MidEspritThreshold || Esprit >= BurstEspritThreshold - 10 && StandardStepIn(5)) ||
                               Esprit >= HighEspritThreshold;

        if ((IsBurstPhase && HasLastDance && HasFinishingMove && Esprit < MidEspritThreshold && !FinishingMovePvE.Cooldown.HasOneCharge) ||
            (!IsBurstPhase && Esprit < MidEspritThreshold))
        {
            useSaberDance = false;
        }

        return useSaberDance && SaberDancePvE.CanUse(out act);
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

        var useFlourish = IsBurstPhase || !IsBurstPhase && TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(25);

        if (ShouldUseTechStep && (!IsBurstPhase && TechnicalStepPvE.Cooldown is { IsCoolingDown: false, HasOneCharge: true } ||
                                  !ShouldUseTechStep && (TechnicalStepPvE.Cooldown is {IsCoolingDown:false,HasOneCharge:true} && !HasTillana ||
                                  TechnicalStepPvE.Cooldown.WillHaveOneCharge(15))))
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

        if (IsBurstPhase || (hasEnoughFeathers && HasAnyProc && !CanUseTechnicalStep) || IsMedicated)
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        return false;
    }
    #endregion

    #region Potions
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

            bool canUse = false;
            if (isOpenerPotion)
            {
                // Allow a slightly larger window for opener potion
                canUse = !InCombat && Countdown.TimeRemaining <= 2.0f && Countdown.TimeRemaining >= 0f;
            }
            else
            {
                canUse = InCombat && CombatTime >= potionTimeInSeconds && CombatTime < potionTimeInSeconds + 59;
            }

            if (!canUse) continue;

            var condition = (isEvenMinutePotion ? InTwoMinuteWindow : InOddMinuteWindow) || isOpenerPotion;

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

    /*private void ResetPotions()
    {
        // Only reset if not in combat, combat time is 0, and the first potion is not an opener
        if (!InCombat && CombatTime == 0 && _potions.Count > 0 && _potions[0].Time != 0)
        {
            for (var i = 0; i < _potions.Count; i++)
            {
                var (time, enabled, _) = _potions[i];
                _potions[i] = (time, enabled, false);
            }
        }
    }*/

    #endregion

    #endregion

}
