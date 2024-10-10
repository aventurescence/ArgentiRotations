namespace DefaultRotations.Healer;

[Rotation("BMR", CombatType.PvE, GameVersion = "7.05")]
[SourceCode(Path = "main/DefaultRotations/Healer/WHM_Default.cs")]
[Api(4)]
public sealed class WHM_BMR : WhiteMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use spells with cast times to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use DOT while moving even if it does not need refresh (disabling is a damage down)")]
    public bool DOTUpkeep { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Lily at max stacks.")]
    public bool UseLilyWhenFull { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Regen on Tank at 5 seconds remaining on Prepull Countdown.")]
    public bool UsePreRegen { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Divine Carress as soon as its available")]
    public bool UseDivine { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Asylum as soon a single player heal (i.e. tankbusters) while moving, in addition to normal logic")]
    public bool AsylumSingle { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum health threshold party member needs to be to use Benediction")]
    public float BenedictionHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "If a party member's health drops below this percentage, the Regen healing ability will not be used on them")]
    public float RegenHeal { get; set; } = 0.3f;

    [Range(0, 10000, ConfigUnitType.None, 100)]
    [RotationConfig(CombatType.PvE, Name = "Casting cost requirement for Thin Air to be used")]

    public float ThinAirNeed { get; set; } = 1000;
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < StonePvE.Info.CastTime + CountDownAhead
            && StonePvE.CanUse(out var act)) return act;

        if (UsePreRegen && remainTime <= 5 && remainTime > 3)
        {
            if (RegenPvE.CanUse(out act)) return act;
            if (DivineBenisonPvE.CanUse(out act)) return act;
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TemperancePvE, ActionID.LiturgyOfTheBellPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DivineBenisonPvE, ActionID.AquaveilPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AsylumPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AsylumPvE.CanUse(out act)) return true;
        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BenedictionPvE, ActionID.AsylumPvE, ActionID.DivineBenisonPvE, ActionID.TetragrammatonPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (BenedictionPvE.CanUse(out act) &&
            RegenPvE.Target.Target?.GetHealthRatio() < BenedictionHeal) return true;

        if (AsylumSingle && !IsMoving && AsylumPvE.CanUse(out act)) return true;

        if (DivineBenisonPvE.CanUse(out act)) return true;

        if (TetragrammatonPvE.CanUse(out act, usedUp: true)) return true;
        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    [RotationDesc(ActionID.AfflatusRapturePvE, ActionID.MedicaIiPvE, ActionID.CureIiiPvE, ActionID.MedicaPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;

        if (HasSwift && SwiftLogic && RaisePvE.CanUse(out _)) return false;

        if (AfflatusRapturePvE.CanUse(out act)) return true;

        int hasMedica2 = PartyMembers.Count((n) => n.HasStatus(true, StatusID.MedicaIi));

        if (MedicaIiPvE.CanUse(out act) && hasMedica2 < PartyMembers.Count() / 2 && !IsLastAction(true, MedicaIiPvE)) return true;

        if (CureIiiPvE.CanUse(out act)) return true;

        if (MedicaPvE.CanUse(out act)) return true;

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.AfflatusSolacePvE, ActionID.RegenPvE, ActionID.CureIiPvE, ActionID.CurePvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;

        if (HasSwift && SwiftLogic && RaisePvE.CanUse(out _)) return false;

        if (AfflatusSolacePvE.CanUse(out act)) return true;

        if (RegenPvE.CanUse(out act) && (RegenPvE.Target.Target?.GetHealthRatio() > RegenHeal)) return true;

        if (CureIiPvE.CanUse(out act)) return true;

        if (CurePvE.CanUse(out act)) return true;

        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public WHM_BMR()
    {
        AfflatusRapturePvE.Setting.RotationCheck = () => BloodLily < 3;
        AfflatusSolacePvE.Setting.RotationCheck = () => BloodLily < 3;
    }
    public override bool CanHealSingleSpell => base.CanHealSingleSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);
    public override bool CanHealAreaSpell => base.CanHealAreaSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);

    #endregion
}
