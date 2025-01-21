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
    public static float downtimeEnd;

    bool hasSpellinWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
    bool hasReturn = Player.HasStatus(false, StatusID.Return);
    bool returnEnding = Player.WillStatusEnd(5, false, StatusID.Return);
    bool RemoveFinishingMove = false;
    bool hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
    bool weBall = true;
    #endregion

    #region FRU Methods
    public static Downtime CheckPhaseEnding()
    {
        if ((IsInFRU || TestingFRUModule) && InCombat)
        {
            if (currentBoss == FRUBoss.Fatebreaker && CombatElapsedLess(UtopianSkyEnd) && !CombatElapsedLess(UtopianSkyStart))
            {
                downtimeEnd = UtopianSkyEnd;
                currentDowntime = Downtime.UtopianSky; 
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Usurper && CombatElapsedLess(DiamondDustEnd) && !CombatElapsedLess(DiamondDustStart))
            {
                downtimeEnd = DiamondDustEnd;
                currentDowntime = Downtime.DiamondDust;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Usurper && CombatElapsedLess(LightRampantEnd) && !CombatElapsedLess(LightRampantStart))
            {
                downtimeEnd = LightRampantEnd;
                currentDowntime = Downtime.LightRampant;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Gaia && CombatElapsedLess(UltimateRelativityEnd) && !CombatElapsedLess(UltimateRelativityStart))
            {
                downtimeEnd = UltimateRelativityEnd;
                currentDowntime = Downtime.UltimateRelativity;
                return currentDowntime;
            }
            if (currentBoss == FRUBoss.Lesbians && CombatElapsedLess(CrystalizeTimeEnd) && !CombatElapsedLess(CrystalizeTimeStart))
            {
                downtimeEnd = CrystalizeTimeEnd;
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
    private bool CheckFRULogic()
    {
        if (LoadFRU && InCombat)
        {
            switch (currentBoss)
            {
                case FRUBoss.Fatebreaker:
                    return HandleFatebreakerLogic();

                case FRUBoss.Usurper:
                    return HandleUsurperLogic();

                case FRUBoss.Adds:
                    return HandleAddsLogic();

                case FRUBoss.Gaia:
                    return HandleGaiaLogic();

                case FRUBoss.Lesbians:
                    return HandleLesbiansLogic();

                case FRUBoss.Pandora:
                    return HandlePandoraLogic();

                case FRUBoss.None:
                    return false;
            }
        }
        return false;
    }
    private bool HandleFatebreakerLogic()
    {
        if (!areDanceTargetsInRange)
        {
            switch(currentDowntime)
            {
                case Downtime.UtopianSky:
                     {
                        shouldUseStandardStep = true;
                        shouldUseFlourish = true;
                        shouldFinishingMove = false;
                    
                        if (FlourishPvE.Cooldown.IsCoolingDown && hasFinishingMove)
                        {
                            RemoveFinishingMove = true;
                        }
                     }
                break;
                case Downtime.None:
                {
                    shouldUseStandardStep = true;
                    shouldUseFlourish = true;
                }
                break;
            }
        }

        // If Fatebreaker is dying
        if (DanceDance && HostileTarget != null && (HostileTarget.IsDying() || HostileTarget.GetHealthRatio() < 0.35f))
        {
            shouldUseFlourish = false;
            shouldUseStandardStep = false;
            shouldFinishingMove = false;
        }

        return false;
    }

    private bool HandleUsurperLogic()
    {
        shouldUseStandardStep = true;
        shouldUseFlourish = true;
        Downtime currentDowntime = CheckPhaseEnding();

        if (!areDanceTargetsInRange)
        {
            switch(currentDowntime)
            {
                case Downtime.DiamondDust:
                     {
                        shouldFinishingMove = false;
                        if (FlourishPvE.Cooldown.IsCoolingDown && hasFinishingMove)
                        {
                            RemoveFinishingMove = true;
                        }
                        if (DiamondDustEnd - CombatTime <= 15)
                        {
                            shouldUseStandardStep = true;
                        }
                     }
                break;
                case Downtime.LightRampant:
                    {
                        shouldUseFlourish = true;
                        shouldFinishingMove = false;
                        if (downtimeEnd - CombatTime <= 15)
                        {
                            if (FlourishPvE.Cooldown.IsCoolingDown && Player.HasStatus(true,StatusID.FinishingMoveReady))
                            {
                            RemoveFinishingMove = true;
                            shouldUseStandardStep = true;
                            }
                        }
                     }
                break;
                case Downtime.None:
                {
                    if ((StepFinishReady && CombatTime - LightRampantEnd > 5) || Player.HasStatus(true,StatusID.StandardStep) && CompletedSteps == 1 && CombatTime - LightRampantEnd > 3)
                    {
                        return weBall = true;
                    }
                }
                break;
            }
        }

        // Before Diamond Dust starts
        if (CombatElapsedLess(DiamondDustStart - 5))
        {
            shouldUseStandardStep = true;
            shouldUseFlourish = true;
        }

        // Before Light Rampant starts
        if (CombatElapsedLess(LightRampantStart - 5))
        {
            shouldUseStandardStep = true;
            shouldUseFlourish = true;
        }

        return false;
    }

    private bool HandleAddsLogic()
    {
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Adds phase logic here
        return false;
    }

    private bool HandleGaiaLogic()
    {
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Gaia phase logic here
        return false;
    }

    private bool HandleLesbiansLogic()
    {
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Lesbians phase logic here
        return false;
    }

    private bool HandlePandoraLogic()
    {
        Downtime currentDowntime = CheckPhaseEnding();
        // Implement Pandora phase logic here
        return false;
    }
    #endregion
}