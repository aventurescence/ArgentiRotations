using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("ChurinDNC", CombatType.PvE, GameVersion = "7.15", Description = "Only for level 100 content, ok?")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed partial class ChurinDNC : DancerRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    public bool HoldTechForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Dance Partner Name (If empty or not found uses default dance partner priority)")]
    public string DancePartnerName { get; set; } = "";
    
    [RotationConfig(CombatType.PvE, Name = "Load FRU module?")]
    public bool LoadFRU { get; set; } = false;

    #endregion

    #region  Properties

    bool shouldUseLastDance = true;
    bool shouldUseTechStep = true;
    bool shouldUseStandardStep = true;
    bool shouldUseFlourish = false;
    bool shouldFinishingMove = true;
    bool AboutToDance => StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    static bool DanceDance => Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);
    bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    static bool StepFinishReady => Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 || Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4;
    static bool AreDanceTargetsInRange => AllHostileTargets.Any(AllHostileTargets => AllHostileTargets.DistanceToPlayer() <= 15);

    #endregion
    public override void DisplayStatus()
    {
        DisplayStatusHelper.BeginPaddedChild("The CustomRotation's status window", true, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        string text = "Rotation: " + Name;
        float textSize = ImGui.CalcTextSize(text).X;
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, text);
            DisplayStatusHelper.HoveredTooltip(Description);
        }, ImGui.GetWindowWidth(), textSize);
        ImGui.BeginGroup();
        ImGui.TextColored(ImGuiColors.HealerGreen, "current FRU Boss:" + CheckFRUPhase());
        ImGui.SameLine();
        ImGui.Text("Combat Time:" + CombatTime);
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Current Downtime:" + CheckCurrentDowntime());
        ImGui.Text("Should Use Tech Step?:" + shouldUseTechStep);
        ImGui.Text("Should Use Flourish?:" + shouldUseFlourish);
        ImGui.Text("Should Use Last Dance?:" + shouldUseLastDance);
        ImGui.Text("Should Use Standard Step?:" + shouldUseStandardStep);
        ImGui.Text("has Return:" + hasReturn);
        ImGui.Text("Return ending?" + returnEnding);
        ImGui.Text("has Spell in Waiting Return:" + hasSpellinWaitingReturn);
        ImGui.Text("has Dance Targets in Range" + AreDanceTargetsInRange);
        ImGui.EndGroup();
        ImGui.Separator();
        DisplayStatusHelper.EndPaddedChild();
    }

    #region Countdown Logic
    // Override the method for actions to be taken during countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        // If there are 15 or fewer seconds remaining in the countdown 
        if (remainTime <= 15)
        {
            // Attempt to use Standard Step if applicable
            if (StandardStepPvE.CanUse(out var act, skipAoeCheck: true)) return act;
            // Fallback to executing step GCD action if Standard Step is not used
            if (ExecuteStepGCD(out act)) return act;
        }
        if (remainTime <= 0.54f)
        {
            if (DoubleStandardFinishPvE.CanUse(out var act, skipAoeCheck: true))
            { 
            return act;
            }
        }
        // If none of the above conditions are met, fallback to the base class method
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic

    // Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.TechnicalFinish))
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }

        // Special handling if the last action was Quadruple Technical Finish and level requirement is met
        if (IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE))
        {
            // Attempt to use Devilment ignoring clipping checks
            if (DevilmentPvE.CanUse(out act)) return true;
        }

        // If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance, except for Devilment
        if (!IsDancing && !(StandardReady || TechnicalReady))
            return base.EmergencyAbility(nextGCD, out act); // Fallback to base class method if none of the above conditions are met

        act = null;
        return false;
    }

    // Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        CheckFRUPhase();
        CheckCurrentDowntime();
        CheckFRULogic();
        act = null;

        // If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance
        if (IsDancing || AboutToDance) return false;

        // Prevent triple weaving by checking if an action was just used
        if (nextGCD.AnimationLockTime > 0.75f) return false;
        
        if (shouldUseFlourish && FlourishPvE.CanUse(out act))
        {
            return true;
        }

        // Check for conditions to use Flourish
        if (DanceDance)
        {
            shouldUseFlourish = true;
        }

        if (RemoveFinishingMove && Player.HasStatus(true, StatusID.FinishingMoveReady))
            {
                StatusHelper.StatusOff(StatusID.FinishingMoveReady);
                RemoveFinishingMove = false;
            }

        // Attempt to use Fan Dance III if available
        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;

        // Use all feathers on burst or if about to overcap
        if (ShouldUseFeathers(nextGCD, out act)) return true;

        // Other attacks
        if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (UseClosedPosition(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        CheckFRULogic();

        if (TryUseClosedPosition(out act)) return true;
        if (FinishTheDance(out act)) return true;
        if (ExecuteStepGCD(out act)) return true;
        if (TryUseStandardStep(out act)) return true;
        if (TryUseSaberDance(out act)) return true;
        if (TryUseFinishingMove(out act)) return true;
        if (TryUseDoubleStandardFinish(out act)) return true;
        if (TryUseTechnicalStep(out act)) return true;
        if (AttackGCD(out act, DanceDance)) return true;

        return base.GeneralGCD(out act);
    }

    private bool TryUseClosedPosition(out IAction? act)
    {
        if (!InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act))
        {
            if (DancePartnerName != "")
                foreach (var player in PartyMembers)
                    if (player.Name.ToString() == DancePartnerName)
                        ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);

            return true;
        }
        act = null;
        return false;
    }

    private bool TryUseStandardStep(out IAction? act)
    {
        if (shouldUseStandardStep && StandardStepPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }
        act = null;
        return false;
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        if (Esprit >= 70 && (!TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(1) || !StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1)) && SaberDancePvE.CanUse(out act))
        {
            return true;
        }
        act = null;
        return false;
    }

    private bool TryUseFinishingMove(out IAction? act)
    {
        if (shouldFinishingMove && AreDanceTargetsInRange && FinishingMovePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }
        act = null;
        return false;
    }

    private bool TryUseDoubleStandardFinish(out IAction? act)
    {
        if (weBall && CompletedSteps == 2 && (CurrentPhaseStart - CombatTime <= 3))
        {
            if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        else
            {
                if (SingleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }
        
        act = null;
        return false;
    }

    private bool TryUseTechnicalStep(out IAction? act)
    {
        if (HoldTechForTargets)
        {
            if (HasHostilesInRange && IsBurst && InCombat && shouldUseTechStep && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }
        else
        {
            if (IsBurst && InCombat && shouldUseTechStep && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }
        act = null;
        return false;
    }
    #endregion

    #region Extra Methods
    // Helper method to handle attack actions during GCD based on certain conditions
    private bool AttackGCD(out IAction? act, bool DanceDance)
    {
        CheckFRUPhase();
        CheckCurrentDowntime();
        act = null;

        CheckFRULogic();

        if (IsDancing)
        {
            return false;
        } 

        if (FinishTheDance(out act))
        { 
            return true;
        }
        if (HandleTillana(out act)) return true;
        if (HandleLastDance(out act, DanceDance)) return true;
        if (HandleDanceDance(out act, DanceDance)) return true;
        if (HandleBasicGCD(out act)) return true;

        return false;
    }

    private bool HandleTillana(out IAction? act)
    {
        act = null;
        if (!DevilmentPvE.CanUse(out _, skipComboCheck: true) && Esprit <= 50 && !FinishingMovePvE.Cooldown.WillHaveOneCharge(2.5f))
        {
            if (TillanaPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        return false;
    }

    private bool HandleLastDance(out IAction? act, bool DanceDance)
    {
        act = null;
        if (TechnicalStepPvE.Cooldown.ElapsedAfter(103))
        {
            shouldUseLastDance = false;
        }

        if (DanceDance && (StandardStepPvE.Cooldown.WillHaveOneCharge(2.5f) || FinishingMovePvE.Cooldown.WillHaveOneCharge(2.5f)))
        {
            shouldUseLastDance = true;
        }

        if (shouldUseLastDance)
        {
            if (LastDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        return false;
    }

    private bool HandleDanceDance(out IAction? act, bool DanceDance)
    {
        act = null;
        if (DanceDance)
        {
            if (Esprit >= 50 && DanceOfTheDawnPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (Esprit >= 50 && SaberDancePvE.CanUse(out act, skipAoeCheck: true) && !FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1)) return true;
            if (DevilmentPvE.Cooldown.ElapsedAfter(10) && !FinishingMovePvE.Cooldown.WillHaveOneCharge(3) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (!Player.HasStatus(true, StatusID.LastDanceReady) && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        return false;
    }

    private bool HandleBasicGCD(out IAction? act)
    {
        act = null;

        if (!(StandardReady || TechnicalReady) &&
            (!shouldUseLastDance || !LastDancePvE.CanUse(out act, skipAoeCheck: true)) || Esprit < 50)
        {
            return TryUsePrioritizedGCDAbilities(out act);
        }
        return false;
    }

    private bool TryUsePrioritizedGCDAbilities(out IAction? act)
    {
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (WindmillPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        if (CascadePvE.CanUse(out act)) return true;

        act = null;
        return false;
    }

    // Helper method to decide usage of Closed Position based on specific conditions
    private bool UseClosedPosition(out IAction? act)
    {
        // Attempt to use Closed Position if available and certain conditions are met
        if (!ClosedPositionPvE.CanUse(out act)) return false;

        if (InCombat && Player.HasStatus(true, StatusID.ClosedPosition))
        {
            // Check for party members with Closed Position status
            foreach (var friend in PartyMembers)
            {
                if (friend.HasStatus(true, StatusID.ClosedPosition_2026))
                {
                    // Use Closed Position if target is not the same as the friend with the status
                    if (ClosedPositionPvE.Target.Target != friend) return true;
                    break;
                }
            }
        }
        return false;
    }
    // Rewrite of method to hold dance finish until target is in range 14 yalms
    private bool FinishTheDance(out IAction? act)
    {
        // Check for Standard Step if targets are in range or status is about to end.
        if (StepFinishReady && AreDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.StandardStep) || Player.WillStatusEnd(1f,true,StatusID.TechnicalStep))
        {
           if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
           if (QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        act = null;
        return false;
    }

    private bool ShouldUseFeathers(IAction nextGCD, out IAction? act)
    {
        IAction[] FeathersGCDs = [ReverseCascadePvE, FountainfallPvE, RisingWindmillPvE, BloodshowerPvE];
        if ((!DevilmentPvE.EnoughLevel || Player.HasStatus(true, StatusID.Devilment) || (Feathers > 3 && FeathersGCDs.Contains(nextGCD))) && !Player.HasStatus(true, StatusID.ThreefoldFanDance))
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }
    #endregion
}