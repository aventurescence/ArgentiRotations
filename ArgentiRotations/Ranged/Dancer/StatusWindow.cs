using ArgentiRotations.Common;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

public sealed partial class ChurinDNC
{
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

        ImGui.EndGroup();
    }

    private void DrawCombatStatusText()
    {
        try
        {
            ImGui.Columns(2, "CombatStatusColumns", false);

            // Column headers
            ImGui.Text("Status");
            ImGui.NextColumn();
            ImGui.Text("Value");
            ImGui.NextColumn();
            ImGui.Separator();

            ImGui.Text("Should Use Tech Step?");
            ImGui.NextColumn();
            ImGui.Text(ShouldUseTechStep.ToString());
            ImGui.NextColumn();

            ImGui.Text("Should Use Flourish?");
            ImGui.NextColumn();
            ImGui.Text(ShouldUseFlourish.ToString());
            ImGui.NextColumn();

            ImGui.Text("Should Use Standard Step?");
            ImGui.NextColumn();
            ImGui.Text(ShouldUseStandardStep.ToString());
            ImGui.NextColumn();

            ImGui.Text("Should Use Last Dance?");
            ImGui.NextColumn();
            ImGui.Text(ShouldUseLastDance.ToString());
            ImGui.NextColumn();

            ImGui.Text("In Burst:");
            ImGui.NextColumn();
            ImGui.Text(DanceDance.ToString());
            ImGui.NextColumn();

            ImGui.Text("Should Hold For Tech Step?");
            ImGui.NextColumn();
            ImGui.Text(ShouldHoldForTechStep.ToString());
            ImGui.NextColumn();

            ImGui.Text("Should Hold For Standard Step?");
            ImGui.NextColumn();
            ImGui.Text(ShouldHoldForStandard.ToString());
            ImGui.NextColumn();

            ImGui.Text("Is Dancing:");
            ImGui.NextColumn();
            ImGui.Text(IsDancing.ToString());
            ImGui.NextColumn();

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
            var debugVisible = RotationDebugManager.IsDebugTableVisible;
            if (ImGui.Checkbox("Enable Debug Table", ref debugVisible))
                RotationDebugManager.IsDebugTableVisible = debugVisible;
            DrawRotationStatus();
            DrawCombatStatus();
            RotationDebugManager.DrawGCDMethodDebugTable();
            DisplayStatusHelper.EndPaddedChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying status: {ex.Message}");
        }
    }

    #endregion
}