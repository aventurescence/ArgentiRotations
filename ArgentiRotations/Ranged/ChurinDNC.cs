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

    [RotationConfig(CombatType.PvE, Name = "Holds Standard Step if no targets in range (Warning, will drift & Buff may fall off)")]
    public bool HoldStepForTargets { get; set; } = false;

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
    bool DanceDance => Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);
    bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    bool StepFinishReady => Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 || Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4;
    bool areDanceTargetsInRange => AllHostileTargets.Any(AllHostileTargets => AllHostileTargets.DistanceToPlayer() <= 15);

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
        ImGui.TextColored(ImGuiColors.DalamudRed, "Fatebreaker Kill Time:" + FatebreakerKillTime);
        ImGui.Text("Combat Time:" + CombatTime);
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Usurper Kill Time:" + UsurperKillTime);
        ImGui.TextColored(ImGuiColors.DalamudRed, "Current Downtime:" + CheckCurrentDowntime());
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Adds Kill Time:" + AddsKillTime);
        ImGui.Text("Should Use Tech Step?:" + shouldUseTechStep);
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Gaia Kill Time:" + GaiaKillTime);
        ImGui.Text("Should Use Flourish?:" + shouldUseFlourish);
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Lesbians Kill TimeL" + LesbiansKillTime);
        ImGui.Text("Should Use Last Dance?:" + shouldUseLastDance);
        ImGui.SameLine();
        ImGui.Text("Should Use Standard Step?:" + shouldUseStandardStep);
        ImGui.Text("has Return:" + hasReturn);
        ImGui.Text("Return ending?" + returnEnding);
        ImGui.Text("has Spell in Waiting Return:" + hasSpellinWaitingReturn);
        ImGui.Text("has Dance Targets in Range" + areDanceTargetsInRange);
        ImGui.EndGroup();
        ImGui.BeginGroup();
        ImGui.Text("FRU Test:" + TestingFRUModule);
        if (ImGui.Button("Toggle FRU Test"))
        {
            TestingFRUModule = !TestingFRUModule;
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset Phase"))
        {
            currentBoss = FRUBoss.None;
            currentDowntime = Downtime.None;
        }
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
            if (DoubleStandardFinishPvE.CanUse(out var act, skipAoeCheck: true)) return act;
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
        FRUBoss currentBoss = CheckFRUPhase();
        Downtime currentDowntime = CheckCurrentDowntime();
        act = null;
        CheckFRULogic();

        // If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance
        if (IsDancing || AboutToDance) return false;

        // Prevent triple weaving by checking if an action was just used
        if (nextGCD.AnimationLockTime > 0.75f) return false;

        // Check for conditions to use Flourish
        if (DanceDance || TechnicalFinishPvE.Cooldown.ElapsedAfter(69))
        {
            {
                if (!Player.HasStatus(true, StatusID.ThreefoldFanDance))
                {
                    shouldUseFlourish = true;
                }
            }
        }

        if (shouldUseFlourish)
        {
            if (FlourishPvE.CanUse(out act)) return true;
        }
        
        if (RemoveFinishingMove)
        {
            if (Player.HasStatus(true, StatusID.FinishingMoveReady))
            {
                StatusHelper.StatusOff(StatusID.FinishingMoveReady);
                RemoveFinishingMove = false;

                if (StandardStepPvE.CanUse(out act))
                {
                    return true;
                }
            }
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

        // Attempt to use Closed Position if applicable
        if (!InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act))
        {

            if (DancePartnerName != "")
                foreach (var player in PartyMembers)
                    if (player.Name.ToString() == DancePartnerName)
                        ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);

            return true;
        }

        // Try to finish the dance if applicable
        if (FinishTheDance(out act))
        {
            return true;
        }

        // Execute a Step GCD if available
        if (ExecuteStepGCD(out act))
        {
            return true;
        }

        if (shouldUseStandardStep)
        {
            if (StandardStepPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (!areDanceTargetsInRange && StepFinishReady && currentDowntime != Downtime.None && currentDowntimeEnd - CombatTime >= 15)
            {
                if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }
            else
            {
                if(!areDanceTargetsInRange && StepFinishReady && currentDowntime != Downtime.None && currentDowntimeEnd - CombatTime <= 3)
                {
                    if (FinishTheDance(out act)) return true;  
                }
            }
        }

        if (Esprit >= 70 && SaberDancePvE.CanUse(out act) && !TechnicalFinishPvE.Cooldown.WillHaveOneChargeGCD(1))
        {
            return true;
        }

        if (shouldFinishingMove)
        {
            if (FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (weBall)
        {
            if (CompletedSteps == 2)
            {
                if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }
            else
            {
                if (SingleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;

            }
            
        }


        // Use Technical Step in burst mode if applicable
        if (HoldTechForTargets)
        {
            if (HasHostilesInRange && IsBurst && InCombat && shouldUseTechStep && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true) )
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
            

        // Attempt to use a general attack GCD if none of the above conditions are met
        if (AttackGCD(out act, DanceDance))
        {
            return true;
        }

        // Fallback to the base method if no custom GCD actions are found
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Helper method to handle attack actions during GCD based on certain conditions
    private bool AttackGCD(out IAction? act, bool DanceDance)
    {
        FRUBoss currentBoss = CheckFRUPhase();
        Downtime currentDowntime = CheckCurrentDowntime();
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
        // Prevent Espirit overcapping
        if (!DevilmentPvE.CanUse(out _, skipComboCheck: true) && Esprit <=50)
        {
            if (TillanaPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        // Don't use Last Dance when Tech Step is about to come off cooldown
        if (TechnicalStepPvE.Cooldown.ElapsedAfter(103))
        {
            shouldUseLastDance = false;
        }
        // Last Dance to be used before Standard Step or Finishing Move is about to come off cooldown when in burst
        if (DanceDance && (StandardStepPvE.Cooldown.WillHaveOneCharge(2.5f) || FinishingMovePvE.Cooldown.WillHaveOneCharge(2.5f)))
        {
            shouldUseLastDance = true;
        }

        if (DanceDance)
        {
            if (Esprit >= 50 && DanceOfTheDawnPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (Esprit >= 50 && SaberDancePvE.CanUse(out act, skipAoeCheck: true) && !FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1)) return true;
            // Make sure Starfall gets used before end of party buffs
            if (DevilmentPvE.Cooldown.ElapsedAfter(10) && !FinishingMovePvE.Cooldown.WillHaveOneCharge(3) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
            // Make sure to FM with enough time left in burst window to LD and SFD while leaving a GCD for a Sabre if needed
            if (!Player.HasStatus(true,StatusID.LastDanceReady) && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (shouldUseLastDance)
        {
            if (LastDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        
        // Further prioritized GCD abilities
        if (StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (!(StandardReady || TechnicalReady) &&
            (!shouldUseLastDance || !LastDancePvE.CanUse(out act, skipAoeCheck: true))|| Esprit < 50)
        {
            if (BloodshowerPvE.CanUse(out act)) return true;
            if (FountainfallPvE.CanUse(out act)) return true;
            if (RisingWindmillPvE.CanUse(out act)) return true;
            if (ReverseCascadePvE.CanUse(out act)) return true;
            if (BladeshowerPvE.CanUse(out act)) return true;
            if (WindmillPvE.CanUse(out act)) return true;
            if (FountainPvE.CanUse(out act)) return true;
            if (CascadePvE.CanUse(out act)) return true;
        }

        return false;
    }
    // Method for Standard Step Logic
    private bool UseStandardStep(out IAction act)
    {
        // Attempt to use Standard Step if available and certain conditions are met
        if (!StandardStepPvE.CanUse(out act, skipAoeCheck: true)) return false;
        if (Player.WillStatusEnd(5f, true, StatusID.StandardFinish)) return true;

        // Check for hostiles in range and technical step conditions
        if (!HasHostilesInRange) return false;
        if (Player.HasStatus(true, StatusID.TechnicalFinish) && Player.WillStatusEndGCD(2, 0, true, StatusID.TechnicalFinish) || (TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5))) return false;

        return true;
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
        if (StepFinishReady &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.StandardStep)) &&
            DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Check for Technical Step if targets are in range or status is about to end.
        if (StepFinishReady &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.TechnicalStep)) &&
            QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
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