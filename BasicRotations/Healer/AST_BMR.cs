namespace DefaultRotations.Healer;

[Rotation("BMR", CombatType.PvE, GameVersion = "7.05")]
[SourceCode(Path = "main/DefaultRotations/Healer/AST_Default.cs")]
[Api(4)]
public sealed class AST_BMR : AstrologianRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [Range(4, 20, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star during countdown timer.")]
    public float UseEarthlyStarTime { get; set; } = 15;

    [RotationConfig(CombatType.PvE, Name = "Prevent actions while you have the bubble mit up")]
    public bool BubbleProtec { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Microcosmos over all other healing when available")]
    public bool MicroPrio { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Aspected Benefic")]
    public float AspectedBeneficHeal { get; set; } = 0.4f;

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead
            && MaleficPvE.CanUse(out var act)) return act;
        if (remainTime < 3 && UseBurstMedicine(out act)) return act;
        if (remainTime is < 4 and > 3 && AspectedBeneficPvE.CanUse(out act)) return act;
        if (remainTime < UseEarthlyStarTime
            && EarthlyStarPvE.CanUse(out act)) return act;
        if (remainTime < 30 && AstralDrawPvE.CanUse(out act)) return act;

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (!InCombat) return false;

        if (OraclePvE.CanUse(out act)) return true;
        if (nextGCD.IsTheSameTo(true, AspectedHeliosPvE, HeliosPvE))
        {
            if (HoroscopePvE.CanUse(out act)) return true;
            if (NeutralSectPvE.CanUse(out act)) return true;
        }

        if (nextGCD.IsTheSameTo(true, BeneficPvE, BeneficIiPvE, AspectedBeneficPvE))
        {
            if (SynastryPvE.CanUse(out act)) return true;
        }
        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ExaltationPvE, ActionID.TheArrowPvE, ActionID.TheSpirePvE, ActionID.TheBolePvE, ActionID.TheEwerPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.MacrocosmosPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {
        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.CollectiveUnconsciousPvE, ActionID.SunSignPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TheArrowPvE, ActionID.TheEwerPvE, ActionID.EssentialDignityPvE,
        ActionID.CelestialIntersectionPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (InCombat && TheArrowPvE.CanUse(out act)) return true;
        if (InCombat && TheEwerPvE.CanUse(out act)) return true;

        if (EssentialDignityPvE.CanUse(out act, usedUp: true)) return true;

        if (CelestialIntersectionPvE.CanUse(out act, usedUp: true)) return true;

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CelestialOppositionPvE, ActionID.StellarDetonationPvE, ActionID.HoroscopePvE, ActionID.HoroscopePvE_16558, ActionID.LadyOfCrownsPvE, ActionID.HeliosConjunctionPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        if (MicrocosmosPvE.CanUse(out act)) return true;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (CelestialOppositionPvE.CanUse(out act)) return true;

        if (StellarDetonationPvE.CanUse(out act)) return true;

        if (HoroscopePvE.CanUse(out act)) return true;

        if (HoroscopePvE_16558.CanUse(out act)) return true;

        if (LadyOfCrownsPvE.CanUse(out act)) return true;

        if (HeliosConjunctionPvE.CanUse(out act)) return true;
        return base.HealAreaAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.AspectedBeneficPvE, ActionID.BeneficIiPvE, ActionID.BeneficPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;
        if (HasSwift && SwiftLogic && AscendPvE.CanUse(out _)) return false;

        if (AspectedBeneficPvE.CanUse(out act)
            && (IsMoving
            || AspectedBeneficPvE.Target.Target?.GetHealthRatio() > AspectedBeneficHeal)) return true;

        if (BeneficIiPvE.CanUse(out act)) return true;
        if (BeneficPvE.CanUse(out act)) return true;

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.AspectedHeliosPvE, ActionID.HeliosPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;
        if (HasSwift && SwiftLogic && AscendPvE.CanUse(out _)) return false;

        if (AspectedHeliosPvE.CanUse(out act)) return true;
        if (HeliosPvE.CanUse(out act)) return true;
        return base.HealAreaGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {

        return base.GeneralGCD(out act);
    }
    #endregion

}
