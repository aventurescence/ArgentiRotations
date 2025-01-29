namespace ArgentiRotations.Ranged
{
    public sealed partial class ChurinDNC : ICustomRotation
    {
        #region FRU Properties

        private const string Fatebreaker = "Fatebreaker";
        private const string Usurper = "Usurper of Frost";
        private const string Gaia = "Oracle of Darkness";
        private const string Pandora = "Pandora";
        private const string Adds = "Ice Veil";
        private static readonly string[] Lesbians = { Usurper, Gaia };

        /// <summary>
        /// Enum representing the different bosses in the FRU.
        /// </summary>
        public enum FRUBoss
        {
            None,
            Fatebreaker,
            Usurper,
            Adds,
            Gaia,
            Lesbians,
            Pandora
        }

        /// <summary>
        /// Enum representing the different downtimes in the FRU.
        /// </summary>
        public enum Downtime
        {
            None,
            UtopianSky,
            DiamondDust,
            LightRampant,
            GaiaTransition,
            UltimateRelativity,
            OracleTargetable,
            CrystalizeTime,
        }

        private static Downtime currentDowntime = Downtime.None;
        private static FRUBoss currentBoss = FRUBoss.None;
        private static float CurrentDowntimeStart { get; set; }
        private static float CurrentDowntimeEnd { get; set; }
        private static float CurrentPhaseEnd { get; set; }

        /// <summary>
        /// Checks if the specified boss is dead.
        /// </summary>
        /// <param name="boss">The boss to check.</param>
        /// <returns>True if the boss is dead; otherwise, false.</returns>
        private static bool IsBossDead(FRUBoss boss)
        {
            return PhaseTimings.GetBossHealthRatio(boss.ToString()) <= 1;
        }

        #endregion

        #region FRU Timers

        /// <summary>
        /// Class representing the start and end timings of a downtime.
        /// </summary>
        public class DowntimeTimings
        {
            public float Start { get; set; }
            public float End { get; set; }

            public DowntimeTimings(float start, float end)
            {
                Start = start;
                End = end;
            }

            public static void InitializeDowntimeTimers(FRUBoss boss)
            {
                if (PhaseTimings.phaseTimers.TryGetValue(boss, out PhaseTimings? phaseTimings))
                {
                    float phaseStartTime = phaseTimings.Start;
                    downtimeTimers[Downtime.UtopianSky] = new DowntimeTimings(phaseStartTime + 34.8f, phaseStartTime + 80f);
                    downtimeTimers[Downtime.DiamondDust] = new DowntimeTimings(phaseStartTime + 35.1f, phaseStartTime + 72f);
                    downtimeTimers[Downtime.LightRampant] = new DowntimeTimings(phaseStartTime + 131.7f, phaseStartTime + 160.7f);
                    downtimeTimers[Downtime.GaiaTransition] = new DowntimeTimings(phaseStartTime + 41.2f, phaseStartTime + 66.8f);
                    downtimeTimers[Downtime.UltimateRelativity] = new DowntimeTimings(phaseStartTime + 18.3f, phaseStartTime + 62.2f);
                    downtimeTimers[Downtime.OracleTargetable] = new DowntimeTimings(phaseStartTime + 25.4f, phaseStartTime + 25.5f);
                    downtimeTimers[Downtime.CrystalizeTime] = new DowntimeTimings(phaseStartTime + 98.5f, phaseStartTime + 148.2f);
                }
                // Add a default entry for Downtime.None
                downtimeTimers[Downtime.None] = new DowntimeTimings(0f, 0f);
            }

            internal static readonly Dictionary<Downtime, DowntimeTimings> downtimeTimers = new();

            public static DowntimeTimings GetDowntimeTimings(Downtime downtime)
            {
                return downtimeTimers.TryGetValue(downtime, out DowntimeTimings? value) ? value : downtimeTimers[Downtime.None];
            }

            public static bool IsDowntime(FRUBoss boss, Downtime downtime)
            {
                if (downtime == Downtime.None)
                {
                    currentDowntime = Downtime.None;
                    CurrentDowntimeStart = 0f;
                    CurrentDowntimeEnd = 0f;
                    return false;
                }

                if (currentBoss == boss)
                {
                    DowntimeTimings timings = GetDowntimeTimings(downtime);
                    if (CombatElapsedLess(timings.End) && !CombatElapsedLess(timings.Start))
                    {
                        currentDowntime = downtime;
                        CurrentDowntimeStart = timings.Start;
                        CurrentDowntimeEnd = timings.End;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Class representing the start and end timings of a phase.
        /// </summary>
        public class PhaseTimings
        {
            public float Start { get; set; }
            public float End { get; set; }

            public PhaseTimings(float start, float end)
            {
                Start = start;
                End = end;
            }

            internal static readonly Dictionary<FRUBoss, PhaseTimings> phaseTimers = new();

            /// <summary>
            /// Initializes the phase timers and returns the phase durations for each phase.
            /// </summary>
            /// <returns>A dictionary containing the phase durations for each boss.</returns>
            public static Dictionary<FRUBoss, (string[] phaseName, float duration, FRUBoss? requiredPreviousBoss, float healthThreshold)> InitializeAndGetPhaseDurations(FRUBoss currentBoss)
            {
                var phaseDurations = new Dictionary<FRUBoss, (string[] phaseName, float duration, FRUBoss? requiredPreviousBoss, float healthThreshold)>
                {
                    { FRUBoss.Fatebreaker, (new[] { Fatebreaker }, 160.1f, null, 30) },
                    { FRUBoss.Usurper, (new[] { Usurper }, 185f, FRUBoss.Fatebreaker, 20) },
                    { FRUBoss.Adds, (new[] { Adds }, 41.2f, FRUBoss.Usurper, 1) },
                    { FRUBoss.Gaia, (new[] { Gaia }, 157.9f, FRUBoss.Adds, 20) },
                    { FRUBoss.Lesbians, (Lesbians, 175.9f, FRUBoss.Gaia, 25) },
                    { FRUBoss.Pandora, (new[] { Pandora }, 271.9f, FRUBoss.Lesbians, 1) }
                };

                if (phaseDurations.TryGetValue(currentBoss, out var phaseInfo))
                {
                    phaseTimers[currentBoss] = new PhaseTimings(CombatTime, CombatTime + phaseInfo.duration);
                    DowntimeTimings.InitializeDowntimeTimers(currentBoss);
                }

                return phaseDurations;
            }

            /// <summary>
            /// Handles the phase transition logic for the specified phase.
            /// </summary>
            /// <param name="phase">The phase to handle the transition for.</param>
            public static void HandlePhaseTransition(KeyValuePair<FRUBoss, (string[] phaseName, float duration, FRUBoss? requiredPreviousBoss, float healthThreshold)> phase)
            {
                if (ShouldEndCurrentPhaseEarly())
                {
                    CurrentPhaseEnd = CombatTime;
                }

                if (CombatTime >= CurrentPhaseEnd)
                {
                    currentBoss = phase.Key;
                    CurrentPhaseEnd = CombatTime + phase.Value.duration;
                    DowntimeTimings.InitializeDowntimeTimers(currentBoss);
                }
            }

            /// <summary>
            /// Checks if the phase transition is valid based on the current hostile targets and required previous boss.
            /// </summary>
            /// <param name="phase">The phase to check the transition for.</param>
            /// <param name="hostileTargetNames">The list of hostile target names.</param>
            /// <returns>True if the phase transition is valid; otherwise, false.</returns>
            public static bool IsPhaseTransitionValid(KeyValuePair<FRUBoss, (string[] phaseName, float duration, FRUBoss? requiredPreviousBoss, float healthThreshold)> phase, List<string> hostileTargetNames)
            {
                bool doesPhaseNameContain = phase.Key == FRUBoss.Lesbians
                    ? hostileTargetNames.Contains(Usurper) // Check if Usurper of Frost is present
                    : phase.Value.phaseName.All(hostileTargetNames.Contains);
                bool isPreviousBossRequired = phase.Value.requiredPreviousBoss == null || currentBoss == phase.Value.requiredPreviousBoss;
                return doesPhaseNameContain && isPreviousBossRequired;
            }

            /// <summary>
            /// Checks if the current phase should end early based on the boss health.
            /// </summary>
            /// <returns>True if the current phase should end early; otherwise, false.</returns>
            public static bool ShouldEndCurrentPhaseEarly()
            {
                if (currentBoss == FRUBoss.Lesbians)
                {
                    return AreBothBossesDead("Oracle of Darkness", "Usurper of Frost");
                }
                return IsBossDead(currentBoss);
            }

            /// <summary>
            /// Checks if both specified bosses are dead.
            /// </summary>
            /// <param name="bossName1">The name of the first boss.</param>
            /// <param name="bossName2">The name of the second boss.</param>
            /// <returns>True if both bosses are dead; otherwise, false.</returns>
            public static bool AreBothBossesDead(string bossName1, string bossName2)
            {
                return GetBossHealthRatio(bossName1) <= 1 && GetBossHealthRatio(bossName2) <= 1;
            }

            /// <summary>
            /// Gets the health ratio of the specified boss.
            /// </summary>
            /// <param name="bossName">The name of the boss.</param>
            /// <returns>The health ratio of the boss.</returns>
            public static float GetBossHealthRatio(string bossName)
            {
                var boss = AllHostileTargets.FirstOrDefault(target => target.Name.ToString() == bossName);
                return boss?.GetHealthRatio() ?? 999f;
            }
        }

        /// <summary>
        /// Checks and sets the current FRU phase based on the combat state and hostile targets.
        /// </summary>
        /// <returns>The current boss phase.</returns>
        public static FRUBoss CheckAndSetFRUPhase()
        {
            if (IsInFRU && InCombat)
            {
                // Initialize phase timers and get phase durations
                var phaseDurations = PhaseTimings.InitializeAndGetPhaseDurations(currentBoss);
                var hostileTargetNames = AllHostileTargets.Select(obj => obj.Name.ToString()).ToList();
                
                // Update action flags based on current boss, downtime, and combat state
                UpdateActionFlags();

                // Handle phase transitions
                foreach (var phase in phaseDurations)
                {
                    if (PhaseTimings.IsPhaseTransitionValid(phase, hostileTargetNames))
                    {
                        PhaseTimings.HandlePhaseTransition(phase);
                    }
                }
            }
            else
            {
                // Reset timers if not in combat
                ResetFRUTimers();
            }
            return currentBoss;
        }

        /// <summary>
        /// Resets the FRU phase and downtime timers.
        /// </summary>
        private static void ResetFRUTimers()
        {
            currentBoss = FRUBoss.None;
            currentDowntime = Downtime.None;
            CurrentDowntimeStart = 0f;
            CurrentDowntimeEnd = 0f;
            CurrentPhaseEnd = 0f;

            // Clear phase timers and downtime timers
            PhaseTimings.phaseTimers.Clear();
            DowntimeTimings.downtimeTimers.Clear();
        }

        #endregion

        #region FRU Conditions

        /// <summary>
        /// Checks if the player has the "Spell in Waiting Return" status.
        /// </summary>
        public static bool HasSpellinWaitingReturn => Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);

        /// <summary>
        /// Checks if the player has the "Return" status.
        /// </summary>
        public static bool HasReturn => Player.HasStatus(false, StatusID.Return);

        /// <summary>
        /// Checks if the "Return" status is ending within 6 seconds.
        /// </summary>
        public static bool ReturnEnding => HasReturn && Player.WillStatusEnd(6, false, StatusID.Return);

        /// <summary>
        /// Checks if the player has the "Finishing Move Ready" status.
        /// </summary>
        public static bool HasFinishingMove => Player.HasStatus(true, StatusID.FinishingMoveReady);

        /// <summary>
        /// Indicates whether the finishing move should be removed.
        /// </summary>
        public static bool ShouldRemoveFinishingMove { get; set; } = false;

        #endregion

        #region FRU Methods

        /// <summary>
        /// Updates the action flags based on current boss, downtime, and combat state.
        /// </summary>
        private static void UpdateActionFlags()
        {
            switch (currentBoss)
            {
                case FRUBoss.Fatebreaker:
                    HandleFatebreaker();
                    break;
                case FRUBoss.Usurper:
                    HandleUsurper();
                    break;
                case FRUBoss.Adds:
                    HandleAdds();
                    break;
                case FRUBoss.Gaia:
                    HandleGaia();
                    break;
                case FRUBoss.Lesbians:
                    HandleLesbians();
                    break;
                case FRUBoss.Pandora:
                    HandlePandora();
                    break;
                default:
                    HandleNoDowntime();
                    break;
            }
        }

        /// <summary>
        /// Handles the logic for the Fatebreaker boss.
        /// Sets various flags based on the current downtime.
        /// </summary>
        private static void HandleFatebreaker()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Fatebreaker, Downtime.UtopianSky))
            {
                CustomRotationAg.Debug("Fatebreaker: UtopianSky");
                ShouldUseStandardStep = true;
                ShouldUseFlourish = true;
                ShouldFinishingMove = false;
                ShouldRemoveFinishingMove = HasFinishingMove;
            }
            else
            {
                CustomRotationAg.Debug("Fatebreaker: No downtime");
                HandleNoDowntime();
            }
        }

        /// <summary>
        /// Handles the logic for the Usurper boss.
        /// Calls specific downtime handlers based on the current downtime.
        /// </summary>
        private static void HandleUsurper()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Usurper, Downtime.DiamondDust))
            {
                HandleDiamondDustDowntime();
            }
            else if (DowntimeTimings.IsDowntime(FRUBoss.Usurper, Downtime.LightRampant))
            {
                HandleLightRampantDowntime();
            }
            else
            {
                CustomRotationAg.Debug("Usurper: No downtime");
                HandleNoDowntime();
            }
        }

        /// <summary>
        /// Handles the logic for the Diamond Dust downtime.
        /// Sets various flags based on the remaining downtime.
        /// </summary>
        private static void HandleDiamondDustDowntime()
        {
            CustomRotationAg.Debug("Usurper: DiamondDust");
            ShouldUseStandardStep = false;
            ShouldUseFlourish = false;
            ShouldFinishingMove = false;
            ShouldRemoveFinishingMove = HasFinishingMove;
            if (CurrentDowntimeEnd - CombatTime <= 15)
            {
                ShouldUseStandardStep = true;
            }
        }

        /// <summary>
        /// Handles the logic for the Light Rampant downtime.
        /// Sets various flags based on the remaining downtime and checks if Flourish can be used.
        /// </summary>
        private static void HandleLightRampantDowntime()
        {
            CustomRotationAg.Debug("Usurper: LightRampant");
            ShouldUseStandardStep = false;
            ShouldUseFlourish = false;
            ShouldFinishingMove = false;
            ShouldRemoveFinishingMove = false;

            var instance = new ChurinDNC();
            // Check if the current downtime is ending in 15 seconds and if Flourish can be used
            if (CurrentDowntimeEnd - CombatTime <= 15 && instance.FlourishPvE.CanUse(out _))
            {
                ShouldUseFlourish = true;
            }

            // Additional check if the current downtime is ending in more than 5 seconds
            if (CurrentDowntimeEnd - CombatTime > 5)
            {
                ShouldRemoveFinishingMove = true;
                ShouldUseStandardStep = true;
            }
            else
            {
                ShouldRemoveFinishingMove = false;
            }
        }

        /// <summary>
        /// Handles the logic for the Adds phase.
        /// Sets various flags based on the current downtime.
        /// </summary>
        private static void HandleAdds()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Adds, Downtime.GaiaTransition))
            {
                CustomRotationAg.Debug("Adds: GaiaTransition");
                ShouldUseStandardStep = CurrentDowntimeEnd - CombatTime <= 14;
            }
            else
            {
                CustomRotationAg.Debug("Adds: No downtime");
                ShouldUseStandardStep = false;
                ShouldUseTechStep = true;
                ShouldFinishingMove = true;

                if (TryUseTechnicalStep(out _))
                {
                    ShouldUseStandardStep = true;
                }

                HandleNoDowntime();
            }
        }

        /// <summary>
        /// Handles the logic for the Gaia boss.
        /// Sets various flags based on the current downtime.
        /// </summary>
        private static void HandleGaia()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Gaia, Downtime.UltimateRelativity))
            {
                CustomRotationAg.Debug("Gaia: UltimateRelativity");
                ShouldUseTechStep = ReturnEnding && !HasSpellinWaitingReturn;
            }
            else
            {
                CustomRotationAg.Debug("Gaia: No downtime");
                HandleNoDowntime();
            }
        }

        /// <summary>
        /// Handles the logic for the Lesbians phase.
        /// Sets various flags based on the current downtime.
        /// </summary>
        private static void HandleLesbians()
        {
            ShouldUseTechStep = false;

            if (DowntimeTimings.IsDowntime(FRUBoss.Lesbians, Downtime.OracleTargetable) && CurrentDowntimeStart - CombatTime <= 6)
            {
                CustomRotationAg.Debug("Lesbians: OracleTargetable");
                ShouldUseTechStep = true;
            }
            else if (DowntimeTimings.IsDowntime(FRUBoss.Lesbians, Downtime.CrystalizeTime) && CurrentDowntimeEnd - CombatTime <= 15)
            {
                CustomRotationAg.Debug("Lesbians: CrystalizeTime");
                ShouldUseStandardStep = true;
                ShouldUseTechStep = false;
            }
            else
            {
                CustomRotationAg.Debug("Lesbians: No downtime");
                HandleNoDowntime();
            }
        }
        /// <summary>
        /// Handles the logic for the Pandora boss.
        /// Sets various flags and calls HandleNoDowntime.
        /// </summary>
        private static void HandlePandora()
        {
            ShouldUseStandardStep = false;
            ShouldUseTechStep = true;
            ShouldFinishingMove = true;

            if (TryUseTechnicalStep(out _))
            {
                ShouldUseStandardStep = true;
            }
            HandleNoDowntime();
        }

        /// <summary>
        /// Handles the logic when there is no downtime.
        /// Sets various flags based on the remaining downtime and calls BossDying.
        /// </summary>
        private static void HandleNoDowntime()
        {
            if (CurrentDowntimeStart - CombatTime <= 4)
            {
                ShouldUseStandardStep = false;
                ShouldFinishingMove = true;
                ShouldRemoveFinishingMove = false;
            }
            else
            {
                ShouldUseStandardStep = true;
            }
        
            string[] bossNames = new[] { Fatebreaker, Usurper, Gaia, "Lesbians" };
            BossDying(bossNames);
        }

        /// <summary>
        /// Handles the logic for determining if a boss is dying based on health thresholds.
        /// Sets various flags based on the boss health ratios.
        /// </summary>
        /// <param name="bossNames">An array of boss names to check.</param>
        private static void BossDying(string[] bossNames)
        {
            var bossActions = new Dictionary<string, Action>
            {
                { Fatebreaker, () => { if (PhaseTimings.GetBossHealthRatio(Fatebreaker) <= 30f) { ShouldFinishingMove = false; ShouldUseFlourish = false; } } },
                { Usurper, () => { if (PhaseTimings.GetBossHealthRatio(Usurper) <= 25f) { ShouldUseStandardStep = true; } } },
                { Gaia, () => { if (PhaseTimings.GetBossHealthRatio(Gaia) <= 20f) { ShouldUseStandardStep = false; } } },
                { "Lesbians", () =>
                    {
                        // Ensure the Lesbians array is not null
                        if (Lesbians != null && Lesbians.Any(boss => PhaseTimings.GetBossHealthRatio(boss) <= 20f))
                        {
                            ShouldUseStandardStep = true;
                            ShouldUseTechStep = false;
                            ShouldUseFlourish = true;
                        }
                    }
                }
            };

            foreach (var bossName in bossNames)
            {
                if (bossActions.TryGetValue(bossName, out Action? value))
                {
                    value.Invoke();
                }
                else
                {
                    CustomRotationAg.Debug($"BossDying: No action found for boss {bossName}");
                }
            }
        }
        #endregion
    }
}