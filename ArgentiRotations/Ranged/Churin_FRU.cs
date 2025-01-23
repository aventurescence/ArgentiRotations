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
    public static float UtopianSkyStart { get; private set; }
    public static float UtopianSkyEnd { get; private set; }
    public static float DiamondDustStart { get; private set; }
    public static float DiamondDustEnd { get; private set; }
    public static float LightRampantStart { get; private set; }
    public static float LightRampantEnd { get; private set; }
    public static float GaiaTransitionStart { get; private set; }
    public static float GaiaTransitionEnd { get; private set; }
    public static float UltimateRelativityStart { get; private set; }
    public static float UltimateRelativityEnd { get; private set; }
    public static float OracleTargetable { get; private set; }
    public static float CrystalizeTimeStart { get; private set; }
    public static float CrystalizeTimeEnd { get; private set; }

    //Phase Start Times
    private static float _fatebreakerStartTime;
    public static float FatebreakerStartTime
    {
        get => _fatebreakerStartTime;
        set
        {
            _fatebreakerStartTime = value;
            UpdateFatebreakerTimings(value);
        }
    }

    private static float _usurperStartTime;
    public static float UsurperStartTime
    {
        get => _usurperStartTime;
        set
        {
            _usurperStartTime = value;
            UpdateUsurperTimings(value);
        }
    }

    private static float _addsStartTime;
    public static float AddsStartTime
    {
        get => _addsStartTime;
        set
        {
            _addsStartTime = value;
            UpdateAddsTimings(value);
        }
    }

    private static float _gaiaStartTime;
    public static float GaiaStartTime
    {
        get => _gaiaStartTime;
        set
        {
            _gaiaStartTime = value;
            UpdateGaiaTimings(value);
        }
    }

    private static float _lesbiansStartTime;
    public static float LesbiansStartTime
    {
        get => _lesbiansStartTime;
        set
        {
            _lesbiansStartTime = value;
            UpdateLesbiansTimings(value);
        }
    }

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
    public static readonly bool returnEnding = hasReturn && Player.WillStatusEnd(7, false, StatusID.Return);
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
        return true;
    }

    private static bool AreBothBossesDead(string bossName1, string bossName2)
    {
        return GetBossHealthRatio(bossName1) <= 1 && GetBossHealthRatio(bossName2) <= 0;
    }

    private static float GetBossHealthRatio(string bossName)
    {
        var target = AllHostileTargets.FirstOrDefault(obj => obj.Name.ToString() == bossName);
        return target != null ? target.GetHealthRatio() : 1.0f; // Return 1.0f if the boss is not found (considered alive)
    }

    private static bool IsBossDead(string bossName)
    {
        var target = AllHostileTargets.FirstOrDefault(obj => obj.Name.ToString() == bossName);
        return target != null && target.GetHealthRatio() <= 1;
    }

    public static FRUBoss CheckFRUPhase()
    {
        if (IsInFRU && InCombat)
        {
            var hostileTargetNames = AllHostileTargets.Select(obj => obj.Name.ToString()).ToList();
            if (CheckPhase(FRUBoss.Fatebreaker, hostileTargetNames, "Fatebreaker", 160.1f)) return currentBoss;
            if (CheckPhase(FRUBoss.Usurper, hostileTargetNames, "Usurper of Frost", 185f, FRUBoss.Fatebreaker)) return currentBoss;
            if (CheckPhase(FRUBoss.Adds, hostileTargetNames, "Ice Veil", 41.2f, FRUBoss.Usurper)) return currentBoss;
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
            // End the current phase early if the next boss becomes targetable or if the current boss is dead
            if (currentBoss == FRUBoss.Lesbians)
            {
                if (AreBothBossesDead("Oracle of Darkness", "Usurper of Frost"))
                {
                    CurrentPhaseEnd = CombatTime;
                }
            }
            else if (IsBossDead(currentBoss.ToString()))
            {
                CurrentPhaseEnd = CombatTime;
            }

            // Fallback to advance to the next phase if duration is exceeded
            if (CombatTime >= CurrentPhaseEnd)
            {
                SetPhase(boss, duration);
                return true;
            }
        }
        return false;
    }

    private static void SetPhase(FRUBoss boss, float duration)
    {
        CurrentPhaseStart = CombatTime;
        CurrentPhaseEnd = CombatTime + duration;
        currentBoss = boss;

        UpdateBossStartTime(boss, CombatTime);
    }

    private static void UpdateBossStartTime(FRUBoss boss, float startTime)
    {
        switch (boss)
        {
            case FRUBoss.Fatebreaker:
                FatebreakerStartTime = startTime;
                break;
            case FRUBoss.Usurper:
                UsurperStartTime = startTime;
                UpdateUsurperTimings(startTime);
                break;
            case FRUBoss.Adds:
                AddsStartTime = startTime;
                UpdateAddsTimings(startTime);
                break;
            case FRUBoss.Gaia:
                GaiaStartTime = startTime;
                UpdateGaiaTimings(startTime);
                break;
            case FRUBoss.Lesbians:
                LesbiansStartTime = startTime;
                UpdateLesbiansTimings(startTime);
                break;
            case FRUBoss.Pandora:
                PandoraStartTime = startTime;
                break;
        }
    }

    private static void UpdateFatebreakerTimings(float startTime)
    {
        UtopianSkyStart = startTime + 34.8f;
        UtopianSkyEnd = UtopianSkyStart + 45.2f;
    }

    private static void UpdateUsurperTimings(float startTime)
    {
        DiamondDustStart = startTime + 35.1f;
        DiamondDustEnd = DiamondDustStart + 36.9f;
        LightRampantStart = DiamondDustEnd + 59.7f;
        LightRampantEnd = LightRampantStart + 29f;
    }

    private static void UpdateAddsTimings(float startTime)
    {
        GaiaTransitionStart = startTime + 41.2f;
        GaiaTransitionEnd = GaiaTransitionStart + 25.6f;
    }

    private static void UpdateGaiaTimings(float startTime)
    {
        UltimateRelativityStart = startTime + 18.3f;
        UltimateRelativityEnd = UltimateRelativityStart + 43.9f;
    }

    private static void UpdateLesbiansTimings(float startTime)
    {
        OracleTargetable = startTime + 25.4f;
        CrystalizeTimeStart = startTime + 98.5f;
        CrystalizeTimeEnd = CrystalizeTimeStart + 49.7f;
    }
    
    //Handling logic for each boss
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
        if (OracleTargetable - CombatTime <= 7)
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