using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

[Rotation("ChurinDNC", CombatType.PvE, GameVersion = "7.15", Description = "Only for level 100 content, ok?")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDNC : DancerRotation
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
    public enum FRUBoss
    {
        None,
        Fatebreaker,
        Usurper,
        Adds,
        Gaia,
        Lesbians,
        Pandora
    }
    public enum Downtime
    {
        None,
        UtopianSky,
        DiamondDust,
        LightRampant,
        UltimateRelativity,
        CrystalizeTime,
    }
    private static Downtime currentDowntime = Downtime.None;
    private static FRUBoss currentBoss = FRUBoss.None;
    public static float UtopianSkyStart = 35f;
    public static float UtopianSkyEnd = 80f;
    public static float DiamondDustStart = UsurperStartTime + 35.1f;
    public static float DiamondDustEnd = DiamondDustStart + 36.9f;
    public static float LightRampantStart = DiamondDustEnd + 60f;
    public static float LightRampantEnd = LightRampantStart + 29f;
    public static float GaiaTransitionStart = AddsStartTime + 52.5f;
    public static float UltimateRelativityStart = GaiaStartTime + 18.3f;
    public static float UltimateRelativityEnd = UltimateRelativityStart + 53.9f;
    public static float CrystalizeTimeStart = LesbiansStartTime + 98.5f;
    public static float CrystalizeTimeEnd = CrystalizeTimeStart + 49.7f;

    public static float FatebreakerKillTime;
    public static float UsurperKillTime;
    public static float AddsKillTime;
    public static float GaiaKillTime;
    public static float LesbiansKillTime;
    public static float PandoraKillTime;
    public static float UsurperStartTime = FatebreakerKillTime + 3f;
    public static float AddsStartTime = UsurperKillTime + 25.8f;
    public static float GaiaStartTime = GaiaTransitionStart + 25.6f;
    public static float LesbiansStartTime = GaiaKillTime + 8.8f;
    public static float PandoraStartTime = LesbiansKillTime + 76.1f;

    bool shouldUseLastDance = true;
    bool AboutToDance => StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    bool DanceDance => Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish);
    bool StandardReady => StandardStepPvE.Cooldown.ElapsedAfter(28);
    bool TechnicalReady => TechnicalStepPvE.Cooldown.ElapsedAfter(118);
    bool StepFinishReady => Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 || Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4;
    bool areDanceTargetsInRange = AllHostileTargets.Any(hostile => hostile.DistanceToPlayer() < 14);

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
        ImGui.Text("Time To Kill:" + AverageTimeToKill);
        ImGui.Text("Combat Time:" + CombatTime);
        ImGui.TextColored(ImGuiColors.DalamudRed, "Current Downtime:" + CheckPhaseEnding());
        ImGui.Text("Finish the Dance?:" + FinishTheDance(out _));
        ImGui.Text("Can Use Flourish:" + ShouldUseFlourish(out _));
        ImGui.Text("Can Standard Step:" + UseStandardStep(out _));
        ImGui.Text("Dance Targets In Range?:" + areDanceTargetsInRange);
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
        act = null;
        // Check for FRU Phase and execute logic
        // If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance
        if (IsDancing || AboutToDance) return false;

        // Prevent triple weaving by checking if an action was just used
        if (nextGCD.AnimationLockTime > 0.75f) return false;

        // Check for conditions to use Flourish
        if (DanceDance || TechnicalFinishPvE.Cooldown.WillHaveOneCharge(69f))
            {
                if (!Player.HasStatus(true, StatusID.ThreefoldFanDance) && FlourishPvE.CanUse(out act))
                {
                    return true;
                }
            }
        

        // Attempt to use Fan Dance III if available
        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;

        IAction[] FeathersGCDs = [ReverseCascadePvE, FountainfallPvE, RisingWindmillPvE, BloodshowerPvE];

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
        FRUBoss currentBoss = CheckFRUPhase();
        Downtime currentDowntime = CheckPhaseEnding();
        
        // Attempt to use Closed Position if applicable
        if (!InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act))
        {

            if (DancePartnerName != "")
                foreach (var player in PartyMembers)
                    if (player.Name.ToString() == DancePartnerName)
                        ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);

            return true;
        }
        if (CheckFRULogic(currentBoss, out act)) return true;

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

        bool hasSpellinWaitingReturn = Player.HasStatus(false, (StatusID)4208);
        bool hasReturn = Player.HasStatus(false, (StatusID)4252);
        bool canUseTechnicalStep = TechnicalStepPvE.CanUse(out act, skipAoeCheck: true);
        bool returnEnding = Player.WillStatusEnd(5f, false, (StatusID)4252);

        if (currentBoss == FRUBoss.Gaia && currentDowntime == Downtime.UltimateRelativity && (hasSpellinWaitingReturn || (hasReturn && !returnEnding && canUseTechnicalStep)))
        {
            return false;
        }
        
        if (currentBoss == FRUBoss.Gaia && currentDowntime == Downtime.UltimateRelativity && hasReturn && returnEnding && canUseTechnicalStep)
        {
            return true;
        }

        

        // Use Technical Step in burst mode if applicable
        if (HoldTechForTargets)
        {
            if (HasHostilesInMaxRange && IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))

            {
                return true;
            }
        }
        else
        {
            if (IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
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
        act = null;

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
        if (DanceDance && (StandardStepPvE.Cooldown.WillHaveOneCharge(3) || FinishingMovePvE.Cooldown.WillHaveOneCharge(3) || Esprit <= 50))
        {
            shouldUseLastDance = true;
        }

        if (DanceDance)
        {
            if (Esprit >= 50 && DanceOfTheDawnPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (Esprit >= 60 && SaberDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
            // Make sure Starfall gets used before end of party buffs
            if (DevilmentPvE.Cooldown.ElapsedAfter(10) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
            // Make sure to FM with enough time left in burst window to LD and SFD while leaving a GCD for a Sabre if needed
            if (DevilmentPvE.Cooldown.ElapsedAfter(15) && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (shouldUseLastDance)
        {
            if (LastDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        // Check FRU Logic before doing these.
        if (CheckFRULogic(currentBoss, out act)) return true;
     
        if (HoldStepForTargets)
        {
            if (HasHostilesInMaxRange && UseStandardStep(out act)) return true;
        }
        else
        {
            if (UseStandardStep(out act)) return true;
        }
        if (FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        
        // Further prioritized GCD abilities
        if (DanceDance || (Esprit >= 80 && SaberDancePvE.CanUse(out act, skipAoeCheck: true))) return true;

        if (StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (!(StandardReady || TechnicalReady) &&
            (!shouldUseLastDance || !LastDancePvE.CanUse(out act, skipAoeCheck: true)))
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
    private bool ShouldUseFlourish(out IAction? act)
    {
        if (DanceDance || (!DanceDance && TechnicalStepPvE.Cooldown.ElapsedAfter(69)))
        {
            if (!Player.HasStatus(true, StatusID.ThreefoldFanDance) && FlourishPvE.CanUse(out act))
            {
                return true;
            }
        }
        act = null;
        return false;
    }
    private bool ShouldUseFeathers(IAction nextGCD, out IAction? act)
    {
        IAction[] FeathersGCDs = { ReverseCascadePvE, FountainfallPvE, RisingWindmillPvE, BloodshowerPvE };
        if ((!DevilmentPvE.EnoughLevel || Player.HasStatus(true, StatusID.Devilment) || (Feathers > 3 && FeathersGCDs.Contains(nextGCD))) && !Player.HasStatus(true, StatusID.ThreefoldFanDance))
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }
    private bool RemoveFinishingMove()
    {
        if (!HasHostilesInMaxRange && InCombat && Player.HasStatus(true,StatusID.FinishingMoveReady) && !StandardReady)
            {
                StatusHelper.StatusOff(StatusID.FinishingMoveReady); 
                return true;
            }
        return false;
    }
    public static Downtime CheckPhaseEnding()
    {
        if ((IsInFRU || TestingFRUModule) && InCombat)
        {
            if (currentBoss == FRUBoss.Fatebreaker && CombatElapsedLess(UtopianSkyEnd) && !CombatElapsedLess(UtopianSkyStart))
            {
                currentDowntime = Downtime.UtopianSky; 
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Usurper && CombatElapsedLess(DiamondDustEnd) && !CombatElapsedLess(DiamondDustStart))
            {
                currentDowntime = Downtime.DiamondDust;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Usurper && CombatElapsedLess(LightRampantEnd) && !CombatElapsedLess(LightRampantStart))
            {
                currentDowntime = Downtime.LightRampant;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Gaia && CombatElapsedLess(UltimateRelativityEnd) && !CombatElapsedLess(UltimateRelativityStart))
            {
                currentDowntime = Downtime.UltimateRelativity;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Lesbians && CombatElapsedLess(CrystalizeTimeEnd) && !CombatElapsedLess(CrystalizeTimeStart))
            {
                currentDowntime = Downtime.CrystalizeTime;
                return currentDowntime;
            }
        }
        return currentDowntime;
    }
    
    public static FRUBoss CheckFRUPhase()
    {
        // Targets for phase detection
        string FRUPhase1Name = "Fatebreaker";
        string FRUPhase2Name = "Usurper of Frost";
        string FRUAddPhaseName = "Crystal of Light";
        string FRUPhase3Name = "Oracle of Darkness";
        string FRUPhase4Name = "Usurper of Frost";
        string FRUPhase5Name = "Pandora";

        if (IsInFRU && InCombat)
        {
            foreach (var obj in AllHostileTargets)
            {
                if (obj.Name.ToString() == FRUPhase1Name)
                {
                    currentBoss = FRUBoss.Fatebreaker;
                    return currentBoss;
                }
                if ((obj.Name.ToString() == FRUPhase1Name && currentBoss == FRUBoss.Fatebreaker && obj.IsDead) || (obj.Name.ToString() == FRUPhase2Name && currentBoss == FRUBoss.Fatebreaker))
                {
                    FatebreakerKillTime = CombatTime;
                    currentBoss = FRUBoss.Usurper;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUAddPhaseName && currentBoss == FRUBoss.Usurper)
                {
                    currentBoss = FRUBoss.Adds;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase3Name && currentBoss == FRUBoss.Adds)
                {
                    currentBoss = FRUBoss.Gaia;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase4Name && currentBoss == FRUBoss.Gaia)
                {
                    currentBoss = FRUBoss.Lesbians;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase5Name && currentBoss == FRUBoss.Lesbians)
                {
                    currentBoss = FRUBoss.Pandora;
                    return currentBoss;
                }
            }
        }
        else
        {
            if (TestingFRUModule && InCombat)
            {
                if (CombatElapsedLess(UtopianSkyStart) || (currentDowntime == Downtime.UtopianSky && CombatElapsedLess(153f) && !CombatElapsedLess(80f)))
                {
                    currentBoss = FRUBoss.Fatebreaker;
                    return currentBoss;
                }
                if ((currentBoss == FRUBoss.Fatebreaker && CombatElapsedLess(198f) && !CombatElapsedLess(80f)) || (currentDowntime == Downtime.DiamondDust && CombatElapsedLess(295f) && !CombatElapsedLess(198f)) || (currentDowntime == Downtime.LightRampant && CombatElapsedLess(349f) && !CombatElapsedLess(324f)))
                {
                    currentBoss = FRUBoss.Usurper;
                    return currentBoss;
                }
                if (currentBoss == FRUBoss.Usurper && CombatElapsedLess(374f) && !CombatElapsedLess(349f))
                {
                    currentBoss = FRUBoss.Adds;
                    return currentBoss;
                }
                if (currentBoss == FRUBoss.Adds && CombatElapsedLess(UltimateRelativityStart) && !CombatElapsedLess(GaiaStartTime))
                {
                    currentBoss = FRUBoss.Gaia;
                    return currentBoss;
                }
            }
        }

        return currentBoss;
    }
    #endregion

    #region Testing
    // Override the method for handling testing actions
    private static bool TestingFRUModule {get; set;} = false;
    private bool CheckFRULogic(FRUBoss currentBoss, out IAction? act)
    {
        if (TestingFRUModule && InCombat)
        {
            switch (currentBoss)
            {
                case FRUBoss.Fatebreaker:
                {
                    if (currentDowntime == Downtime.UtopianSky)
                    {
                        if (!StandardReady || !FlourishPvE.CanUse(out _))
                        {
                            act= null;
                            return true;
                        }
                        else
                        {
                            if (StandardReady && StandardStepPvE.CanUse(out act, skipAoeCheck: true))
                            {
                                if (ExecuteStepGCD(out act)) return true;
                                if (StandardReady && StepFinishReady && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
                            }
                            if (FlourishPvE.CanUse(out act)) return true;
                            if (RemoveFinishingMove()) return true;
                            if (FinishTheDance(out act)) return true;
                        }
                    }
                }
                break;

                case FRUBoss.Usurper:
                // Handle Usurper phase logic
                break;


                case FRUBoss.Adds:
                // Handle Adds phase logic
                break;

                case FRUBoss.Gaia:
                // Handle Gaia phase logic
                break;

                case FRUBoss.Lesbians:
                // Handle Lesbians phase logic
                break;

                case FRUBoss.Pandora:
                // Handle Pandora phase logic
                break;

        }
        act = null;
        return false;
    }
        else
        {
            if (LoadFRU && InCombat)
            {
                switch (currentBoss)
                {
                    case FRUBoss.Fatebreaker:
                    {
                    // Handle Utopian Sky Downtime
                        if (currentDowntime == Downtime.UtopianSky)
                        {
                            if (StandardStepPvE.CanUse(out act, skipAoeCheck: true))
                            {
                                if (ExecuteStepGCD(out act))
                                {
                                    if (StepFinishReady && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true) && (UtopianSkyEnd - CombatTime > 3)) return true;
                                }
                            }
                            if (FlourishPvE.CanUse(out act))
                            {
                                if (RemoveFinishingMove())
                                {
                                    if (FinishTheDance(out act)) return true;
                                }
                            }
                        }
                    }
                    break;
                    case FRUBoss.Usurper:
                    //Handle Diamond Dust && Light Rampant Downrtime
                    break;
                    case FRUBoss.Adds:
                    //Handle Adds Downtime
                    break;
                    case FRUBoss.Gaia:
                    //Handle Ultimate Relativity
                    break;
                    case FRUBoss.Lesbians:
                    //Handle Burst Logic and Crystallize Time
                    break;
                    case FRUBoss.Pandora:
                    //NAH, I'D WIN. FULL SEND EVERYTHING.
                    break;
                }
            }
        }
        act = null;
        return false;
    }
    #endregion
}

    