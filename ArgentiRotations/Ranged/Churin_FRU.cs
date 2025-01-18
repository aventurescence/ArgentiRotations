namespace ArgentiRotations.Ranged;

public sealed partial class ChurinDNC : DancerRotation
{
    #region FRU Properties
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
    private static bool TestingFRUModule {get; set;} = false;
    //Downtime Timers
    public static float UtopianSkyStart = 35f;
    public static float UtopianSkyEnd = 80f;
    public static float DiamondDustStart = UsurperStartTime + 35.1f;
    public static float DiamondDustEnd = DiamondDustStart + 36.9f;
    public static float LightRampantStart = DiamondDustEnd + 60f;
    public static float LightRampantEnd = LightRampantStart + 29f;
    public static float GaiaTransitionStart = AddsStartTime + 52.5f;
    public static float UltimateRelativityStart = GaiaStartTime + 18.3f;
    public static float UltimateRelativityEnd = UltimateRelativityStart + 43.9f;
    public static float OracleTargetable = LesbiansStartTime + 25.4f;
    public static float CrystalizeTimeStart = LesbiansStartTime + 98.5f;
    public static float CrystalizeTimeEnd = CrystalizeTimeStart + 49.7f;
    //Boss Timers
    public static float FatebreakerKillTime = UsurperStartTime - 3f;
    public static float UsurperKillTime = AddsStartTime - 25.8f;
    public static float AddsKillTime = GaiaTransitionStart - 25.6f;
    public static float GaiaKillTime = LesbiansStartTime - 8.8f;
    public static float LesbiansKillTime = PandoraStartTime - 76.1f;
    public static float PandoraKillTime;
    public static float UsurperStartTime;
    public static float AddsStartTime;
    public static float GaiaStartTime;
    public static float LesbiansStartTime;
    public static float PandoraStartTime;

    bool hasSpellinWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
    bool hasReturn = Player.HasStatus(false, StatusID.Return);
    bool returnEnding = Player.WillStatusEnd(5, false, StatusID.Return);
    #endregion

    #region FRU Methods
    private bool RemoveFinishingMove()
    {
        if (!areDanceTargetsInRange && InCombat && Player.HasStatus(true, StatusID.FinishingMoveReady))
            {
                StatusHelper.StatusOff(StatusID.FinishingMoveReady);
                {
                    return true;
                }
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
        currentDowntime = Downtime.None;
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
                    UsurperStartTime = CombatTime;
                    currentBoss = FRUBoss.Usurper;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUAddPhaseName && currentBoss == FRUBoss.Usurper)
                {
                    AddsStartTime = CombatTime;
                    currentBoss = FRUBoss.Adds;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase3Name && currentBoss == FRUBoss.Adds)
                {
                    GaiaStartTime = CombatTime;
                    currentBoss = FRUBoss.Gaia;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase4Name && currentBoss == FRUBoss.Gaia)
                {
                    LesbiansStartTime = CombatTime;
                    currentBoss = FRUBoss.Lesbians;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase5Name && currentBoss == FRUBoss.Lesbians)
                {
                    PandoraStartTime = CombatTime;
                    currentBoss = FRUBoss.Pandora;
                    return currentBoss;
                }
            }
            return currentBoss;
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
    private bool CheckFRULogic(FRUBoss currentBoss, out IAction? act)
    {
        act = null;

        if (LoadFRU && InCombat)
        {
            switch (currentBoss)
            {
                case FRUBoss.Fatebreaker:
                    return HandleFatebreakerLogic(out act);

                case FRUBoss.Usurper:
                    return HandleUsurperLogic(out act);

                case FRUBoss.Adds:
                    return HandleAddsLogic(out act);

                case FRUBoss.Gaia:
                    return HandleGaiaLogic(out act);

                case FRUBoss.Lesbians:
                    return HandleLesbiansLogic(out act);

                case FRUBoss.Pandora:
                    return HandlePandoraLogic(out act);

                case FRUBoss.None:
                    act = null;
                    return false;
            }
        }

        return false;
    }
       private bool HandleFatebreakerLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();

        if (currentDowntime == Downtime.UtopianSky)
        {
            if (!HasHostilesInRange && InCombat)
            {
                if (HoldStepForTargets)
                {
                    if (StepFinishReady && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
                else
                {
                    act = null;
                    return true;
                }
                if (FlourishPvE.CanUse(out act))
                {
                    if (RemoveFinishingMove()) return true;
                }
                else
                {
                    act = null;
                    return true;
                }
            }
        }

        // If Fatebreaker is dying
        if (DanceDance && HostileTarget != null && (HostileTarget.IsDying() || HostileTarget.GetHealthRatio() < 0.3f))
        {
            if (FlourishPvE.CanUse(out act)) return false;
            if (StandardStepPvE.CanUse(out act) || FinishingMovePvE.CanUse(out act)) return false;
        }

        return false;
    }

    private bool HandleUsurperLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();

        // Before Diamond Dust starts
        if (CombatElapsedLess(DiamondDustStart - 5))
        {
            if (StandardStepPvE.CanUse(out act)) return true;
            if (FlourishPvE.CanUse(out act)) return true;
        }

        // Before Light Rampant starts
        if (CombatElapsedLess(LightRampantStart - 5))
        {
            if (StandardStepPvE.CanUse(out act)) return true;
            if (FlourishPvE.CanUse(out act)) return true;
        }

        if (currentDowntime == Downtime.DiamondDust)
        {
            if (StandardStepPvE.CanUse(out act) && (CombatElapsedLess(DiamondDustEnd - 15) || TechnicalFinishPvE.Cooldown.WillHaveOneCharge(15))) return true;
            if (FlourishPvE.CanUse(out act)) return false;
            if (RemoveFinishingMove()) return true;
        }

        if (currentDowntime == Downtime.LightRampant)
        {
            if (StandardStepPvE.CanUse(out act) && CombatElapsedLess(LightRampantEnd - 15)) return true;
            if (FlourishPvE.CanUse(out act)) return false;
            if (RemoveFinishingMove()) return true;
        }

        return false;
    }

    private bool HandleAddsLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Adds phase logic here
        return false;
    }

    private bool HandleGaiaLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Gaia phase logic here
        return false;
    }

    private bool HandleLesbiansLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Lesbians phase logic here
        return false;
    }

    private bool HandlePandoraLogic(out IAction? act)
    {
        act = null;
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Pandora phase logic here
        return false;
    }
    #endregion
}