﻿using ArgentiRotations.Common;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

public sealed partial class ChurinDNC
{
    #region Fields
    private Vector4[] _shouldUseConditions = [];
    private Vector4[] _statusConditions = [];

    #endregion
    #region Status Window Override

    public override void DisplayStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinDNC Status", true,
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
            InitializeColorData();
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var gcdsUntilTech = 0;
                for (uint i = 1; i <= 30; i++)
                {
                    if (TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
                    {
                        gcdsUntilTech = (int)i;
                        break;
                    }
                }
                ImGui.Columns(2, "GCDsUntilTechStep", false);
                ImGui.Text("GCDs Until Tech Step:");
                ImGui.NextColumn();
                ImGui.Text(gcdsUntilTech.ToString());
                ImGui.NextColumn();

                var elapsed = TechnicalStepPvE.Cooldown.HasOneCharge ||!TechnicalStepPvE.Cooldown.IsCoolingDown
                    ? 120f
                    : TechnicalStepPvE.Cooldown.RecastTimeElapsed + WeaponRemain;
                var diff = TechnicalStepCooldown - elapsed;

                var color = diff switch
                {
                    >= 120 and > 80 => ImGuiColors.DalamudRed,
                    <= 80 and > 40 => ImGuiColors.DalamudYellow,
                    _ => ImGuiColors.HealerGreen
                };

                ImGui.TextColored(ImGuiColors.ParsedGold, "Tech Step Recast Time:");
                ImGui.NextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold,$"{TechnicalStepCooldown:F1} seconds");
                ImGui.NextColumn();

                ImGui.TextColored(ImGuiColors.HealerGreen, "Tech Step Elapsed Time:");
                ImGui.NextColumn();
                ImGui.TextColored(ImGuiColors.HealerGreen,$"{elapsed:F1} seconds");
                ImGui.NextColumn();

                ImGui.TextColored(color, "Time until Tech Step:");
                ImGui.NextColumn();
                ImGui.TextColored(color,$"{diff:F1} seconds");
                ImGui.NextColumn();

                ImGui.TextColored(ImGuiColors.ParsedGold, "Technical Step Will Have One Charge in 0.5s");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[8], TechnicalStepPvE.Cooldown.WillHaveOneCharge( 0.5f).ToString());
                ImGui.NextColumn();

                ImGui.Columns(1);
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

                ImGui.TextColored(_shouldUseConditions[0], "Should Use Tech Step?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[0], ShouldUseTechStep.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[1], "Should Use Standard Step?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[1], ShouldUseStandardStep.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[2], "Can Use Saber Dance?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[2], TryUseSaberDance(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[3], "Should Use Starfall Dance?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[3], TryUseStarfallDance(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[4], "Should Use Last Dance?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[4], TryUseLastDance(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[5], "Should Use Flourish?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[5], TryUseFlourish(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[6], "Should Hold For Tech Step?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[6], CanUseTechnicalStep.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[7], "Should Hold For Standard Step?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[7], CanUseStandardStep.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[8], "Are Dance Targets In Range?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[8], AreDanceTargetsInRange.ToString());
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

                ImGui.TextColored(_statusConditions[0], "Has Tillana:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[0], HasTillana.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[1], "Has Last Dance:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[1], HasLastDance.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[2], "Has Starfall:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[2], HasStarfall.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[3], "Has Finishing Move:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[3], HasFinishingMove.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[4], "Has Any Proc");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[4], HasAnyProc.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[5], "Has Fourfold Fan Dance:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[5], HasFourfoldFanDance.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[6], "Has Threefold Fan Dance:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[6], HasThreefoldFanDance.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[7], "In Burst:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[7], IsBurstPhase.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[8], "Is Dancing:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[8], IsDancing.ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[9], "Is Medicated:");
                ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[9], IsMedicated.ToString());
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
        var shouldUseConditions = new[]
        {
            ShouldUseTechStep, ShouldUseStandardStep, TryUseSaberDance(out _), TryUseStarfallDance(out _),
            TryUseLastDance(out _), TryUseFlourish(out _), CanUseTechnicalStep, CanUseStandardStep, TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(1,0.5f),
            AreDanceTargetsInRange
        };

        var statusConditions = new[]
        {
            HasTillana, HasLastDance, HasStarfall, HasFinishingMove, HasAnyProc, HasFourfoldFanDance, HasThreefoldFanDance,
            IsBurstPhase, IsDancing, IsMedicated
        };

        _shouldUseConditions = shouldUseConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _statusConditions = statusConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
    }

    #endregion
}