using System.Globalization;
using ArgentiRotations.Common;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace ArgentiRotations.Ranged;

/// <summary>
/// UI and Status Window implementation for ChurinBRD
/// </summary>
public sealed partial class ChurinBRD
{
    #region Fields
    private Vector4[] _shouldUseConditions = [];
    private Vector4[] _statusConditions = [];
    private Vector4[] _timingConditions = [];
    
#endregion
    #region Status Window Override
    public override void DisplayStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinBRD Status", true,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            DrawRotationStatus();
            DrawPartyCompositionHeader();
            DrawCombatStatusHeader();
            DrawSongTimingHeader();
            DrawPotionStatusHeader();
            DisplayStatusHelper.EndPaddedChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying status: {ex.Message}");
        }
    }
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

    #region Party Composition
    private static void DrawPartyCompositionHeader()
    {
        try
        {
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Party Composition", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawPartyCompositionText();
            }
            ImGui.EndGroup();
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying party composition");
        }
    }
    private static void DrawPartyCompositionText()
    {
        ArgentiRotations.Common.PartyComposition.StatusList();
        if (ImGui.BeginTable("PartyCompositionTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Role");
            ImGui.TableSetupColumn("Job");
            ImGui.TableSetupColumn("Buffs Provided");
            ImGui.TableHeadersRow();

            if (PartyComposition.Count == 0)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(Player.ClassJob.Value.GetJobRole().ToString());

                ImGui.TableSetColumnIndex(1);
                var jobAbbr = Player.ClassJob.Value.Abbreviation.ToString();
                ImGui.Text(jobAbbr);

                ImGui.TableSetColumnIndex(2);
                var buffs = ArgentiRotations.Common.PartyComposition.Buffs
                    .Where(b => b.JobAbbr == jobAbbr)
                    .Select(b => $"{b.Name} ({b.Type}")
                    .ToList();

                var buffText = buffs.Count != 0 ? string.Join(", ", buffs) : "None";
                ImGui.Text(buffText);
            }

            foreach (var member in PartyComposition)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text(member.Value.GetJobRole().ToString());

                ImGui.TableSetColumnIndex(1);
                var jobAbbr = member.Value.Abbreviation.ToString();
                ImGui.Text(jobAbbr);

                ImGui.TableSetColumnIndex(2);
                var buffs = ArgentiRotations.Common.PartyComposition.Buffs
                    .Where(b => b.JobAbbr == jobAbbr)
                    .Select(b => b.Name)
                    .ToList();

                var buffText = buffs.Count != 0 ? string.Join(", ", buffs) : "None";
                ImGui.Text(buffText);
            }
        }
        ImGui.EndTable();
    }

    #endregion
    #region Combat Status
    private void DrawCombatStatusHeader()
        {
            try
            {
                InitializeColorData();
                ImGui.BeginGroup();
                if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawCombatStatusText();
                }
                ImGui.EndGroup();
            }
            catch (Exception)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying combat status");
            }
        }
    private void DrawCombatStatusText()
    {
        try
        {
            InitializeColorData();
            if (ImGui.TreeNode("Rotation Booleans"))
            {
                ImGui.Columns(2, "CombatStatusColumns", false);

                // Column headers
                ImGui.Text("Condition");
                ImGui.NextColumn();
                ImGui.Text("Value");
                ImGui.NextColumn();
                ImGui.Separator();

                ImGui.TextColored(_shouldUseConditions[0], "Is First Cycle?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[0], IsFirstCycle.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[1], "Target Has DoTs?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[1], TargetHasDoTs.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[2], "Can Early Weave?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[2], CanEarlyWeave.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[3], "Can Late Weave?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[3], CanLateWeave.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[4], "Enough Weave Time?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[4], EnoughWeaveTime.ToString());
                ImGui.NextColumn();

                // Reset columns
                ImGui.Columns(1);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Status Booleans"))
            {
                ImGui.Columns(2, "StatusColumns", false);
                ImGui.Text("Status");
                ImGui.NextColumn();
                ImGui.Text("Value");
                ImGui.NextColumn();
                ImGui.Separator();

                ImGui.TextColored(_statusConditions[0], "Has Raging Strikes:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[0], HasRagingStrikes.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[1], "Has Battle Voice:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[1], HasBattleVoice.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[2], "Has Radiant Finale:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[2], HasRadiantFinale.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[3], "In Wanderer's Minuet:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[3], InWanderers.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[4], "In Mage's Ballad:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[4], InMages.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[5], "In Army's Paeon:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[5], InArmys.ToString());
                ImGui.NextColumn();

                // Reset columns
                ImGui.Columns(1);
                ImGui.TreePop();
            }
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying combat status");
        }
    }
    #endregion
    #region Songs
    private void DrawSongTimingHeader()
        {
            try
            {
                ImGui.BeginGroup();
                if (ImGui.CollapsingHeader("Song Timing Information", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawSongTimingDetails();
                }
                ImGui.EndGroup();
            }
            catch (Exception)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying song timing");
            }
        }
    private void DrawSongTimingDetails()
        {
            if (ImGui.BeginTable("SongTimingTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Label");
                ImGui.TableSetupColumn("Value");
                ImGui.TableHeadersRow();

                DrawSongTimingInfo();
                DrawCurrentSongInfo();
                DrawSongUptimeInfo();
                DrawCycleInfo();

                ImGui.EndTable();
            }
        }
    private void DrawSongTimingInfo()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Song Timing Preset");
            ImGui.TableNextColumn();
            ImGui.Text(SongTimings.ToString());
        }
    private static void DrawCurrentSongInfo()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Current Song");
        ImGui.TableNextColumn();
        ImGui.Text(Song.ToString());
    }
    private void DrawSongUptimeInfo()
        {
            // Wanderer's Minuet
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Wanderer's Minuet Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {WandTime} seconds");
            ImGui.Text($"Remaining Time: {WandRemainTime} seconds");

            // Mage's Ballad
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Mage's Ballad Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {MageTime} seconds");
            ImGui.Text($"Remaining Time: {MageRemainTime} seconds");

            // Army's Paeon
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Army's Paeon Uptime & Remaining Time");
            ImGui.TableNextColumn();
            ImGui.Text($"Preset Time: {ArmyTime} seconds");
            ImGui.Text($"Remaining Time: {ArmyRemainTime} seconds");
        }
    private static void DrawCycleInfo()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Is First Cycle?");
            ImGui.TableNextColumn();
            ImGui.Text(IsFirstCycle ? "Yes" : "No");
        }
    #endregion
    #region Potions
    private void DrawPotionStatusHeader()
    {
        try
        {
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Potion Status", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawPotionStatusText();
            }
            ImGui.EndGroup();
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying potion status");
        }
    }
    private void DrawPotionStatusText()
    {
        try
        {
            if (ImGui.BeginTable("PotionStatusTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Potion");
                ImGui.TableSetupColumn("Enabled");
                ImGui.TableSetupColumn("Used");
                ImGui.TableSetupColumn("Time");
                ImGui.TableHeadersRow();

                var trueColor = ImGuiColors.HealerGreen;
                var falseColor = ImGuiColors.DalamudRed;

                for (var i = 0; i < _potions.Count; i++)
                {
                    var (time, enabled, used) = _potions[i];

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"Potion {i + 1}");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextColored(GetColor(enabled, trueColor, falseColor), enabled.ToString());

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextColored(GetColor(used, trueColor, falseColor), used.ToString());

                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text($"{time} min");
                }
                ImGui.EndTable();
            }

        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying potion status: {ex.Message}");
        }
    }

    #endregion
    #endregion
    #region Status Window Helper Methods

    private static Vector4 GetColor(bool condition, Vector4 trueColor, Vector4 falseColor)
        => condition ? trueColor : falseColor;

    private void InitializeColorData()
    {
        var shouldUseConditions = new[]
        {
            IsFirstCycle, TargetHasDoTs, CanEarlyWeave, CanLateWeave, EnoughWeaveTime
        };

        var statusConditions = new[]
        {
            HasRagingStrikes, HasBattleVoice, HasRadiantFinale, InWanderers, InMages, InArmys
        };

        _shouldUseConditions = shouldUseConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _statusConditions = statusConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
    }

    #endregion
}
