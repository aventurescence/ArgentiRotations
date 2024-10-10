namespace DefaultRotations.Magical;

[Rotation("BMR", CombatType.PvE, GameVersion = "7.05")]
[SourceCode(Path = "main/DefaultRotations/Magical/RDM_Default.cs")]
[Api(4)]
public sealed class RDM_BMR : RedMageRotation
{
    #region Config Options
    private static BaseAction VerthunderStartUp { get; } = new BaseAction(ActionID.VerthunderPvE, false);

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < VerthunderStartUp.Info.CastTime + CountDownAhead
            && VerthunderStartUp.CanUse(out var act)) return act;

        //Remove Swift
        StatusHelper.StatusOff(StatusID.Dualcast);
        StatusHelper.StatusOff(StatusID.Acceleration);
        StatusHelper.StatusOff(StatusID.Swiftcast);

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if ((BlackMana > 50 && WhiteMana > 50) && CorpsacorpsPvE.CanUse(out act) && !IsMoving) return true;
        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool GeneralGCD(out IAction? act)
    {
        return base.GeneralGCD(out act);
    }
    #endregion

}
