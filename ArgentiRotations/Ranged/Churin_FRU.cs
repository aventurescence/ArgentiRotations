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

    //Phase Start Times
    public static float FatebreakerStartTime { get; set; }
    public static float UsurperStartTime { get; set; }
    public static float AddsStartTime { get; set; }
    public static float PandoraStartTime { get; set; }
    //Timer Getter/Setters
    public static float CurrentDowntimeStart { get; set; }
    public static float CurrentDowntimeEnd { get; set; }
    public static float CurrentPhaseStart { get; set; }
    public static float CurrentPhaseEnd { get; set; }

    // Downtime Timers
    private static bool IsUtopianSky() => IsDowntime(FRUBoss.Fatebreaker,UtopianSkyStart, UtopianSkyEnd, Downtime.UtopianSky);
    private static bool IsDiamondDust() => IsDowntime(FRUBoss.Usurper, DiamondDustStart, DiamondDustEnd, Downtime.DiamondDust);
    private static bool IsLightRampant() => IsDowntime(FRUBoss.Usurper, LightRampantStart, LightRampantEnd, Downtime.LightRampant);
    private static bool IsUltimateRelativity() => IsDowntime(FRUBoss.Gaia, UltimateRelativityStart, UltimateRelativityEnd, Downtime.UltimateRelativity);
    private static bool IsCrystalizeTime() => IsDowntime(FRUBoss.Lesbians, CrystalizeTimeStart, CrystalizeTimeEnd,Downtime.CrystalizeTime);
    private static bool IsNoDowntime() => currentDowntime == Downtime.None;

    //FRU Specific Conditions
    public static readonly bool hasSpellinWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
    public static readonly bool hasReturn = Player.HasStatus(false, StatusID.Return);
    public static readonly bool returnEnding = Player.WillStatusEnd(5, false, StatusID.Return);
    public static readonly bool hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
    bool RemoveFinishingMove = false;
    bool weBall = true;
    #endregion

    #region FRU Methods
    public static Downtime CheckCurrentDowntime()
    {
        if ((IsInFRU || TestingFRUModule) && InCombat && CheckDowntimeConditions())
        {
            return currentDowntime;
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
            IsCrystalizeTime,
            IsNoDowntime
        ];

        return downtimeChecks.Any(check => check());
    }

    private static bool IsDowntime(FRUBoss boss, float start, float end, Downtime downtime)
    {
        if (currentBoss == boss && CombatElapsedLess(end) && !CombatElapsedLess(start))
        {
            CurrentDowntimeStart = start;
            CurrentDowntimeEnd = end;
            currentDowntime = downtime;
            return true;
        }
        return false;
    }

    public static FRUBoss CheckFRUPhase()
    {
        if (IsInFRU && InCombat)
        {
            var hostileTargetNames = AllHostileTargets.Select(obj => obj.Name.ToString());
            if (CheckPhase(FRUBoss.Fatebreaker, hostileTargetNames, "Fatebreaker", 160.1f)) return currentBoss;
            if (CheckPhase(FRUBoss.Usurper, hostileTargetNames, "Usurper of Frost", 185f, FRUBoss.Fatebreaker)) return currentBoss;
            if (CheckPhase(FRUBoss.Adds, hostileTargetNames, "Crystal of Light", 41.2f, FRUBoss.Usurper)) return currentBoss;
            if (CheckPhase(FRUBoss.Gaia, hostileTargetNames, "Oracle of Darkness", 157.9f, FRUBoss.Adds)) return currentBoss;
            if (CheckPhase(FRUBoss.Lesbians, hostileTargetNames, "Usurper of Frost", 175.9f, FRUBoss.Gaia)) return currentBoss;
            if (CheckPhase(FRUBoss.Pandora, hostileTargetNames, "Pandora", 271.9f, FRUBoss.Lesbians)) return currentBoss;
        }
        return currentBoss;
    }

    private static bool CheckPhase(FRUBoss boss, IEnumerable<string> hostileTargetNames, string phaseName, float duration, FRUBoss? requiredPreviousBoss = null)
    {
        if (hostileTargetNames.Contains(phaseName) && (requiredPreviousBoss == null || currentBoss == requiredPreviousBoss))
        {
            SetPhase(boss, duration);
            return true;
        }
        return false;
    }

    private static void SetPhase(FRUBoss boss, float duration)
    {
        CurrentPhaseStart = CombatTime;
        CurrentPhaseEnd = CombatTime + duration;
        currentBoss = boss;
    }

    private void CheckFRULogic()
    {
        if (LoadFRU && InCombat)
        {
            switch (currentBoss)
            {
                case FRUBoss.Fatebreaker:
                    HandleFatebreakerLogic();
                break;
                case FRUBoss.Usurper:
                    HandleUsurperLogic();
                break;
                case FRUBoss.Adds:
                    HandleAddsLogic();
                break;
                case FRUBoss.Gaia:
                    HandleGaiaLogic();
                break;
                case FRUBoss.Lesbians:
                    HandleLesbiansLogic();
                break;
                case FRUBoss.Pandora:
                    HandlePandoraLogic();
                break;
            }
        }
    }
    private void HandleFatebreakerLogic()
    {
        switch(currentDowntime)
        {
            case Downtime.UtopianSky:
            {
                shouldUseStandardStep = true;
                shouldUseFlourish = true;
                shouldFinishingMove = false;
                    
                if (hasFinishingMove)
                    {
                        RemoveFinishingMove = true;
                    }
            }
            break;
            case Downtime.None:
                {
                    HandleNoDowntime();
                }
            break;
        }
    }
    private void HandleUsurperLogic()
    {
        shouldUseStandardStep = true;
        shouldUseFlourish = true;

        switch(currentDowntime)
        {
            case Downtime.DiamondDust:
                HandleDiamondDustDowntime();
                break;
            case Downtime.LightRampant:
                HandleLightRampantDowntime();
                break;
            case Downtime.None:
                HandleNoDowntime();
                break;
        }
    }
    private void HandleDiamondDustDowntime()
    {
        shouldFinishingMove = false;
        if (FlourishPvE.Cooldown.IsCoolingDown && hasFinishingMove)
        {
            RemoveFinishingMove = true;
        }
        if (CurrentDowntimeEnd - CombatTime <= 15)
        {
            shouldUseStandardStep = true;
        }
    }
    private void HandleLightRampantDowntime()
    {
        shouldUseFlourish = true;
        shouldFinishingMove = false;
        if (CurrentDowntimeEnd - CombatTime <= 15 && FlourishPvE.Cooldown.IsCoolingDown && Player.HasStatus(true, StatusID.FinishingMoveReady))
        {
            RemoveFinishingMove = true;
            shouldUseStandardStep = true;
        }
    }
    private void HandleNoDowntime()
    {
        shouldUseTechStep = true;

        switch (currentBoss)
        {
            case FRUBoss.Fatebreaker:
                HandleNoDowntimeForFatebreaker();
            break;
            case FRUBoss.Usurper:
                HandleNoDowntimeForUsurper();
            break;
            case FRUBoss.Adds:
                HandleNoDowntimeForAdds();
            break;
            case FRUBoss.Gaia:
                HandleNoDowntimeForGaia();
            break;
            case FRUBoss.Lesbians:
                HandleNoDowntimeForLesbians();
            break;
            case FRUBoss.Pandora:
                HandleNoDowntimeForPandora();
            break;
        }
    }
    private void HandleNoDowntimeForFatebreaker()
    {
        shouldUseStandardStep = true;
        shouldUseTechStep = true;
        shouldFinishingMove = true;
        // If Fatebreaker is dying
        if (DanceDance && HostileTarget != null && (HostileTarget.IsDying() || HostileTarget.GetHealthRatio() < 0.35f))
        {
            shouldUseFlourish = false;
            shouldUseStandardStep = false;
            shouldFinishingMove = false;
        }
    }
    private void HandleNoDowntimeForUsurper()
    {
        if (CombatElapsedLess(DiamondDustStart - 5))
            {
                shouldUseStandardStep = true;
                shouldUseFlourish = true;
            }
        if (CombatElapsedLess(LightRampantStart - 5))
            {
                shouldUseStandardStep = true;
                shouldUseFlourish = true;
            }
        if ((StepFinishReady && CombatTime - CurrentPhaseEnd > 5) || Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 1 && CombatTime - LightRampantEnd > 3)
            {
                weBall = true;
            }
    }
    private void HandleNoDowntimeForAdds()
    {
        shouldUseTechStep = true;
        shouldUseFlourish = true;
        shouldUseStandardStep = true;
        shouldFinishingMove = true;
    }
    private void HandleNoDowntimeForGaia()
    {
        shouldUseStandardStep = true;
        shouldUseFlourish = true;
        shouldFinishingMove = true;
    }
    private void HandleNoDowntimeForLesbians()
    {
        shouldUseStandardStep = true;
        shouldUseFlourish = true;
        shouldFinishingMove = true;
        shouldUseTechStep = false;
        CheckOracleTargetable();
    }
    private void CheckOracleTargetable()
    {
        if (OracleTargetable - CombatTime < 7)
        {
            shouldUseTechStep = true;
        }
    }
    private void HandleNoDowntimeForPandora()
    {
        shouldUseTechStep = true;
        shouldUseFlourish = true;
        shouldFinishingMove = true;
        shouldUseStandardStep = true;
    }
    private void HandleAddsLogic()
    {
        HandleNoDowntime();
    }
    private void HandleGaiaLogic()
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
                        HandleNoDowntime();
                    }
                break;
        }
    }
    private void HandleLesbiansLogic()
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
                        HandleNoDowntime();
                    }
                break;
            }
    }
    private void HandlePandoraLogic()
    {
        HandleNoDowntime();
    }
    
    #endregion
}