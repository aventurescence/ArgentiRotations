using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace DefaultRotations.Ranged;

[Rotation("369 BRD Test", CombatType.PvE, GameVersion = "7.05",
    Description = "Please make sure that the three song times add up to 120 seconds, Wanderers default first song for now. Only intended to be used in level 100 content.")]
[SourceCode(Path = "main/BasicRotations/Ranged/BRD_Default.cs")]
[Api(4)]
public sealed class BRD_Test : BardRotation
{
    #region Config Options
    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Wanderer's Minuet Uptime")]
    public float WANDTime { get; set; } = 42;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Mage's Ballad Uptime")]
    public float MAGETime { get; set; } = 39;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Army's Paeon Uptime")]
    public float ARMYTime { get; set; } = 36;

    private float WANDRemainTime => 45 - WANDTime;
    private float MAGERemainTime => 45 - MAGETime;
    private float ARMYRemainTime => 45 - ARMYTime;

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

    private static bool InBurstStatus => Player.HasStatus(true, StatusID.RagingStrikes) || Player.HasStatus(true, StatusID.BattleVoice) || Player.HasStatus(true, StatusID.RadiantFinale);

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

    private static int InBurstStatusCount = 0;
    private static DateTime lastIncrementTime = DateTime.MinValue;
    
    private static void UpdateBurstStatus()
    {
        if (CombatTime < 5)
        {
            InBurstStatusCount = 0;
            lastIncrementTime = DateTime.Now; // Update the timestamp
        }
        if (InBurstStatus && Song == Song.WANDERER)
        {
            if (InBurstStatusCount < 1)
            {
                InBurstStatusCount++;
                lastIncrementTime = DateTime.Now;
            }
            if (InBurstStatusCount >= 1 && (DateTime.Now - lastIncrementTime).TotalSeconds >= 120)
                {
                    InBurstStatusCount++;
                    lastIncrementTime = DateTime.Now;
                }
        }
    }


    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {

        if (nextGCD.IsTheSameTo(true, WindbitePvE, VenomousBitePvE, StraightShotPvE, IronJawsPvE))
        {
            return base.EmergencyAbility(nextGCD, out act);
        }
        else if (!RagingStrikesPvE.EnoughLevel || Player.HasStatus(true, StatusID.RagingStrikes))
        {
            if ((EmpyrealArrowPvE.Cooldown.IsCoolingDown && !EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) || !EmpyrealArrowPvE.EnoughLevel) && Repertoire != 3)
            {
                if (!Player.HasStatus(true, StatusID.HawksEye_3861) && BarragePvE.CanUse(out act)) return true;
            }
            if (HeartbreakShotPvE.Cooldown.WillHaveXChargesGCD(BloodletterMax, 1)) 
            {
                if (HeartbreakShotPvE.CanUse(out act)) return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        UpdateBurstStatus();
        if (Song == Song.NONE && InCombat)
        {
           if (InBurstStatusCount < 1)
            { 
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain <1.25f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
                if (ArmysPaeonPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
            }
            else if (InBurstStatusCount >= 1)
            {
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 0.8f) return true;
                if (ArmysPaeonPvE.CanUse(out act)) return true;
            }
        }
      
        if (Song == Song.WANDERER)
        {
            UpdateBurstStatus();
            if (InBurstStatusCount < 1)
            {
                if ((HostileTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true) && (HostileTarget?.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == true)
                    && IsLastGCD(true, VenomousBitePvE))
                    {
                        if ((PotionTimings == PotionTimingOption.ZeroAndSixMins || PotionTimings == PotionTimingOption.ZeroFiveAndTenMins) && UseBurstMedicine(out act)) return true;
                        if (WeaponRemain < 1.25 && RadiantFinalePvE.CanUse(out act)) return true;
                    }
        
                if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale)
                    && BattleVoicePvE.EnoughLevel && BattleVoicePvE.CanUse(out act)) return true;
        
                if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale)
                    && BattleVoicePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.BattleVoice)
                    && WeaponRemain < 1.25f
                    && RagingStrikesPvE.CanUse(out act)) return true;
            }
            if (EnablePrepullHeartbreakShot)
            {
                if (InBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfter(1))
                    {
                        if (InBurstStatusCount == 1 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 3 && PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 4 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                    }
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfterGCD(1)
                        && RadiantFinalePvE.CanUse(out act)) return true;
        
                    if (RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.CanUse(out act)) return true;

                    if (HeartbreakShotPvE.Cooldown.HasOneCharge && HeartbreakShotPvE.CanUse(out act)) return true;
        
                    if (RadiantFinalePvE.Cooldown.IsCoolingDown
                        && BattleVoicePvE.Cooldown.IsCoolingDown
                        && WeaponRemain < 1.045f
                        && RagingStrikesPvE.CanUse(out act)) return true;
                }
            }
            else if (!EnablePrepullHeartbreakShot)
            {
                if (InBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfterGCD(1)
                        && RadiantFinalePvE.CanUse(out act)) return true;
        
                    if (RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.CanUse(out act)) return true;
                    if (BattleVoicePvE.Cooldown.IsCoolingDown)
                    {
                        if (InBurstStatusCount == 1 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 3 && PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 4 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
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

        if (TheWanderersMinuetPvE.CanUse(out act) && InCombat && WeaponRemain < 0.9f)
        {
            if (SongEndAfter(ARMYRemainTime) && (Song != Song.NONE || Player.HasStatus(true, StatusID.ArmysEthos))) return true;
        }

        if (EmpyrealArrowPvE.CanUse(out act))
        {
            UpdateBurstStatus();
            if (Song == Song.WANDERER)
            { 
                if ((RadiantFinalePvE.Cooldown.IsCoolingDown
                    && BattleVoicePvE.Cooldown.IsCoolingDown
                    && RagingStrikesPvE.Cooldown.IsCoolingDown)
                    || (Player.HasStatus(true, StatusID.RadiantFinale) 
                    && Player.HasStatus(true, StatusID.BattleVoice) 
                    && Player.HasStatus(true, StatusID.RagingStrikes))) return true;
            }
            else if (Song == Song.MAGE)
            {
                if (InBurstStatusCount <= 1)
                {
                    if (SongEndAfter(MAGERemainTime))
                    {
                        if (EmpyrealArrowPvE.CanUse(out act)) return true;
                        if (ArmysPaeonPvE.CanUse(out act)) return true;
                    }
                    else return true;
                }
                else if (InBurstStatusCount > 1)
                {
                    if (SongEndAfter(MAGERemainTime))
                    {
                        if (ArmysPaeonPvE.CanUse(out act)) return true;
                        if (EmpyrealArrowPvE.CanUse(out act)) return true;
                    }
                    else return true;
                }
            }
            else if (Song == Song.ARMY && WeaponRemain > 0.75f) return true; 
        }
        if (PitchPerfectPvE.CanUse(out act))
        {
            if (SongEndAfter(WANDRemainTime - 0.8f) && Repertoire > 0 && WeaponRemain > 0.8f) return true;
            
            if (Repertoire == 3)
            {
                if (Player.HasStatus(true, StatusID.RadiantFinale) 
                    && Player.HasStatus(true, StatusID.BattleVoice) 
                    && Player.HasStatus(true, StatusID.RagingStrikes) 
                    || (Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice))
                    || Player.HasStatus(true, StatusID.RagingStrikes)) return true;
                if (!InBurstStatus) return true;
            }
        
            if (Repertoire >= 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(0,1) && RadiantFinalePvE.Cooldown.IsCoolingDown && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;
        }

        if (InBurstStatusCount == 2 && Song == Song.WANDERER && PotionTimings == PotionTimingOption.ZeroFiveAndTenMins && SongEndAfter(5) && UseBurstMedicine(out act)) return true;

        if (MagesBalladPvE.CanUse(out act) && InCombat && WeaponRemain < 0.7f)
        {
            if (Song == Song.WANDERER && SongEndAfter(WANDRemainTime - 0.7f)) return true;
        }

        if (ArmysPaeonPvE.CanUse(out act) && InCombat) 
        {
            if (Song == Song.MAGE && SongEndAfter(MAGERemainTime) && InBurstStatusCount <= 1)
            {
                if (WeaponRemain < 0.9f) return true;
            }
            else if (Song == Song.MAGE && SongEndAfter(MAGERemainTime) && InBurstStatusCount > 1) return true;
        }
        if (SidewinderPvE.CanUse(out act))
        {
            if (Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale) && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;

            if (!BattleVoicePvE.Cooldown.WillHaveOneCharge(10) && !RadiantFinalePvE.Cooldown.WillHaveOneCharge(10) && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;

            if (RagingStrikesPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.RagingStrikes)) return true;
        }

        
            
        // Bloodletter Overcap protection
        if (BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 2.49f) && WeaponRemain > 1)
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Prevents Bloodletter bumpcapping when MAGE is the song due to Repetoire procs
        if (BloodletterPvE.Cooldown.WillHaveXCharges(2, 7.5f) && Song == Song.MAGE && !SongEndAfterGCD(1) && WeaponRemain > 1)
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Stop using HeartbreakShotPvE during Army's Paeon to ensure 3 charges before Raging Strikes in Wanderer's Minuet
        if (Song == Song.ARMY && HeartbreakShotPvE.Cooldown.WillHaveXCharges(3, RagingStrikesPvE.Cooldown.RecastTimeRemainOneCharge))
        {
            if (HeartbreakShotPvE.CanUse(out act, usedUp: false)) return false;
        }

        // Ensure HeartbreakShotPvE has 3 charges without overcapping during Wanderer's Minuet after Army's Paeon
        if (Song == Song.WANDERER && ArmysPaeonPvE.Cooldown.IsCoolingDown && HeartbreakShotPvE.Cooldown.WillHaveXCharges(3, 0) && RagingStrikesPvE.Cooldown.WillHaveOneCharge(0))
        {
            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;
        }

        if (BetterBloodletterLogic(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
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
            if (HostileTarget?.WillStatusEndGCD(1,1, true, StatusID.Windbite, StatusID.Stormbite, StatusID.VenomousBite, StatusID.CausticBite) ?? false) return false;
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

        if (Song == Song.WANDERER && SoulVoice >= 80 && !Player.HasStatus(true, StatusID.RagingStrikes)) return false;

        if (HostileTarget?.WillStatusEndGCD(1,1, true, StatusID.Windbite, StatusID.Stormbite, StatusID.VenomousBite, StatusID.CausticBite) ?? false) return false;

        if (SoulVoice >= 80 && Player.HasStatus(true, StatusID.RagingStrikes) && Player.WillStatusEnd(10, false, StatusID.RagingStrikes)) return true;

        if (SoulVoice == 100 && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice)) return true;

        if (Song == Song.MAGE && SoulVoice >= 80 && SongEndAfter(22) && SongEndAfter(18)) return true;

        if (!Player.HasStatus(true, StatusID.RagingStrikes) && SoulVoice == 100) return true;

        return false;
    }
    private bool BetterBloodletterLogic(out IAction? act)
    {

        bool isRagingStrikesLevel = RagingStrikesPvE.EnoughLevel;
        bool isBattleVoiceLevel = BattleVoicePvE.EnoughLevel;
        bool isRadiantFinaleLevel = RadiantFinalePvE.EnoughLevel;
        bool isMedicated = Player.HasStatus(true, StatusID.Medicated);

        if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }

        if (RainOfDeathPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }

        if (BloodletterPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)
                || isMedicated) return true;
        }
        return false;
    }

    
    #endregion

}
