using ArgentiRotations.Common;

namespace ArgentiRotations.Encounter.StateMachine
{
    /// <summary>
    /// Builder class for creating ArgentiStateMachine instances with a fluent API.
    /// Supports phase-based construction and mechanic configuration.
    /// </summary>
    public class ArgentiStateMachineBuilder
    {
    #region Fields

    private uint _bossActorId;
    private ushort _territoryId;
    private readonly List<IMechanic> _mechanics = [];
    private readonly List<Phase> _phases = [];
    private Phase? _currentPhase;

    #endregion

        #region Configuration Methods        /// <summary>
        /// Sets the boss actor ID for the state machine.
        /// </summary>
        /// <param name="bossActorId">The actor ID of the boss</param>
        /// <returns>The builder instance for method chaining</returns>
        public ArgentiStateMachineBuilder WithBossActorId(uint bossActorId)
        {
            _bossActorId = bossActorId;
            return this;
        }

        /// <summary>
        /// Sets the expected territory ID for the state machine.
        /// This allows the state machine to validate it's being used in the correct encounter.
        /// </summary>
        /// <param name="territoryId">The territory ID where this state machine should be active</param>
        /// <returns>The builder instance for method chaining</returns>
        public ArgentiStateMachineBuilder WithTerritoryId(ushort territoryId)
        {
            _territoryId = territoryId;
            return this;
        }

        #endregion

        #region Mechanic Management

        /// <summary>
        /// Adds a mechanic to the state machine.
        /// If a phase is currently being built, the mechanic will be associated with that phase.
        /// </summary>
        /// <param name="mechanic">The mechanic to add</param>
        /// <returns>The builder instance for method chaining</returns>
        private ArgentiStateMachineBuilder AddMechanic(IMechanic mechanic)
        {
            _mechanics.Add(mechanic);
            
            // Add to current phase if one is being built
            _currentPhase?.AddMechanic(mechanic);
            
            return this;
        }

        /// <summary>
        /// Adds a mechanic to the state machine with the specified parameters.
        /// This is a convenience method that creates a BasicMechanic instance.
        /// </summary>
        /// <param name="actorId">The actor ID that triggers this mechanic</param>
        /// <param name="name">The name of the mechanic</param>
        /// <param name="duration">The duration of the mechanic in seconds</param>
        /// <param name="castId">The cast ID of the mechanic (optional)</param>
        /// <param name="type">The type of mechanic (optional, defaults to Unknown)</param>
        /// <param name="isInterruptible">Whether the mechanic can be interrupted (optional, defaults to false)</param>
        /// <param name="isAoE">Whether the mechanic is an area of effect (optional, defaults to false)</param>
        /// <param name="expectedStartTime">The expected start time of the mechanic (optional)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ArgentiStateMachineBuilder AddMechanic(
            uint actorId,
            string name,
            float duration,
            uint castId = 0,
            MechanicType type = MechanicType.None,
            bool isInterruptible = false,
            bool isAoE = false,
            DateTime? expectedStartTime = null)
        {
            var mechanic = new BasicMechanic(
                actorId, name, duration, castId, type,
                isInterruptible, isAoE, expectedStartTime);
            return AddMechanic(mechanic);
        }

        #endregion

        #region Phase Management        
        /// <summary>
        /// Begins building a new phase with the specified name.
        /// All mechanics added after this call will be associated with this phase
        /// until EndPhase() is called.
        /// </summary>
        /// <param name="phaseName">The name of the phase</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown if a phase is already being built</exception>
        public ArgentiStateMachineBuilder BeginPhase(string phaseName)
        {
            if (_currentPhase != null)
            {
                ArgentiUtilities.Warning($"Cannot begin phase '{phaseName}' - phase '{_currentPhase.Name}' is still active");
                throw new InvalidOperationException("Cannot begin a new phase without ending the current one");
            }

            _currentPhase = new Phase(phaseName);
            return this;
        }
        /// <summary>
        /// Ends the current phase being built and adds it to the state machine.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown if no phase is currently being built</exception>
        public ArgentiStateMachineBuilder EndPhase()
        {
            if (_currentPhase == null)
            {
                ArgentiUtilities.Warning("Cannot end phase - no phase is currently active");
                throw new InvalidOperationException("No phase to end");
            }

            _phases.Add(_currentPhase);
            _currentPhase = null;
            return this;
        }

        #endregion

        #region Build Method        
        /// <summary>
        /// Builds the ArgentiStateMachine instance with all configured mechanics and phases.
        /// </summary>
        /// <returns>A new ArgentiStateMachine instance</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if boss actor ID is not set or if there's an unfinished phase
        /// </exception>
        public ArgentiStateMachine Build()
        {
            if (_bossActorId == 0)
            {
                ArgentiUtilities.Warning("Cannot build state machine - boss actor ID is not set");
                throw new InvalidOperationException("Boss actor ID must be set");
            }

            if (_currentPhase != null)
            {
                ArgentiUtilities.Warning($"Cannot build state machine - phase '{_currentPhase.Name}' is unfinished");
                throw new InvalidOperationException("Cannot build with an unfinished phase. Call EndPhase() first.");
            }

            return new ArgentiStateMachine(_bossActorId, _mechanics, _phases, _territoryId);
        }

        #endregion
    }
}
