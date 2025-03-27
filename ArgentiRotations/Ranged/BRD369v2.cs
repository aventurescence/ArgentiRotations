using Dalamud.Game.ClientState.JobGauge.Enums;

namespace ArgentiRotations.Ranged;

[Rotation("BRD369v2", CombatType.PvE, GameVersion = "7.2",
    Description = "Don't touch the config options! Only intended to be used in level 100 savage content. :3")]
[SourceCode(Path = "ArgentiRotations/Ranged/369BRDv2.cs")]
[Api(4)]
public sealed class Brd369V2 : BardRotation
{
    #region Config Options
    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Wanderer's Minuet Uptime")]
    public float WandTime { get; set; } = 42;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Mage's Ballad Uptime")]
    public float MageTime { get; set; } = 39;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Army's Paeon Uptime")]
    public float ArmyTime { get; set; } = 36;

    private float WandRemainTime => 45 - WandTime;
    private float MageRemainTime => 45 - MageTime;
    private float ArmyRemainTime => 45 - ArmyTime;

    // New configuration for enabling prepull Heartbreak Shot
    [RotationConfig(CombatType.PvE, Name = "Enable Prepull Heartbreak Shot")]
    public bool EnablePrepullHeartbreakShot { get; set; } = false;

    // Removed RotationConfig attribute for First song to disable changing the option

    [RotationConfig(CombatType.PvE, Name = "Potion Timings")]
    public PotionTimingOption PotionTimings { get; set; } = PotionTimingOption.None;

    public enum PotionTimingOption
    {
        None,
        ZeroAndSixMins,
        TwoAndEightMins,
        ZeroFiveAndTenMins
    }

    #endregion

    #region Prepull Heartbreak Shot
    protected override IAction? CountDownAction(float remainTime)
    {
        // Prepulls Heartbreak Shot at 1.6 seconds before pull
        if (EnablePrepullHeartbreakShot == true && remainTime <= 1.6f && HeartbreakShotPvE.CanUse(out IAction? act)) return act;
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGcd, out IAction? act)
    {

        if (nextGcd.IsTheSameTo(true, WindbitePvE, VenomousBitePvE, StraightShotPvE, IronJawsPvE))
        {
            return base.EmergencyAbility(nextGcd, out act);
        }
        else if (Player.HasStatus(true, StatusID.RagingStrikes))
        {
            if ((EmpyrealArrowPvE.Cooldown.IsCoolingDown && !EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) || !EmpyrealArrowPvE.EnoughLevel) && Repertoire != 3)
            {
                if (!Player.HasStatus(true, StatusID.HawksEye_3861) && BarragePvE.CanUse(out act)) return true;
            }
            if (HeartbreakShotPvE.Cooldown.WillHaveXChargesGCD(BloodletterMax))
            {
                if (HeartbreakShotPvE.CanUse(out act)) return true;
            }
        }

        return base.EmergencyAbility(nextGcd, out act);
    }

    protected override bool AttackAbility(IAction nextGcd, out IAction? act)
    {
        act = null;
        UpdateBurstStatus();
        if (Song == Song.None && InCombat)
        {
            if (_inBurstStatusCount < 1)
            {
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
                if (ArmysPaeonPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
            }
            else if (_inBurstStatusCount >= 1)
            {
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
                if (ArmysPaeonPvE.CanUse(out act)) return true;
            }
        }

        if (Song == Song.Wanderer)
        {
            UpdateBurstStatus();
            if (_inBurstStatusCount < 1)
            {
                if ((HostileTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true) && (HostileTarget?.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == true)
                    && IsLastGCD(true, VenomousBitePvE))
                {
                    if ((PotionTimings == PotionTimingOption.ZeroAndSixMins || PotionTimings == PotionTimingOption.ZeroFiveAndTenMins) && UseBurstMedicine(out act)) return true;

                    if (WeaponRemain < 1.25 && RadiantFinalePvE.CanUse(out act)) return true;


                    if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale)
                        && BattleVoicePvE.EnoughLevel && BattleVoicePvE.CanUse(out act)) return true;

                    if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale)
                        && BattleVoicePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.BattleVoice)
                        && WeaponRemain < 1.25f
                        && RagingStrikesPvE.CanUse(out act)) return true;
                }
            }
            if (EnablePrepullHeartbreakShot)
            {
                if (_inBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfter(1))
                    {
                        if (_inBurstStatusCount == 1 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                        if (_inBurstStatusCount == 3 && PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                        if (_inBurstStatusCount == 4 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                    }
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfter(2.01f)
                        && RadiantFinalePvE.CanUse(out act)) return true;

                    if (RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.CanUse(out act)) return true;

                    if (HeartbreakShotPvE.CanUse(out act)) return true;

                    if (RadiantFinalePvE.Cooldown.IsCoolingDown
                        && BattleVoicePvE.Cooldown.IsCoolingDown
                        && WeaponRemain < 1.045f
                        && RagingStrikesPvE.CanUse(out act)) return true;
                }
            }
            else if (!EnablePrepullHeartbreakShot)
            {
                if (_inBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfterGCD(1)
                        && RadiantFinalePvE.CanUse(out act)) return true;

                    if (RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.CanUse(out act)) return true;
                    if (BattleVoicePvE.Cooldown.IsCoolingDown)
                    {
                        if (_inBurstStatusCount == 1 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                        if (_inBurstStatusCount == 3 && PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                        if (_inBurstStatusCount == 4 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                    }
                    if (RadiantFinalePvE.Cooldown.IsCoolingDown
                        && BattleVoicePvE.Cooldown.IsCoolingDown
                        && WeaponRemain < 1.045f
                        && RagingStrikesPvE.CanUse(out act)) return true;
                }
            }
            UpdateBurstStatus();
        }
        if (RadiantFinalePvE.EnoughLevel && RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.EnoughLevel && !BattleVoicePvE.Cooldown.IsCoolingDown) return false;

        if (TheWanderersMinuetPvE.CanUse(out act) && InCombat && WeaponRemain < 1.25f)
        {
            if (SongEndAfter(ArmyRemainTime) && (Song != Song.None || Player.HasStatus(true, StatusID.ArmysEthos))) return true;
        }

        if (EmpyrealArrowPvE.CanUse(out act))
        {
            if (Song == Song.Wanderer)
            {
                if ((RadiantFinalePvE.Cooldown.IsCoolingDown
                    && BattleVoicePvE.Cooldown.IsCoolingDown
                    && RagingStrikesPvE.Cooldown.IsCoolingDown)
                    || InBurstStatus) return true;
            }
            else if (Song == Song.Mage)
            {
                if (_inBurstStatusCount <= 1)
                {
                    if (SongEndAfter(MageRemainTime))
                    {
                        if (EmpyrealArrowPvE.CanUse(out act)) return true;
                        if (ArmysPaeonPvE.CanUse(out act)) return true;
                    }
                    else return true;
                }
                else if (_inBurstStatusCount > 1)
                {
                    if (SongEndAfter(MageRemainTime))
                    {
                        if (ArmysPaeonPvE.CanUse(out act)) return true;
                        if (EmpyrealArrowPvE.CanUse(out act)) return true;
                    }
                    else return true;
                }
            }
            else if (Song == Song.Army && WeaponRemain > 0.9f) return true;
        }
        if (PitchPerfectPvE.CanUse(out act))
        {
            if (SongEndAfter(WandRemainTime) && Repertoire > 0 && WeaponRemain > 1.3f) return true;

            if (Repertoire == 3)
            {
                if (InBurstStatus
                    || (Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice))
                    || Player.HasStatus(true, StatusID.RagingStrikes)) return true;
                if (!InBurstStatus) return true;
            }

            if (Repertoire >= 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(0, 1.25f) && RadiantFinalePvE.Cooldown.IsCoolingDown && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;
        }

        if (_inBurstStatusCount == 2 && Song == Song.Wanderer && PotionTimings == PotionTimingOption.ZeroFiveAndTenMins && SongEndAfter(5) && UseBurstMedicine(out act)) return true;

        if (MagesBalladPvE.CanUse(out act) && InCombat && WeaponRemain < 1)
        {
            if (Song == Song.Wanderer && SongEndAfter(WandRemainTime)) return true;
        }

        if (ArmysPaeonPvE.CanUse(out act) && InCombat)
        {
            if (Song == Song.Mage && SongEndAfter(MageRemainTime) && _inBurstStatusCount <= 1)
            {
                if (WeaponRemain < 1.25f) return true;
            }
            else if (Song == Song.Mage && SongEndAfter(MageRemainTime) && _inBurstStatusCount > 1) return true;
        }
        if (SidewinderPvE.CanUse(out act))
        {
            if (HeartbreakShotPvE.Cooldown.WillHaveXCharges(3, 1.25f)) return false;

            if (Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale) && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;

            if (!BattleVoicePvE.Cooldown.WillHaveOneCharge(10) && !RadiantFinalePvE.Cooldown.WillHaveOneCharge(10) && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;

            if (RagingStrikesPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.RagingStrikes)) return true;
        }


        // Bloodletter Overcap protection
        if (BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 3) && WeaponRemain > 0.8f)
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Prevents Bloodletter bumpcapping when.Mage is the song due to Repetoire procs
        if (BloodletterPvE.Cooldown.WillHaveXCharges(2, 7.5f) && Song == Song.Mage && !SongEndAfterGCD(1))
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Stop using HeartbreakShotPvE during Army's Paeon to ensure 3 charges before Raging Strikes in Wanderer's Minuet
        if (Song == Song.Army && ArmyTime >= 35 && EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(0))
        {
            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Logic to ensure 4 HeartbreakShots in burst window
        if (_inBurstStatusCount >= 1 && Song == Song.Wanderer && RagingStrikesPvE.Cooldown.IsCoolingDown && RagingStrikesPvE.Cooldown.RecastTimeRemainOneCharge <= 45f)
        {
            if (HeartbreakShotPvE.Cooldown.WillHaveXCharges(3, 0) && RagingStrikesPvE.Cooldown.WillHaveOneCharge(0))
            {
                if (HeartbreakShotPvE.CanUse(out act)) return true;
            }
        }

        if (BetterBloodletterLogic(out act)) return true;

        return base.AttackAbility(nextGcd, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {

        if (IronJawsPvE.CanUse(out act)) return true;
        if (IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true) && (IronJawsPvE.Target.Target?.WillStatusEnd(30, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) ?? false))
        {
            if (Player.HasStatus(true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes) && Player.WillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes)) return true;
        }

        if (ResonantArrowPvE.CanUse(out act)) return true;

        if (CanUseApexArrow(out act)) return true;
        if (RadiantEncorePvE.CanUse(out act, skipComboCheck: true))
        {
            if (InBurstStatus && Player.HasStatus(true, StatusID.RagingStrikes)) return true;
        }

        if (BlastArrowPvE.CanUse(out act))
        {
            if (!Player.HasStatus(true, StatusID.RagingStrikes)) return true;
            if (Player.HasStatus(true, StatusID.RagingStrikes) && BarragePvE.Cooldown.IsCoolingDown) return true;
            if (HostileTarget?.WillStatusEndGCD(1, 0.5f, true, StatusID.Windbite, StatusID.Stormbite, StatusID.VenomousBite, StatusID.CausticBite) ?? false) return false;
        }

        //aoe
        if (ShadowbitePvE.CanUse(out act)) return true;
        if (WideVolleyPvE.CanUse(out act)) return true;
        if (QuickNockPvE.CanUse(out act)) return true;

        if (IronJawsPvE.EnoughLevel && (HostileTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true) && (HostileTarget?.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == true))
        {
            // Do not use WindbitePvE or VenomousBitePvE if both statuses are present and IronJawsPvE has enough level
        }
        else
        {
            if (WindbitePvE.CanUse(out act)) return true;
            if (VenomousBitePvE.CanUse(out act)) return true;
        }


        if (RefulgentArrowPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (StraightShotPvE.CanUse(out act)) return true;
        if (HeavyShotPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.HawksEye_3861)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    private bool CanUseApexArrow(out IAction act)
    {

        if (!ApexArrowPvE.CanUse(out act, skipAoeCheck: true)) return false;

        if (QuickNockPvE.CanUse(out _) && SoulVoice == 100) return true;

        if (SoulVoice == 100 && BattleVoicePvE.Cooldown.WillHaveOneCharge(25)) return false;

        if (Song == Song.Wanderer && SoulVoice >= 80 && !Player.HasStatus(true, StatusID.RagingStrikes)) return false;

        if (HostileTarget?.WillStatusEndGCD(1, 1, true, StatusID.Windbite, StatusID.Stormbite, StatusID.VenomousBite, StatusID.CausticBite) ?? false) return false;

        if (SoulVoice >= 80 && Player.HasStatus(true, StatusID.RagingStrikes) && Player.WillStatusEnd(10, false, StatusID.RagingStrikes)) return true;

        if (SoulVoice == 100 && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice)) return true;

        if (Song == Song.Mage && SoulVoice >= 80 && SongEndAfter(22) && SongEndAfter(18)) return true;

        if (!Player.HasStatus(true, StatusID.RagingStrikes) && SoulVoice == 100) return true;

        return false;
    }
    private bool BetterBloodletterLogic(out IAction? act)
    {

        bool isMedicated = Player.HasStatus(true, StatusID.Medicated);

        if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
        {
            if ( Player.HasStatus(true, StatusID.RagingStrikes)
                || Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }

        if (RainOfDeathPvE.CanUse(out act, usedUp: true))
        {
            if ( Player.HasStatus(true, StatusID.RagingStrikes)
                || Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }

        if (BloodletterPvE.CanUse(out act, usedUp: true))
        {
            if ( Player.HasStatus(true, StatusID.RagingStrikes)
                || Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }
        return false;
    }
    private static bool InBurstStatus => Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale);
    private static int _inBurstStatusCount = 0;
    private static DateTime _lastIncrementTime = DateTime.MinValue;
    private static void UpdateBurstStatus()
    {
        if (CombatTime < 5)
        {
            _inBurstStatusCount = 0;
            _lastIncrementTime = DateTime.Now; // Update the timestamp
        }
        if (InBurstStatus && Song == Song.Wanderer)
        {
            if (_inBurstStatusCount < 1)
            {
                _inBurstStatusCount++;
                _lastIncrementTime = DateTime.Now;
            }
            if (_inBurstStatusCount >= 1 && (DateTime.Now - _lastIncrementTime).TotalSeconds >= 120)
            {
                _inBurstStatusCount++;
                _lastIncrementTime = DateTime.Now;

            }
        }
    }

    #endregion

}
