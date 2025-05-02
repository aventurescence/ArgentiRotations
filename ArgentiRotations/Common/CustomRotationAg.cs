using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Serilog;

namespace ArgentiRotations.Common;

internal unsafe class CustomRotationAg
{
    #region CountDown

    /// <summary>
    ///     The struct about countdown.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct Countdown
    {
        /// <summary>
        ///     Timer.
        /// </summary>
        [FieldOffset(0x28)] internal float Timer;

        /// <summary>
        ///     Is this action active.
        /// </summary>
        [FieldOffset(0x38)] internal byte Active;

        /// <summary>
        ///     Init.
        /// </summary>
        [FieldOffset(0x3C)] internal uint Initiator;

        /// <summary>
        ///     The instance about this struct.
        /// </summary>
        private static Countdown* Instance =>
            (Countdown*)Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(
                AgentId.CountDownSettingDialog);

        private static RandomDelay _delay = new(() => (0f, 1f));

        /// <summary>
        ///     TimeRemaining.
        /// </summary>
        internal static float TimeRemaining
        {
            get
            {
                var inst = Instance;
                return _delay.Delay(inst->Active != 0) ? inst->Timer : 0;
            }
        }

        /// <summary>
        ///     Is Countdown Active.
        /// </summary>
        internal static bool IsCountdownActive
        {
            get
            {
                var inst = Instance;
                return inst->Active != 0;
            }
        }
    }

    internal static bool CountDownActive => Countdown.IsCountdownActive;
    internal static float CountDownTime => Countdown.TimeRemaining;

    #endregion

    #region Openers

    private static int OpenerStep { get; set; }
    private static bool OpenerHasFinished { get; set; }
    private static bool OpenerHasFailed { get; set; }
    internal const float UniversalFailsafeThreshold = 5.0f;

    internal static bool OpenerTimeout { get; set; } =
        false; // TODO - make a method that when true, sends a debug log  and then sets the value back to false

    private static bool TestOpenerAvailable { get; set; } = false;
    private static bool OpenerAvailable { get; set; } = false;
    private static bool OpenerAvailableNoCountdown { get; set; } = false;
    private static bool OpenerAvailableSavage { get; set; } = false;

    private static bool OpenerAvailableUltimate { get; set; } = false;

    // Use a generic handler for determining if an opener is available
    internal static bool IsOpenerAvailable => OpenerAvailable || OpenerAvailableNoCountdown || OpenerAvailableSavage ||
                                              OpenerAvailableUltimate || TestOpenerAvailable;

    internal static bool TestStartOpener { get; set; } = false;
    private static bool StartOpener { get; set; }
    internal static bool StartOpenerNoCountdown { get; set; } = false;
    internal static bool StartOpenerSavage { get; set; } = false;
    internal static bool StartOpenerUltimate { get; set; } = false;

    internal static bool TestOpenerInProgress { get; set; } = false;
    private static bool OpenerInProgress { get; set; }
    private static bool OpenerInProgressNoCountdown { get; set; }
    internal static bool OpenerInProgressSavage { get; set; } = false;
    internal static bool OpenerInProgressUltimate { get; set; } = false;

    internal static void StateOfOpener()
    {
        if (StartOpener && !OpenerInProgress)
        {
            OpenerInProgress = true;
            StartOpener = false;
        }

        if (OpenerAvailableNoCountdown && !OpenerInProgressNoCountdown) OpenerInProgressNoCountdown = true;

        if (OpenerHasFinished || OpenerHasFailed) ResetOpenerProperties();
    }

    private static void ResetOpenerProperties()
    {
        OpenerInProgress = false;
        OpenerInProgressNoCountdown = false;
        OpenerStep = 0;
        OpenerHasFinished = false;
        OpenerHasFailed = false;
        Debug("Opener values have been reset.");
    }

    internal static bool OpenerController(bool lastAction, bool nextAction)
    {
        if (lastAction)
        {
            OpenerStep++;
            Debug($"Last action matched! Proceeding to step: {OpenerStep}");
            return false;
        }

        return nextAction;
    }

    #endregion

    #region Logging

    private const string ArgentiLog = "[Argenti Rotations]";

    /// <summary>
    ///     Sends a debug level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    private static void Debug(string message)
    {
        Log.Debug("{ArgentiLog} {Message}", ArgentiLog, message);
    }

    /// <summary>
    ///     Sends a warning level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    internal static void Warning(string message)
    {
        Log.Warning("{ArgentiLog} {Message}", ArgentiLog, message);
    }

    #endregion
}