namespace ArgentiRotations.Encounter;

public abstract class FuturesRewritten : DancerRotation
{
    #region FRU Phases

    protected enum FruPhase
    {
        None,
        Fatebreaker,
        Usurper,
        Adds,
        Gaia,
        LesRoomates,
        Pandora
    }

    protected static FruPhase CurrentPhase { get; private set; } = FruPhase.None;

    protected static FruPhase CheckBoss()
    {
        if (!IsInFRU || !InCombat) return FruPhase.None;
        foreach (var obj in AllHostileTargets)
        {
            var phase = GetPhaseForTarget(obj);
            if (phase == FruPhase.None) continue;
            CurrentPhase = phase;
            return phase;
        }

        return FruPhase.None;
    }

    private static FruPhase GetPhaseForTarget(IBattleChara obj)
    {
        return obj.Name.ToString() switch
        {
            "Fatebreaker" => FruPhase.Fatebreaker,
            "Usurper of Frost" when CheckBoss() == FruPhase.Fatebreaker => FruPhase.Usurper,
            "Ice Veil" when CheckBoss() == FruPhase.Usurper => FruPhase.Adds,
            "Oracle of Darkness" when CheckBoss() == FruPhase.Adds => FruPhase.Gaia,
            "Usurper of Frost" or "Oracle of Darkness" when CheckBoss() == FruPhase.Gaia => FruPhase.LesRoomates,
            "Pandora" when CheckBoss() == FruPhase.LesRoomates => FruPhase.Pandora,
            _ => FruPhase.None
        };
    }

    #endregion

    #region FRU Downtimes

    protected enum FruDowntime
    {
        None,
        UtopianSky,
        DiamondDust,
        LightRampant,
        GaiaTransition,
        UltimateRelativity,
        CrystalizeTime
    }

    protected static FruDowntime CurrentDowntime { get; private set; } = FruDowntime.None;

    protected static FruDowntime CheckDowntime()
    {
        if (!IsInFRU || !InCombat) return FruDowntime.None;
        foreach (var obj in AllHostileTargets)
        {
            var downtime = GetDowntimeForTarget(obj);
            if (downtime == FruDowntime.None) continue;
            CurrentDowntime = downtime;
            return downtime;
        }

        return FruDowntime.None;
    }

    private static FruDowntime GetDowntimeForTarget(IBattleChara obj)
    {
        var bossPhase = CheckBoss();
        switch (bossPhase)
        {
            case FruPhase.Fatebreaker when obj.CastActionId == 40154:
                StartDowntimeTimer(FruDowntime.UtopianSky);
                return FruDowntime.UtopianSky;
            case FruPhase.Usurper when obj.CastActionId == 40197:
                StartDowntimeTimer(FruDowntime.DiamondDust);
                return FruDowntime.DiamondDust;
            case FruPhase.Usurper when obj.CastActionId == 40212:
                StartDowntimeTimer(FruDowntime.LightRampant);
                return FruDowntime.LightRampant;
            case FruPhase.Adds when obj.CastActionId == 40226:
                StartDowntimeTimer(FruDowntime.GaiaTransition);
                return FruDowntime.GaiaTransition;
            case FruPhase.Gaia when obj.CastActionId == 40266:
                StartDowntimeTimer(FruDowntime.UltimateRelativity);
                return FruDowntime.UltimateRelativity;
            case FruPhase.LesRoomates when obj.CastActionId == 40298:
                StartDowntimeTimer(FruDowntime.CrystalizeTime);
                return FruDowntime.CrystalizeTime;
            case FruPhase.Pandora:
                return FruDowntime.None;
            case FruPhase.None:
                break;
            default:
                return FruDowntime.None;
        }

        return FruDowntime.None;
    }

    #endregion

    #region FRU Timers

    private static readonly Dictionary<FruDowntime, float> DowntimeDurations = new()
    {
        { FruDowntime.UtopianSky, 49.2f },
        { FruDowntime.DiamondDust, 40.1f },
        { FruDowntime.LightRampant, 32.1f },
        { FruDowntime.GaiaTransition, 14.3f },
        { FruDowntime.UltimateRelativity, 43.9f },
        { FruDowntime.CrystalizeTime, 52.8f }
    };

    // Active downtime timers stored as expiration timestamps.
    private static readonly Dictionary<FruDowntime, float> ActiveDowntimeTimers = new();

    // Updates the downtime timer by setting its expiration time.
    private static void StartDowntimeTimer(FruDowntime downtime)
    {
        if (DowntimeDurations.TryGetValue(downtime, out var duration))
            ActiveDowntimeTimers[downtime] = CombatTime + duration;
    }

    // Checks if the current downtime has expired and resets it if needed.
    protected static void UpdateFruDowntime()
    {
        if (CurrentDowntime != FruDowntime.None &&
            ActiveDowntimeTimers.TryGetValue(CurrentDowntime, out var expiration) &&
            CombatTime > expiration)
            CurrentDowntime = FruDowntime.None;
    }

    #endregion
}