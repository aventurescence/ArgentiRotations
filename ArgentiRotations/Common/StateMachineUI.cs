using ArgentiRotations.Encounter.StateMachine;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Common;

/// <summary>
/// Configuration for encounter-specific state machine display.
/// </summary>
public class EncounterConfig
{
    public string Name { get; init; } = string.Empty;
    public ushort TerritoryId { get; init; }
    public string HeaderText { get; init; } = string.Empty;
    public bool ShowTerritoryConfirmation { get; init; } = true;
    public bool ShowPhaseProgress { get; init; } = true;
    public bool ShowQueuedMechanics { get; init; } = true;
    public bool ShowPhaseDetails { get; init; } = true;
}

/// <summary>
/// UI helper class for drawing state machine status information.
/// Provides methods for rendering state machine data in ImGui windows.
/// </summary>
public static class StateMachineUI
{
    // Pre-configured encounter settings
    private static readonly Dictionary<ushort, EncounterConfig> EncounterConfigs = new()
    {
        [1263] = new EncounterConfig 
        { 
            Name = "M8S", 
            TerritoryId = 1263, 
            HeaderText = "M8S State Machine Status" 
        }
        // Add more encounters as needed
    };

    /// <summary>
    /// Draws the complete state machine status UI with automatic encounter detection.
    /// </summary>
    /// <param name="stateMachine">The state machine to display</param>
    /// <param name="currentTerritoryId">The current territory ID</param>
    public static void DrawStateMachineStatus(ArgentiStateMachine? stateMachine, ushort currentTerritoryId)
    {
        if (EncounterConfigs.TryGetValue(currentTerritoryId, out var config))
        {
            DrawEncounterStateMachineStatus(stateMachine, config);
        }
        else
        {
            DrawGeneralStateMachineStatus(stateMachine);
        }
    }

    /// <summary>
    /// Draws encounter-specific state machine status information.
    /// </summary>
    /// <param name="stateMachine">The encounter state machine to display</param>
    /// <param name="config">The encounter configuration</param>
    private static void DrawEncounterStateMachineStatus(ArgentiStateMachine? stateMachine, EncounterConfig config)
    {
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader(config.HeaderText, ImGuiTreeNodeFlags.DefaultOpen))
        {
            try
            {
                ImGui.Columns(2, $"{config.Name}StateMachineColumns", false);
                
                // Territory confirmation (if enabled)
                if (config.ShowTerritoryConfirmation)
                {
                    ImGui.Text("Territory:"); ImGui.NextColumn();
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"{config.Name} ({config.TerritoryId})"); ImGui.NextColumn();
                }
                
                if (stateMachine != null)
                {
                    // Encounter-specific state machine information
                    ImGui.Text($"{config.Name} State Machine:"); ImGui.NextColumn();
                    var stateColor = stateMachine.CurrentState switch
                    {
                        MechanicState.Idle => ImGuiColors.DalamudGrey,
                        MechanicState.CastingMechanic => ImGuiColors.DalamudOrange,
                        MechanicState.ExecutingMechanic => ImGuiColors.DalamudRed,
                        MechanicState.Transitioning => ImGuiColors.DalamudYellow,
                        _ => ImGuiColors.DalamudWhite
                    };
                    ImGui.TextColored(stateColor, stateMachine.CurrentState.ToString()); ImGui.NextColumn();
                    
                    // Current Mechanic
                    ImGui.Text($"Current {config.Name} Mechanic:"); ImGui.NextColumn();
                    if (stateMachine.CurrentMechanic != null)
                    {
                        ImGui.TextColored(ImGuiColors.HealerGreen, stateMachine.CurrentMechanic.Name);
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "None");
                    }
                    ImGui.NextColumn();
                    
                    // Phase Information
                    ImGui.Text($"{config.Name} Phase:"); ImGui.NextColumn();
                    if (stateMachine.CurrentPhase != null)
                    {
                        var phaseColor = stateMachine.CurrentPhase.State switch
                        {
                            PhaseState.Active => ImGuiColors.HealerGreen,
                            PhaseState.Completed => ImGuiColors.DalamudGrey,
                            PhaseState.Skipped => ImGuiColors.DalamudOrange,
                            _ => ImGuiColors.DalamudWhite
                        };
                        ImGui.TextColored(phaseColor, $"{stateMachine.CurrentPhase.Name} ({stateMachine.CurrentPhase.State})");
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"No {config.Name} Phase Active");
                    }
                    ImGui.NextColumn();

                    // Show queued mechanics count if enabled
                    if (config.ShowQueuedMechanics)
                    {
                        var queuedMechanics = stateMachine.GetQueuedMechanics();
                        ImGui.Text("Queued Mechanics:"); ImGui.NextColumn();
                        ImGui.Text(queuedMechanics.Count.ToString()); ImGui.NextColumn();
                    }

                    // Show phase progress if enabled
                    if (config.ShowPhaseProgress && stateMachine.Phases.Any())
                    {
                        ImGui.Text("Phase Progress:"); ImGui.NextColumn();
                        var currentPhaseIndex = stateMachine.Phases.ToList().FindIndex(p => p.IsActive);
                        var totalPhases = stateMachine.Phases.Count;
                        if (currentPhaseIndex >= 0)
                        {
                            ImGui.Text($"{currentPhaseIndex + 1}/{totalPhases}");
                        }
                        else
                        {
                            ImGui.Text($"0/{totalPhases}");
                        }
                        ImGui.NextColumn();
                    }
                }
                else
                {
                    ImGui.Text($"{config.Name} State Machine:"); ImGui.NextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "Not Initialized"); ImGui.NextColumn();
                }
                
                ImGui.Columns(1);

                // Show detailed information if configured
                if (stateMachine != null)
                {
                    if (config.ShowPhaseDetails)
                    {
                        DrawPhaseDetails(stateMachine);
                    }

                    if (config.ShowQueuedMechanics)
                    {
                        var queuedMechanics = stateMachine.GetQueuedMechanics();
                        DrawQueuedMechanics(queuedMechanics);
                    }
                }
            }
            catch (Exception ex)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying {config.Name} state machine: {ex.Message}");
            }
        }
        ImGui.EndGroup();
    }

    /// <summary>
    /// Draws general state machine status information for non-M8S territories.
    /// </summary>
    /// <param name="stateMachine">The state machine to display</param>
    private static void DrawGeneralStateMachineStatus(ArgentiStateMachine? stateMachine)
    {
        if (stateMachine == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "State Machine: Not Initialized");
            return;
        }

        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("State Machine Status", ImGuiTreeNodeFlags.DefaultOpen))
        {
            try
            {
                ImGui.Columns(2, "StateMachineColumns", false);

                // Current State
                ImGui.Text("Current State:"); ImGui.NextColumn();
                var stateColor = stateMachine.CurrentState switch
                {
                    MechanicState.Idle => ImGuiColors.DalamudGrey,
                    MechanicState.CastingMechanic => ImGuiColors.DalamudOrange,
                    MechanicState.ExecutingMechanic => ImGuiColors.DalamudRed,
                    MechanicState.Transitioning => ImGuiColors.DalamudYellow,
                    _ => ImGuiColors.DalamudWhite
                };
                ImGui.TextColored(stateColor, stateMachine.CurrentState.ToString()); ImGui.NextColumn();

                // Time in Current State
                ImGui.Text("Time in State:"); ImGui.NextColumn();
                ImGui.Text($"{stateMachine.TimeInCurrentState.TotalSeconds:F1}s"); ImGui.NextColumn();

                // Current Mechanic
                ImGui.Text("Current Mechanic:"); ImGui.NextColumn();
                if (stateMachine.CurrentMechanic != null)
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, stateMachine.CurrentMechanic.Name);
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "None");
                }
                ImGui.NextColumn();

                // Current Phase
                ImGui.Text("Current Phase:"); ImGui.NextColumn();
                if (stateMachine.CurrentPhase != null)
                {
                    var phaseColor = stateMachine.CurrentPhase.State switch
                    {
                        PhaseState.Active => ImGuiColors.HealerGreen,
                        PhaseState.Completed => ImGuiColors.DalamudGrey,
                        PhaseState.Skipped => ImGuiColors.DalamudOrange,
                        _ => ImGuiColors.DalamudWhite
                    };
                    ImGui.TextColored(phaseColor, $"{stateMachine.CurrentPhase.Name} ({stateMachine.CurrentPhase.State})");
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "No Phase Active");
                }
                ImGui.NextColumn();

                // Queued Mechanics Count
                var queuedMechanics = stateMachine.GetQueuedMechanics();
                ImGui.Text("Queued Mechanics:"); ImGui.NextColumn();
                ImGui.Text(queuedMechanics.Count.ToString()); ImGui.NextColumn();

                // Phase Progress
                if (stateMachine.Phases.Any())
                {
                    ImGui.Text("Phase Progress:"); ImGui.NextColumn();
                    var currentPhaseIndex = stateMachine.Phases.ToList().FindIndex(p => p.IsActive);
                    var totalPhases = stateMachine.Phases.Count;
                    if (currentPhaseIndex >= 0)
                    {
                        ImGui.Text($"{currentPhaseIndex + 1}/{totalPhases}");
                    }
                    else
                    {
                        ImGui.Text($"0/{totalPhases}");
                    }
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);

                // Show phase details if expanded
                DrawPhaseDetails(stateMachine);

                // Show queued mechanics if any
                DrawQueuedMechanics(queuedMechanics);
            }
            catch (Exception ex)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying state machine: {ex.Message}");
            }
        }
        ImGui.EndGroup();
    }

    /// <summary>
    /// Draws detailed phase information in an expandable tree.
    /// </summary>
    /// <param name="stateMachine">The state machine containing phases</param>
    private static void DrawPhaseDetails(ArgentiStateMachine stateMachine)
    {
        if (ImGui.TreeNode("Phase Details"))
        {
            foreach (var phase in stateMachine.Phases)
            {
                var phaseIcon = phase.State switch
                {
                    PhaseState.Active => "▶",
                    PhaseState.Completed => "✓",
                    PhaseState.Skipped => "⚠",
                    _ => "○"
                };
                
                var phaseColor = phase.State switch
                {
                    PhaseState.Active => ImGuiColors.HealerGreen,
                    PhaseState.Completed => ImGuiColors.DalamudGrey,
                    PhaseState.Skipped => ImGuiColors.DalamudOrange,
                    _ => ImGuiColors.DalamudWhite
                };
                
                ImGui.TextColored(phaseColor, $"{phaseIcon} {phase.Name}");
                
                if (phase.IsActive && phase.StartTime.HasValue)
                {
                    ImGui.SameLine();
                    var elapsed = DateTime.UtcNow - phase.StartTime.Value;
                    ImGui.TextColored(ImGuiColors.DalamudGrey2, $"({elapsed.TotalSeconds:F1}s)");
                }
            }
            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Draws queued mechanics information in an expandable tree.
    /// </summary>
    /// <param name="queuedMechanics">Collection of queued mechanics to display</param>
    private static void DrawQueuedMechanics(IReadOnlyCollection<IMechanic> queuedMechanics)
    {
        if (queuedMechanics.Any() && ImGui.TreeNode("Queued Mechanics"))
        {
            foreach (var mechanic in queuedMechanics)
            {
                ImGui.TextColored(ImGuiColors.TankBlue, $"• {mechanic.Name}");
                if (mechanic.ExpectedStartTime.HasValue)
                {
                    ImGui.SameLine();
                    var timeUntil = mechanic.ExpectedStartTime.Value - DateTime.UtcNow;
                    if (timeUntil.TotalSeconds > 0)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey2, $"(in {timeUntil.TotalSeconds:F1}s)");
                    }
                }
            }
            ImGui.TreePop();
        }
    }
}
