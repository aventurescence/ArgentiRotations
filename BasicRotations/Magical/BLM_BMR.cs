namespace DefaultRotations.Magical;

[Rotation("BMR", CombatType.PvE, GameVersion = "7.01")]
[SourceCode(Path = "main/DefaultRotations/Magical/BLM_Default.cs")]
[Api(4)]
public class BLM_BMR : BlackMageRotation
{
    protected override IAction? CountDownAction(float remainTime)
    {
        IAction act;
        if (remainTime < FireIiiPvE.Info.CastTime + CountDownAhead)
        {
            if (FireIiiPvE.CanUse(out act)) return act;
        }
        //if (remainTime <= 12 && SharpcastPvE.CanUse(out act, usedUp: true)) return act;
        return base.CountDownAction(remainTime);
    }

    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AetherialManipulationPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BetweenTheLinesPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE, ActionID.AddlePvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    #region oGCD Logic
    [RotationDesc(ActionID.ManafontPvE, ActionID.TransposePvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RetracePvE, ActionID.SwiftcastPvE, ActionID.TriplecastPvE, ActionID.AmplifierPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    protected override bool GeneralGCD(out IAction? act)
    {
        return base.GeneralGCD(out act);
    }

}