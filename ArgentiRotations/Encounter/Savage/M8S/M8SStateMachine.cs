using ArgentiRotations.Encounter.StateMachine;

namespace ArgentiRotations.Encounter;

/// <summary>
/// M8S Howling Blade state machine implementation.
/// Handles the complete encounter including Phase 1 and Phase 2.
/// </summary>
public static class M8SStateMachine
{
    #region Constants
    // Territory ID for Hunter's Ring
    private const ushort TerritoryId = 1263;
    // Boss Actor IDs
    private const uint BossP1ActorId = 0x4727;  // R5.005
    private const uint BossP2ActorId = 0x472E;  // R19.0

    // Ability IDs (from M08SHowlingBladeEnums.cs)
    private const uint ExtraplanarPursuit = 42831;
    private const uint StonefangCross1 = 41890;
    private const uint StonefangCross2 = 41889;
    private const uint WindfangCross1 = 41885;
    private const uint WindfangCross2 = 41886;
    private const uint WolvesReignCircle1 = 43308;
    private const uint MillennialDecay = 41906;
    private const uint GreatDivide = 41944;
    private const uint TrackingTremors = 41915;
    private const uint TerrestrialTitans = 41925;
    private const uint TacticalPack = 41928;
    private const uint TerrestrialRage = 41918;
    private const uint BeckonMoonlight = 41921;
    private const uint QuakeIII = 42075;
    private const uint UltraviolentRay = 42077;
    private const uint Twinbite = 42190;
    private const uint HerosBlow1 = 42082;
    private const uint Mooncleaver1 = 42086;
    private const uint ElementalPurge = 42087;
    private const uint ProwlingGaleP2 = 42095;        private const uint TwofoldTempestRect = 42099;
    private const uint ChampionsCircuitCW = 42103;
    private const uint RiseOfTheHuntersBlade = 43052;
    private const uint HowlingEightFirst1 = 43523;

    #endregion

    #region Public Methods        
    /// <summary>
    /// Creates the complete M08S state machine with both phases.
    /// Includes territory validation to ensure it only operates in M8S Savage.
    /// </summary>
    /// <returns>Configured ArgentiStateMachine for M08S</returns>
    public static ArgentiStateMachine CreateStateMachine()
    {
        var builder = new ArgentiStateMachineBuilder()
            .WithBossActorId(BossP1ActorId)
            .WithTerritoryId(TerritoryId);

        // Build Phase 1
        BuildPhase1(builder);

        // Build Phase 2  
        BuildPhase2(builder);

        return builder.Build();
    }

    #endregion

    #region Phase 1 Implementation

    /// <summary>
    /// Builds Phase 1 mechanics and timeline.
    /// </summary>
    private static void BuildPhase1(ArgentiStateMachineBuilder builder)
    {
        builder.BeginPhase("Phase 1");

        // ExtraplanarPursuit (10.3s)
        builder.AddMechanic(
            BossP1ActorId, "Extraplanar Pursuit", 4.0f, ExtraplanarPursuit,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(10.3f));

        // WindfangStonefang1 (9f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Windfang/Stonefang 1", 6.0f, WindfangCross1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(19.3f));

        // WolvesReign (5.2f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Wolves Reign", 7.0f, WolvesReignCircle1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(24.5f));

        // ExtraplanarPursuit (2.2f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Extraplanar Pursuit 2", 4.0f, ExtraplanarPursuit,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(26.7f));

        // MillennialDecay (8.5f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Millennial Decay", 5.0f, MillennialDecay,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(35.2f));

        // TrackingTremors (6.6f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Tracking Tremors", 7.5f, TrackingTremors,
            MechanicType.Stack, expectedStartTime: DateTime.Now.AddSeconds(41.8f));

        // ExtraplanarPursuit (1.9f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Extraplanar Pursuit 3", 4.0f, ExtraplanarPursuit,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(43.7f));

        // GreatDivide (3.8f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Great Divide", 5.0f, GreatDivide,
            MechanicType.Tankbuster, expectedStartTime: DateTime.Now.AddSeconds(47.5f));

        // TerrestrialTitans (14.8f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Terrestrial Titans", 4.0f, TerrestrialTitans,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(62.3f));

        // WolvesReign (0.5f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Wolves Reign 2", 7.0f, WolvesReignCircle1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(62.8f));

        // TacticalPack (9.2f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Tactical Pack", 3.0f, TacticalPack,
            MechanicType.Adds, expectedStartTime: DateTime.Now.AddSeconds(72.0f));

        // TerrestrialRage1 (14.5f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Terrestrial Rage", 3.0f, TerrestrialRage,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(86.5f));

        // WolvesReign3 (4.1f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Wolves Reign 3", 7.0f, WolvesReignCircle1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(90.6f));

        // GreatDivide (5.4f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Great Divide 2", 5.0f, GreatDivide,
            MechanicType.Tankbuster, expectedStartTime: DateTime.Now.AddSeconds(96.0f));

        // BeckonMoonlight (11.3f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Beckon Moonlight", 9.0f, BeckonMoonlight,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(107.3f));

        // WindfangStonefang2 (3.3f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Windfang/Stonefang 2", 6.0f, WindfangCross1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(110.6f));

        // TrackingTremors (10f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Tracking Tremors 2", 7.5f, TrackingTremors,
            MechanicType.Stack, expectedStartTime: DateTime.Now.AddSeconds(120.6f));

        // ExtraplanarPursuit (1.8f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Extraplanar Pursuit 4", 4.0f, ExtraplanarPursuit,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(122.4f));

        // ExtraplanarPursuit (10.8f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Extraplanar Pursuit 5", 4.0f, ExtraplanarPursuit,
            MechanicType.Raidwide, expectedStartTime: DateTime.Now.AddSeconds(133.2f));

        // Enrage (7.4f after previous)
        builder.AddMechanic(
            BossP1ActorId, "Enrage", 10.0f, 0,
            MechanicType.Enrage, expectedStartTime: DateTime.Now.AddSeconds(140.6f));

        builder.EndPhase();
    }

    #endregion

    #region Phase 2 Implementation

    /// <summary>
    /// Builds Phase 2 mechanics and timeline.
    /// </summary>
    private static void BuildPhase2(ArgentiStateMachineBuilder builder)
    {
        builder.BeginPhase("Phase 2");

        // Boss becomes targetable (45.5f delay from phase start)
        builder.AddMechanic(
            BossP2ActorId, "Boss Targetable", 0.0f, 0,
            MechanicType.PhaseTransition, expectedStartTime: DateTime.Now.AddSeconds(185.5f));

        // QuakeIII (12.2f after boss targetable)
        builder.AddMechanic(
            BossP2ActorId, "Quake III", 5.0f, QuakeIII,
            MechanicType.Stack, expectedStartTime: DateTime.Now.AddSeconds(197.7f));

        // UltraviolentRay (12.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Ultraviolent Ray", 6.0f, UltraviolentRay,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(209.9f));

        // Twinbite (11.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Twinbite", 7.0f, Twinbite,
            MechanicType.Tankbuster, expectedStartTime: DateTime.Now.AddSeconds(221.1f));

        // HerosBlow (12.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Hero's Blow", 7.0f, HerosBlow1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(233.3f));

        // UltraviolentRay (10.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Ultraviolent Ray 2", 6.0f, UltraviolentRay,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(243.5f));

        // QuakeIII (11.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Quake III 2", 5.0f, QuakeIII,
            MechanicType.Stack, expectedStartTime: DateTime.Now.AddSeconds(254.7f));

        // Mooncleaver1 (13.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Mooncleaver", 5.0f, Mooncleaver1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(267.9f));

        // ElementalPurge (7.1f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Elemental Purge", 5.0f, ElementalPurge,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(275.0f));

        // ProwlingGaleP2 (14f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Prowling Gale P2", 8.0f, ProwlingGaleP2,
            MechanicType.Tower, expectedStartTime: DateTime.Now.AddSeconds(289.0f));

        // TwofoldTempest (11.5f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Twofold Tempest", 7.0f, TwofoldTempestRect,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(300.5f));

        // ChampionsCircuit (18.3f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Champions Circuit", 8.0f, ChampionsCircuitCW,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(318.8f));

        // QuakeIII (9.9f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Quake III 3", 5.0f, QuakeIII,
            MechanicType.Stack, expectedStartTime: DateTime.Now.AddSeconds(328.7f));

        // UltraviolentRay (14.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Ultraviolent Ray 3", 6.0f, UltraviolentRay,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(342.9f));

        // Twinbite (11.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Twinbite 2", 7.0f, Twinbite,
            MechanicType.Tankbuster, expectedStartTime: DateTime.Now.AddSeconds(354.1f));

        // RiseOfTheHuntersBlade (8.2f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Rise of the Hunter's Blade", 7.0f, RiseOfTheHuntersBlade,
            MechanicType.Special, expectedStartTime: DateTime.Now.AddSeconds(362.3f));

        // HerosBlow (9.4f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Hero's Blow 2", 7.0f, HerosBlow1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(371.7f));

        // UltraviolentRay (12.3f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Ultraviolent Ray 4", 6.0f, UltraviolentRay,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(384.0f));

        // HowlingEight (16f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Howling Eight", 15.1f, HowlingEightFirst1,
            MechanicType.AOE, expectedStartTime: DateTime.Now.AddSeconds(400.0f));
            
        // Enrage (11.3f after previous)
        builder.AddMechanic(
            BossP2ActorId, "Enrage P2", 11.0f, 0,
            MechanicType.Enrage, expectedStartTime: DateTime.Now.AddSeconds(411.3f));
              builder.EndPhase();
    }

    #endregion
}
