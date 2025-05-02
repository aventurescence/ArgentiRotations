using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ArgentiRotations.Common;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2.1", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDNC : DancerRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    private static bool HoldTechForTargets { get; set;} = true;

    [RotationConfig(CombatType.PvE,Name = "Hold Standard Step if no targets in range (Warning, will drift)")]
    private static bool HoldStandardForTargets { get; set;} = true;

    [RotationConfig(CombatType.PvE, Name = "Use Opener Pot")]
    private static bool UseOpenerPot { get; set;} = true;

    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "First Dance Partner Priority")]
    private static DancePartnerJob Prio0 { get; set; } = DancePartnerJob.SAM;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Second Dance Partner Priority")]
    private static DancePartnerJob Prio1 { get; set; } = DancePartnerJob.PCT;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Third Dance Partner Priority")]
    private static DancePartnerJob Prio2 { get; set; } = DancePartnerJob.RPR;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Fourth Dance Partner Priority")]
    private static DancePartnerJob Prio3 { get; set; } = DancePartnerJob.VPR;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Fifth Dance Partner Priority")]
    private static DancePartnerJob Prio4 { get; set; } = DancePartnerJob.MNK;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Sixth Dance Partner Priority")]
    private static DancePartnerJob Prio5 { get; set; } = DancePartnerJob.NIN;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Seventh Dance Partner Priority")]
    private static DancePartnerJob Prio6 { get; set; } = DancePartnerJob.DRG;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Eighth Dance Partner Priority")]
    private static DancePartnerJob Prio7 { get; set; } = DancePartnerJob.BLM;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Ninth Dance Partner Priority")]
    private static DancePartnerJob Prio8 { get; set; } = DancePartnerJob.RDM;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Tenth Dance Partner Priority")]
    private static DancePartnerJob Prio9 { get; set; } = DancePartnerJob.SMN;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Eleventh Dance Partner Priority")]
    private static DancePartnerJob Prio10 { get; set; } = DancePartnerJob.MCH;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Twelfth Dance Partner Priority")]
    private static DancePartnerJob Prio11 { get; set; } = DancePartnerJob.BRD;
    [Range(0, 12, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Thirteenth Dance Partner Priority")]
    private static DancePartnerJob Prio12 { get; set; } = DancePartnerJob.DNC;



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
        // Use pot at 1 second if config is enabled
        if (remainTime <= 1.5 && UseOpenerPot && UseBurstMedicine(out act)) return act;
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
        if (TargetClosedPosition(out act)) return true;
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
        if (TryUseClosedPosition(out act)) return true;
        if (SwapDancePartner(out act)) return true;

        return HoldGcd(out act) ||
               base.GeneralGCD(out act);
    }

    #endregion
    
    #region Properties

    #region Boolean Properties
    private static bool ShouldUseLastDance { get; set; }
    private static bool ShouldUseTechStep { get; set; }
    public ChurinDNC()
    {
        ShouldUseTechStep = TechnicalStepPvE.IsEnabled;
    }
    private static bool ShouldUseStandardStep { get; set; }
    private static bool ShouldUseFlourish { get; set; }
    //private static readonly bool HasSpellInWaitingReturn = Player.HasStatus(false, StatusID.SpellInWaitingReturn_4208);
    //private static readonly bool HasReturn = Player.HasStatus(false, StatusID.Return);
    //private static readonly bool ReturnEnding = HasReturn && Player.WillStatusEnd(7, false, StatusID.Return);
    private static bool DanceDance =>
        Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);

    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated);
    private bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28.5f);
    private bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118.5f);
    private static bool StepFinishReady => (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2) ||
                                           (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15);
    private static bool IsStatusEnding(StatusID status, float threshold)
    {
        if (!Player.HasStatus(true, status)) return false;

        var remaining = Player.StatusTime(true, status);
        if (remaining <= 0) return false;

        if (StatusDurations.TryGetValue(status, out var maxDuration) &&
            maxDuration - remaining < GracePeriod)
        {
            return false;
        }

        return remaining <= threshold;
    }

    #endregion

    #region Other Properties
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

    /*private static List<(string Name, string Job)> GetDancePartnerCandidates()
    {
       var candidatesTemp = new List<(int Order, string Name, string Job)>();

            foreach (var member in PartyMembers)
            {
                // Skip members that are dead or have forbidden statuses.
                if (member.IsDead ||
                    member.HasStatus(false, StatusID.DamageDown_2911, StatusID.Weakness, StatusID.BrinkOfDeath))
                {
                    continue;
                }

                // Determine the order index from DancePartnerPrioList.
                var order = int.MaxValue;
                var currentIndex = 0;
                var valid = false;
                foreach (var prio in DancePartnerPrioList)
                {
                    if (member.IsJobs((Job)prio))
                    {
                        order = currentIndex;
                        valid = true;
                        break;
                    }
                    currentIndex++;
                }
                if (!valid)
                {
                    continue;
                }

                var name = member.Name.TextValue;
                var job = member.ClassJob.Value.Abbreviation.ToString();
                candidatesTemp.Add((order, name, job));
            }

            // Sort candidates by the order index.
            candidatesTemp.Sort((a, b) => a.Order.CompareTo(b.Order));

            var candidates = new List<(string Name, string Job)>();
            foreach (var candidate in candidatesTemp)
            {
                candidates.Add((candidate.Name, candidate.Job));
            }

            return candidates;
    }
    */

    private static string CurrentDancePartner =>
        PartyMembers.FirstOrDefault(member => member.HasStatus(true, StatusID.ClosedPosition_2026))?.Name.TextValue ?? string.Empty;

    private const float GracePeriod = 0.5f;

    private enum DancePartnerJob
    {
        SAM = Job.SAM,
        PCT = Job.PCT,
        RPR = Job.RPR,
        VPR = Job.VPR,
        MNK = Job.MNK,
        NIN = Job.NIN,
        DRG = Job.DRG,
        BLM = Job.BLM,
        RDM = Job.RDM,
        SMN = Job.SMN,
        MCH = Job.MCH,
        BRD = Job.BRD,
        DNC = Job.DNC,
    }

    private static List<DancePartnerJob> DancePartnerPrioList =>
    [
        Prio0,
        Prio1,
        Prio2,
        Prio3,
        Prio4,
        Prio5,
        Prio6,
        Prio7,
        Prio8,
        Prio9,
        Prio10,
        Prio11,
        Prio12
    ];

    #endregion

    #region Status Window Properties

    public override void DisplayStatus()
    {
        DisplayStatusHelper.BeginPaddedChild("The CustomRotation's status window", true,
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        DrawRotationStatus();
        DrawCombatStatus();
        //DrawPartyMembers();
        ImGui.Separator();
        DisplayStatusHelper.EndPaddedChild();
    }

    /*private static void DrawPartyMembers()
    {
        if (ImGui.BeginTable("PartyMembersTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Job");
            ImGui.TableHeadersRow();

            foreach (var (name, job) in GetDancePartnerCandidates())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, string.IsNullOrEmpty(name) ? "N/A" : name);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, string.IsNullOrEmpty(job) ? "N/A" : job);
            }

            ImGui.EndTable();
        }
    }
    */
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
            ImGui.Text($"Ending: {IsStatusEnding(StatusID.SilkenFlow, 3)}  Duration: {StatusTimeConverter(true, StatusID.SilkenFlow)}");

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

            // Row 11: Should Hold for Tech Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Hold for Tech Step?");
            ImGui.TableNextColumn();
            ImGui.Text(ShouldHoldForTechnicalStep().ToString());

            // Row 12: Has Dance Targets in Range?
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Has Dance Targets in Range?");
            ImGui.TableNextColumn();
            ImGui.Text(AreDanceTargetsInRange.ToString());

            //Row 13: Current Dance Partner
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Dance Partner");
            ImGui.TableNextColumn();
            var displayPartner = string.IsNullOrEmpty(CurrentDancePartner) ? "N/A" : CurrentDancePartner;
            ImGui.Text(displayPartner);

            //Row 14: Swap Dance Partner?
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Should Swap Dance Partner?");
            ImGui.TableNextColumn();
            ImGui.Text(SwapDancePartner(out _).ToString());

            ImGui.EndTable();
        }
    }
    #endregion

    #endregion
    
    #region Extra Methods

        #region Dance Partner Logic

        private const TargetType ChurinDancePartner = TargetType.DancePartner;
        private bool TryUseClosedPosition(out IAction? act)
        {
            if (Player.HasStatus(true, StatusID.ClosedPosition)) return SetActToNull(out act);
            return ChurinClosedPositionPvE.CanUse(out act) || SetActToNull(out act);
        }

        private bool SwapDancePartner(out IAction? act)
        {
            var standardOrFinishingCharge =
                StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneCharge(5) ||
                FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(5);
            if (!Player.HasStatus(true, StatusID.ClosedPosition) || !CheckDancePartnerStatus()) return SetActToNull(out act);
            // Check cooldown conditions
            if (standardOrFinishingCharge && CheckDancePartnerStatus()) return EndingPvE.CanUse(out act);
            return SetActToNull(out act);
        }

        private static bool CheckDancePartnerStatus()
        {
            foreach (var dancePartner in PartyMembers)
            {
                if (dancePartner.HasStatus(true, StatusID.ClosedPosition_2026) && dancePartner.HasStatus(false, StatusID.DamageDown_2911, StatusID.Weakness, StatusID.BrinkOfDeath))
                    return true;
            }
            return false;
        }

        // ReSharper disable once InconsistentNaming
        private static IBattleChara? SetDancePartner(IEnumerable<IBattleChara> IGameObjects, TargetType type)
        {
            if (type != ChurinDancePartner)
                return null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (IGameObjects == null) return null;
            var partyMembers = new List<IBattleChara>();

            foreach (var obj in IGameObjects)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (PartyMembers.Contains(obj) && obj.StatusList != null)
                {
                    partyMembers.Add(obj);
                }
            }

            foreach (var prio in DancePartnerPrioList)
            {
                foreach (var member in partyMembers)
                {
                    if (member.IsJobs((Job)prio) && !member.IsDead && !member.HasStatus(false, StatusID.DamageDown_2911,
                            StatusID.Weakness, StatusID.BrinkOfDeath))

                        return member;
                }
            }
            return null;
        }

        private bool TargetClosedPosition(out IAction act)
        {
            if (!ChurinClosedPositionPvE.CanUse(out act) || Player.HasStatus(true, StatusID.ClosedPosition))
                return false;

            var target = SetDancePartner(PartyMembers, ChurinDancePartner);
            if (target == null)
                return false;

            return ChurinClosedPositionPvE.Target.Target != target;
        }

        private static void ModifyChurinClosedPositionPvE(ref ActionSetting setting)
        {
            setting.IsFriendly = true;
            setting.TargetType = ChurinDancePartner;
            setting.TargetStatusProvide =
            [
                StatusID.ClosedPosition_2026
            ];
            setting.StatusProvide =
            [
                StatusID.ClosedPosition
            ];
            setting.RotationCheck = (Func<bool>) (() => !IsDancing && !PartyMembers.Any(member => member.HasStatus(true, StatusID.ClosedPosition_2026)));
        }


        private readonly Lazy<IBaseAction> _churinClosedPositionPvECreator = new(() =>
        {
            IBaseAction action = new BaseAction(ActionID.ClosedPositionPvE);
            LoadActionSetting(action);
            ActionSetting setting = action.Setting;
            ModifyChurinClosedPositionPvE(ref setting);
            action.Setting = setting;
            return action;
        });

        private static void LoadActionSetting(IBaseAction action)
        {
            Lumina.Excel.Sheets.Action action1 = action.Action;
            if (action1.CanTargetAlly || action1.CanTargetParty)
                action.Setting.IsFriendly = true;
            else if (action1.CanTargetHostile)
                action.Setting.IsFriendly = false;
            else
                action.Setting.IsFriendly = action.TargetInfo.EffectRange > 5.0;
        }
        private IBaseAction ChurinClosedPositionPvE => _churinClosedPositionPvECreator.Value;

        #endregion

        #region Technical Step Logic
        private bool HoldGcd(out IAction? act)
        {
            // If technical step conditions require holding GCD attacks, exit early.
            if (ShouldHoldForTechnicalStep())
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
                                        TechnicalReady || TechnicalStepPvE.Cooldown.WillHaveOneCharge(1.5f);
            var noTechStatus = !Player.HasStatus(true, StatusID.TechnicalStep)
                && !Player.HasStatus(true, StatusID.TechnicalFinish) && !DanceDance;
            var actionsUnavailable = !TechnicalStepPvE.CanUse(out _) &&
                                     !TillanaPvE.CanUse(out _);
            var alreadyDancing = IsDancing && StepFinishReady;

            return noTechStatus && techStepSoon && actionsUnavailable && !alreadyDancing && ShouldUseTechStep ;
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
                return TryUseTillana(out act) ||
                       TryUseDanceOfTheDawn(out act) ||
                       TryUseLastDance(out act) ||
                       TryUseFinishingMove(out act) ||
                       TryUseStarfallDance(out act) ||
                       TryUseSaberDanceBurst(out act) ||
                       SetActToNull(out act);
            }

        private bool TryUseDanceOfTheDawn(out IAction? act)
        {
            return Esprit >= 50 ? DanceOfTheDawnPvE.CanUse(out act) : SetActToNull(out act);
        }

        private bool TryUseTillana(out IAction? act)
        {
            var finishingMoveReady = FinishingMovePvE.Cooldown.IsCoolingDown &&
                                     FinishingMovePvE.Cooldown.WillHaveOneCharge(3);
            var burstEnding = IsStatusEnding(StatusID.TechnicalFinish, 5) || IsStatusEnding(StatusID.Devilment, 5);

            if (Esprit <= 30 || IsStatusEnding(StatusID.FlourishingFinish, 3.5f) || finishingMoveReady && Esprit <= 10 || (burstEnding || !DanceDance) && Esprit >= 50)
                return TillanaPvE.CanUse(out act);

            return SetActToNull(out act);
        }


        private bool TryUseLastDance(out IAction? act)
        {
            var finishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady) && FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneCharge(3);
            var standardReady = StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneCharge(3);

            if (Player.HasStatus(true, StatusID.LastDanceReady) &&
                TechnicalStepPvE.Cooldown.WillHaveOneCharge(15) || DanceDance && Esprit >= 70)
            {
                ShouldUseLastDance = false;
            }

            if (DanceDance && Esprit < 50 || finishingMoveReady || standardReady || IsStatusEnding(StatusID.LastDanceReady, 4))
            {
                ShouldUseLastDance = true;
            }

            return ShouldUseLastDance switch
            {
                false => SetActToNull(out act),
                true => LastDancePvE.CanUse(out act),
            };
        }

        private bool TryUseFinishingMove(out IAction? act)
        {
            // First, check if we have the status
            var hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);

            // Return early if we don't have the status
            if (!hasFinishingMove) return SetActToNull(out act);

            // During burst window (Technical + Devilment)
            if (DanceDance && Player.HasStatus(true, StatusID.LastDanceReady))
            {
                return FinishingMovePvE.CanUse(out act);
            }

            // Outside burst window, use if ready
            return FinishingMovePvE.CanUse(out act) || SetActToNull(out act);
        }

        private bool TryUseStarfallDance(out IAction? act)
        {
            // Check if the proc is active and about to end.
            var starfallEnding = IsStatusEnding(StatusID.FlourishingStarfall, 7);

            // Use Starfall Dance if the proc is about to expire or if dance steps are not ready.
            if (starfallEnding || Esprit < 80)
                return StarfallDancePvE.CanUse(out act);

            return SetActToNull(out act);
        }

        private bool TryUseSaberDanceBurst(out IAction? act)
        {
            return Esprit >= 50 ? SaberDancePvE.CanUse(out act) : SetActToNull(out act);
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
                   ExecuteStepGCD(out act) ||
                   FinishTheDance(out act) ||
                   ProcHelper(out act) ||
                   FeatherGCDHelper(out act) ||
                   TechGCD(out act, Player.HasStatus(true, StatusID.Devilment)) ||
                   TryUseTechnicalStep(out act) ||
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
            return BloodshowerPvE.CanUse(out act) ||
                   RisingWindmillPvE.CanUse(out act) ||
                   FountainfallPvE.CanUse(out act) ||
                   ReverseCascadePvE.CanUse(out act) ||
                   FountainPvE.CanUse(out act) ||
                   CascadePvE.CanUse(out act);
        }

        private bool FeatherGCDHelper(out IAction? act)
        {
            var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                                 Player.HasStatus(true, StatusID.SilkenSymmetry);
            var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) ||
                                      Player.HasStatus(true, StatusID.FlourishingSymmetry);

            if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < 50 && !DanceDance)
                return FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act);
            if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > 50 && !DanceDance)
                return SaberDancePvE.CanUse(out act);
            return SetActToNull(out act);
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
            if ((Esprit >= 70 && !(danceCooldown && danceReady)) || (Feathers > 3 & hasProcs && Esprit >= 50) || IsMedicated || !DanceDance && Player.HasStatus(true, StatusID.FlourishingFinish) && Esprit >= 50)
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
            var starfallEnding = IsStatusEnding(StatusID.FlourishingStarfall, 7) ||
                                 IsStatusEnding(StatusID.Devilment, 7);
            var silkenFlowEnding = IsStatusEnding(StatusID.SilkenFlow, 5);
            var silkenSymmetryEnding = IsStatusEnding(StatusID.SilkenSymmetry, 5);
            var flourishingFlowEnding = IsStatusEnding(StatusID.FlourishingFlow, 5);
            var flourishingSymmetryEnding = IsStatusEnding(StatusID.FlourishingSymmetry, 5);
            var useProc = starfallEnding || silkenFlowEnding || silkenSymmetryEnding ||
                          flourishingFlowEnding || flourishingSymmetryEnding;

            return useProc switch
            {
                true when starfallEnding => StarfallDancePvE.CanUse(out act),
                true when silkenSymmetryEnding || flourishingSymmetryEnding => FountainfallPvE.CanUse(out act),
                true when silkenFlowEnding || flourishingFlowEnding => ReverseCascadePvE.CanUse(out act),
                _ => SetActToNull(out act)
            };
        }
        #endregion

        #region OGCD Helpers
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

            return TryUseFlourish(out act) ||
                   FanDanceIiiPvE.CanUse(out act) ||
                   TryUseFeathers(out act) ||
                   FanDanceIvPvE.CanUse(out act, skipAoeCheck: true) ||
                   SetActToNull(out act);
        }


        /// <summary>
        ///     Handles the logic for using the Flourish action.
        /// </summary>
        /// <param name="act">The action to be performed, if any.</param>
        /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
        private bool TryUseFlourish(out IAction? act)
        {
            var burstReady = TechnicalStepPvE.CanUse(out _) || DevilmentPvE.CanUse(out _);

            if (!InCombat || Player.HasStatus(true, StatusID.ThreefoldFanDance) || burstReady || !ShouldUseTechStep)
                ShouldUseFlourish = false;


            if (DanceDance || TechnicalStepPvE.Cooldown.ElapsedAfter(67))
                ShouldUseFlourish = true;

            return ShouldUseFlourish ? FlourishPvE.CanUse(out act) : SetActToNull(out act);
        }

        /// <summary>
        /// Determines whether feathers should be used based on the next GCD action and current player status.
        /// </summary>
        /// <param name="act"> The action to be performed, if any.</param>
        /// <returns>True if a feather action was performed; otherwise, false.</returns>
        private bool TryUseFeathers(out IAction? act)
        {
            var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                           Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                           Player.HasStatus(true, StatusID.FlourishingFlow) ||
                           Player.HasStatus(true, StatusID.FlourishingSymmetry);
            var hasDevilment = Player.HasStatus(true, StatusID.Devilment);
            var hasEnoughFeathers = Feathers > 3;
            var noThreefoldFanDance = !Player.HasStatus(true, StatusID.ThreefoldFanDance);

            if ((hasDevilment || hasEnoughFeathers && hasProcs  && !ShouldHoldForTechnicalStep() || IsMedicated) &&
                noThreefoldFanDance)
                return FanDanceIiPvE.CanUse(out act) ||
                       FanDancePvE.CanUse(out act);

            return SetActToNull(out act);
        }


        private static bool SetActToNull(out IAction? act)
        {
            act = null;
            return false;
        }

        #endregion

        #endregion
}