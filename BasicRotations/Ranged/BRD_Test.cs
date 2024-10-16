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
    public float WANDTime { get; set; } = 43;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Mage's Ballad Uptime")]
    public float MAGETime { get; set; } = 40;

    [Range(0, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Army's Paeon Uptime")]
    public float ARMYTime { get; set; } = 37;

    private float WANDRemainTime => 45 - WANDTime;
    private float MAGERemainTime => 45 - MAGETime;
    private float ARMYRemainTime => 45 - ARMYTime;
    

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
    

    
    private static bool InBurstStatus => (Player.Level > 50 && !Player.WillStatusEnd(0, true, StatusID.RagingStrikes))
        || (Player.Level >= 50 && Player.Level < 90 && !Player.WillStatusEnd(0, true, StatusID.RagingStrikes) && !Player.WillStatusEnd(0, true, StatusID.BattleVoice))
        || (MinstrelsCodaTrait.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RagingStrikes) && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale) && !Player.WillStatusEnd(0, true, StatusID.BattleVoice));

    #endregion

    #region oGCD Logic
    private float LastBurstIncrementTime = 0;
    public static int InBurstStatusCount = 0;
    public void UpdateBurstStatus()
    {
        if (CombatTime <= 1)
        {
            InBurstStatusCount = 0;
            LastBurstIncrementTime = 0;
        }

        // Increment InBurstStatusCount every 2 minutes (120 seconds) of combat time
        if (CombatTime - LastBurstIncrementTime >= 120)
        {
            InBurstStatusCount++;
            LastBurstIncrementTime += 120;
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
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (Song == Song.NONE && InCombat)
        {
            UpdateBurstStatus();
           if (InBurstStatusCount < 1)
            { 
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (ArmysPaeonPvE.CanUse(out act)) return true;
            }
            else if (InBurstStatusCount >= 1)
            {
                if (TheWanderersMinuetPvE.CanUse(out act) && WeaponRemain < 1.05f) return true;
                if (MagesBalladPvE.CanUse(out act) && WeaponRemain < 1.25f) return true;
                if (ArmysPaeonPvE.CanUse(out act)) return true;
            }
        }
      
        if (IsBurst && Song == Song.WANDERER)
        {
            UpdateBurstStatus();
            if (InBurstStatusCount < 1)
            {
                if (PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                
                if ((HostileTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true) && (HostileTarget?.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == true)
                    && IsLastGCD(true, VenomousBitePvE)
                    && WeaponRemain < 1.25f
                    && RadiantFinalePvE.EnoughLevel && RadiantFinalePvE.CanUse(out act)) return true;

                if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale) 
                    && BattleVoicePvE.EnoughLevel && BattleVoicePvE.CanUse(out act))  return true;

                if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale) 
                    && BattleVoicePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.BattleVoice)
                    && WeaponRemain < 1.25f 
                    && RagingStrikesPvE.CanUse(out act)) return true;
            }
            else
            {
                UpdateBurstStatus();
                if (InBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.Cooldown.IsCoolingDown && TheWanderersMinuetPvE.Cooldown.ElapsedAfterGCD(1)
                        && RadiantFinalePvE.CanUse(out act)) return true;

                    if (RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.CanUse(out act)) return true;
                    
                    UpdateBurstStatus();
                    if (Player.HasStatus(true, StatusID.RadiantFinale))
                    {
                        if (InBurstStatusCount == 1 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 3 && PotionTimings == PotionTimingOption.ZeroAndSixMins && UseBurstMedicine(out act)) return true;
                        if (InBurstStatusCount == 4 && PotionTimings == PotionTimingOption.TwoAndEightMins && UseBurstMedicine(out act)) return true;
                    }
                    if (RadiantFinalePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.RadiantFinale)
                        && BattleVoicePvE.EnoughLevel && !Player.WillStatusEnd(0, true, StatusID.BattleVoice)
                        && WeaponRemain < 1.05f
                        && RagingStrikesPvE.CanUse(out act, isLastAbility: true)) return true; 
                }
            }
        }
        if (RadiantFinalePvE.EnoughLevel && RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.EnoughLevel && !BattleVoicePvE.Cooldown.IsCoolingDown) return false;

        if (TheWanderersMinuetPvE.CanUse(out act, isLastAbility: true) && InCombat && WeaponRemain < 1.25f)
        {
            if (SongEndAfter(ARMYRemainTime) && (Song != Song.NONE || Player.HasStatus(true, StatusID.ArmysEthos))) return true;
        }

        if (EmpyrealArrowPvE.CanUse(out act) && WeaponRemain > 0.825f)
        {
            if (Song == Song.WANDERER)
            { 
                if (InBurstStatusCount < 1
                    && RadiantFinalePvE.Cooldown.IsCoolingDown
                    && BattleVoicePvE.Cooldown.IsCoolingDown
                    && RagingStrikesPvE.Cooldown.IsCoolingDown
                    && Player.HasStatus(true, StatusID.RadiantFinale, StatusID.BattleVoice, StatusID.RagingStrikes)) return true;

                if (RadiantFinalePvE.Cooldown.IsCoolingDown
                    && BattleVoicePvE.Cooldown.IsCoolingDown
                    && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;
            }
            else if (Song == Song.MAGE)
            {
                UpdateBurstStatus();
                if (InBurstStatusCount < 1) return true;
                if (InBurstStatusCount >= 1 && ArmysPaeonPvE.Cooldown.IsCoolingDown && !ArmysPaeonPvE.Cooldown.WillHaveOneCharge(2)) return true;
                if (InBurstStatusCount >= 1 && ArmysPaeonPvE.Cooldown.IsCoolingDown && ArmysPaeonPvE.Cooldown.WillHaveOneCharge(2) && EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(1)) return false;
            }
            else if (Song == Song.ARMY)
            {
                return true;
            }
        }
        if (PitchPerfectPvE.CanUse(out act, skipCastingCheck: true, skipAoeCheck: true, skipComboCheck: true))
        {
            if (SongEndAfter(3.05f) && Repertoire > 0 && PitchPerfectPvE.CanUse(out act, isFirstAbility: true)) return true;

            if (Repertoire == 3) return true;

            if (Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) && RadiantFinalePvE.Cooldown.IsCoolingDown) return true;
        }

        if (MagesBalladPvE.CanUse(out act) && InCombat && WeaponRemain < 1.20f)
        {
            if (Song == Song.WANDERER && SongEndAfter(WANDRemainTime) && WeaponRemain < 1.20f) return true;
            if (Song == Song.ARMY && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown) return true;
        }

        if (ArmysPaeonPvE.CanUse(out act) && InCombat)
        {   
            UpdateBurstStatus();
            if (InBurstStatusCount < 1)
            {
                if (TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(MAGERemainTime) && Song == Song.MAGE & WeaponRemain <1.25) return true;
                if (TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown && Song == Song.WANDERER) return true;
                if (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)) return true;
            }
            else
            {
                UpdateBurstStatus();
                if (InBurstStatusCount >= 1)
                {
                    if (TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(MAGERemainTime) && Song == Song.MAGE) return true;
                    if (TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown && Song == Song.WANDERER) return true;
                    if (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2)) return true;
                }
            }
        }
        if (SidewinderPvE.CanUse(out act))
        {
            if (Player.HasStatus(true, StatusID.BattleVoice) && (Player.HasStatus(true, StatusID.RadiantFinale) && RagingStrikesPvE.Cooldown.IsCoolingDown || !RadiantFinalePvE.EnoughLevel)) return true;

            if (!BattleVoicePvE.Cooldown.WillHaveOneCharge(10) && !RadiantFinalePvE.Cooldown.WillHaveOneCharge(10) && RagingStrikesPvE.Cooldown.IsCoolingDown) return true;

            if (RagingStrikesPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.RagingStrikes)) return true;
        }
            
        // Bloodletter Overcap protection
        if (BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 3) & WeaponRemain > 0.85f)
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Prevents Bloodletter bumpcapping when MAGE is the song due to Repetoire procs
        if (BloodletterPvE.Cooldown.WillHaveXCharges(3, 12f) && Song == Song.MAGE && !SongEndAfterGCD(2) & WeaponRemain > 0.85f)
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;

            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
        }

        // Use Bloodletter stacks when in potion burst window
        if (Player.HasStatus(true, StatusID.Medicated) || Player.HasStatus(true, StatusID.RadiantFinale) || Player.HasStatus(true, StatusID.BattleVoice) || Player.HasStatus(true, StatusID.RagingStrikes))
        {
            if (RainOfDeathPvE.CanUse(out act, usedUp: true)) return true;
            
            if (HeartbreakShotPvE.CanUse(out act, usedUp: true)) return true;

            if (BloodletterPvE.CanUse(out act, usedUp: true)) return true;
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
            if (InBurstStatus) return true;
        }

        if (BlastArrowPvE.CanUse(out act))
        {
            if (!Player.HasStatus(true, StatusID.RagingStrikes)) return true;
            if (Player.HasStatus(true, StatusID.RagingStrikes) && BarragePvE.Cooldown.IsCoolingDown) return true;
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

        if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || (isRagingStrikesLevel && !isBattleVoiceLevel && Player.HasStatus(true, StatusID.RagingStrikes))
                || (isBattleVoiceLevel && !isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice))
                || isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)) return true;
        }

        if (RainOfDeathPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || (isRagingStrikesLevel && !isBattleVoiceLevel && Player.HasStatus(true, StatusID.RagingStrikes))
                || (isBattleVoiceLevel && !isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice))
                || isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)) return true;
        }

        if (BloodletterPvE.CanUse(out act, usedUp: true))
        {
            if ((!isRagingStrikesLevel)
                || (isRagingStrikesLevel && !isBattleVoiceLevel && Player.HasStatus(true, StatusID.RagingStrikes))
                || (isBattleVoiceLevel && !isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice))
                || isRadiantFinaleLevel && Player.HasStatus(true, StatusID.RagingStrikes) && Player.HasStatus(true, StatusID.BattleVoice) && Player.HasStatus(true, StatusID.RadiantFinale)) return true;
        }
        return false;
    }

    
    #endregion

}
