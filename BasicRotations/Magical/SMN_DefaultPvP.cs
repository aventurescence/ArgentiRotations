namespace DefaultRotations.Magical;

[Rotation("Lelia's PvP", CombatType.PvP, GameVersion = "6.58")]
[SourceCode(Path = "main/DefaultRotations/Magical/SMN_DefaultPvP.cs")]
[Api(4)]
public sealed class SMN_LeliaDefaultPvP : SummonerRotation
{
    public static IBaseAction SummonBahamutPvP = new BaseAction((ActionID)29673);
    public static IBaseAction SummonPhoenixPvP = new BaseAction((ActionID)29678);

    [RotationConfig(CombatType.PvP, Name = "LBを使用します。")]
    private bool LBInPvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "LB:サモン・バハムートを行うために必要なターゲットのHP%%は？")]
    public int SBValue { get; set; } = 30;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "LB:サモン・フェニックスを行うために必要なプレイヤーのHP%%は？")]
    public int SPValue { get; set; } = 40;

    [RotationConfig(CombatType.PvP, Name = "クリムゾンサイクロンを使用しますか？")]
    private bool CCPvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "クリムゾンサイクロンを使用するターゲットのHP%%は？")]
    public int CrimsonValue { get; set; } = 35;

    [RotationConfig(CombatType.PvP, Name = "守りの光を使用しますか？")]
    private bool RadiantA { get; set; } = true;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "守りの光を使用するプレイヤーのHP%%は？")]
    public int RAValue { get; set; } = 40;

    [RotationConfig(CombatType.PvP, Name = "スプリントを使います。")]
    private bool UseSprintPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "快気を使います。")]
    private bool UseRecuperatePvP { get; set; } = false;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "快気を使うプレイヤーのHP%%は？")]
    public int RCValue { get; set; } = 75;

    [RotationConfig(CombatType.PvP, Name = "浄化を使います。")]
    private bool UsePurifyPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "スタン:Stun")]
    private bool Use1343PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "氷結:DeepFreeze")]
    private bool Use3219PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "徐々に睡眠:HalfAsleep")]
    private bool Use3022PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "睡眠:Sleep")]
    private bool Use1348PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "バインド:Bind")]
    private bool Use1345PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "ヘヴィ:Heavy")]
    private bool Use1344PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "沈黙:Silence")]
    private bool Use1347PvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "自分が防御中は攻撃を中止します。\n Stop attacking while in Guard.")]
    private bool GuardCancel { get; set; } = false;

    private bool TryPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP) return false;

        var purifyStatuses = new Dictionary<int, bool>
        {
            { 1343, Use1343PvP },
            { 3219, Use3219PvP },
            { 3022, Use3022PvP },
            { 1348, Use1348PvP },
            { 1345, Use1345PvP },
            { 1344, Use1344PvP },
            { 1347, Use1347PvP }
        };

        foreach (var status in purifyStatuses)
        {
            if (status.Value && Player.HasStatus(true, (StatusID)status.Key))
            {
                return PurifyPvP.CanUse(out action);
            }
        }

        return false;
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;

        if (GuardCancel && Player.HasStatus(true, StatusID.Guard)) return false;

        if (!Player.HasStatus(true, StatusID.Guard) && UseRecuperatePvP && Player.GetHealthRatio() * 100 < RCValue &&
            RecuperatePvP.CanUse(out act, usedUp: true)) return true;

        if (LimitBreakLevel >= 1 && SummonBahamutPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
        if (LimitBreakLevel >= 1 && SummonPhoenixPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;

        if (LimitBreakLevel >= 1 && (!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && LBInPvP &&
                HostileTarget?.GetHealthRatio() * 100 <= SBValue && Player.GetHealthRatio() * 100 >= SPValue)
        {
            if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && SummonBahamutPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
        }
        else if (LimitBreakLevel >= 1 && (!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && LBInPvP && 
            HostileTarget?.GetHealthRatio() * 100 <= SPValue && Player.GetHealthRatio() * 100 < SPValue)
        {
            if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && SummonPhoenixPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
        }

        //if (CrimsonCyclonePvP.CanUse(out act, skipAoeCheck: true)) return true;
        if (CCPvP && HostileTarget?.GetHealthRatio() < CrimsonValue/100)
        {
            if (CrimsonCyclonePvP.CanUse(out act, skipAoeCheck: true)) return true;
            //if (CrimsonCyclonePvP.Cooldown.IsCoolingDown && CrimsonStrikePvP.CanUse(out act, skipAoeCheck: true)) return true;
            //if (CrimsonCyclonePvP.IsEnabled && CrimsonStrikePvP.CanUse(out act, skipAoeCheck: true)) return true;
        }


        if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) &&
                SlipstreamPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;

        if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && Player.HasStatus(true, StatusID.DreadwyrmTrance_3228) &&
            AstralImpulsePvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
        if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && Player.HasStatus(true, StatusID.FirebirdTrance) &&
            FountainOfFirePvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;


        if (RuinIiiPvP.CanUse(out act)) return true;

        if (!Player.HasStatus(true, StatusID.Guard) && UseSprintPvP && !Player.HasStatus(true, StatusID.Sprint) &&
            SprintPvP.CanUse(out act)) return true;

        return base.GeneralGCD(out act);

    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (UseRecuperatePvP && Player.GetHealthRatio() * 100 < RCValue && RecuperatePvP.CanUse(out act)) return true;
        if (RadiantA && Player.GetHealthRatio() * 100 <= RAValue &&
            RadiantAegisPvP.CanUse(out act)) return true;

        if (TryPurify(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false))
        {
            if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) &&
                MountainBusterPvP.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;
            if ((!HostileTarget?.HasStatus(true, StatusID.Guard) ?? false) && InCombat &&
                FesterPvP.CanUse(out act, usedUp: true)) return true;

        }

        return base.AttackAbility(nextGCD, out act);
    }


}

