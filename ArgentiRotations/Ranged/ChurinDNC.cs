using System.Diagnostics.CodeAnalysis;
using ArgentiRotations.Encounter;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("ChurinDNC", CombatType.PvE, GameVersion = "7.2", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDnc : FuturesRewritten
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    private static bool HoldTechForTargets { get; set;} = true;

    [RotationConfig(CombatType.PvE,
        Name = "Dance Partner Name (If empty or not found uses default dance partner priority)")]
    private string DancePartnerName { get; set; } = "";

    [RotationConfig(CombatType.PvE, Name = "Load FRU module?")]
    private bool LoadFru { get; set; } = false;

    #endregion
    
    #region Countdown Logic

    // Override the method for actions to be taken during countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        // If there are 15 or fewer seconds remaining in the countdown 
        if (remainTime >= 15) return base.CountDownAction(remainTime);
        // Attempt to use Standard Step if applicable
        if (StandardStepPvE.CanUse(out var act)) return act;
        // Fallback to executing step GCD action if Standard Step is not used
        if (ExecuteStepGCD(out act)) return act;
        // Finish the dance if the conditions are met
        if (remainTime <= 0.54f && FinishTheDance(out act)) return act;
        // If none of the above conditions are met, fallback to the base class method
        return base.CountDownAction(remainTime);
    }

    #endregion
    
    #region oGCD Logic

    /// Override the method for handling emergency abilities
    // ReSharper disable once InconsistentNaming
    protected override bool EmergencyAbility(IAction? nextGCD, out IAction? act)
    {
        if (DevilmentAfterFinish(out act)) return true;
        if (FallbackDevilment(out act)) return true;
        if (NotDancing(nextGCD, out act)) return true;

        act = null;
        return false;
    }

    /// Override the method for handling attack abilities
    // ReSharper disable once InconsistentNaming
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (LoadFru)
        {
            CheckBoss();
            CheckDowntime();
            UpdateFruDowntime();
        }

        if (IsDancing || AboutToDance) return false;

        if (nextGCD.AnimationLockTime > 0.75f) return false;

        return oGCDHelper(out act, nextGCD) || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (!LoadFru) return HoldGcd(out act) || base.GeneralGCD(out act);
        CheckBoss();
        CheckDowntime();
        UpdateFruDowntime();

        return HoldGcd(out act) || base.GeneralGCD(out act);
    }

    #endregion
    
    #region Properties

    private static bool ShouldUseLastDance { get; set; } = true;
    private static bool ShouldUseTechStep { get; set; } = true;
    private static bool ShouldUseStandardStep { get; set; } = true;
    private static bool ShouldUseFlourish { get; set; }
    private static bool ShouldFinishingMove => true;
    private static readonly bool HasSpellInWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
    private static readonly bool HasReturn = Player.HasStatus(false, StatusID.Return);
    private static readonly bool ReturnEnding = HasReturn && Player.WillStatusEnd(7, false, StatusID.Return);
    public static readonly bool HasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
    private static bool _shouldRemoveFinishingMove;

    private bool AboutToDance =>
        StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118);

    private static bool DanceDance =>
        Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);

    private bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    private bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);

    private static bool StepFinishReady => (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2) ||
                                           (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4);

    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15);


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
            var windowWidth = ImGui.GetWindowWidth();

            // Calculate the width of the text for current FRU Boss
            var textWidth = ImGui.CalcTextSize("current FRU Boss: " + CurrentPhase).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.HealerGreen, "current FRU Boss: " + CurrentPhase);

            // Calculate the width of the text for Combat Time
            textWidth = ImGui.CalcTextSize("Combat Time: " + CombatTime).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text("Combat Time: " + CombatTime);

            // Calculate the width of the text for Current Downtime
            textWidth = ImGui.CalcTextSize("Current Downtime: " + CurrentDowntime).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.DalamudRed, "Current Downtime: " + CurrentDowntime);
        }, ImGui.GetWindowWidth(), ImGui.CalcTextSize("Combat Status").X);
        ImGui.Text("Should Use Tech Step?:" + ShouldUseTechStep);
        ImGui.Text("Should Use Flourish?:" + ShouldUseFlourish);
        ImGui.Text("Should Use Standard Step?:" + ShouldUseStandardStep);
        ChurinDnc instance = new();
        ImGui.Text("Should Hold for Tech Step?:" + instance.ShouldHoldForTechnicalStep());
        ImGui.Text("has Return:" + HasReturn);
        ImGui.Text("Return ending?" + ReturnEnding);
        ImGui.Text("has Spell in Waiting Return:" + HasSpellInWaitingReturn);
        ImGui.Text("has Dance Targets in Range" + AreDanceTargetsInRange);
    }

    #endregion
    
    #region Extra Methods

        #region Technical Step Logic
        private bool HoldGcd(out IAction? act)
        {
            // If technical step conditions require holding GCD attacks, exit early.
            if (ShouldHoldForTechnicalStep())
            {
                return SetActToNull(out act);
            }
            // Otherwise, try to use technical step first then other GCD actions.
            return TryUseTechnicalStep(out act) || HandleGcdActions(out act);
        }

        private bool ShouldHoldForTechnicalStep()
        {
            var noTechStatus = !Player.HasStatus(true, StatusID.TechnicalStep) &&
                               !Player.HasStatus(true, StatusID.TechnicalFinish);
            var techStepSoon = TechnicalStepPvE.IsEnabled &&
                               TechnicalStepPvE.Cooldown.WillHaveOneCharge(1);
            var actionsUnavailable = !TechnicalStepPvE.CanUse(out _) &&
                                     !TillanaPvE.CanUse(out _);
            var notInTechPhase = !DanceDance;

            return noTechStatus && techStepSoon && actionsUnavailable && notInTechPhase && !IsDancing;
        }

        private bool TryUseTechnicalStep(out IAction? act)
        {
            // Use the current instance rather than creating a new one.
            var shouldHoldForTechFinish = InCombat && HoldTechForTargets && AreDanceTargetsInRange;
            if (shouldHoldForTechFinish)
            {
                ShouldUseTechStep = true;
            }

            if (ShouldUseTechStep &&
                TechnicalStepPvE.IsEnabled &&
                TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
            return FinishTheDance(out act) || SetActToNull(out act);
        }
        #endregion

        #region Burst Logic

        private bool AttackGcd(out IAction? act, bool burst)
        {
            if (IsDancing) return SetActToNull(out act);
            if (!burst) return SetActToNull(out act);
            if (HandleTillana(out act)) return true;
            if (HandleLastDance(out act)) return true;
            if (HandleDanceDance(out act, burst)) return true;
            if (TryUseStandardStep(out act)) return true;
            if (TryUseFinishingMove(out act)) return true;
            return SetActToNull(out act);
        }

        private bool HandleDanceDance(out IAction? act, bool burst)
        {
            if (!burst) return SetActToNull(out act);
            if (TryUseDanceOfTheDawn(out act)) return true;
            if (TryUseFinishingMoveBurst(out act)) return true;
            if (TryUseStarfallDance(out act)) return true;
            return TryUseSaberDanceBurst(out act) || SetActToNull(out act);
        }

        private bool TryUseDanceOfTheDawn(out IAction? act)
        {
            if (Esprit >= 50 && DanceOfTheDawnPvE.CanUse(out act, skipAoeCheck: true)) return true;
            return SetActToNull(out act);
        }

        private bool HandleTillana(out IAction? act)
        {
            if (Esprit <= 45 && !DevilmentPvE.CanUse(out _, skipComboCheck: true) && TillanaPvE.CanUse(out act))
                return true;
            return SetActToNull(out act);
        }

        private bool HandleLastDance(out IAction? act)
        {
            if (TechnicalStepPvE.Cooldown.ElapsedAfter(103))
                ShouldUseLastDance = false;

            var standardOrFinishingCharge = StandardStepPvE.Cooldown.WillHaveOneChargeGCD(2) ||
                                            FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(2);

            if (DanceDance || standardOrFinishingCharge || Player.WillStatusEnd(2.5f, true, StatusID.LastDanceReady))
                ShouldUseLastDance = true;

            if (ShouldUseLastDance && LastDancePvE.CanUse(out act))
                return true;

            return SetActToNull(out act);
        }

        private bool TryUseStarfallDance(out IAction? act)
        {
            var devilmentElapsed = DevilmentPvE.Cooldown.ElapsedAfter(7) || !Player.WillStatusEndGCD(2,0, true, StatusID.Devilment);
            var standardOrFinishingCharge = StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1) ||
                                            FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1);
            if (devilmentElapsed && !standardOrFinishingCharge && StarfallDancePvE.CanUse(out act))
                return true;

            return SetActToNull(out act);
        }

        private bool TryUseSaberDanceBurst(out IAction? act)
        {
            var hasEnoughEsprit = Esprit >= 50;
            var canUseSaberDance = SaberDancePvE.CanUse(out act, skipAoeCheck: true);
            var noFinishingMove =
                !(FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1) ||
                  StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1)) &&
                !(FinishingMovePvE.CanUse(out _, skipAoeCheck: true) ||
                  StandardStepPvE.CanUse(out _, skipAoeCheck: true)) &&
                !Player.WillStatusEnd(2, true, StatusID.Devilment);

            if (noFinishingMove && hasEnoughEsprit && canUseSaberDance) return true;

            return SetActToNull(out act);
        }

        private bool TryUseFinishingMoveBurst(out IAction? act)
        {
            var canUseFinishingMove = ShouldFinishingMove && AreDanceTargetsInRange;
            var hasLastDance = Player.HasStatus(true, StatusID.LastDanceReady);
            if (!hasLastDance && canUseFinishingMove && FinishingMovePvE.CanUse(out act, skipAoeCheck: true))
                return true;

            return SetActToNull(out act);
        }

        private bool TryUseFinishingMove(out IAction? act)
        {
            var hasLastDance = Player.HasStatus(true, StatusID.LastDanceReady);
            var canUseFinishingMove = ShouldFinishingMove && !hasLastDance;
            if (canUseFinishingMove && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;

            return SetActToNull(out act);
        }


        #endregion

        #region GCD Helpers
        /// <summary>
        ///     Handles the logic for executing general global cooldown (GCD) actions.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a GCD action was performed; otherwise, false.</returns>
        private bool HandleGcdActions(out IAction? act)
        {
            return AttackGcd(out act, Player.HasStatus(true, StatusID.Devilment)) ||
                   ExecuteStepGCD(out act) ||
                   FinishTheDance(out act) ||
                   TryUseTechnicalStep(out act) ||
                   TryUseStandardStep(out act) ||
                   HandleLastDance(out act) ||
                   AvoidFeatherOvercap(out act) ||
                   TryUseSaberDance(out act) ||
                   HandleBasicGcd(out act);
        }

        /// <summary>
        ///     Handles the logic for using basic global cooldown (GCD) actions.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a basic GCD action was performed; otherwise, false.</returns>
        private bool HandleBasicGcd(out IAction? act)
        {
            // Determine if neither Standard Step nor Technical Step is ready
            var isNotStepReady = !(StandardReady || TechnicalReady);

            // Determine if Last Dance cannot be used
            var cannotUseLastDance = !ShouldUseLastDance || !LastDancePvE.CanUse(out _, skipAoeCheck: true);

            // Determine if prioritized GCD actions should be used
            var shouldUseBasicGcd = isNotStepReady && cannotUseLastDance;

            // Attempt to use basic GCD actions if conditions are met
            if (shouldUseBasicGcd) return TryUseBasicGcDs(out act);

            // No basic GCD action was performed
            return SetActToNull(out act);
        }

        /// <summary>
        ///     Attempts to use the Standard Step action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Standard Step action was performed; otherwise, false.</returns>
        private bool TryUseStandardStep(out IAction? act)
        {
            // Check if Standard Step should be used and if it can be used
            if (!Player.HasStatus(true,StatusID.Devilment) && ShouldUseStandardStep && StandardStepPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (Player.HasStatus(true, StatusID.Devilment) &&
                (Player.WillStatusEnd( 5, true, StatusID.StandardFinish) || !Player.HasStatus(true, StatusID.StandardFinish)) &&
                StandardStepPvE.CanUse(out act)) return true;

            // No Standard Step action was performed
            return SetActToNull(out act);
        }

        /// <summary>
        ///     Attempts to use basic global cooldown (GCD) actions in a prioritized order.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a basic GCD action was performed; otherwise, false.</returns>
        private bool TryUseBasicGcDs(out IAction? act)
        {
            return BloodshowerPvE.CanUse(out act) ||
                   RisingWindmillPvE.CanUse(out act) ||
                   FountainfallPvE.CanUse(out act) ||
                   ReverseCascadePvE.CanUse(out act) ||
                   FountainPvE.CanUse(out act) ||
                   CascadePvE.CanUse(out act);
        }

        private bool AvoidFeatherOvercap(out IAction? act)
        {
            var procsAvailable = Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                                 Player.HasStatus(true, StatusID.SilkenFlow) ||
                                 Player.HasStatus(true, StatusID.FlourishingSymmetry) ||
                                 Player.HasStatus(true, StatusID.FlourishingFlow);
            var hasEnoughFeathers = Feathers > 3;
            var noDevilment = !Player.HasStatus(true, StatusID.Devilment);

            if (procsAvailable && noDevilment && hasEnoughFeathers)
            {
                if (FanDanceIiPvE.CanUse(out act)) return true;
                if (FanDancePvE.CanUse(out act)) return true;
                if (Esprit >= 50 && SaberDancePvE.CanUse(out act)) return true;
                if (FinishingMovePvE.CanUse(out act)) return true;
                if (!Player.HasStatus(true, StatusID.SilkenSymmetry) && CascadePvE.CanUse(out act)) return true;
            }

            return SetActToNull(out act);
        }
        /// <summary>
        ///     Attempts to use Saber Dance.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if Saber Dance was performed; otherwise, false.</returns>
        private bool TryUseSaberDance(out IAction? act)
        {
            // Check if the player has enough Esprit
            var hasEnoughEsprit = Esprit >= 70;

            // Check if Technical Step is not ready
            var techStepNotReady = !TechnicalStepPvE.Cooldown.WillHaveOneCharge(0.5f);

            // Check if Standard Step is not ready
            var standardStepNotReady = !StandardStepPvE.Cooldown.WillHaveOneCharge(0.5f);

            // Check if Saber Dance can be used
            var canUseSaberDance = SaberDancePvE.CanUse(out act);

            // Determine if Saber Dance should be used
            var shouldUseSaberDance = hasEnoughEsprit && (techStepNotReady || standardStepNotReady) && canUseSaberDance;

            // Attempt to use Saber Dance if conditions are met
            return shouldUseSaberDance ||
                   // No Saber Dance action was performed
                   SetActToNull(out act);
        }
        /// <summary>
        ///     Holds the dance finish until the target is in range (14 yalms).
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a dance finish action was performed; otherwise, false.</returns>
        private bool FinishTheDance(out IAction? act)
        {
            // Guard clause: return early if none of the finish conditions are met.
            var isStepReadyAndInRange = StepFinishReady && AreDanceTargetsInRange;
            var isStandardEnding = Player.WillStatusEnd(1f, true, StatusID.StandardStep);
            var isTechnicalEnding = Player.WillStatusEnd(1f, true, StatusID.TechnicalStep);
            var shouldFinishDance = isStepReadyAndInRange || isStandardEnding || isTechnicalEnding;

            if (!shouldFinishDance) return SetActToNull(out act);

            if (DoubleStandardFinishPvE.CanUse(out act) ||
                QuadrupleTechnicalFinishPvE.CanUse(out act)) return true;

            return SetActToNull(out act);
        }
        #endregion

        #region OGCD Helpers

        /// <summary>
        ///     Attempts to use the Closed Position action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Closed Position action was performed; otherwise, false.</returns>
        private bool TryUseClosedPosition(out IAction? act)
        {
            // Check if Closed Position can be used
            if (!CanUseClosedPosition(out act)) return false;

            // Set the dance partner target
            SetDancePartnerTarget();

            return true;
        }

        /// <summary>
        ///     Determines if the Closed Position action can be used.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Closed Position action can be used; otherwise, false.</returns>
        private bool CanUseClosedPosition(out IAction? act)
        {
            act = null;
            return !InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act);
        }

        /// <summary>
        ///     Sets the dance partner target based on the specified dance partner name.
        /// </summary>
        private void SetDancePartnerTarget()
        {
            // Check if the dance partner name is specified
            if (string.IsNullOrEmpty(DancePartnerName)) return;

            // Iterate through party members to find the dance partner
            foreach (var player in PartyMembers)
            {
                if (player.Name.ToString() != DancePartnerName) continue;
                // Set the Closed Position target to the dance partner
                ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);
                break;
            }
        }

        /// <summary>
        ///     Determines whether the Devilment action can be used after the Technical Finish status is active.
        /// </summary>
        /// <param name="act">The action to be performed if Devilment can be used.</param>
        /// <returns>
        ///     <c>true</c> if the Devilment action can be used; otherwise, <c>false</c>.
        /// </returns>
        private bool DevilmentAfterFinish(out IAction? act)
        {
            if (Player.HasStatus(true, StatusID.TechnicalFinish) && DevilmentPvE.CanUse(out act)) return true;
            return SetActToNull(out act);
        }

        /// <summary>
        ///     Attempts to use the Devilment action if the last GCD action was Quadruple Technical Finish.
        /// </summary>
        /// <param name="act">The action to be used if the conditions are met.</param>
        /// <returns>True if the Devilment action can be used; otherwise, false.</returns>
        private bool FallbackDevilment(out IAction? act)
        {
            if (IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE) && DevilmentPvE.CanUse(out act)) return true;
            return SetActToNull(out act);
        }

        /// <summary>
        ///     Determines if the character is not dancing and can use an emergency ability.
        /// </summary>
        /// <param name="nextGcd">The next global cooldown action.</param>
        /// <param name="act">The action to be performed if the emergency ability can be used.</param>
        /// <returns>
        ///     True if the character can use an emergency ability; otherwise, false.
        /// </returns>
        private bool NotDancing(IAction? nextGcd, out IAction? act)
        {
            if (!IsDancing && !(StandardReady || TechnicalReady) && nextGcd != null)
                return base.EmergencyAbility(nextGcd, out act);

            return SetActToNull(out act);
        }

        /// <summary>
        ///     Helper method to handle off-global cooldown (oGCD) actions.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <param name="nextGCD">The next global cooldown (GCD) action to be performed.</param>
        /// <returns>True if an oGCD action was performed; otherwise, false.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private bool oGCDHelper(out IAction? act, IAction nextGCD)
        {
            if (IsDancing) return SetActToNull(out act);
            if (HandleFlourish(out act)) return true;
            if (_shouldRemoveFinishingMove) RemoveFinishingMove();
            if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (ShouldUseFeathers(nextGCD, out act)) return true;
            if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (TryUseClosedPosition(out act)) return true;
            if (UseClosedPosition(out act)) return true;

            return SetActToNull(out act);
        }

        /// <summary>
        ///     Handles the logic for removing the Finishing Move status.
        /// </summary>
        /// <returns>True if removing Finishing Move was performed; otherwise, false.</returns>
        private static void RemoveFinishingMove()
        {
            if (Player.HasStatus(true, StatusID.FinishingMoveReady))
            {
                StatusHelper.StatusOff(StatusID.FinishingMoveReady);
                _shouldRemoveFinishingMove = false;
            }
        }

        /// <summary>
        ///     Handles the logic for using the Flourish action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
        private bool HandleFlourish(out IAction? act)
        {
            act = null;
            if (DanceDance || TechnicalStepPvE.Cooldown.WillHaveOneCharge(67)) ShouldUseFlourish = true;

            if (ShouldUseFlourish && FlourishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }

            if (!TechnicalStepPvE.IsEnabled || Player.HasStatus(true, StatusID.ThreefoldFanDance))
            {
                ShouldUseFlourish = false;
            }

            return false;
        }

        /// <summary>
        /// Determines whether feathers should be used based on the next GCD action and current player status.
        /// </summary>
        /// <param name="nextGCD"></param>
        /// <param name="act"> The action to be performed, if any.</param>
        /// <returns>True if a feather action was performed; otherwise, false.</returns>
        private bool ShouldUseFeathers(IAction nextGCD, out IAction? act)
        {
            // Define GCD actions that can support feathers usage.
            IAction[] feathersGCD = [ReverseCascadePvE, FountainfallPvE, RisingWindmillPvE, BloodshowerPvE];

            var hasDevilment = Player.HasStatus(true, StatusID.Devilment);
            var hasEnoughFeathers = Feathers > 3 && feathersGCD.Contains(nextGCD);
            var noThreefoldFanDance = !Player.HasStatus(true, StatusID.ThreefoldFanDance);
            var flourishReady = FlourishPvE.Cooldown.WillHaveOneCharge(2.5f);

            if ((hasDevilment || hasEnoughFeathers) && (noThreefoldFanDance || flourishReady))
            {
                if (FanDanceIiPvE.CanUse(out act)) return true;
                if (FanDancePvE.CanUse(out act)) return true;
            }

            return SetActToNull(out act);
        }



        /// <summary>
        ///     Handles the logic for using the Closed Position action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Closed Position action was performed; otherwise, false.</returns>
        private bool UseClosedPosition(out IAction? act)
        {
            // Check if Closed Position can be used
            if (!ClosedPositionPvE.CanUse(out act)) return false;

            // Check if the player is in combat and has the Closed Position status
            if (InCombat && Player.HasStatus(true, StatusID.ClosedPosition))
                // Check party members for Closed Position status
                return CheckPartyMembersForClosedPosition();

            // Closed Position action was not performed
            return false;
        }

        /// <summary>
        ///     Checks party members for the Closed Position status and ensures the target is set correctly.
        /// </summary>
        /// <returns>
        ///     True if a party member with the Closed Position status is found and the target is set correctly; otherwise,
        ///     false.
        /// </returns>
        private bool CheckPartyMembersForClosedPosition()
        {
            foreach (var friend in PartyMembers)
            {
                // Check if the party member has the Closed Position status
                if (!friend.HasStatus(true, StatusID.ClosedPosition_2026)) continue;
                // Check if the Closed Position target is not set to this party member
                if (ClosedPositionPvE.Target.Target != friend) return true;
                break;
            }

            // No party member with the Closed Position status was found or the target is already set correctly
            return false;
        }

        // Helper method to set act to null and return false.
        private static bool SetActToNull(out IAction? act)
        {
            act = null;
            return false;
        }

        #endregion

        #endregion
}