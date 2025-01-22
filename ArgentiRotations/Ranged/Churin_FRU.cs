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
    private static bool TestingFRUModule { get; set; } = false;

    // Downtime Timers
    public static readonly float UtopianSkyStart = 35f;
    public static readonly float UtopianSkyEnd = 80f;

    private static float _diamondDustStart;
    public static float DiamondDustStart
    {
        get => _diamondDustStart;
        set
        {
            _diamondDustStart = value;
            DiamondDustEnd = _diamondDustStart + 36.9f;
            LightRampantStart = DiamondDustEnd + 60f;
        }
    }
    public static float DiamondDustEnd { get; private set; }

    private static float _lightRampantStart;
    public static float LightRampantStart
    {
        get => _lightRampantStart;
        set
        {
            _lightRampantStart = value;
            LightRampantEnd = _lightRampantStart + 29f;
        }
    }
    public static float LightRampantEnd { get; private set; }

    private static float _gaiaStartTime;
    public static float GaiaStartTime
    {
        get => _gaiaStartTime;
        set
        {
            _gaiaStartTime = value;
            UltimateRelativityStart = _gaiaStartTime + 18.3f;
            UltimateRelativityEnd = UltimateRelativityStart + 43.9f;
        }
    }
    public static float UltimateRelativityStart { get; private set; }
    public static float UltimateRelativityEnd { get; private set; }

    private static float _lesbiansStartTime;
    public static float LesbiansStartTime
    {
        get => _lesbiansStartTime;
        set
        {
            _lesbiansStartTime = value;
            OracleTargetable = _lesbiansStartTime + 25.4f;
            CrystalizeTimeStart = _lesbiansStartTime + 98.5f;
            CrystalizeTimeEnd = CrystalizeTimeStart + 49.7f;
        }
    }
    public static float OracleTargetable { get; private set; }
    public static float CrystalizeTimeStart { get; private set; }
    public static float CrystalizeTimeEnd { get; private set; }

    // Boss Timers
    public static float FatebreakerKillTime;
    public static float UsurperKillTime;
    public static float AddsKillTime;
    public static float GaiaKillTime;
    public static float LesbiansKillTime;
    public static float PandoraKillTime;
    public static float FatebreakerStartTime;
    public static float UsurperStartTime;
    public static float AddsStartTime;
    public static float currentDowntimeStart;
    public static float currentDowntimeEnd;
    public static float currentPhaseStart;
    public static float currentPhaseEnd;
    private static bool IsUtopianSky() => IsDowntime(FRUBoss.Fatebreaker,UtopianSkyStart, UtopianSkyEnd, Downtime.UtopianSky);
    private static bool IsDiamondDust() => IsDowntime(FRUBoss.Usurper, DiamondDustStart, DiamondDustEnd, Downtime.DiamondDust);
    private static bool IsLightRampant() => IsDowntime(FRUBoss.Usurper, LightRampantStart, LightRampantEnd, Downtime.LightRampant);
    private static bool IsUltimateRelativity() => IsDowntime(FRUBoss.Gaia, UltimateRelativityStart, UltimateRelativityEnd, Downtime.UltimateRelativity);
    private static bool IsCrystalizeTime() => IsDowntime(FRUBoss.Lesbians, CrystalizeTimeStart, CrystalizeTimeEnd,Downtime.CrystalizeTime);

    bool hasSpellinWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
    bool hasReturn = Player.HasStatus(false, StatusID.Return);
    bool returnEnding = Player.WillStatusEnd(5, false, StatusID.Return);
    bool RemoveFinishingMove = false;
    bool hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
    bool weBall = true;
    #endregion

    #region FRU Methods
    public static Downtime CheckCurrentDowntime()
    {
        if ((IsInFRU || TestingFRUModule) && InCombat)
        {
            if (CheckDowntimeConditions()) return currentDowntime;
        }
        currentDowntime = Downtime.None;
        return currentDowntime;
    }

    private static bool CheckDowntimeConditions()
    {
        Func<bool>[] downtimeChecks =
        [
            IsUtopianSky,
            IsDiamondDust,
            IsLightRampant,
            IsUltimateRelativity,
            IsCrystalizeTime
        ];

    foreach (var check in downtimeChecks)
    {
        if (check()) return true;
    }

    return false;
    }
    private static bool IsDowntime(FRUBoss boss, float start, float end, Downtime downtime)
    {
        if (currentBoss == boss && CombatElapsedLess(end) && !CombatElapsedLess(start))
        {
            currentDowntimeStart = start;
            currentDowntimeEnd = end;
            currentDowntime = downtime;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks the current phase of the FRU encounter.
    /// </summary>
    /// <returns>The current FRUBoss phase.</returns>
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
                    FatebreakerStartTime = CombatTime;
                    currentPhaseStart = FatebreakerStartTime;
                    currentPhaseEnd = FatebreakerStartTime + 160.1f;
                    currentBoss = FRUBoss.Fatebreaker;
                    return currentBoss;
                }
                if ((obj.Name.ToString() == FRUPhase1Name && currentBoss == FRUBoss.Fatebreaker && obj.IsDead) || (obj.Name.ToString() == FRUPhase2Name && currentBoss == FRUBoss.Fatebreaker))
                {
                    UsurperStartTime = CombatTime;
                    FatebreakerKillTime = UsurperStartTime - 3f;
                    currentBoss = FRUBoss.Usurper;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUAddPhaseName && currentBoss == FRUBoss.Usurper)
                {
                    AddsStartTime = CombatTime;
                    UsurperKillTime = AddsStartTime - 25.8f;
                    currentBoss = FRUBoss.Adds;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase3Name && currentBoss == FRUBoss.Adds)
                {
                    GaiaStartTime = CombatTime;
                    AddsKillTime  = GaiaTransitionStart - 25.6f;
                    currentBoss = FRUBoss.Gaia;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase4Name && currentBoss == FRUBoss.Gaia)
                {
                    LesbiansStartTime = CombatTime;
                    GaiaKillTime = LesbiansStartTime - 8.8f;
                    currentBoss = FRUBoss.Lesbians;
                    return currentBoss;
                }
                if (obj.Name.ToString() == FRUPhase5Name && currentBoss == FRUBoss.Lesbians)
                {
                    PandoraStartTime = CombatTime;
                    LesbiansKillTime = PandoraStartTime - 76.1f;
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
                    break;
            }
        }
        return false;
    }
  
    private bool HandleFatebreakerLogic()
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
                    shouldUseTechStep = true;
                    // If Fatebreaker is dying
                    if (DanceDance && HostileTarget != null && (HostileTarget.IsDying() || HostileTarget.GetHealthRatio() < 0.35f))
                    {
                        shouldUseFlourish = false;
                        shouldUseStandardStep = false;
                        shouldFinishingMove = false;
                    }
                }
                break;
            }

        return HandleFatebreakerLogic();
    }

    private bool HandleUsurperLogic()
    {
        shouldUseStandardStep = true;
        shouldUseFlourish = true;
        Downtime currentDowntime = CheckCurrentDowntime();
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
                        if (currentDowntimeEnd - CombatTime <= 15)
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
                    shouldUseTechStep = true;
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
                    if ((StepFinishReady && CombatTime - LightRampantEnd > 5) || Player.HasStatus(true,StatusID.StandardStep) && CompletedSteps == 1 && CombatTime - LightRampantEnd > 3)
                    {
                        weBall = true;
                    }
                }
                break;
            }
        }

        return HandleUsurperLogic();
    }

    private bool HandleAddsLogic()
    {
        Downtime currentDowntime = CheckCurrentDowntime();
        {
           switch (currentDowntime)
           { 
            case Downtime.None:
            {
                shouldUseTechStep = true;
                shouldUseStandardStep = true;
                shouldUseFlourish = true;
                shouldFinishingMove = true;
            }
        break;
            }

        return HandleAddsLogic();

        }
    }

    private bool HandleGaiaLogic()
    {
        Downtime currentDowntime = CheckCurrentDowntime();
        {
        switch (currentDowntime)
            {
                case Downtime.UltimateRelativity:
                    {
                        shouldUseStandardStep = true;
                        shouldUseFlourish = false;
                        shouldFinishingMove = true;
                        if (hasSpellinWaitingReturn || (hasReturn && !returnEnding))
                        {
                           shouldUseTechStep = false;
                        }
                        else
                        {
                            if (returnEnding)
                            {
                                shouldUseTechStep = true;
                            }
                        }
                    }
                break;
                case Downtime.None:
                    {
                        shouldUseStandardStep = true;
                        shouldUseFlourish = true;
                        shouldFinishingMove = true;
                    }
                break;
            }
        return HandleAddsLogic();
        }
    }

    private bool HandleLesbiansLogic()
    {
        Downtime currentDowntime = CheckCurrentDowntime();
        {
            switch (currentDowntime)
            {
                case Downtime.CrystalizeTime:
                    {
                        shouldUseStandardStep = false;
                        shouldUseFlourish = false;
                        shouldFinishingMove = false;
                        if (CrystalizeTimeEnd - CombatTime <=5 )
                        {
                            shouldUseStandardStep = true;
                            shouldUseFlourish = true;
                            shouldFinishingMove = true;
                        }
                    }
                break;
                case Downtime.None:
                    {
                        shouldUseStandardStep = true;
                        shouldUseFlourish = true;
                        shouldFinishingMove = true;
                        shouldUseTechStep = false;
                        if (NumberOfHostilesInRange < 2)
                        {
                            shouldUseTechStep = false;
                        }
                        else
                        {
                            if (OracleTargetable - CombatTime < 7)
                            {
                                shouldUseTechStep = true;
                            }
                            else
                            {
                                shouldUseTechStep = false;
                            }
                            return false;

                        }
                    break;
                    }
            }
        }
        return HandleLesbiansLogic();
    }

    private bool HandlePandoraLogic()
    {
        Downtime currentDowntime = CheckCurrentDowntime();
        // Implement Pandora phase logic here
        return HandlePandoraLogic();
    }
    
    #endregion
}