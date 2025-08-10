using ArgentiRotations.Common;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Tank;

public sealed partial class ChurinDRK
{
    #region Fields
    private Vector4[] _statusConditions = [];

    #endregion
    #region Status Window Override
    public override void DisplayRotationStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinDrk Status", true,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            DrawRotationStatus();
            DrawPartyCompositionHeader();
            DrawCombatStatusHeader();
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
                    .Select(b => b.Name)
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
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns();
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

                // Reset columns
                ImGui.Columns();
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

                ImGui.TextColored(_statusConditions[0], "Is Medicated:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[0], IsMedicated.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[1], "In Burst Window:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[1], InBurstWindow.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[2], "Has Party Buffs:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[2], HasBuffs.ToString());
                ImGui.NextColumn();

                // Reset columns
                ImGui.Columns();
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Active Buffs/Debuffs"))
            {
                ArgentiRotations.Common.PartyComposition.StatusList();

                if (ArgentiRotations.Common.PartyComposition.Buffs.Count == 0)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "No active buffs or debuffs found.");
                }
                else if (ImGui.BeginTable("ActiveBuffsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Type");
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Source");
                    ImGui.TableHeadersRow();

                    var activeBuffs = ArgentiRotations.Common.PartyComposition.Buffs.Where(b =>
                        b.Type == StatusType.Buff && Player.HasStatus(false, b.Ids) &&
                        !Player.WillStatusEnd(0, false, b.Ids));
                    var activeDebuffs = ArgentiRotations.Common.PartyComposition.Buffs.Where(b =>
                        b.Type == StatusType.Debuff && HostileTarget != null && HostileTarget.HasStatus(false, b.Ids) &&
                        !HostileTarget.WillStatusEnd(0, false, b.Ids));

                    var hasActiveEffects = false;

                    foreach (var buff in activeBuffs)
                    {
                        hasActiveEffects = true;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextColored(ImGuiColors.HealerGreen, "Buff");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(buff.Name);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text(buff.JobAbbr);
                    }

                    foreach (var debuff in activeDebuffs)
                    {
                        hasActiveEffects = true;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Debuff");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(debuff.Name);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text(debuff.JobAbbr);
                    }

                    if (!hasActiveEffects)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextColored(ImGuiColors.DalamudRed, "No active buffs or debuffs found.");
                        ImGui.TableSetColumnIndex(2);
                    }

                    ImGui.EndTable();
                }

                ImGui.TreePop();
            }

        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying combat status");
        }
    }

    #endregion
    #region Potion Status
        private void DrawPotionStatusHeader()
        {
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Potion Status", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawPotionStatusText();
            }
            ImGui.EndGroup();
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

            var statusConditions = new[]
            {
                IsMedicated, InBurstWindow, HasBuffs
            };

            _statusConditions = statusConditions
                .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        }



        #endregion
}