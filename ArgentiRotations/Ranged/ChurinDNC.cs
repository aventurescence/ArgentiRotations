using System.ComponentModel;
using System.Globalization;
using ArgentiRotations.Common;
using Dalamud.Interface.Colors;
using ECommons.Logging;

namespace ArgentiRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.2.1", Description = "For High end content use, stay cute my dancer friends. <3")]
[SourceCode(Path = "ArgentiRotations/Ranged/ChurinDNC.cs")]
[Api(4)]
public sealed class ChurinDNC : DancerRotation
{
    #region Properties

    #region Boolean Properties

    private bool ShouldUseLastDance { get; set; } = true;
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled;
    private bool ShouldUseStandardStep { get; set; }
    private bool ShouldUseFlourish { get; set; }

    private static bool DanceDance => Player.HasStatus(true, StatusID.Devilment, StatusID.TechnicalFinish);
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated);
    private static bool AreDanceTargetsInRange => AllHostileTargets.Any(target => target.DistanceToPlayer() <= 15) || CurrentTarget?.DistanceToPlayer() <= 15;

    private DateTime _lastPotionUsed = DateTime.MinValue;

    #endregion

    #region Other Properties
    private string CurrentGCDEvaluation { get; set; } = "No GCD Found";
    internal static Dictionary<string, Dictionary<string, string>> GCDMethodDebugInfo { get; } = new();

    public static List<Dictionary<string, object>> GetGCDMethodDebugInfo()
    {
        var debugInfo = new List<Dictionary<string, object>>();
        foreach (var (methodName, conditions) in GCDMethodDebugInfo)
        {
            var debugEntry = new Dictionary<string, object>
            {
                { "Method Name", methodName },
                { "Conditions", conditions }
            };
            debugInfo.Add(debugEntry);
        }
        return debugInfo;
    }
    private static void UpdateMethodDebugInfo(string methodName, Dictionary<string, string> conditions)
    {
        GCDMethodDebugInfo[methodName] = new Dictionary<string, string>(conditions);
    }
    private Dictionary<string, string> CreateDebugInfo(params (string key, object value)[] entries)
    {
        if (!IsDebugTableVisible && (DateTime.Now - LastDebugUpdateTime).TotalSeconds < 0.5)
            return new Dictionary<string, string>(); // Empty dict if debug not visible

        var dict = new Dictionary<string, string>(entries.Length + 1);
        foreach (var (key, value) in entries)
        {
            dict[key] = value?.ToString() ?? "null";
        }
        return dict;
    }

    public static bool IsDebugTableVisible { get; set; }
    private int MaxDebugEntries { get; set; } = 50;
    private DateTime LastDebugUpdateTime { get; set; } = DateTime.MinValue;
    private DateTime _lastDebugClear = DateTime.MinValue;
    private readonly DebugSettings _debugSettings = new();

    private bool LogMethodResult(string methodName, bool result, Dictionary<string, string> info)
    {
        // Only update debug info if the debug table is visible or within update interval
        if (IsDebugTableVisible || (DateTime.Now - LastDebugUpdateTime).TotalSeconds >= 0.5)
        {
            info["Final Result"] = result.ToString();
            info["Timestamp"] = DateTime.Now.ToString("HH:mm:ss.fff");

            // Limit dictionary size
            if (GCDMethodDebugInfo.Count >= MaxDebugEntries && !GCDMethodDebugInfo.ContainsKey(methodName))
            {
                // Remove oldest entry
                var oldestEntry = GCDMethodDebugInfo.Keys.FirstOrDefault();
                if (oldestEntry != null)
                    GCDMethodDebugInfo.Remove(oldestEntry);
            }

            UpdateMethodDebugInfo(methodName, info);
            LastDebugUpdateTime = DateTime.Now;
        }

        return result;
    }

    private const float DefaultAnimationLock = 0.6f;

    private enum PotionTimings
    {
        [Description("None")] None,

        [Description("Opener and Six Minutes")]
        ZeroSix,

        [Description("Two Minutes and Eight Minutes")]
        TwoEight,

        [Description("Opener, Five Minutes and Ten Minutes")]
        ZeroFiveTen,

        [Description("Custom - set values below")]
        Custom
    }

    private int FirstPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 0,
        PotionTimings.TwoEight => 2,
        PotionTimings.ZeroFiveTen => 0,
        PotionTimings.Custom => CustomFirstPotionTime,
        _ => 9999
    };

    private int SecondPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 6,
        PotionTimings.TwoEight => 8,
        PotionTimings.ZeroFiveTen => 5,
        PotionTimings.Custom => CustomSecondPotionTime,
        _ => 9999
    };

    private int ThirdPotionTime => PotionTiming switch
    {
        PotionTimings.None => 9999,
        PotionTimings.ZeroSix => 9999,
        PotionTimings.TwoEight => 9999,
        PotionTimings.ZeroFiveTen => 10,
        PotionTimings.Custom => CustomThirdPotionTime,
        _ => 9999
    };

    private bool EnableFirstPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => true,
        PotionTimings.TwoEight => true,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableFirstPotion,
        _ => false
    };

    private bool EnableSecondPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => true,
        PotionTimings.TwoEight => true,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableSecondPotion,
        _ => false
    };

    private bool EnableThirdPotion => PotionTiming switch
    {
        PotionTimings.None => false,
        PotionTimings.ZeroSix => false,
        PotionTimings.TwoEight => false,
        PotionTimings.ZeroFiveTen => true,
        PotionTimings.Custom => CustomEnableThirdPotion,
        _ => false
    };

    private static IBattleChara? CurrentDancePartner =>
        PartyMembers.FirstOrDefault(member => member.HasStatus(true, StatusID.DancePartner));

    #endregion

    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    private bool HoldTechForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold Standard Step if no targets in range (Warning, will drift)")]
    private bool HoldStandardForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Potion Presets")]
    private PotionTimings PotionTiming { get; set; } = PotionTimings.None;

    [RotationConfig(CombatType.PvE, Name = "Enable First Potion for Custom Potion Timings?")]
    private bool CustomEnableFirstPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "First Potion Usage for custom timings - enter time in minutes")]
    private int CustomFirstPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Second Potion?")]
    private bool CustomEnableSecondPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Second Potion Usage for custom timings - enter time in minutes")]
    private int CustomSecondPotionTime { get; set; } = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Third Potion?")]
    private bool CustomEnableThirdPotion { get; set; } = true;

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Third Potion Usage for custom timings - enter time in minutes")]
    private int CustomThirdPotionTime { get; set; } = 0;



    #endregion

    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        ShouldUseFlourish = false;

        if (remainTime >= 15) return base.CountDownAction(remainTime);
        if (TryUseClosedPosition(out var act) ||
            StandardStepPvE.CanUse(out act) ||
            ExecuteStepGCD(out act) || remainTime <= 1 && EnableFirstPotion && FirstPotionTime == 0 && UseBurstMedicine(out act)
            || remainTime <= 0.54f && DoubleStandardFinishPvE.CanUse(out act))
        {
            return act;
        }
        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        CheckDancePartnerStatus();
        if (IsLastGCD(ActionID.TechnicalStepPvE)) return TryUsePots(out act);

        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (!IsDancing && !(StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118)))
            return base.EmergencyAbility(nextGCD, out act);

        return SetActToNull(out act);
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsDancing || nextGCD.AnimationLockTime > DefaultAnimationLock) return SetActToNull(out act);

        return TryUseFlourish(out act) ||
               TryUseFeathers(out act) ||
               base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (_debugSettings.AutoClearDebugLogs && (DateTime.Now - _lastDebugClear).TotalSeconds > _debugSettings.DebugClearInterval)
        {
            GCDMethodDebugInfo.Clear();
            _lastDebugClear = DateTime.Now;
        }

        if (IsDancing)
        {
            CurrentGCDEvaluation = "Dancing: TryFinishTheDance or ExecuteStepGCD";
            return TryFinishTheDance(out act) || ExecuteStepGCD(out act);
        }

        CurrentGCDEvaluation = "Trying: TryUseTechnicalStep";
        if (TryUseTechnicalStep(out act)) return true;

        CurrentGCDEvaluation = "Trying: TryUseStandardStep";
        if (TryUseStandardStep(out act)) return true;

        CurrentGCDEvaluation = "Trying: TryUseFinishingMove";
        if (TryUseFinishingMove(out act)) return true;

        CurrentGCDEvaluation = "Trying: TryHoldGCD";
        if (TryHoldGCD(out act)) return true;

        CurrentGCDEvaluation = "Trying: ProcHelper";
        if (TryProcHelper(out act)) return true;

        CurrentGCDEvaluation = "Trying: TryUseTechGCD";
        if (TryUseTechGCD(out act, Player.HasStatus(true, StatusID.Devilment))) return true;

        CurrentGCDEvaluation = "Trying: TryUseFillerGCD";
        if (TryUseFillerGCD(out act)) return true;

        CurrentGCDEvaluation = "No Valid GCD Found";
        return base.GeneralGCD(out act);
    }

    #endregion

    #region Extra Methods

    #region Dance Partner Logic

    private bool TryUseClosedPosition(out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.ClosedPosition) || !PartyMembers.Any())
            return SetActToNull(out act);

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        if (!Player.HasStatus(true, StatusID.ClosedPosition) || !CheckDancePartnerStatus())
            return SetActToNull(out act);

        var standardOrFinishingCharge =
            (StandardStepPvE.Cooldown.IsCoolingDown && StandardStepPvE.Cooldown.WillHaveOneChargeGCD(2, 1)) ||
            (FinishingMovePvE.Cooldown.IsCoolingDown && FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(2, 1));

        // Check cooldown conditions
        if (standardOrFinishingCharge && CheckDancePartnerStatus()) return EndingPvE.CanUse(out act);
        return SetActToNull(out act);
    }

    private static bool CheckDancePartnerStatus()
    {
        return CurrentDancePartner != null &&
               CurrentDancePartner.HasStatus(true, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911);
    }

    #endregion

    #region Dance Logic

    private bool TryHoldGCD(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Should Hold For Technical Step", ShouldHoldForTechnicalStep()),
            ("Should Hold For Standard Step", ShouldHoldForStandardStep()));

        // Check conditions for holding GCDs
        var shouldHoldForTech = ShouldHoldForTechnicalStep();

        var shouldHoldForStandard = ShouldHoldForStandardStep();

        if (shouldHoldForTech || shouldHoldForStandard)
        {
            debug["Decision"] = shouldHoldForTech ? "Holding for Technical Step" : "Holding for Standard Step";
            var result = base.GeneralGCD(out act);
            debug["base.GeneralGCD Result"] = result.ToString();

            return LogMethodResult("TryHoldGCD", result, debug);
        }

        debug["Decision"] = "Not holding GCD";
        return LogMethodResult("TryHoldGCD", SetActToNull(out act), debug);
    }

    private bool ShouldHoldForTechnicalStep()
    {
        var debug = CreateDebugInfo(
            ("Is Dancing", IsDancing),
            ("Should Use Tech Step", ShouldUseTechStep)
        );

        if (IsDancing || !ShouldUseTechStep)
        {
            debug["Early Exit"] = IsDancing ? "Currently dancing" : "Tech Step not enabled";
            UpdateMethodDebugInfo("ShouldHoldForTechnicalStep", debug);
            return false;
        }

        var techWillHaveOneCharge = TechnicalStepPvE.Cooldown.IsCoolingDown &&
                                   TechnicalStepPvE.Cooldown.WillHaveOneCharge(1.5f) ||
                                   TechnicalStepPvE.Cooldown.HasOneCharge;

        debug["Tech Will Have One Charge"] = techWillHaveOneCharge.ToString();
        debug["Hold Tech For Targets"] = HoldTechForTargets.ToString();
        debug["Are Dance Targets In Range"] = AreDanceTargetsInRange.ToString();
        debug["Has Technical Finish"] = Player.HasStatus(true, StatusID.TechnicalFinish).ToString();

        var holdForTarget = HoldTechForTargets && AreDanceTargetsInRange;
        var hasTechFinish = Player.HasStatus(true, StatusID.TechnicalFinish);

        debug["Hold For Target"] = holdForTarget.ToString();

        var result = techWillHaveOneCharge &&
                    ShouldUseTechStep &&
                    (holdForTarget || !HoldTechForTargets) &&
                    !IsDancing && !hasTechFinish;

        debug["Decision Reason"] = result ?
            "Tech charge available and conditions met for hold" :
            "One or more conditions not met";
        debug["Final Result"] = result.ToString();

        UpdateMethodDebugInfo("ShouldHoldForTechnicalStep", debug);
        return result;
    }

    private bool ShouldHoldForStandardStep()
    {
        var debug = CreateDebugInfo(
            ("Is Dancing", IsDancing),
            ("Standard Step Cooldown Status", StandardStepPvE.Cooldown.IsCoolingDown),
            ("Standard Step Cooldown Remaining", StandardStepPvE.Cooldown.IsCoolingDown ? StandardStepPvE.Cooldown.RecastTimeRemainOneCharge.ToString(CultureInfo.CurrentCulture) : "N/A")
        );

        // Check if we should hold for Technical Step first
        var shouldHoldForTech = ShouldHoldForTechnicalStep();
        debug["Should Hold For Technical Step"] = shouldHoldForTech.ToString();

        // Fix operator precedence with parentheses and increase window to 3 seconds
        var standardWillBeAvailableSoon = (StandardStepPvE.Cooldown.IsCoolingDown &&
                                         StandardStepPvE.Cooldown.WillHaveOneCharge(3.0f)) ||
                                         StandardStepPvE.Cooldown.HasOneCharge;

        debug["Standard Will Be Available Soon"] = standardWillBeAvailableSoon.ToString();
        debug["Standard Cooldown Has One Charge"] = StandardStepPvE.Cooldown.HasOneCharge.ToString();

        if (StandardStepPvE.Cooldown.IsCoolingDown)
            debug["Will Have One Charge in 3s"] = StandardStepPvE.Cooldown.WillHaveOneCharge(3.0f).ToString();

        var holdForTarget = HoldStandardForTargets && AreDanceTargetsInRange;
        debug["Hold For Target"] = holdForTarget.ToString();

        var result = !IsDancing && standardWillBeAvailableSoon &&
                    (holdForTarget || !HoldStandardForTargets) &&
                    !shouldHoldForTech;

        debug["Decision Reason"] = result ?
            "Standard Step will be available soon and not holding for Tech" :
            "Either dancing, Standard Step not coming off CD soon, or holding for Tech";
        debug["Final Result"] = result.ToString();

        UpdateMethodDebugInfo("ShouldHoldForStandardStep", debug);
        return result;
    }

    private bool TryUseTechnicalStep(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("In Combat", InCombat),
            ("Hold Tech For Targets", HoldTechForTargets),
            ("Are Dance Targets In Range", AreDanceTargetsInRange),
            ("Is Dancing", IsDancing),
            ("Should Use Tech Step", ShouldUseTechStep)
        );

        if (!InCombat || (HoldTechForTargets && !AreDanceTargetsInRange) || IsDancing || !ShouldUseTechStep)
        {
            var exitReason = !InCombat ? "Not in combat" :
                HoldTechForTargets && !AreDanceTargetsInRange ? "Hold Tech for targets and no targets in range" :
                IsDancing ? "Currently dancing" :
                !ShouldUseTechStep ? "Tech Step not enabled" : "No valid reason";

            debug["Early Exit"] = exitReason;
            return LogMethodResult("TryUseTechnicalStep", SetActToNull(out act), debug);
        }

        debug["Attempting Tech Step"] = "True";
        var result = TechnicalStepPvE.CanUse(out act);
        debug["TechnicalStepPvE.CanUse Result"] = result.ToString();

        return LogMethodResult("TryUseTechnicalStep", result, debug);
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Has Standard Step", Player.HasStatus(true, StatusID.StandardStep)),
            ("Has Technical Step", Player.HasStatus(true, StatusID.TechnicalStep)),
            ("Completed Steps", CompletedSteps),
            ("Is Dancing", IsDancing)
        );

        if (!Player.HasStatus(true, StatusID.StandardStep) && !Player.HasStatus(true, StatusID.TechnicalStep))
        {
            debug["Early Exit"] = "No dance in progress";
            return LogMethodResult("TryFinishTheDance", SetActToNull(out act), debug);
        }

        if (IsDancing)
        {
            if (Player.HasStatus(true, StatusID.StandardStep))
            {
                var shouldFinish = CompletedSteps == 2 && (!HoldStandardForTargets || AreDanceTargetsInRange);
                var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

                debug["Standard: Steps Completed"] = CompletedSteps + "/2";
                debug["Standard: Should Finish (steps complete)"] = shouldFinish.ToString();
                debug["Standard: About To Time Out"] = aboutToTimeOut.ToString();

                if (shouldFinish || aboutToTimeOut)
                {
                    debug["Standard: Attempting Finish"] = "True";
                    var result = DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true);
                    debug["Standard: DoubleStandardFinishPvE.CanUse Result"] = result.ToString();
                    if (result)
                        return LogMethodResult("TryFinishTheDance", true, debug);
                }
            }

            if (Player.HasStatus(true, StatusID.TechnicalStep))
            {
                var shouldFinish = CompletedSteps == 4 &&
                                   ((HoldTechForTargets && AreDanceTargetsInRange) || !HoldTechForTargets);
                var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.TechnicalStep);

                debug["Technical: Steps Completed"] = CompletedSteps + "/4";
                debug["Technical: Should Finish (steps complete)"] = shouldFinish.ToString();
                debug["Technical: About To Time Out"] = aboutToTimeOut.ToString();

                if (shouldFinish || aboutToTimeOut)
                {
                    debug["Technical: Attempting Finish"] = "True";
                    var result = QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true);
                    debug["Technical: QuadrupleTechnicalFinishPvE.CanUse Result"] = result.ToString();
                    if (result)
                        return LogMethodResult("TryFinishTheDance", true, debug);
                }
            }
        }

        debug["Fallback"] = "Executing dance steps";
        var stepResult = ExecuteStepGCD(out act);
        debug["ExecuteStepGCD Result"] = stepResult.ToString();

        return LogMethodResult("TryFinishTheDance", stepResult, debug);
    }

    private bool TryUseStandardStep(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Is Dancing", IsDancing),
            ("Hold Standard For Targets", HoldStandardForTargets),
            ("Are Dance Targets In Range", AreDanceTargetsInRange),
            ("Last Dance Ready", Player.HasStatus(true, StatusID.LastDanceReady))
        );

        if (IsDancing || (HoldStandardForTargets && !AreDanceTargetsInRange) ||
            Player.HasStatus(true, StatusID.LastDanceReady))
        {
            var exitReason = IsDancing ? "Is Dancing" :
                HoldStandardForTargets && !AreDanceTargetsInRange ? "Hold Standard for targets and no targets in range" :
                Player.HasStatus(true, StatusID.LastDanceReady) ? "Has Last Dance Ready" : "No valid reason";

            debug["Early Exit"] = exitReason;
            return LogMethodResult("TryUseStandardStep", SetActToNull(out act), debug);
        }

        var techStepWillBeReadySoon = TechnicalStepPvE.Cooldown.IsCoolingDown &&
                                      TechnicalStepPvE.Cooldown.WillHaveOneCharge(5) ||
                                      TechnicalStepPvE.Cooldown.HasOneCharge;
        debug["Tech Step Will Be Ready Soon"] = techStepWillBeReadySoon.ToString();

        var isStandardStepReady = StandardStepPvE.Cooldown.IsCoolingDown &&
                                  StandardStepPvE.Cooldown.WillHaveOneCharge(5);
        debug["Is Standard Step Ready"] = isStandardStepReady.ToString();

        if (isStandardStepReady)
        {
            var standardFinishRemaining = Player.StatusTime(true, StatusID.StandardFinish);
            debug["Standard Finish Remaining"] = standardFinishRemaining.ToString("F1") + " seconds";
            debug["Will End Soon"] = Player.WillStatusEnd(0, true, StatusID.StandardFinish).ToString();
        }

        if (techStepWillBeReadySoon || TechnicalStepPvE.Cooldown.HasOneCharge)
        {
            debug["Decision Reason"] = "Tech Step will be ready soon or has one charge";
            ShouldUseStandardStep = false;
        }
        else if (!Player.WillStatusEnd(0, true, StatusID.StandardFinish))
        {
            debug["Decision Reason"] = "Standard Finish not ending soon";
            ShouldUseStandardStep = true;
        }
        debug["Should Use Standard Step"] = ShouldUseStandardStep.ToString();

        var result = ShouldUseStandardStep ? StandardStepPvE.CanUse(out act) : SetActToNull(out act);
        if (ShouldUseStandardStep)
        {
            debug["StandardStepPvE.CanUse Result"] = result.ToString();
        }

        return LogMethodResult("TryUseStandardStep", result, debug);
    }

    private bool TryUseFinishingMove(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Has Finishing Move Ready", Player.HasStatus(true, StatusID.FinishingMoveReady)),
            ("Hold Standard For Targets", HoldStandardForTargets),
            ("Are Dance Targets In Range", AreDanceTargetsInRange),
            ("Has Last Dance Ready", Player.HasStatus(true, StatusID.LastDanceReady))
        );

        if (!Player.HasStatus(true, StatusID.FinishingMoveReady) ||
            (HoldStandardForTargets && !AreDanceTargetsInRange) ||
            Player.HasStatus(true, StatusID.LastDanceReady))
        {
            var exitReason = !Player.HasStatus(true, StatusID.FinishingMoveReady) ? "No Finishing Move Ready" :
                HoldStandardForTargets && !AreDanceTargetsInRange ? "Hold Standard for targets and no targets in range" :
                Player.HasStatus(true, StatusID.LastDanceReady) ? "Has Last Dance Ready" : "No valid reason";

            debug["Early Exit"] = exitReason;
            return LogMethodResult("TryUseFinishingMove", SetActToNull(out act), debug);
        }

        debug["Attempting Finishing Move"] = "True";
        var result = FinishingMovePvE.CanUse(out act);
        debug["FinishingMovePvE.CanUse Result"] = result.ToString();

        return LogMethodResult("TryUseFinishingMove", result, debug);
    }

    #endregion

    #region Burst Logic

    private bool TryUseTechGCD(out IAction? act, bool burst)
    {
        var debug = CreateDebugInfo(
            ("In Burst", burst),
            ("Is Dancing", IsDancing)
        );

        if (!burst || IsDancing)
        {
            var reason = !burst ? "Not in burst phase" : "Currently dancing";
            debug["Early Exit"] = reason;
            return LogMethodResult("TryUseTechGCD", SetActToNull(out act), debug);
        }

        // Try each method in priority order, logging attempts
        var currentAttempt = "TryUseDanceOfTheDawn";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseDanceOfTheDawn(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        currentAttempt = "TryUseTillana";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseTillana(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }


        currentAttempt = "TryUseLastDance";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseLastDance(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        currentAttempt = "TryUseFinishingMove";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseFinishingMove(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        currentAttempt = "TryUseStarfallDance";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseStarfallDance(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        currentAttempt = "TryUseSaberDance";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseSaberDance(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        currentAttempt = "TryUseFillerGCD";
        CurrentGCDEvaluation = "TechGCD: " + currentAttempt;
        if (TryUseFillerGCD(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseTechGCD", true, debug);
        }

        debug["Result"] = "No valid action found";
        return LogMethodResult("TryUseTechGCD", false, debug);
    }

    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Current Esprit", Esprit),
            ("Required Esprit", 50),
            ("Has Enough Esprit", Esprit >= 50)
        );

        if (Esprit < 50)
        {
            debug["Early Exit"] = "Not enough Esprit";
            return LogMethodResult("TryUseDanceOfTheDawn", SetActToNull(out act), debug);
        }

        var result = DanceOfTheDawnPvE.CanUse(out act);
        debug["DanceOfTheDawnPvE.CanUse Result"] = result.ToString();
        return LogMethodResult("TryUseDanceOfTheDawn", result, debug);
    }

    private bool TryUseTillana(out IAction? act)
    {
        var hasFlourishingFinish = Player.HasStatus(true, StatusID.FlourishingFinish);

        var debug = CreateDebugInfo(
            ("Has FlourishingFinish", hasFlourishingFinish)
        );

        if (!hasFlourishingFinish)
        {
            debug["Early Exit"] = "Missing FlourishingFinish status";
            return LogMethodResult("TryUseTillana", SetActToNull(out act), debug);
        }

        // Record status timers and conditions
        var flourishingFinishRemaining = Player.StatusTime(true, StatusID.FlourishingFinish);
        var tillanaEnding = Player.HasStatus(true, StatusID.FlourishingFinish) && Player.WillStatusEnd(3, true, StatusID.FlourishingFinish);
        var hasFinishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady);
        var finishingMoveWillBeReady = FinishingMovePvE.Cooldown.IsCoolingDown &&
                                     FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(2, 1);
        var inBurst = DanceDance;
        var burstEnding = inBurst && Player.WillStatusEnd(3, true, StatusID.TechnicalFinish, StatusID.Devilment);
        var finishingMoveCooling = FinishingMovePvE.Cooldown.IsCoolingDown;

        // Add more detailed info to debug
        debug["FlourishingFinish Remaining"] = flourishingFinishRemaining.ToString("F1") + "s";
        debug["Tillana Status Ending Soon (<3s)"] = tillanaEnding.ToString();
        debug["Has FinishingMoveReady"] = hasFinishingMoveReady.ToString();
        debug["Finishing Move Will Be Ready Soon"] = finishingMoveWillBeReady.ToString();
        debug["In Burst (DanceDance)"] = inBurst.ToString();
        debug["Burst Ending Soon (<3s)"] = burstEnding.ToString();
        debug["Current Esprit"] = Esprit.ToString();
        debug["Finishing Move Cooling Down"] = finishingMoveCooling.ToString();

        // Decision logic
        string decisionReason;
        bool shouldUse;

        if (Esprit <= 10 && hasFinishingMoveReady && !finishingMoveWillBeReady)
        {
            decisionReason = "Low Esprit with Finishing Move Ready";
            shouldUse = true;
        }
        else if (tillanaEnding)
        {
            decisionReason = "Flourishing Finish ending soon";
            shouldUse = true;
        }
        else if (burstEnding)
        {
            decisionReason = "Burst phase ending soon";
            shouldUse = true;
        }
        else if (!inBurst)
        {
            decisionReason = "Not in burst phase";
            shouldUse = true;
        }
        else if (finishingMoveCooling && Esprit < 30)
        {
            decisionReason = "FinishingMove on cooldown with low Esprit";
            shouldUse = true;
        }
        else
        {
            decisionReason = "Conditions not met for use";
            shouldUse = false;
        }

        debug["Decision Reason"] = decisionReason;
        debug["Should Use Tillana"] = shouldUse.ToString();

        // Final result
        var result = shouldUse ? TillanaPvE.CanUse(out act) : SetActToNull(out act);
        if (shouldUse)
        {
            debug["TillanaPvE.CanUse Result"] = result.ToString();
        }

        return LogMethodResult("TryUseTillana", result, debug);
    }

    private bool TryUseLastDance(out IAction? act)
    {
        var hasLastDanceReady = Player.HasStatus(true, StatusID.LastDanceReady);

        var debug = CreateDebugInfo(
            ("Has Last Dance Ready", hasLastDanceReady)
        );

        if (!hasLastDanceReady)
        {
            debug["Early Exit"] = "Missing Last Dance Ready";
            return LogMethodResult("TryUseLastDance", SetActToNull(out act), debug);
        }

        var techWillHaveCharge = TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(15) && !TillanaPvE.CanUse(out act) || ShouldHoldForTechnicalStep() || TechnicalStepPvE.CanUse(out _);
        var inBurst = DanceDance;
        var espritHighCondition = Esprit >= 60;
        var notEndingSoonCondition = !Player.WillStatusEnd(3.5f, true, StatusID.LastDanceReady);

        debug["Tech Will Have Charge (15s)"] = techWillHaveCharge.ToString();
        debug["In Dance Burst (DanceDance)"] = inBurst.ToString();
        debug["Esprit >= 60"] = espritHighCondition.ToString();
        debug["LastDanceReady Not Ending Soon"] = notEndingSoonCondition.ToString();

        string reason;
        if (techWillHaveCharge || (inBurst && espritHighCondition && notEndingSoonCondition))
        {
            reason = techWillHaveCharge
                ? "Tech coming soon"
                : "In burst with high Esprit and buff not ending soon";
            ShouldUseLastDance = false;
        }
        else if (hasLastDanceReady && StandardStepPvE.Cooldown.IsCoolingDown && (!StandardStepPvE.Cooldown.WillHaveOneCharge(5.5f) || notEndingSoonCondition || inBurst && Esprit < 50))
        {
            reason = "Has LastDanceReady and conditions favorable";
            ShouldUseLastDance = true;
        }
        else
        {
            reason = "Default case - no change";
        }

        debug["Decision Reason"] = reason;
        debug["ShouldUseLastDance Value"] = ShouldUseLastDance.ToString();

        // Record final result
        var result = ShouldUseLastDance ? LastDancePvE.CanUse(out act) : SetActToNull(out act);
        if (ShouldUseLastDance)
        {
            debug["LastDancePvE.CanUse Result"] = result.ToString();
        }

        return LogMethodResult("TryUseLastDance", result, debug);
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        var hasStarfallStatus = Player.HasStatus(true, StatusID.FlourishingStarfall);

        var debug = CreateDebugInfo(
            ("Has FlourishingStarfall", hasStarfallStatus)
        );

        if (!hasStarfallStatus)
        {
            debug["Early Exit"] = "Missing Flourishing Starfall status";
            return LogMethodResult("TryUseStarfallDance", SetActToNull(out act), debug);
        }

        // Record remaining time on the buff
        var remainingTime = Player.StatusTime(true, StatusID.FlourishingStarfall);
        var willEndSoon = Player.HasStatus(true, StatusID.FlourishingStarfall) && Player.WillStatusEnd(7, true, StatusID.FlourishingStarfall);
        var hasLowEsprit = Esprit < 80;

        debug["FlourishingStarfall Remaining"] = remainingTime.ToString("F1") + "s";
        debug["Will End Soon (<7s)"] = willEndSoon.ToString();
        debug["Current Esprit"] = Esprit.ToString();
        debug["Esprit < 80"] = hasLowEsprit.ToString();

        // Decision logic
        string decisionReason;
        bool shouldUse;

        if (willEndSoon)
        {
            decisionReason = "Status ending soon";
            shouldUse = true;
        }
        else if (hasLowEsprit)
        {
            decisionReason = "Low Esprit (<80)";
            shouldUse = true;
        }
        else
        {
            decisionReason = "Saving for later use";
            shouldUse = false;
        }

        debug["Decision Reason"] = decisionReason;
        debug["Should Use Starfall Dance"] = shouldUse.ToString();

        // Final result
        var result = shouldUse ? StarfallDancePvE.CanUse(out act) : SetActToNull(out act);
        if (shouldUse)
        {
            debug["StarfallDancePvE.CanUse Result"] = result.ToString();
        }

        return LogMethodResult("TryUseStarfallDance", result, debug);
    }
    #endregion

    #region GCD Skills
    private bool TryUseBasicGCD(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Should Hold For Technical Step", ShouldHoldForTechnicalStep())
        );

        if (ShouldHoldForTechnicalStep())
        {
            debug["Action"] = "Using base.GeneralGCD - holding for Technical Step";
            var result = base.GeneralGCD(out act);
            debug["base.GeneralGCD Result"] = result.ToString();
            return LogMethodResult("TryUseBasicGCD", result, debug);
        }

        // Try each action in priority order
        if (BloodshowerPvE.CanUse(out act))
        {
            debug["Selected Action"] = "Bloodshower";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        if (RisingWindmillPvE.CanUse(out act))
        {
            debug["Selected Action"] = "Rising Windmill";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        if (FountainfallPvE.CanUse(out act))
        {
            debug["Selected Action"] = "Fountainfall";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        if (ReverseCascadePvE.CanUse(out act))
        {
            debug["Selected Action"] = "Reverse Cascade";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        if (FountainPvE.CanUse(out act))
        {
            debug["Selected Action"] = "Fountain";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        if (CascadePvE.CanUse(out act))
        {
            debug["Selected Action"] = "Cascade";
            return LogMethodResult("TryUseBasicGCD", true, debug);
        }

        debug["Result"] = "No basic GCD action available";
        return LogMethodResult("TryUseBasicGCD", SetActToNull(out act), debug);
    }

    private bool FeatherGCDHelper(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Current Feathers", Feathers),
            ("Feathers Threshold", 3),
            ("Esprit", Esprit),
            ("In Burst Phase (DanceDance)", DanceDance)
        );

        if (Feathers <= 3)
        {
            debug["Early Exit"] = "Not enough Feathers (≤3)";
            return LogMethodResult("FeatherGCDHelper", SetActToNull(out act), debug);
        }

        var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                             Player.HasStatus(true, StatusID.SilkenSymmetry);
        var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) ||
                                  Player.HasStatus(true, StatusID.FlourishingSymmetry);

        debug["Has Silken Procs"] = hasSilkenProcs.ToString();
        debug["Has Flourishing Procs"] = hasFlourishingProcs.ToString();

        if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < 50 && !DanceDance)
        {
            debug["Decision"] = "Use Fountain/Cascade (High feathers, flourishing proc, low esprit, outside burst)";

            if (FountainPvE.CanUse(out act))
            {
                debug["Selected Action"] = "Fountain";
                return LogMethodResult("FeatherGCDHelper", true, debug);
            }

            if (CascadePvE.CanUse(out act))
            {
                debug["Selected Action"] = "Cascade";
                return LogMethodResult("FeatherGCDHelper", true, debug);
            }
        }

        if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > 50 && !DanceDance)
        {
            debug["Decision"] = "Use Saber Dance (High feathers, has procs, high esprit, outside burst)";
            var result = SaberDancePvE.CanUse(out act);
            debug["SaberDancePvE.CanUse Result"] = result.ToString();
            return LogMethodResult("FeatherGCDHelper", result, debug);
        }

        debug["Decision"] = "No condition met for using feathers";
        return LogMethodResult("FeatherGCDHelper", SetActToNull(out act), debug);
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("Current Esprit", Esprit),
            ("Esprit >= 50", Esprit >= 50),
            ("In Burst Phase", DanceDance)
        );

        if (Esprit < 50)
        {
            debug["Early Exit"] = "Insufficient Esprit (<50)";
            return LogMethodResult("TryUseSaberDance", SetActToNull(out act), debug);
        }

        // Log status checks
        var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry,
                                      StatusID.FlourishingFlow, StatusID.FlourishingSymmetry) || CascadePvE.CanUse(out _);
        debug["Has Any Procs"] = hasProcs.ToString();
        if (hasProcs)
        {
            debug["Silken Flow"] = Player.HasStatus(true, StatusID.SilkenFlow).ToString();
            debug["Silken Symmetry"] = Player.HasStatus(true, StatusID.SilkenSymmetry).ToString();
            debug["Flourishing Flow"] = Player.HasStatus(true, StatusID.FlourishingFlow).ToString();
            debug["Flourishing Symmetry"] = Player.HasStatus(true, StatusID.FlourishingSymmetry).ToString();
        }

        var shouldHoldForTech = ShouldHoldForTechnicalStep();
        debug["Should Hold For Technical Step"] = shouldHoldForTech.ToString();

        var techStepSoon = TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(3, 0.5f);
        debug["Tech Step Coming Soon"] = techStepSoon.ToString();

        // Calculate burst condition
        var espritOutOfBurst = Esprit >= 50 && !shouldHoldForTech && !hasProcs && techStepSoon;
        debug["Esprit Out Of Burst Condition Met"] = espritOutOfBurst.ToString();

        // Check for priority abilities
        var hasLastDanceReady = Player.HasStatus(true, StatusID.LastDanceReady);
        debug["Has Last Dance Ready"] = hasLastDanceReady.ToString();
        var lastDanceEnding = Player.WillStatusEnd(3.5f, true, StatusID.LastDanceReady);
        debug["Last Dance Ready Ending Soon"] = lastDanceEnding.ToString();
        var hasStarfall = Player.HasStatus(true, StatusID.FlourishingStarfall);
        var starfallEnding = Player.HasStatus(true, StatusID.FlourishingStarfall) && Player.WillStatusEnd(3.5f, true, StatusID.FlourishingStarfall);
        debug["Flourishing Starfall Ending Soon"] = starfallEnding.ToString();
        var finishingMoveReady = Player.HasStatus(true, StatusID.FinishingMoveReady) && FinishingMovePvE.Cooldown.WillHaveOneCharge(3.5f);
        debug["Finishing Move Ready Soon"] = finishingMoveReady.ToString();

        // Get initial result
        var saberDanceCanUse = SaberDancePvE.CanUse(out act);
        debug["SaberDancePvE.CanUse Result"] = saberDanceCanUse.ToString();

        // Decision logic
        string decisionReason;
        bool shouldUse;

        if (!saberDanceCanUse)
        {
            return LogMethodResult("TryUseSaberDance", false, debug);
        }

        switch (DanceDance)
        {
            case true when hasLastDanceReady && lastDanceEnding || hasStarfall && starfallEnding || finishingMoveReady:
                decisionReason = "In burst but saving for higher priority abilities";
                shouldUse = false;
                act = null;
                break;
            case true when Esprit >= 50 && (hasLastDanceReady && !lastDanceEnding || hasStarfall && !starfallEnding || !finishingMoveReady):
                decisionReason = "In burst with sufficient Esprit && no priority abilities";
                shouldUse = true;
                break;
            case false when espritOutOfBurst:
                decisionReason = "Not in burst, Tech Step coming soon";
                shouldUse = true;
                break;
            case false when Esprit >= 70:
                decisionReason = "Not in burst, high Esprit";
                shouldUse = true;
                break;
            default:
                decisionReason = "No condition matched";
                shouldUse = false;
                act = null;
                break;
        }

        debug["Decision Reason"] = decisionReason;
        debug["Should Use Saber Dance"] = shouldUse.ToString();

        return LogMethodResult("TryUseSaberDance", shouldUse, debug);
    }

    private bool TryProcHelper(out IAction? act)
    {
        var debug = CreateDebugInfo(
            ("In Burst Phase", DanceDance)
        );

        // Check status for each important proc
        var hasStarfall = Player.HasStatus(true, StatusID.FlourishingStarfall);
        debug["Has Flourishing Starfall"] = hasStarfall.ToString();

        var hasLastDanceReady = Player.HasStatus(true, StatusID.LastDanceReady);
        debug["Has Last Dance Ready"] = hasLastDanceReady.ToString();

        // Check if any proc is ending soon
        var starfallEnding = Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall);
        debug["Flourishing Starfall Ending Soon"] = starfallEnding.ToString();

        var lastDanceEnding = Player.WillStatusEnd(5, true, StatusID.LastDanceReady);
        debug["Last Dance Ready Ending Soon"] = lastDanceEnding.ToString();

        // Decision logic
        string decisionReason;
        bool result;

        if (DanceDance && hasStarfall && starfallEnding)
        {
            decisionReason = "In burst and Flourishing Starfall ending soon";
            result = TryUseStarfallDance(out act);
            debug["TryUseStarfallDance Result"] = result.ToString();
        }
        else if (hasLastDanceReady && lastDanceEnding)
        {
            decisionReason = "Last Dance Ready ending soon";
            result = TryUseLastDance(out act);
            debug["TryUseLastDance Result"] = result.ToString();
        }
        else
        {
            decisionReason = "No procs ending soon";
            result = SetActToNull(out act);
        }

        debug["Decision Reason"] = decisionReason;
        return LogMethodResult("ProcHelper", result, debug);
    }

    private bool TryUseFillerGCD(out IAction? act)
    {
        var debug = CreateDebugInfo();

        // Try each method in priority order, logging attempts
        var currentAttempt = "FeatherGCDHelper";
        CurrentGCDEvaluation = "Filler GCD: " + currentAttempt;
        debug["Attempting"] = currentAttempt;

        currentAttempt = "TryUseFeatherGCD";
        if (FeatherGCDHelper(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseFillerGCD", true, debug);
        }

        currentAttempt = "TryUseLastDance";
        CurrentGCDEvaluation = "Filler GCD: " + currentAttempt;
        debug["Attempting"] = currentAttempt;
        if (TryUseLastDance(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseFillerGCD", true, debug);
        }

        currentAttempt = "TryUseSaberDance";
        CurrentGCDEvaluation = "Filler GCD: " + currentAttempt;
        debug["Attempting"] = currentAttempt;
        if (TryUseSaberDance(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseFillerGCD", true, debug);
        }

        currentAttempt = "TryUseBasicGCD";
        CurrentGCDEvaluation = "Filler GCD: " + currentAttempt;
        debug["Attempting"] = currentAttempt;

        if (TryUseBasicGCD(out act))
        {
            debug["Successful Method"] = currentAttempt;
            return LogMethodResult("TryUseFillerGCD", true, debug);
        }

        debug["Result"] = "No valid filler action found";
        return LogMethodResult("TryUseFillerGCD", false, debug);
    }

    #endregion

    #region OGCD Abilities

    /// <summary>
    ///     Determines whether the Devilment action can be used after the Technical Finish status is active.
    /// </summary>
    /// <param name="act">The action to be performed if Devilment can be used.</param>
    /// <returns>
    ///     <c>true</c> if the Devilment action can be used; otherwise, <c>false</c>.
    /// </returns>
    private bool TryUseDevilment(out IAction? act)
    {
        if ((Player.HasStatus(true, StatusID.TechnicalFinish) ||
             IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE)) && DevilmentPvE.CanUse(out act))
            return true;

        return SetActToNull(out act);
    }

    /// <summary>
    ///     Handles the logic for using the Flourish action.
    /// </summary>
    /// <param name="act">The action to be performed, if any.</param>
    /// <returns>True if the Flourish action was performed; otherwise, false.</returns>
    private bool TryUseFlourish(out IAction? act)
    {
        var debug = new Dictionary<string, string>
        {
            // Log initial conditions
            ["In Combat"] = InCombat.ToString(),
            ["Has Threefold Fan Dance"] = Player.HasStatus(true, StatusID.ThreefoldFanDance).ToString(),
            ["Should Use Tech Step"] = ShouldUseTechStep.ToString()
        };

        // Check conditions for early exit
        if (!InCombat || Player.HasStatus(true, StatusID.ThreefoldFanDance) || !ShouldUseTechStep)
        {
            debug["Early Exit"] = "Not in combat, has Threefold Fan Dance, or Tech Step not enabled";
            return LogMethodResult("TryUseFlourish", SetActToNull(out act), debug);
        }

        // Log burst phase and cooldown conditions
        debug["In Burst Phase (DanceDance)"] = DanceDance.ToString();
        debug["Tech Step Cooldown Elapsed after 67"] = TechnicalStepPvE.Cooldown.ElapsedAfter(67).ToString();
        debug["Tech Step Cooldown Will Have One Charge (30s)"] = TechnicalStepPvE.Cooldown.WillHaveOneCharge(30).ToString();

        // Decision logic
        ShouldUseFlourish = DanceDance || TechnicalStepPvE.Cooldown.ElapsedAfter(67) && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(30);
        debug["Should Use Flourish"] = ShouldUseFlourish.ToString();

        // Final result
        var result = ShouldUseFlourish ? FlourishPvE.CanUse(out act) : SetActToNull(out act);
        if (ShouldUseFlourish)
        {
            debug["FlourishPvE.CanUse Result"] = result.ToString();
        }

        return LogMethodResult("TryUseFlourish", result, debug);
    }

    /// <summary>
    /// Determines whether feathers should be used based on the next GCD action and current player status.
    /// </summary>
    /// <param name="act"> The action to be performed, if any.</param>
    /// <returns>True if a feather action was performed; otherwise, false.</returns>
    private bool TryUseFeathers(out IAction? act)
    {
        var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow) ||
                       Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                       Player.HasStatus(true, StatusID.FlourishingFlow) ||
                       Player.HasStatus(true, StatusID.FlourishingSymmetry);
        var hasDevilment = Player.HasStatus(true, StatusID.Devilment);
        var hasEnoughFeathers = Feathers > 3;
        var hasThreefoldFanDance = Player.HasStatus(true, StatusID.ThreefoldFanDance);
        var hasFourfoldFanDance = Player.HasStatus(true, StatusID.FourfoldFanDance);

        if (hasThreefoldFanDance && WeaponRemain > DefaultAnimationLock) return FanDanceIiiPvE.CanUse(out act);

        if (hasFourfoldFanDance && WeaponRemain > DefaultAnimationLock) return FanDanceIvPvE.CanUse(out act);

        if ((hasDevilment || (hasEnoughFeathers && hasProcs && !ShouldHoldForTechnicalStep()) || IsMedicated) &&
            !hasThreefoldFanDance && WeaponRemain > DefaultAnimationLock)
            return FanDanceIiPvE.CanUse(out act) ||
                   FanDancePvE.CanUse(out act);

        return SetActToNull(out act);
    }

    private bool TryUsePots(out IAction? act)
    {
        if (FirstPot(out act) || SecondPot(out act) || ThirdPot(out act)) return true;

        return SetActToNull(out act);
    }

    private bool FirstPot(out IAction? act)
    {
        switch (EnableFirstPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime - FirstPotionTime * 60 >= 0 && CombatTime < SecondPotionTime * 60 &&
                           CombatTime < ThirdPotionTime * 60 && DateTime.Now - _lastPotionUsed > TimeSpan.FromSeconds(270):
                _lastPotionUsed = DateTime.Now;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }

    private bool SecondPot(out IAction? act)
    {
        switch (EnableSecondPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime - SecondPotionTime * 60 >= 0 && SecondPotionTime * 60 > FirstPotionTime * 60 &&
                           SecondPotionTime * 60 < ThirdPotionTime * 60 && DateTime.Now - _lastPotionUsed > TimeSpan.FromSeconds(270):
                _lastPotionUsed = DateTime.Now;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }

    private bool ThirdPot(out IAction? act)
    {
        switch (EnableThirdPotion)
        {
            case false:
                return SetActToNull(out act);
            case true when CombatTime - ThirdPotionTime * 60 >= 0 && ThirdPotionTime * 60 > FirstPotionTime * 60 &&
                           ThirdPotionTime * 60 > SecondPotionTime * 60 && DateTime.Now - _lastPotionUsed > TimeSpan.FromSeconds(270):
                _lastPotionUsed = DateTime.Now;
                return UseBurstMedicine(out act);
            default:
                return SetActToNull(out act);
        }
    }


    private static bool SetActToNull(out IAction? act)
    {
        act = null;
        return false;
    }

    #endregion

    #endregion

    #region Status Window Override


    private void DrawRotationStatus()
    {
        var text = "Rotation: " + Name;
        var textSize = ImGui.CalcTextSize(text).X;
        DisplayStatusHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, text);
            DisplayStatusHelper.HoveredTooltip(Description);
        }, ImGui.GetWindowWidth(), textSize);
    }

    private void DrawCombatStatus()
    {
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawCombatStatusText();
        }

        ImGui.Spacing();
        if (ImGui.CollapsingHeader("GCD Method Debug Info", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawGCDMethodDebugTable();
        }
        ImGui.EndGroup();
    }

    private void DrawGCDMethodDebugTable()
    {
        try
        {
            IsDebugTableVisible = true;

            ImGui.Text("Debug Controls:");
            var condenseEntries = _debugSettings?.CondenseEntries ?? false;
            if (ImGui.Checkbox("Condense Entries", ref condenseEntries) && _debugSettings != null)
                _debugSettings.CondenseEntries = condenseEntries;

            ImGui.SameLine();
            if (ImGui.Button("Clear All"))
                GCDMethodDebugInfo.Clear();

            ImGui.TextColored(ImGuiColors.DalamudYellow, $"Current GCD Method: {CurrentGCDEvaluation}");
            ImGui.Text($"Debug Entries ({GCDMethodDebugInfo.Count}):");

            // Use TreeNodes instead of table rows
            foreach (var entry in GCDMethodDebugInfo.Take(_debugSettings?.MaxDebugEntries ?? 50))
            {
                if (ImGui.TreeNode($"{entry.Key}##debug"))
                {
                    foreach (var (key, value) in entry.Value)
                    {
                        ImGui.BulletText($"{key}: {value}");
                    }
                    ImGui.TreePop();
                }
            }
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error: {ex.Message}");
        }
    }

    private void DrawCombatStatusText()
    {
        try
        {
            // Use columns instead of tables which are causing crashes
            ImGui.Columns(2, "CombatStatusColumns", false);

            // Column headers
            ImGui.Text("Status"); ImGui.NextColumn();
            ImGui.Text("Value"); ImGui.NextColumn();
            ImGui.Separator();

            // Row for Current GCD Evaluation
            ImGui.Text("Current GCD Evaluation:"); ImGui.NextColumn();
            ImGui.TextWrapped(CurrentGCDEvaluation); ImGui.NextColumn();

            // Row for Should Use Tech Step
            ImGui.Text("Should Use Tech Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseTechStep.ToString()); ImGui.NextColumn();

            // Row for Should Use Flourish
            ImGui.Text("Should Use Flourish?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseFlourish.ToString()); ImGui.NextColumn();

            // Additional rows...
            ImGui.Text("Should Use Standard Step?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseStandardStep.ToString()); ImGui.NextColumn();

            ImGui.Text("Should Use Last Dance?"); ImGui.NextColumn();
            ImGui.Text(ShouldUseLastDance.ToString()); ImGui.NextColumn();

            ImGui.Text("In Burst:"); ImGui.NextColumn();
            ImGui.Text(DanceDance.ToString()); ImGui.NextColumn();

            // Reset columns
            ImGui.Columns(1);
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying combat status");
        }
    }

    public override void DisplayStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinDNC Status", true,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);

            DrawRotationStatus();
            DrawCombatStatus();

            DisplayStatusHelper.EndPaddedChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying status: {ex.Message}");
        }
    }

    #endregion
}
