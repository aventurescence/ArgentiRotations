using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ArgentiRotations.Ranged.common;
internal unsafe class BRDcustom
{
    #region Openers 
        internal static int OpenerStep { get; set; } = 0;
        internal static bool OpenerHasFinished { get; set; } = false;
        internal static bool OpenerHasFailed { get; set; } = false;
        internal const float universalFailsafeThreshold = 5.0f;

        internal static bool OpenerAvailable { get; set; } = false;
        internal static bool OpenerAvailableNoCountdown { get; set; } = false;
        // Use a generic handler for determining if an opener is available
        internal static bool IsOpenerAvailable => OpenerAvailable || OpenerAvailableNoCountdown;

        internal static bool StartOpener { get; set; } = false;
        internal static bool StartOpenerNoCountdown { get; set; } = false;

        internal static bool OpenerInProgress { get; set; } = false;
        internal static bool OpenerInProgressNoCountdown { get; set; } = false;

        internal static void StateOfOpener()
        {
            if (StartOpener && !OpenerInProgress)
            {
                OpenerInProgress = true;
                StartOpener = false;
            }

            if (OpenerAvailableNoCountdown && !OpenerInProgressNoCountdown)
            {
                OpenerInProgressNoCountdown = true;
            }

            if (OpenerHasFinished || OpenerHasFailed)
            {
                ResetOpenerProperties();
            }
        }

        internal static void ResetOpenerProperties()
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
    /// Sends a debug level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    internal static void Debug(string message) => Serilog.Log.Debug("{ArgentiLog} {Message}", ArgentiLog, message);

    /// <summary>
    /// Sends a warning level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    internal static void Warning(string message) => Serilog.Log.Warning("{ArgentiLog} {Message}", ArgentiLog, message);
    #endregion

    #region Custom Actions
 
    #endregion
}