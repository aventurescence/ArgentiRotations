namespace ArgentiRotations.Encounter.StateMachine
{
    #region Enums

    public enum MechanicState
    {
        Idle,
        CastingMechanic,
        ExecutingMechanic,
        Transitioning
    }

    public enum PhaseState
    {
        NotStarted,
        Active,
        Completed,
        Skipped
    }

    public enum MechanicType
    {
        None,
        Raidwide,
        Stack,
        Debuff,
        Tankbuster,
        Multihit,
        AOE,
        Spread,
        Tower,
        Adds,
        PhaseTransition,
        Special,
        Enrage
    }

    #endregion

    #region Interfaces

    public interface IPhase
    {
        string Name { get; }
        PhaseState State { get; }
        DateTime? StartTime { get; }
        DateTime? EndTime { get; }
        TimeSpan? Duration { get; }
        IReadOnlyCollection<IMechanic> Mechanics { get; }
        bool IsActive { get; }
        bool IsCompleted { get; }
    }

    public interface IMechanic
    {
        uint ActorId { get; }
        string Name { get; }
        float Duration { get; }
        uint CastId { get; }
        MechanicType Type { get; }
        bool IsInterruptible { get; }
        bool IsAoE { get; }
        DateTime? ExpectedStartTime { get; }
    }

    #endregion

    #region Phase Implementation

    public class Phase(string name) : IPhase
    {
        private readonly List<IMechanic> _mechanics = [];

        public string Name { get; } = name;
        public PhaseState State { get; private set; } = PhaseState.NotStarted;
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue ? EndTime - StartTime : null;
        public IReadOnlyCollection<IMechanic> Mechanics => _mechanics.AsReadOnly();
        public bool IsActive => State == PhaseState.Active;
        public bool IsCompleted => State == PhaseState.Completed;

        public void AddMechanic(IMechanic mechanic)
        {
            if (State is PhaseState.Active or PhaseState.Completed)
                throw new InvalidOperationException("Cannot add mechanics to an active or completed phase");

            _mechanics.Add(mechanic);
        }

        public void Start()
        {
            if (State != PhaseState.NotStarted)
                throw new InvalidOperationException($"Phase {Name} cannot be started from state {State}");

            State = PhaseState.Active;
            StartTime = DateTime.UtcNow;
        }

        public void Complete()
        {
            if (State != PhaseState.Active)
                throw new InvalidOperationException($"Phase {Name} cannot be completed from state {State}");

            State = PhaseState.Completed;
            EndTime = DateTime.UtcNow;
        }

        public void Skip()
        {
            if (State == PhaseState.Completed)
                throw new InvalidOperationException($"Phase {Name} is already completed and cannot be skipped");

            State = PhaseState.Skipped;
            StartTime ??= DateTime.UtcNow;
            EndTime = DateTime.UtcNow;
        }
        public void Reset()
        {
            State = PhaseState.NotStarted;
            StartTime = null;
            EndTime = null;
        }
    }

    #endregion

    #region Mechanic Implementation

    public class BasicMechanic(
    uint actorId,
    string name,
    float duration,
    uint castId = 0,
    MechanicType type = MechanicType.None,
    bool isInterruptible = false,
    bool isAoE = false,
    DateTime? expectedStartTime = null) : IMechanic
    {
        public uint ActorId { get; } = actorId;
        public string Name { get; } = name;
        public float Duration { get; } = duration;
        public uint CastId { get; } = castId;
        public MechanicType Type { get; } = type;
        public bool IsInterruptible { get; } = isInterruptible;
        public bool IsAoE { get; } = isAoE;
        public DateTime? ExpectedStartTime { get; } = expectedStartTime;
    }

    #endregion    #region State Machine

    public class ArgentiStateMachine : IDisposable
    {
        #region Fields and Properties

        private readonly Dictionary<uint, IMechanic> _mechanics;
        private readonly List<IPhase> _phases;
        private DateTime _stateStartTime;

        // Territory validation
        public bool IsInCorrectTerritory => ExpectedTerritoryId == 0 || CustomRotation.IsInTerritory(ExpectedTerritoryId);
        public ushort ExpectedTerritoryId { get; private set; }

        // Add state transition validation
        private static readonly Dictionary<MechanicState, MechanicState[]> ValidTransitions = new()
        {
            { MechanicState.Idle, [MechanicState.CastingMechanic, MechanicState.Transitioning] },
            { MechanicState.CastingMechanic, [MechanicState.ExecutingMechanic, MechanicState.Idle] },
            { MechanicState.ExecutingMechanic, [MechanicState.Transitioning, MechanicState.Idle] },
            { MechanicState.Transitioning, [MechanicState.Idle, MechanicState.CastingMechanic] }
        };
        public event Action<MechanicState, IMechanic?> StateChanged = delegate { };
        public event Action<IMechanic> MechanicTimeout = delegate { };
        public event Action<MechanicState, MechanicState> InvalidTransitionAttempted = delegate { };        public event Action<IPhase> PhaseStarted = delegate { };
        public event Action<IPhase> PhaseCompleted = delegate { }; 
        public event Action<IPhase> PhaseSkipped = delegate { };

        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private Timer? _timeoutTimer;
        private readonly Queue<IMechanic> _mechanicQueue = new();
        private readonly Lock _queueLock = new();

        public MechanicState CurrentState { get; private set; }

        public IMechanic? CurrentMechanic { get; private set; }

        private uint BossActorId { get; }

        public TimeSpan TimeInCurrentState => DateTime.UtcNow - _stateStartTime;
        public IPhase? CurrentPhase => CurrentPhaseIndex >= 0 && CurrentPhaseIndex < _phases.Count ? _phases[CurrentPhaseIndex] : null;
        public IReadOnlyList<IPhase> Phases => _phases.AsReadOnly();
        private int CurrentPhaseIndex { get; set; }

        #endregion

        #region Constructor        
        public ArgentiStateMachine(uint bossActorId, IEnumerable<IMechanic> mechanics, IEnumerable<IPhase>? phases = null, ushort expectedTerritoryId = 0)
        {
            BossActorId = bossActorId;
            _mechanics = [];
            _phases = phases?.ToList() ?? [];
            CurrentPhaseIndex = -1;
            ExpectedTerritoryId = expectedTerritoryId;

            foreach (var mechanic in mechanics)
            {
                _mechanics[mechanic.ActorId] = mechanic;
            }

            CurrentState = MechanicState.Idle;
            _stateStartTime = DateTime.UtcNow;

        }

        #endregion

        #region State Management

        public bool UpdateState(uint actorId, MechanicState newState)
        {
            if (actorId != BossActorId && !_mechanics.ContainsKey(actorId))
                return false;

            // Validate state transition
            if (!IsValidTransition(CurrentState, newState))
            {
                InvalidTransitionAttempted?.Invoke(CurrentState, newState);
                return false;
            }
            var previousState = CurrentState;
            var previousMechanic = CurrentMechanic;

            CurrentState = newState;
            CurrentMechanic = _mechanics.GetValueOrDefault(actorId);
            _stateStartTime = DateTime.UtcNow;

            // Setup timeout for non-idle states
            SetupTimeout();

            if (previousState != newState || previousMechanic != CurrentMechanic)
            {
                StateChanged?.Invoke(CurrentState, CurrentMechanic);
            }

            return true;
        }

        private static bool IsValidTransition(MechanicState from, MechanicState to)
        {
            return ValidTransitions.TryGetValue(from, out var validStates) &&
                   validStates.Contains(to);
        }

        private void SetupTimeout()
        {
            _timeoutTimer?.Dispose();

            if (CurrentState == MechanicState.Idle || CurrentMechanic == null)
                return;

            var timeout = CurrentMechanic.Duration > 0
                ? TimeSpan.FromSeconds(CurrentMechanic.Duration + 5) // Add 5s buffer
                : _defaultTimeout;

            _timeoutTimer = new Timer(OnTimeout, null, timeout, Timeout.InfiniteTimeSpan);
        }
        private void OnTimeout(object? state)
        {
            if (CurrentMechanic != null)
            {
                MechanicTimeout?.Invoke(CurrentMechanic);
            } Reset();
        }

        public void Reset()
        {
            _timeoutTimer?.Dispose();
            CurrentState = MechanicState.Idle;
            CurrentMechanic = null;
            _stateStartTime = DateTime.UtcNow;

            lock (_queueLock)
            {
                _mechanicQueue.Clear();
            }

            // Reset all phases
            ResetAllPhases();

            StateChanged?.Invoke(CurrentState, CurrentMechanic);
        }

        #endregion

        #region Mechanic Management

        public bool IsMechanicActive(uint mechanicActorId)
        { return CurrentMechanic?.ActorId == mechanicActorId &&
                   CurrentState is MechanicState.CastingMechanic or MechanicState.ExecutingMechanic;
        }

        public void EnqueueMechanic(IMechanic mechanic)
        {
            lock (_queueLock)
            {
                _mechanicQueue.Enqueue(mechanic);
            }
        }

        public IMechanic? GetNextMechanic()
        {
            lock (_queueLock)
            {
                return _mechanicQueue.Count > 0 ? _mechanicQueue.Peek() : null;
            }
        }

        public bool TryProcessNextMechanic()
        {
            lock (_queueLock)
            {
                if (_mechanicQueue.Count > 0 && CurrentState == MechanicState.Idle)
                {
                    var nextMechanic = _mechanicQueue.Dequeue();
                    return UpdateState(nextMechanic.ActorId, MechanicState.CastingMechanic);
                }
            }
            return false;
        }

        public IReadOnlyCollection<IMechanic> GetQueuedMechanics()
        {
            lock (_queueLock)
            {
                return _mechanicQueue.ToArray();
            }
        }

        public void AddMechanic(IMechanic mechanic)
        {
            _mechanics[mechanic.ActorId] = mechanic;
        }

        public void RemoveMechanic(uint actorId)
        {
            _mechanics.Remove(actorId);
        }

        public bool HasMechanic(uint actorId)
        {
            return _mechanics.ContainsKey(actorId);
        } public IMechanic? GetMechanic(uint actorId)
        {
            return _mechanics.GetValueOrDefault(actorId);
        }

        #endregion

        #region Phase Management

        // Phase Management Methods
        public bool StartNextPhase()
        {
            if (CurrentPhaseIndex + 1 >= _phases.Count)
                return false;

            // Complete current phase if active
            if (CurrentPhase?.IsActive == true)
            {
                ((Phase)CurrentPhase).Complete();
                PhaseCompleted?.Invoke(CurrentPhase);
            }

            CurrentPhaseIndex++;
            var nextPhase = (Phase)_phases[CurrentPhaseIndex];
            nextPhase.Start();
            PhaseStarted?.Invoke(nextPhase);

            return true;
        }

        private bool StartPhase(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= _phases.Count)
                return false;

            // Complete current phase if active
            if (CurrentPhase?.IsActive == true)
            {
                ((Phase)CurrentPhase).Complete();
                PhaseCompleted?.Invoke(CurrentPhase);
            }

            CurrentPhaseIndex = phaseIndex;
            var phase = (Phase)_phases[CurrentPhaseIndex];
            phase.Start();
            PhaseStarted?.Invoke(phase);

            return true;
        }

        public bool StartPhase(string phaseName)
        {
            var phaseIndex = _phases.ToList().FindIndex(p => p.Name.Equals(phaseName, StringComparison.OrdinalIgnoreCase));
            return phaseIndex >= 0 && StartPhase(phaseIndex);
        }

        public void CompleteCurrentPhase()
        {
            if (CurrentPhase?.IsActive == true)
            {
                ((Phase)CurrentPhase).Complete();
                PhaseCompleted?.Invoke(CurrentPhase);
            }
        }

        public void SkipCurrentPhase()
        {
            if (CurrentPhase is { IsCompleted: false })
            {
                ((Phase)CurrentPhase).Skip();
                PhaseSkipped?.Invoke(CurrentPhase);
            }
        }

        private void ResetAllPhases()
        { foreach (var phase in _phases.Cast<Phase>())
            {
                phase.Reset();
            }
            CurrentPhaseIndex = -1;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()        {
            _timeoutTimer?.Dispose();        }

        #endregion
    }
}