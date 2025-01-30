using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("ChurinDNC", CombatType.PvE, GameVersion = "7.15", Description = "Only for level 100 content, ok?")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed partial class ChurinDNC : DancerRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    public static bool HoldTechForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Dance Partner Name (If empty or not found uses default dance partner priority)")]
    public string DancePartnerName { get; set; } = "";

    [RotationConfig(CombatType.PvE, Name = "Load FRU module?")]
    public bool LoadFRU { get; set; } = false;

    #endregion

    #region  Properties

    public static bool ShouldUseLastDance { get; set; } = true;
    public static bool ShouldUseTechStep { get; set; } = true;
    public static bool ShouldUseStandardStep { get; set; } = true;
    public static bool ShouldUseFlourish { get; set; } = false;
    public static bool ShouldFinishingMove { get; set; } = true;
    bool AboutToDance => StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    static bool DanceDance => Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);
    bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    static bool StepFinishReady => Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 || Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4;
    static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15);

    #endregion
    public override void DisplayStatus()
    {
        DisplayStatusHelper.BeginPaddedChild("The CustomRotation's status window", true, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        DrawRotationStatus();
        DrawCombatStatus();
        ImGui.Separator();
        DisplayStatusHelper.EndPaddedChild();
    }

    private void DrawRotationStatus()
    {
        string text = "Rotation: " + Name;
        float textSize = ImGui.CalcTextSize(text).X;
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, text);
            DisplayStatusHelper.HoveredTooltip(Description);
        }, ImGui.GetWindowWidth(), textSize);
    }

    private static void DrawCombatStatus()
    {
        ImGui.BeginGroup();
        DrawCombatStatusText();
        ImGui.EndGroup();
    }

    private static void DrawCombatStatusText()
    {
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.Text("Combat Status");
        
            // Calculate the width of the window
            float windowWidth = ImGui.GetWindowWidth();
        
            // Calculate the width of the text for current FRU Boss
            float textWidth = ImGui.CalcTextSize("current FRU Boss: " + currentBoss).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.HealerGreen, "current FRU Boss: " + currentBoss);
        
            // Calculate the width of the text for Combat Time
            textWidth = ImGui.CalcTextSize("Combat Time: " + CombatTime).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text("Combat Time: " + CombatTime);
        
            // Calculate the width of the text for Current Downtime
            textWidth = ImGui.CalcTextSize("Current Downtime: " + currentDowntime).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.DalamudRed, "Current Downtime: " + currentDowntime);
        }, ImGui.GetWindowWidth(), ImGui.CalcTextSize("Combat Status").X);
        ImGui.Text("Should Use Tech Step?:" + ShouldUseTechStep);
        ImGui.Text("Should Use Flourish?:" + ShouldUseFlourish);
        ImGui.Text("Should Use Standard Step?:" + ShouldUseStandardStep);
        ChurinDNC instance = new();
        ImGui.Text("Should Hold for Tech Step?:" + instance.ShouldHoldForTechnicalStep());
        ImGui.Text("has Return:" + hasReturn);
        ImGui.Text("Return ending?" + returnEnding);
        ImGui.Text("has Spell in Waiting Return:" + hasSpellinWaitingReturn);
        ImGui.Text("has Dance Targets in Range" + AreDanceTargetsInRange);
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
            // Finish the dance if the conditions are met
            if (remainTime <= 0.54f && FinishTheDance(out act)) return act;
        }
        // If none of the above conditions are met, fallback to the base class method
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic

    // Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction? nextGCD, out IAction? act)
    {
        if (DevilmentAfterFinish(out act)) return true;
        if (FallbackDevilment(out act)) return true;
        if (NotDancing(nextGCD, out act)) return true;

        act = null;
        return false;
    }

    /// <summary>
    /// Determines whether the Devilment action can be used after the Technical Finish status is active.
    /// </summary>
    /// <param name="act">The action to be performed if Devilment can be used.</param>
    /// <returns>
    /// <c>true</c> if the Devilment action can be used; otherwise, <c>false</c>.
    /// </returns>
    private bool DevilmentAfterFinish(out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.TechnicalFinish))
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }
    /// <summary>
    /// Attempts to use the Devilment action if the last GCD action was Quadruple Technical Finish.
    /// </summary>
    /// <param name="act">The action to be used if the conditions are met.</param>
    /// <returns>True if the Devilment action can be used; otherwise, false.</returns>
    private bool FallbackDevilment(out IAction? act)
    {
        if (IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE))
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }
    /// <summary>
    /// Determines if the character is not dancing and can use an emergency ability.
    /// </summary>
    /// <param name="nextGCD">The next global cooldown action.</param>
    /// <param name="act">The action to be performed if the emergency ability can be used.</param>
    /// <returns>
    /// True if the character can use an emergency ability; otherwise, false.
    /// </returns>
    private bool NotDancing(IAction? nextGCD, out IAction? act)
    {
        bool isNotDancing = !IsDancing;
        bool isNotStandardOrTechnicalReady = !(StandardReady || TechnicalReady);
        bool canUseEmergencyAbility = isNotDancing && isNotStandardOrTechnicalReady && nextGCD != null;
        if (canUseEmergencyAbility)
        {
            if (nextGCD != null)
            {
                return base.EmergencyAbility(nextGCD, out act);
            }
        }
        act = null;
        return false;
    }

    // Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (LoadFRU)
        {
            CheckFRUPhase();
            CheckCurrentDowntime();
            CheckFRULogic();
        }

        if (IsDancing || AboutToDance) return false;

        if (nextGCD.AnimationLockTime > 0.75f) return false;

        if (oGCDHelper(out act, nextGCD)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    /// <summary>
    /// Helper method to handle off-global cooldown (oGCD) actions.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <param name="nextGCD">The next global cooldown (GCD) action to be performed.</param>
    /// <returns>True if an oGCD action was performed; otherwise, false.</returns>
    private bool oGCDHelper(out IAction? act, IAction nextGCD)
    {
        if (HandleFlourish(out act)) return true;

        if (ShouldRemoveFinishingMove)
        {
            RemoveFinishingMove();
        }

        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (ShouldUseFeathers(nextGCD, out act)) return true;

        if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (UseClosedPosition(out act)) return true;

        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for removing the Finishing Move status.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if removing Finishing Move was performed; otherwise, false.</returns>
    private static void RemoveFinishingMove()
    {
        // Check if the finishing move should be removed and if the player has the Finishing Move Ready status
        if (Player.HasStatus(true, StatusID.FinishingMoveReady))
        {
            // Remove the Finishing Move Ready status
            StatusHelper.StatusOff(StatusID.FinishingMoveReady);

            // Reset the RemoveFinishingMove flag
            ShouldRemoveFinishingMove = false;
        }
    }

    /// <summary>
    /// Handles the logic for using the Flourish action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
    private static bool HandleFlourish(out IAction? act)
    {
        act = null;

        // Check if DanceDance is active and set shouldUseFlourish accordingly
        if (DanceDance)
        {
            ShouldUseFlourish = true;
        }

        // Attempt to use Flourish if shouldUseFlourish is true
        if (ShouldUseFlourish)
        {
            ChurinDNC instance = new();
            // Check if the player does not have the Threefold Fan Dance status and if Flourish can be used
            if (!Player.HasStatus(true, StatusID.ThreefoldFanDance) && instance.FlourishPvE.CanUse(out act, isFirstAbility: true))
            {
                return true;
            }
        }

        // No Flourish action was performed
        return false;
    }

    /// <summary>
    /// Determines whether feathers should be used based on the next GCD action and current player status.
    /// </summary>
    /// <param name="nextGCD">The next global cooldown (GCD) action to be performed.</param>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a feather action was performed; otherwise, false.</returns>
    private bool ShouldUseFeathers(IAction nextGCD, out IAction? act)
    {
        // Define the GCD actions that can use feathers
        IAction[] FeathersGCDs = { ReverseCascadePvE, FountainfallPvE, RisingWindmillPvE, BloodshowerPvE };

        // Check if the player has the Devilment status
        bool hasDevilment = Player.HasStatus(true, StatusID.Devilment);

        // Check if the player has more than 3 feathers and the next GCD action is one of the feather GCDs
        bool hasEnoughFeathers = Feathers > 3 && FeathersGCDs.Contains(nextGCD);

        // Check if the player does not have the Threefold Fan Dance status
        bool noThreefoldFanDance = !Player.HasStatus(true, StatusID.ThreefoldFanDance);

        // Determine if feathers can be used based on the Devilment status or the number of feathers
        bool canUseFeathers = hasDevilment || hasEnoughFeathers;

        // Attempt to use feathers if conditions are met
        if (canUseFeathers && noThreefoldFanDance)
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        // No feather action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for using the Closed Position action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Closed Position action was performed; otherwise, false.</returns>
    private bool UseClosedPosition(out IAction? act)
    {
        // Check if Closed Position can be used
        if (!ClosedPositionPvE.CanUse(out act)) return false;

        // Check if the player is in combat and has the Closed Position status
        if (InCombat && Player.HasStatus(true, StatusID.ClosedPosition))
        {
            // Check party members for Closed Position status
            return CheckPartyMembersForClosedPosition();
        }

        // Closed Position action was not performed
        return false;
    }

    /// <summary>
    /// Checks party members for the Closed Position status and ensures the target is set correctly.
    /// </summary>
    /// <returns>True if a party member with the Closed Position status is found and the target is set correctly; otherwise, false.</returns>
    private bool CheckPartyMembersForClosedPosition()
    {
        foreach (var friend in PartyMembers)
        {
            // Check if the party member has the Closed Position status
            if (friend.HasStatus(true, StatusID.ClosedPosition_2026))
            {
                // Check if the Closed Position target is not set to this party member
                if (ClosedPositionPvE.Target.Target != friend) return true;
                break;
            }
        }

        // No party member with the Closed Position status was found or the target is already set correctly
        return false;
    }
    #endregion

    #region GCD Logic
    // Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (LoadFRU)
        {
            CheckFRUPhase();
            CheckCurrentDowntime();
            CheckFRULogic();
        }
        if (TryExecuteGCD(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    /// <summary>
    /// Attempts to execute a general global cooldown (GCD) action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a GCD action was performed; otherwise, false.</returns>
    private bool TryExecuteGCD(out IAction? act)
    {
        // Check if GCD attacks should be held for Technical Step
        if (ShouldHoldForTechnicalStep())
        {
            act = null;
            return false;
        }
        if (TryUseTechnicalStep(out act))
        {
            return true;
        }
        // Handle other GCD actions
        return HandleGCDActions(out act);
    }

    /// <summary>
    /// Handles the logic for executing general global cooldown (GCD) actions.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a GCD action was performed; otherwise, false.</returns>
    private bool HandleGCDActions(out IAction? act)
    {
        return TryUseClosedPosition(out act) ||
                TryUseTechnicalStep(out act) ||
               FinishTheDance(out act) ||
               ExecuteStepGCD(out act) ||
               TryUseStandardStep(out act) ||
               TryUseSaberDance(out act) ||
               AttackGCD(out act, Player.HasStatus(true, StatusID.Devilment));
    }
    #endregion

    #region Extra Methods
    /// <summary>
    /// Helper method to handle attack actions during the global cooldown (GCD) based on certain conditions.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <param name="burst">Indicates whether the attack should be a burst attack.</param>
    /// <returns>True if an attack GCD action was performed; otherwise, false.</returns>
    private bool AttackGCD(out IAction? act, bool burst)
    {
        // Check if the player is currently dancing
        if (IsDancing)
        {
            act = null;
            return false;
        }

        // Attempt to handle Tillana
        if (HandleTillana(out act))
        {
            return true;
        }

        // Attempt to handle Last Dance
        if (HandleLastDance(out act))
        {
            return true;
        }

        // Attempt to handle PriorityGCDs during burst windows
        if (HandleDanceDance(out act, burst))
        {
            return true;
        }

        // Attempt to use Standard Step
        if (TryUseStandardStep(out act))
        {
            return true;
        }

        // Attempt to handle Finishing Move
        if (TryUseFinishingMove(out act))
        {
            return true;
        }

        // Attempt to handle basic GCD action
        if (HandleBasicGCD(out act))
        {
            return true;
        }

        // No attack GCD action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for using the Tillana.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if Tillana was performed; otherwise, false.</returns>
    private bool HandleTillana(out IAction? act)
    {
        // Check if Esprit is less than or equal to 50 and Devilment cannot be used
        if (Esprit <= 45 && !DevilmentPvE.CanUse(out _, skipComboCheck: true))
        {
            // Attempt to use Tillana, skipping the AoE check
            if (TillanaPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        // No Tillana action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for using the Last Dance.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Last Dance action was performed; otherwise, false.</returns>
    private static bool HandleLastDance(out IAction? act)
    {
        ChurinDNC instance = new();
        // Check if the Technical Step cooldown has elapsed more than 103 seconds
        if (instance.TechnicalStepPvE.Cooldown.ElapsedAfter(103))
        {
            ShouldUseLastDance = false;
        }

        // Check if buffs are active and Standard Step cooldown will not have one charge in 2.5 seconds
        bool standardStepWillHaveCharge = instance.StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1);
        bool finishingMoveWillHaveCharge = instance.FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1);
        bool StandardOrFinishingCharge = standardStepWillHaveCharge || finishingMoveWillHaveCharge;

        if (DanceDance && StandardOrFinishingCharge)
        {
            ShouldUseLastDance = true;
        }

        // Attempt to use Last Dance if shouldUseLastDance is true
        if (ShouldUseLastDance)
        {
            if (instance.LastDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        // No Last Dance action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for using Priority Actions during burst windows.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <param name="burst">Indicates whether the action should be performed during a burst window.</param>
    /// <returns>True if a Dance Dance action was performed; otherwise, false.</returns>
    private bool HandleDanceDance(out IAction? act, bool burst)
    {
        // Check if the action should be performed during a burst window
        if (burst)
        {
            // Attempt to use Dance of the Dawn
            if (TryUseDanceOfTheDawn(out act)) return true;

            // Attempt to use Saber Dance during burst
            if (TryUseSaberDanceBurst(out act)) return true;

            // Attempt to use Starfall Dance
            if (TryUseStarfallDance(out act)) return true;

            // Attempt to use Finishing Move during burst
            if (TryUseFinishingMoveBurst(out act)) return true;
        }

        // No Dance Dance action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use Dance of the Dawn.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Dance of the Dawn action was performed; otherwise, false.</returns>
    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        // Check if Esprit is greater than or equal to 50 and if Dance of the Dawn can be used
        if (Esprit >= 50 && DanceOfTheDawnPvE.CanUse(out act, skipAoeCheck: true)) return true;

        // Dance of the Dawn was not performed.
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use Saber Dance during a burst window.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if Saber Dance action was performed; otherwise, false.</returns>
    private bool TryUseSaberDanceBurst(out IAction? act)
    {
        // Check if the player has enough Esprit
        bool hasEnoughEsprit = Esprit >= 50;

        // Check if Saber Dance can be used
        bool canUseSaberDance = SaberDancePvE.CanUse(out act, skipAoeCheck: true);

        // Check if Finishing Move or Standard Step is not ready
        bool finishingMoveNotReady = !FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1) || !StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1);

        // Attempt to use Saber Dance if conditions are met
        if (hasEnoughEsprit)
        {
            if (canUseSaberDance)
            {
                if (finishingMoveNotReady)
                {
                    return true;
                }
            }
        }

        //Saber Dance action was not performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use Starfall Dance.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Starfall Dance action was performed; otherwise, false.</returns>
    private bool TryUseStarfallDance(out IAction? act)
    {
        // Check if the Devilment cooldown has elapsed more than 7 seconds
        bool devilmentElapsed = DevilmentPvE.Cooldown.ElapsedAfter(7);

        // Check if the Standard Step cooldown will not have one charge in 1 GCD
        bool standardStepNotReady = !StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1);

        // Check if Starfall Dance can be used
        bool canUseStarfallDance = StarfallDancePvE.CanUse(out act, skipAoeCheck: true);

        // Attempt to use Starfall Dance if conditions are met
        if (devilmentElapsed)
        {
            if (standardStepNotReady)
            {
                if (canUseStarfallDance)
                {
                    return true;
                }
            }
        }

        // No Starfall Dance action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use Finishing Move.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Finishing Move action was performed; otherwise, false.</returns>
    private bool TryUseFinishingMove(out IAction? act)
    {
        bool hasLastDance = Player.HasStatus(true, StatusID.LastDanceReady);
        bool canUseFinishingMove = ShouldFinishingMove && !hasLastDance;
        // Check if the player does not have the Last Dance Ready status and if Finishing Move can be used
        if (canUseFinishingMove && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;

        // No Finishing Move action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Handles the logic for using basic global cooldown (GCD) actions.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a basic GCD action was performed; otherwise, false.</returns>
    private bool HandleBasicGCD(out IAction? act)
    {
        // Determine if neither Standard Step nor Technical Step is ready
        bool isNotStepReady = !(StandardReady || TechnicalReady);

        // Determine if Last Dance cannot be used
        bool cannotUseLastDance = !ShouldUseLastDance || !LastDancePvE.CanUse(out _, skipAoeCheck: true);

        // Check if Esprit is low (less than or equal to 70)
        bool hasLowEsprit = Esprit <= 70;

        // Determine if prioritized GCD actions should be used
        bool shouldUseBasicGCD = (isNotStepReady && cannotUseLastDance) || hasLowEsprit;

        // Attempt to use basic GCD actions if conditions are met
        if (shouldUseBasicGCD)
        {
            return TryUseBasicGCDs(out act);
        }

        // No basic GCD action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use basic global cooldown (GCD) actions in a prioritized order.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a basic GCD action was performed; otherwise, false.</returns>
    private bool TryUseBasicGCDs(out IAction? act)
    {
        return TryUseBladeshower(out act) ||
                TryUseBloodshower(out act) ||
               TryUseFountainfall(out act) ||
               TryUseRisingWindmill(out act) ||
               TryUseReverseCascade(out act) ||
               TryUseWindmill(out act) ||
               TryUseFountain(out act) ||
               TryUseCascade(out act);
    }

    private bool TryUseBloodshower(out IAction? act)
    {
        if (BloodshowerPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseFountainfall(out IAction? act)
    {
        if (FountainfallPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseRisingWindmill(out IAction? act)
    {
        if (RisingWindmillPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseReverseCascade(out IAction? act)
    {
        if (ReverseCascadePvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseBladeshower(out IAction? act)
    {
        if (BladeshowerPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseWindmill(out IAction? act)
    {
        if (WindmillPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseFountain(out IAction? act)
    {
        if (FountainPvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    private bool TryUseCascade(out IAction? act)
    {
        if (CascadePvE.CanUse(out act)) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// Holds the dance finish until the target is in range (14 yalms).
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if a dance finish action was performed; otherwise, false.</returns>
    private bool FinishTheDance(out IAction? act)
    {
        // Check for Standard Step if targets are in range or status is about to end.
        bool isStepFinishReadyAndTargetsInRange = StepFinishReady && AreDanceTargetsInRange;
        bool isStandardStepEnding = Player.WillStatusEnd(1f, true, StatusID.StandardStep);
        bool isTechnicalStepEnding = Player.WillStatusEnd(1f, true, StatusID.TechnicalStep);
        bool shouldFinishDance = isStepFinishReadyAndTargetsInRange || isStandardStepEnding || isTechnicalStepEnding;

        if (shouldFinishDance)
        {
            if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        act = null;
        return false;
    }
    /// <summary>
    /// Attempts to use the Closed Position action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Closed Position action was performed; otherwise, false.</returns>
    private bool TryUseClosedPosition(out IAction? act)
    {
        // Check if Closed Position can be used
        if (!CanUseClosedPosition(out act))
        {
            return false;
        }

        // Set the dance partner target
        SetDancePartnerTarget();

        return true;
    }

    /// <summary>
    /// Determines if the Closed Position action can be used.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Closed Position action can be used; otherwise, false.</returns>
    private bool CanUseClosedPosition(out IAction? act)
    {
        act = null;
        return !InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act);
    }

    /// <summary>
    /// Sets the dance partner target based on the specified dance partner name.
    /// </summary>
    private void SetDancePartnerTarget()
    {
        // Check if the dance partner name is specified
        if (string.IsNullOrEmpty(DancePartnerName)) return;

        // Iterate through party members to find the dance partner
        foreach (var player in PartyMembers)
        {
            if (player.Name.ToString() == DancePartnerName)
            {
                // Set the Closed Position target to the dance partner
                ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);
                break;
            }
        }
    }

    /// <summary>
    /// Attempts to use the Standard Step action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Standard Step action was performed; otherwise, false.</returns>
    private bool TryUseStandardStep(out IAction? act)
    {
        // Check if Standard Step should be used and if it can be used
        if (ShouldUseStandardStep && StandardStepPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // No Standard Step action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use Saber Dance.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if Saber Dance was performed; otherwise, false.</returns>
    private bool TryUseSaberDance(out IAction? act)
    {
        // Check if the player has enough Esprit
        bool hasEnoughEsprit = Esprit >= 70;

        // Check if Technical Step is not ready
        bool techStepNotReady = !TechnicalStepPvE.Cooldown.WillHaveOneCharge(0.5f);

        // Check if Standard Step is not ready
        bool standardStepNotReady = !StandardStepPvE.Cooldown.WillHaveOneCharge(0.5f);

        // Check if Saber Dance can be used
        bool canUseSaberDance = SaberDancePvE.CanUse(out act);

        // Determine if Saber Dance should be used
        bool shouldUseSaberDance = hasEnoughEsprit && (techStepNotReady || standardStepNotReady) && canUseSaberDance;

        // Attempt to use Saber Dance if conditions are met
        if (shouldUseSaberDance)
        {
            return true;
        }

        // No Saber Dance action was performed
        act = null;
        return false;
    }

    /// <summary>
    /// Attempts to use the Finishing Move action during a burst window.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Finishing Move action was performed; otherwise, false.</returns>
    private bool TryUseFinishingMoveBurst(out IAction? act)
    {
        // Determine if Finishing Move should be used and if dance targets are in range
        bool canUseFinishingMove = ShouldFinishingMove && AreDanceTargetsInRange;
        bool hasLastDance = Player.HasStatus(true, StatusID.LastDanceReady);
        bool canFinish = !hasLastDance && canUseFinishingMove;

        // Attempt to use Finishing Move if conditions are met
        if (canFinish && FinishingMovePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // No Finishing Move action was performed
        act = null;
        return false;
    }
    /// <summary>
    /// Determines whether GCD attacks should be held because Technical Step is about to become available.
    /// </summary>
    /// <returns>True if GCD attacks should be held; otherwise, false.</returns>
    private bool ShouldHoldForTechnicalStep()
    {
        if (JustUsedTech()) return false;
        if (ShouldHold()) return true;
        return false;
    }

    private bool JustUsedTech()
    {
        bool canUseTillana = TillanaPvE.CanUse(out _);
        bool technicalStepNotReady = Player.HasStatus(true, StatusID.TechnicalStep);
        bool isDanceDance = DanceDance;
        return canUseTillana || technicalStepNotReady || isDanceDance;
    }

    private bool ShouldHold()
    {
        bool isCoolingDown = !Player.HasStatus(true, StatusID.TechnicalStep) || !Player.HasStatus(true, StatusID.TechnicalFinish);
        bool willHaveOneCharge = TechnicalStepPvE.Cooldown.WillHaveOneCharge(1);
        bool cannotUse = !TechnicalStepPvE.CanUse(out _) && !TillanaPvE.CanUse(out _);

        return isCoolingDown && willHaveOneCharge && cannotUse;
    }
    private static bool TryUseTechnicalStep(out IAction? act)
    {
        ChurinDNC instance = new();
        bool shouldHoldTechFinish = InCombat && HoldTechForTargets && AreDanceTargetsInRange && !IsInFRU;

        if (shouldHoldTechFinish)
        {
            ShouldUseTechStep = true;
        }

        if (ShouldUseTechStep && instance.TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        act = null;
        return false;
    }
    #endregion
}