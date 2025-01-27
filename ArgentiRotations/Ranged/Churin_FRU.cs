namespace ArgentiRotations.Ranged
{
    public sealed partial class ChurinDNC : ICustomRotation
    {
        #region FRU Properties
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

        #endregion

        #region FRU Timers

        /// <summary>
        /// Struct representing the start and end timings of a downtime.
        /// </summary>
        public struct DowntimeTimings
        {
            public float Start { get; set; }
            public float End { get; set; }

            public DowntimeTimings(float start, float end)
            {
                Start = start;
                End = end;
            }
            /// <summary>
            /// Initializes the downtime timers for the specified boss.
            /// </summary>
            /// <param name="boss">The boss for which to initialize the downtime timers.</param>
            public static void InitializeDowntimeTimers(FRUBoss boss)
            {
                if (PhaseTimings.phaseTimers.TryGetValue(boss, out PhaseTimings phaseTimings))
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
            }
            private static readonly Dictionary<Downtime, DowntimeTimings> downtimeTimers = new Dictionary<Downtime, DowntimeTimings>();
            /// <summary>
            /// Gets the downtime timings for the specified downtime.
            /// </summary>
            /// <param name="downtime">The downtime to get the timings for.</param>
            /// <returns>The downtime timings if found; otherwise, a new DowntimeTimings instance.</returns>
            public static DowntimeTimings GetDowntimeTimings(Downtime downtime)
            {
                return downtimeTimers.TryGetValue(downtime, out DowntimeTimings value) ? value : new DowntimeTimings();
            }
            /// <summary>
            /// Checks if the current phase is within the specified downtime range.
            /// </summary>
            /// <param name="boss">The current boss.</param>
            /// <param name="downtime">The downtime to check.</param>
            /// <returns>True if the current phase is within the downtime range; otherwise, false.</returns>
            public static bool IsDowntime(FRUBoss boss, Downtime downtime)
            {
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
        /// Struct representing the start and end timings of a phase.
        /// </summary>
        public struct PhaseTimings
        {
            public float Start { get; set; }
            public float End { get; set; }

            public PhaseTimings(float start, float end)
            {
                Start = start;
                End = end;
            }
            internal static readonly Dictionary<FRUBoss, PhaseTimings> phaseTimers = new Dictionary<FRUBoss, PhaseTimings>();
            /// <summary>
            /// Initializes the phase timers and returns the phase durations for each phase.
            /// </summary>
            /// <returns>A dictionary containing the phase durations for each boss.</returns>
            public static Dictionary<FRUBoss, (string phaseName, float duration, FRUBoss? requiredPreviousBoss)> InitializeAndGetPhaseDurations()
            {
                var phaseDurations = new Dictionary<FRUBoss, (string phaseName, float duration, FRUBoss? requiredPreviousBoss)>
                {
                    { FRUBoss.Fatebreaker, ("Fatebreaker", 160.1f, null) },
                    { FRUBoss.Usurper, ("Usurper of Frost", 185f, FRUBoss.Fatebreaker) },
                    { FRUBoss.Adds, ("Ice Veil", 41.2f, FRUBoss.Usurper) },
                    { FRUBoss.Gaia, ("Oracle of Darkness", 157.9f, FRUBoss.Adds) },
                    { FRUBoss.Lesbians, ("Usurper of Frost", 175.9f, FRUBoss.Gaia) },
                    { FRUBoss.Pandora, ("Pandora", 271.9f, FRUBoss.Lesbians) }
                };

                foreach (var phase in phaseDurations)
                {
                    phaseTimers[phase.Key] = new PhaseTimings(CombatTime, CombatTime + phase.Value.duration);
                    DowntimeTimings.InitializeDowntimeTimers(phase.Key);
                }

                return phaseDurations;
            }
            /// <summary>
            /// Handles the phase transition logic for the specified phase.
            /// </summary>
            /// <param name="phase">The phase to handle the transition for.</param>
            public static void HandlePhaseTransition(KeyValuePair<FRUBoss, (string phaseName, float duration, FRUBoss? requiredPreviousBoss)> phase)
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
            public static bool IsPhaseTransitionValid(KeyValuePair<FRUBoss, (string phaseName, float duration, FRUBoss? requiredPreviousBoss)> phase, List<string> hostileTargetNames)
            {
                bool doesPhaseNameContain = hostileTargetNames.Contains(phase.Value.phaseName);
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
                return IsBossDead(currentBoss.ToString());
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
                var target = AllHostileTargets.FirstOrDefault(obj => obj.Name.ToString() == bossName);
                return target != null ? target.GetHealthRatio() : 999f; // Return 999f if the boss is not found (considered alive)
            }
            /// <summary>
            /// Checks if the specified boss is dead.
            /// </summary>
            /// <param name="bossName">The name of the boss.</param>
            /// <returns>True if the boss is dead; otherwise, false.</returns>
            public static bool IsBossDead(string bossName)
            {
                var target = AllHostileTargets.FirstOrDefault(obj => obj.Name.ToString() == bossName);
                return target != null && target.GetHealthRatio() <= 1;
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
                var phaseDurations = PhaseTimings.InitializeAndGetPhaseDurations(); // Initialize phase timers and get phase durations
                var hostileTargetNames = AllHostileTargets.Select(obj => obj.Name.ToString()).ToList();
                UpdateActionFlags(); // Update action flags based on current boss, downtime, and combat state

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
                ResetFRUTimers();
            }
            return currentBoss;
        }
        /// <summary>
        /// Resets the FRU phase and downtime timers
        /// </summary>
        private static void ResetFRUTimers()
        {
            currentBoss = FRUBoss.None;
            currentDowntime = Downtime.None;
            CurrentDowntimeStart = 0;
            CurrentDowntimeEnd = 0;
            CurrentPhaseEnd = 0;
        }

        #endregion

        #region FRU Conditions

        public static readonly bool hasSpellinWaitingReturn = Player.HasStatus(false, StatusID.SpellinWaitingReturn_4208);
        public static readonly bool hasReturn = Player.HasStatus(false, StatusID.Return);
        public static readonly bool returnEnding = hasReturn && Player.WillStatusEnd(7, false, StatusID.Return);
        public static readonly bool hasFinishingMove = Player.HasStatus(true, StatusID.FinishingMoveReady);
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
            }
        }


        private static void HandleFatebreaker()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Fatebreaker, Downtime.UtopianSky))
            {
                ShouldUseStandardStep = true;
                ShouldUseFlourish = true;
                ShouldFinishingMove = false;

                if (hasFinishingMove)
                {
                    ShouldRemoveFinishingMove = true;
                }
            }
            if (currentDowntime == Downtime.None)
            {
                if (ShouldRemoveFinishingMove)
                {
                    ShouldFinishingMove = true;
                    ShouldRemoveFinishingMove = false;
                }
            }
        }

        private static void HandleUsurper()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Usurper, Downtime.DiamondDust))
            {
                // Do something
            }
            if (DowntimeTimings.IsDowntime(FRUBoss.Usurper, Downtime.LightRampant))
            {
                // Do something
            }
        }

        private static void HandleAdds()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Adds, Downtime.GaiaTransition))
            {
                // Do something
            }
        }

        private static void HandleGaia()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Gaia, Downtime.UltimateRelativity))
            {
                // Do something
            }
        }

        private static void HandleLesbians()
        {
            if (DowntimeTimings.IsDowntime(FRUBoss.Lesbians, Downtime.OracleTargetable))
            {
                // Do something
            }
            if (DowntimeTimings.IsDowntime(FRUBoss.Lesbians, Downtime.CrystalizeTime))
            {
                // Do something
            }
        }
        private static void HandlePandora()
        {
            // Do something
        }
        #endregion
    }
}