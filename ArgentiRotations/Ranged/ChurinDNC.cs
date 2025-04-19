using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDNC : DancerRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    private static bool HoldTechForTargets { get; set;} = true;

    [RotationConfig(CombatType.PvE,Name = "Hold Standard Step if no targets in range (Warning, will drift)")]
    private static bool HoldStandardForTargets { get; set;} = true;

    [RotationConfig(CombatType.PvE, Name = "Set Dance Partner Priority)")]
    private string DancePartnerName { get; set; } = "";

    //[RotationConfig(CombatType.PvE, Name = "Load FRU module?")]
    //private bool LoadFru { get; set; } = false;

    #endregion
    
    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        ShouldUseFlourish = false;
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
        return NotDancing(nextGCD, out act) || SetActToNull(out act);
    }

    /// Override the method for handling attack abilities
    // ReSharper disable once InconsistentNaming
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsDancing || nextGCD.AnimationLockTime > 0.6f) return SetActToNull(out act);

        return oGCDHelper(out act) || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        //if (!LoadFru) return HoldGcd(out act) || base.GeneralGCD(out act);
        //CheckBoss();
        //CheckDowntime();
        //UpdateFruDowntime();

        return HoldGcd(out act) || base.GeneralGCD(out act);
    }

    #endregion
    
    #region Properties

    private static bool ShouldUseLastDance { get; set; } = true;
    private static bool ShouldUseTechStep { get; set; }
    public ChurinDNC()
    {
        ShouldUseTechStep = TechnicalStepPvE.IsEnabled;
    }
    private static bool ShouldUseStandardStep { get; set; } = true;
    private static bool ShouldUseFlourish { get; set; }
    //private static readonly bool HasSpellInWaitingReturn = Player.HasStatus(false, StatusID.SpellInWaitingReturn_4208);
    //private static readonly bool HasReturn = Player.HasStatus(false, StatusID.Return);
    //private static readonly bool ReturnEnding = HasReturn && Player.WillStatusEnd(7, false, StatusID.Return);
    private static bool _shouldRemoveFinishingMove;
    private static bool DanceDance =>
        Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);
    private bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    private bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    private static bool StepFinishReady => (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2) ||
                                           (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15);

    private static int StatusTimeConverter(bool useFlag, StatusID status)
    {
        return (int)Math.Round(Player.StatusTime(useFlag, status));
    }


    private static readonly Dictionary<StatusID, float> StatusDurations = new()
    {
        { StatusID.FlourishingStarfall, 20 },
        { StatusID.Devilment, 20 },
        { StatusID.SilkenFlow, 30 },
        { StatusID.SilkenSymmetry, 30 },
        { StatusID.FlourishingFlow, 30 },
        { StatusID.FlourishingSymmetry, 30 },
        { StatusID.FlourishingFinish, 20 },
        { StatusID.TechnicalFinish, 20 },
        { StatusID.StandardFinish, 60 },
        { StatusID.LastDanceReady, 30 },
        { StatusID.FinishingMoveReady, 30 }
    };

    private const float GracePeriod = 0.5f;

    private static bool IsStatusEnding(StatusID status, float threshold)
    {
        if (!Player.HasStatus(true, status)) return false;

        var remaining = Player.StatusTime(true, status);

        if (remaining == 0) return false;

        if (StatusDurations.TryGetValue(status, out var maxDuration))
        {
            var activeTime = maxDuration - remaining;
            if (activeTime < GracePeriod) return false;
        }

        return remaining <= threshold;
    }


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
        ImGui.BeginGroup();
        DrawCombatStatusText();
        ImGui.EndGroup();
    }

    private void DrawCombatStatusText()
    {
        if (ImGui.BeginTable("CombatStatusTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Label");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            // Row 1: Should Use Tech Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Use Tech Step?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldUseTechStep.ToString());

            // Row 2: Should Use Flourish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Use Flourish?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldUseFlourish.ToString());

            // Row 3: Should Use Standard Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Use Standard Step?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldUseStandardStep.ToString());

            // Row 4: In Burst
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("In Burst:");
            ImGui.TableNextColumn();
            ImGui.Text(DanceDance.ToString());

            // Row 5: Silken Flow with duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Silken Flow:");
            ImGui.TableNextColumn();
            ImGui.Text($"Ending: {Player.HasStatus(true, StatusID.SilkenFlow) && Player.WillStatusEnd(3, true, StatusID.SilkenFlow)}  Duration: {StatusTimeConverter(true, StatusID.SilkenFlow)}");

            // Row 6: Silken Symmetry with duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Silken Symmetry:");
            ImGui.TableNextColumn();
            ImGui.Text($"Ending: {IsStatusEnding(StatusID.SilkenSymmetry, 3)}  Duration: {StatusTimeConverter(true, StatusID.SilkenSymmetry)}");

            // Row 7: Flourishing Flow with duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Flourishing Flow:");
            ImGui.TableNextColumn();
            ImGui.Text($"Ending: {IsStatusEnding(StatusID.FlourishingFlow, 3)}  Duration: {StatusTimeConverter(true, StatusID.FlourishingFlow)}");

            // Row 8: Flourishing Symmetry with duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Flourishing Symmetry:");
            ImGui.TableNextColumn();
            ImGui.Text($"Ending: {IsStatusEnding(StatusID.FlourishingSymmetry, 3)}  Duration: {StatusTimeConverter(true, StatusID.FlourishingSymmetry)}");

            // Row 9: Flourishing Starfall with duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Flourishing Starfall:");
            ImGui.TableNextColumn();
            ImGui.Text($"Ending: {IsStatusEnding(StatusID.FlourishingStarfall, 5)}  Duration: {StatusTimeConverter(true, StatusID.FlourishingStarfall)}");

            // Row 10: Should Hold for Finishing Move
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Hold for Finishing Move?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldHoldForFinishingMove().ToString());

            // Row 11: Should Hold for Tech Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Hold for Tech Step?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldHoldForTechnicalStep().ToString());

            // Row 14: Has Return
            //ImGui.TableNextRow();
            //ImGui.TableNextColumn();
            //.Text("Has Return?");
            //ImGui.TableNextColumn();
            //ImGui.Text(HasReturn.ToString());

            // Row 15: Return Ending
            //ImGui.TableNextRow();
            //ImGui.TableNextColumn();
            //ImGui.Text("Return Ending?");
            //ImGui.TableNextColumn();
            //ImGui.Text(ReturnEnding.ToString());

            // Row 16: Has Spell in Waiting Return
            //ImGui.TableNextRow();
            //ImGui.TableNextColumn();
            //ImGui.Text("Has Spell in Waiting Return?");
            //ImGui.TableNextColumn();
            //ImGui.Text(HasSpellInWaitingReturn.ToString());

            // Row 12: Has Dance Targets in Range?
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Has Dance Targets in Range?");
            ImGui.TableNextColumn();
            ImGui.Text(AreDanceTargetsInRange.ToString());

            ImGui.EndTable();
        }
    }

    #endregion
    
    #region Extra Methods

        #region Technical Step Logic
        private bool HoldGcd(out IAction? act)
        {
            // If technical step conditions require holding GCD attacks, exit early.
            if (ShouldHoldForTechnicalStep() || ShouldHoldForFinishingMove())
            {
                return SetActToNull(out act);
            }

            if (IsDancing && StepFinishReady)
            {
                return FinishTheDance(out act);
            }

            // Otherwise, try to use technical step first then other GCD actions.
            return TryUseTechnicalStep(out act) ||
                   TryUseFinishingMove(out act) ||
                   HandleGcdActions(out act);
        }

        private bool ShouldHoldForTechnicalStep()
        {
            var techStepSoon = TechnicalStepPvE.Cooldown.IsCoolingDown &&
                                        TechnicalStepPvE.Cooldown.WillHaveOneCharge(1.5f);
            var noTechStatus = !Player.HasStatus(true, StatusID.TechnicalStep)
                && !Player.HasStatus(true, StatusID.TechnicalFinish) && !DanceDance;
            var actionsUnavailable = !TechnicalStepPvE.CanUse(out _) &&
                                     !TillanaPvE.CanUse(out _);
            var alreadyDancing = IsDancing && StepFinishReady;

            return noTechStatus && techStepSoon && actionsUnavailable && !alreadyDancing && ShouldUseTechStep ;
        }

        private bool ShouldHoldForFinishingMove()
        {
            var finishingMoveSoon = FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(1.5f);
            var hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
            var actionUnavailable = !FinishingMovePvE.CanUse(out _);

            return hasFinishingMove && finishingMoveSoon && actionUnavailable && DanceDance;
        }


        private bool TryUseTechnicalStep(out IAction? act)
        {
            var shouldHoldForTechFinish = InCombat && HoldTechForTargets && AreDanceTargetsInRange;
            var noValidTargets = InCombat && !AreDanceTargetsInRange && HoldTechForTargets;
            var sendTechStep = InCombat && !HoldTechForTargets && !AreDanceTargetsInRange;

            if ((shouldHoldForTechFinish || sendTechStep) && TechnicalStepPvE.IsEnabled)
            {
                ShouldUseTechStep = true;
            }

            if (noValidTargets || !TechnicalStepPvE.IsEnabled)
            {
                ShouldUseTechStep = false;
            }

            return ShouldUseTechStep switch
            {
                false => SetActToNull(out act),
                true when !IsDancing => TechnicalStepPvE.CanUse(out act),
                _ => SetActToNull(out act)
            };
        }
        #endregion

        #region Burst Logic

        private bool TechGCD(out IAction? act, bool burst)
        {
            if (!burst || IsDancing) return SetActToNull(out act);

            return HandleDanceDance(out act);
        }

        private bool HandleDanceDance(out IAction? act)
        {
            if (TryUseTillana(out act)) return true;
            if (TryUseDanceOfTheDawn(out act)) return true;
            if (TryUseLastDance(out act)) return true;
            if (TryUseStarfallDance(out act)) return true;
            if (TryUseFinishingMove(out act)) return true;
            return TryUseSaberDanceBurst(out act) || SetActToNull(out act);
        }

        private bool TryUseDanceOfTheDawn(out IAction? act)
        {
            return Esprit >= 50 ? DanceOfTheDawnPvE.CanUse(out act) : SetActToNull(out act);
        }

        private bool TryUseTillana(out IAction? act)
        {
            var tillanaEnding = Player.HasStatus(true, StatusID.FlourishingFinish) &&
                Player.WillStatusEnd(2.5f, true, StatusID.FlourishingFinish);

            if (Esprit < 50) return TillanaPvE.CanUse(out act);

            return tillanaEnding ? TillanaPvE.CanUse(out act) : SetActToNull(out act);
        }

        private bool TryUseLastDance(out IAction? act)
        {

            var standardOrFinishingCharge = StandardStepPvE.Cooldown.WillHaveOneCharge(3) ||
                                            FinishingMovePvE.Cooldown.WillHaveOneCharge(3);
            var lastDanceEnding = Player.WillStatusEnd(2.5f, true, StatusID.LastDanceReady);
            var hasLastDance = Player.HasStatus(true, StatusID.LastDanceReady);

            if (TechnicalStepPvE.Cooldown.ElapsedAfter(103))
                ShouldUseLastDance = false;

            if (DanceDance && Esprit >= 50 && !lastDanceEnding && !standardOrFinishingCharge)
                ShouldUseLastDance = false;

            if (!DanceDance && Esprit >= 70 && !lastDanceEnding)
                ShouldUseLastDance = false;

            if (Esprit < 60 && hasLastDance && !TechnicalStepPvE.Cooldown.ElapsedAfter(103))
                ShouldUseLastDance = true;

            if ((DanceDance && standardOrFinishingCharge) || lastDanceEnding)
                ShouldUseLastDance = true;

            return ShouldUseLastDance ? LastDancePvE.CanUse(out act, skipAoeCheck: true) : SetActToNull(out act);
        }

        private bool TryUseFinishingMove(out IAction? act)
        {
            // First, check if we have the status
            var hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);

            // Return early if we don't have the status
            if (!hasFinishingMove) return SetActToNull(out act);

            // During burst window (Technical + Devilment)
            if (DanceDance)
            {
                return FinishingMovePvE.CanUse(out act);
            }

            // Outside burst window, use if ready
            return FinishingMovePvE.CanUse(out act) || SetActToNull(out act);
        }

        private bool TryUseStarfallDance(out IAction? act)
        {
            // Check if the proc is active and about to end.
            var starfallEnding = Player.HasStatus(true, StatusID.FlourishingStarfall) &&
                                  Player.WillStatusEnd(3, true, StatusID.FlourishingStarfall);
            // Check if the player currently has the Starfall proc.
            var hasStarfall = Player.HasStatus(true, StatusID.FlourishingStarfall);

            // If proc is missing or insufficient Esprit gauge & proc is not ending, do not use Starfall Dance.
            if (!hasStarfall || Esprit > 80 && !starfallEnding)
                return SetActToNull(out act);

            // Use Starfall Dance if the proc is about to expire or if dance steps are not ready.
            if (starfallEnding || hasStarfall)
                return StarfallDancePvE.CanUse(out act);

            return SetActToNull(out act);
        }

        private bool TryUseSaberDanceBurst(out IAction? act)
        {
            var hasEnoughEsprit = Esprit >= 50;
            var starfallEnding = Player.HasStatus(true, StatusID.FlourishingStarfall) &&
                                  Player.WillStatusEnd(3, true, StatusID.FlourishingStarfall);
            var canUseSaberDance = SaberDancePvE.CanUse(out act);
            var noFinishingMove =
                !(FinishingMovePvE.Cooldown.WillHaveOneCharge(2.5f) ||
                  StandardStepPvE.Cooldown.WillHaveOneCharge(2.5f)) ||
                !(FinishingMovePvE.CanUse(out _) ||
                  StandardStepPvE.CanUse(out _));

            if (noFinishingMove && hasEnoughEsprit && !starfallEnding && canUseSaberDance) return true;

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
            return TryUseTillana(out act) ||
                   ProcHelper(out act) ||
                   ExecuteStepGCD(out act) ||
                   FinishTheDance(out act) ||
                   TryUseTechnicalStep(out act) ||
                   TechGCD(out act, Player.HasStatus(true, StatusID.Devilment)) ||
                   TryUseLastDance(out act) ||
                   TryUseFinishingMove(out act) ||
                   TryUseStandardStep(out act) ||
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
            var hasProcs = Player.HasStatus(true, StatusID.SilkenSymmetry) || Player.HasStatus(true,StatusID.SilkenFlow) ||
                           Player.HasStatus(true, StatusID.FlourishingSymmetry) || Player.HasStatus(true, StatusID.FlourishingFlow);
            var hasLastDance = Player.HasStatus(true,StatusID.LastDanceReady);
            var noPriorityGCD = DanceDance && !hasLastDance || Esprit > 50 ;


            // Determine if prioritized GCD actions should be used
            var shouldUseBasicGcd = (noPriorityGCD && hasProcs) || !DanceDance;

            // Attempt to use basic GCD actions if conditions are met
            return shouldUseBasicGcd ? TryUseBasicGcDs(out act) :
                // No basic GCD action was performed
                SetActToNull(out act);
        }

        /// <summary>
        ///     Attempts to use the Standard Step action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Standard Step action was performed; otherwise, false.</returns>
        private bool TryUseStandardStep(out IAction? act)
        {
            if (!DanceDance || (Player.HasStatus(true,StatusID.StandardFinish) && Player.WillStatusEnd(5,true, StatusID.StandardFinish) && !Player.HasStatus(true,StatusID.BrinkOfDeath)))
            {
                ShouldUseStandardStep = true;
            }

            if ((TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5)) || (TechnicalStepPvE.CanUse(out _) && TechnicalStepPvE.IsEnabled))
            {
                ShouldUseStandardStep = false;
            }

            return ShouldUseStandardStep ? StandardStepPvE.CanUse(out act) :
                SetActToNull(out act);
        }

        /// <summary>
        ///     Attempts to use basic global cooldown (GCD) actions in a prioritized order.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a basic GCD action was performed; otherwise, false.</returns>
        private bool TryUseBasicGcDs(out IAction? act)
        {
            var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                                 Player.HasStatus(true, StatusID.SilkenSymmetry);
            var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) ||
                                   Player.HasStatus(true, StatusID.FlourishingSymmetry);

            if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit > 50)
                return FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act);

            return BloodshowerPvE.CanUse(out act) ||
                   RisingWindmillPvE.CanUse(out act) ||
                   FountainfallPvE.CanUse(out act) ||
                   ReverseCascadePvE.CanUse(out act) ||
                   FountainPvE.CanUse(out act) ||
                   CascadePvE.CanUse(out act);
        }

        /// <summary>
        ///     Attempts to use Saber Dance.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if Saber Dance was performed; otherwise, false.</returns>
        private bool TryUseSaberDance(out IAction? act)
        {
            var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                           Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                           Player.HasStatus(true, StatusID.FlourishingFlow) ||
                           Player.HasStatus(true, StatusID.FlourishingSymmetry);
            var danceCooldown = (TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(1.5f)) ||
                             (StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneCharge(1.5f)) ||
                             (FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(1.5f));
            var danceReady = StandardStepPvE.CanUse(out _) || TechnicalStepPvE.CanUse(out _) || FinishingMovePvE.CanUse(out _);

            // Check if Saber Dance should be used
            if ((Esprit >= 70 && !(danceCooldown && danceReady)) || (Feathers > 3 & hasProcs && Esprit >= 50))
                return SaberDancePvE.CanUse(out act);

            return SetActToNull(out act);
        }
        /// <summary>
        ///     Holds the dance finish until the target is in range (14 yalms).
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if a dance finish action was performed; otherwise, false.</returns>
        private bool FinishTheDance(out IAction? act)
        {
            // Guard clause: return early if none of the finish conditions are met.
            var waitForTargets = (HoldTechForTargets || HoldStandardForTargets) && AreDanceTargetsInRange;
            var sendDanceFinish = !HoldTechForTargets || !HoldStandardForTargets;
            var isStandardEnding = Player.HasStatus(true,StatusID.StandardStep) && Player.WillStatusEnd(1f, true, StatusID.StandardStep);
            var isTechnicalEnding = Player.HasStatus(true,StatusID.TechnicalStep) && Player.WillStatusEnd(1f, true, StatusID.TechnicalStep);
            var shouldFinishDance = sendDanceFinish || waitForTargets || isStandardEnding || isTechnicalEnding;

            return shouldFinishDance switch
            {
                false => SetActToNull(out act),
                true => DoubleStandardFinishPvE.CanUse(out act) || QuadrupleTechnicalFinishPvE.CanUse(out act)
            };
        }


        private bool ProcHelper(out IAction? act)
        {
            var starfallEnding = IsStatusEnding(StatusID.FlourishingStarfall, 5) ||
                                 IsStatusEnding(StatusID.Devilment, 5);
            var silkenFlowEnding = IsStatusEnding(StatusID.SilkenFlow, 3);
            var silkenSymmetryEnding = IsStatusEnding(StatusID.SilkenSymmetry, 3);
            var flourishingFlowEnding = IsStatusEnding(StatusID.FlourishingFlow, 3);
            var flourishingSymmetryEnding = IsStatusEnding(StatusID.FlourishingSymmetry, 3);

            return DanceDance switch
            {
                true when starfallEnding => StarfallDancePvE.CanUse(out act),
                true when (silkenSymmetryEnding || flourishingSymmetryEnding) => FountainfallPvE.CanUse(out act),
                true when (silkenFlowEnding || flourishingFlowEnding) => ReverseCascadePvE.CanUse(out act),
                _ => SetActToNull(out act)
            };
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
        /// <returns>True if an oGCD action was performed; otherwise, false.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private bool oGCDHelper(out IAction? act)
        {
            if (IsDancing) return SetActToNull(out act);
            if (TryUseFlourish(out act)) return true;
            if (_shouldRemoveFinishingMove) RemoveFinishingMove();
            if (FanDanceIiiPvE.CanUse(out act)) return true;
            if (ShouldUseFeathers(out act)) return true;
            if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (TryUseClosedPosition(out act)) return true;
            return UseClosedPosition(out act) || SetActToNull(out act);
        }

        /// <summary>
        ///     Handles the logic for removing the Finishing Move status.
        /// </summary>
        /// <returns>True if removing Finishing Move was performed; otherwise, false.</returns>
        private static void RemoveFinishingMove()
        {
            if (!Player.HasStatus(true, StatusID.FinishingMoveReady)) return;
            StatusHelper.StatusOff(StatusID.FinishingMoveReady);
            _shouldRemoveFinishingMove = false;
        }

        /// <summary>
        ///     Handles the logic for using the Flourish action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
        private bool TryUseFlourish(out IAction? act)
        {
            var burstReady = TechnicalStepPvE.CanUse(out _) && DevilmentPvE.CanUse(out _);

            if ((!InCombat || Player.HasStatus(true, StatusID.ThreefoldFanDance) || burstReady) && !ShouldUseTechStep)
                ShouldUseFlourish = false;


            if ((DanceDance || TechnicalStepPvE.Cooldown.ElapsedAfter(67)) && ShouldUseTechStep)
                ShouldUseFlourish = true;

            return ShouldUseFlourish ? FlourishPvE.CanUse(out act) : SetActToNull(out act);
        }

        /// <summary>
        /// Determines whether feathers should be used based on the next GCD action and current player status.
        /// </summary>
        /// <param name="act"> The action to be performed, if any.</param>
        /// <returns>True if a feather action was performed; otherwise, false.</returns>
        private bool ShouldUseFeathers(out IAction? act)
        {
            var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                           Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                           Player.HasStatus(true, StatusID.FlourishingFlow) ||
                           Player.HasStatus(true, StatusID.FlourishingSymmetry);
            var hasDevilment = Player.HasStatus(true, StatusID.Devilment);
            var hasEnoughFeathers = Feathers > 3;
            var noThreefoldFanDance = !Player.HasStatus(true, StatusID.ThreefoldFanDance);

            if ((hasDevilment && !ShouldHoldForFinishingMove() || (hasEnoughFeathers && hasProcs )) &&
                noThreefoldFanDance)
                return FanDanceIiPvE.CanUse(out act) ||
                       FanDancePvE.CanUse(out act);

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