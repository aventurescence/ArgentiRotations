namespace DefaultRotations.Healer;

[Rotation("BMR", CombatType.PvE, GameVersion = "7.05")]
[SourceCode(Path = "main/DefaultRotations/Healer/SCH_Default.cs")]
[Api(4)]
public sealed class SCH_BMR : ScholarRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use spells with cast times to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Recitation during Countdown.")]
    public bool PrevDUN { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Adloquium during Countdown")]
    public bool GiveT { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Sacred Soil while moving")]
    public bool SacredMove { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Remove Aetherpact to conserve resources if party member is above this percentage")]
    public float AetherpactRemove { get; set; } = 0.9f;

    [RotationConfig(CombatType.PvE, Name = "Use DOT while moving even if it does not need refresh (disabling is a damage down)")]
    public bool DOTUpkeep { get; set; } = true;
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        var tank = PartyMembers.GetJobCategory(JobRole.Tank);

        if (SummonEosPvE.CanUse(out var act)) return act;

        if (remainTime < RuinPvE.Info.CastTime + CountDownAhead
            && RuinPvE.CanUse(out act)) return act;
        if (remainTime < 3 && UseBurstMedicine(out act)) return act;
        if (remainTime is < 4 and > 3 && DeploymentTacticsPvE.CanUse(out act)) return act;
        if (remainTime is < 7 and > 6 && GiveT && AdloquiumPvE.CanUse(out act)) return act;
        if (remainTime <= 15 && PrevDUN && RecitationPvE.CanUse(out act)) return act;

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.SummonSeraphPvE, ActionID.ConsolationPvE, ActionID.WhisperingDawnPvE, ActionID.SacredSoilPvE, ActionID.IndomitabilityPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AccessionPvE.CanUse(out act)) return true;
        if (ConcitationPvE.CanUse(out act)) return true;
        if (WhisperingDawnPvE_16537.Cooldown.ElapsedOneChargeAfterGCD(1) || FeyIlluminationPvE_16538.Cooldown.ElapsedOneChargeAfterGCD(1) || FeyBlessingPvE.Cooldown.ElapsedOneChargeAfterGCD(1))
        {
            if (SummonSeraphPvE.CanUse(out act)) return true;
        }
        if (ConsolationPvE.CanUse(out act, usedUp: true)) return true;
        if (FeyBlessingPvE.CanUse(out act)) return true;

        if (WhisperingDawnPvE_16537.CanUse(out act)) return true;
        if (SacredSoilPvE.CanUse(out act)) return true;
        if (IndomitabilityPvE.CanUse(out act)) return true;

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AetherpactPvE, ActionID.ProtractionPvE, ActionID.SacredSoilPvE, ActionID.ExcogitationPvE, ActionID.LustratePvE, ActionID.AetherpactPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        var haveLink = PartyMembers.Any(p => p.HasStatus(true, StatusID.FeyUnion_1223));
        if (ManifestationPvE.CanUse(out act)) return true;
        if (AetherpactPvE.CanUse(out act) && FairyGauge >= 70 && !haveLink) return true;
        if (ProtractionPvE.CanUse(out act)) return true;
        if (ExcogitationPvE.CanUse(out act)) return true;
        if (LustratePvE.CanUse(out act)) return true;
        if (AetherpactPvE.CanUse(out act) && !haveLink) return true;

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeyIlluminationPvE, ActionID.ExpedientPvE, ActionID.SummonSeraphPvE, ActionID.ConsolationPvE, ActionID.SacredSoilPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ExcogitationPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ExpedientPvE)]
    protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
    {

        return base.SpeedAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.SuccorPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;

        if (HasSwift && SwiftLogic && ResurrectionPvE.CanUse(out _)) return false;

        if (SuccorPvE.CanUse(out act)) return true;

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.AdloquiumPvE, ActionID.PhysickPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;

        if (HasSwift && SwiftLogic && ResurrectionPvE.CanUse(out _)) return false;

        if (AdloquiumPvE.CanUse(out act)) return true;
        if (PhysickPvE.CanUse(out act)) return true;

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.SuccorPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {

        return base.DefenseAreaGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public override bool CanHealSingleSpell => base.CanHealSingleSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);
    public override bool CanHealAreaSpell => base.CanHealAreaSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);
    #endregion
}