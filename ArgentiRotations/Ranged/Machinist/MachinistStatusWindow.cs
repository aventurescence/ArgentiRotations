using ArgentiRotations.Common;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

public sealed partial class ChurinMCH
{
    #region Fields
    private Vector4[] _shouldUseConditions = [];
    private Vector4[] _hyperChargeConditions = [];
    private const float WildfireCooldown = 120;
    #endregion
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

    private void DrawCombatStatusHeader()
    {
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("Combat Status Details", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var gcdsUntilWildfire = 0;
            for (uint i = 1; i <= 30; i++)
            {
                if (WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
                {
                    gcdsUntilWildfire = (int)i;
                    break;
                }
            }
            var elapsed = WildfirePvE.Cooldown.HasOneCharge || !WildfirePvE.Cooldown.IsCoolingDown
                ? 120f
                : WildfirePvE.Cooldown.RecastTimeElapsed + WeaponRemain;
            var diff = WildfireCooldown - elapsed;

            var color = diff switch
            {
                >= 120 and > 80 => ImGuiColors.DalamudRed,
                <= 80 and > 40 => ImGuiColors.DalamudYellow,
                _ => ImGuiColors.HealerGreen
            };
            ImGui.Columns(2, "GCDsUntilWildfire", false);
            ImGui.Text("GCDs Until Wildfire:");
            ImGui.NextColumn();
            ImGui.Text(gcdsUntilWildfire.ToString());
            ImGui.NextColumn();
            ImGui.TextColored(color, "Time Until Wildfire:");
            ImGui.NextColumn();
            ImGui.Text($"{diff:F1} seconds");
            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.ParsedGold, $"Last Summon Battery Power: {LastSummonBatteryPower}");
            ImGui.Columns();
            DrawCombatStatusText();
        }
        ImGui.EndGroup();
    }

    private void DrawHyperChargeStatusHeader()
    {
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("Hyper Charge Tracking", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawHyperChargeStatusText();
        }
        ImGui.EndGroup();
    }

    private void DrawHyperChargeStatusText()
    {
        try
        {
            InitializeColorData();
            if (ImGui.BeginTable("HyperChargeTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Property");
                ImGui.TableSetupColumn("Value");
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Is Burst Window");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(_hyperChargeConditions[0], IsBurstWindow().ToString());

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Is Heat Overcap Risk");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(_hyperChargeConditions[1], IsHeatOvercapRisk().ToString());

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Is Optimal Heat Threshold");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(_hyperChargeConditions[2], IsOptimalHeatThreshold().ToString());

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Are Tools Coming Off Cooldown");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(_hyperChargeConditions[3], AreToolsComingOffCooldown().ToString());

                ImGui.EndTable();
            }

        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying Hypercharge tracking: {ex.Message}");
        }
    }

    private void DrawPotionStatusHeader()
    {
        ImGui.BeginGroup();
        if (ImGui.CollapsingHeader("Potion Status", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawPotionStatusText();
        }
        ImGui.EndGroup();
    }

    private void DrawQueenStepHeader()
    {
        try
        {
            ImGui.BeginGroup();
            if (ImGui.CollapsingHeader("Queen Step Tracking", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Calculate next target battery
                byte nextTargetBattery = 0;
                if (_queenStep < _queenStepPairs.Length)
                {
                    nextTargetBattery = _queenStepPairs[_queenStep].to;
                }

                var readyForQueen = Battery >= nextTargetBattery;

                if (ImGui.BeginTable("QueenStepTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Property");
                    ImGui.TableSetupColumn("Value");
                    ImGui.TableHeadersRow();

                    // Queen Step
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Current Queen Step");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{_queenStep}/{_queenStepPairs.Length}");

                    //Odd Minute Queen
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Last Odd Minute Queen");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{_lastOddQueenBattery}");
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Next Odd Minute Queen");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{_nextOddQueenBattery}");

                    // Battery Status
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Battery Status");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextColored(readyForQueen ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite,
                        $"{Battery}/{nextTargetBattery}");

                    // Target Pair
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Target Battery Range");
                    ImGui.TableSetColumnIndex(1);
                    if (_queenStep < _queenStepPairs.Length)
                    {
                        ImGui.Text($"{_queenStepPairs[_queenStep].from} → {_queenStepPairs[_queenStep].to}");
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudYellow, "Beyond defined steps");
                    }

                    // Queen Status
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Queen Status");
                    ImGui.TableSetColumnIndex(1);
                    if (IsRobotActive)
                    {
                        ImGui.TextColored(ImGuiColors.ParsedOrange, "Active");
                    }
                    else if (RookAutoturretPvE.Cooldown.IsCoolingDown)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "On Cooldown");
                    }
                    else if (readyForQueen)
                    {
                        ImGui.TextColored(ImGuiColors.HealerGreen, "Ready to Deploy");
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudWhite, "Charging Battery");
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndGroup();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying Queen step tracking: {ex.Message}");
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
                ImGui.Text("Status");
                ImGui.NextColumn();
                ImGui.Text("Value");
                ImGui.NextColumn();
                ImGui.Separator();

                ImGui.TextColored(_shouldUseConditions[0], "Can Use Wildfire?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[0], TryUseWildfire(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[1], "Should Use Queen?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[1], TryUseQueen(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[2], "Should Use Drill?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[2], TryUseDrill(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[3], "Should Use Chainsaw?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[3], TryUseChainSaw(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[4], "Should Use Excavator?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[4], TryUseExcavator(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[5], "Should Use Air Anchor?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[5], TryUseAirAnchor(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[6], "Should Use Full Metal Field?");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[6], TryUseFullMetalField(out _).ToString());
                ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[7], "Is Overheated?:");
                ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[7], HasOverheated.ToString());
                ImGui.NextColumn();

                // Reset columns
                ImGui.Columns();
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }
        catch (Exception)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Error displaying combat status");
        }
    }

    private void DrawPotionStatusText()
    {
        try
        {
            if (ImGui.BeginTable("PotionStatusTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Potion");
                ImGui.TableSetupColumn("Enabled");
                ImGui.TableSetupColumn("Used");
                ImGui.TableSetupColumn("Can Use");
                ImGui.TableSetupColumn("Condition");
                ImGui.TableHeadersRow();

                var trueColor = ImGuiColors.HealerGreen;
                var falseColor = ImGuiColors.DalamudRed;

                for (var i = 0; i < _potions.Count; i++)
                {
                    var (time, enabled, used) = _potions[i];
                    var potionTimeInSeconds = time * 60;
                    var isOpenerPotion = potionTimeInSeconds == 0;
                    var isEvenMinutePotion = time % 2 == 0;

                    var canUse = (isOpenerPotion && Countdown.TimeRemaining <= 1.5f) ||
                                 (!isOpenerPotion && CombatTime >= potionTimeInSeconds &&
                                  CombatTime < potionTimeInSeconds + 10);

                    var condition = (isEvenMinutePotion ? InTwoMinuteWindow : InOddMinuteWindow) || isOpenerPotion;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"Potion {i + 1}");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextColored(GetColor(enabled, trueColor, falseColor), enabled.ToString());

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextColored(GetColor(used, trueColor, falseColor), used.ToString());

                    ImGui.TableSetColumnIndex(3);
                    ImGui.TextColored(GetColor(canUse, trueColor, falseColor), canUse.ToString());

                    ImGui.TableSetColumnIndex(4);
                    ImGui.TextColored(GetColor(condition, trueColor, falseColor), condition.ToString());
                }
                ImGui.EndTable();
            }
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying potion status: {ex.Message}");
        }
    }


    public override void DisplayRotationStatus()
    {
        try
        {
            DisplayStatusHelper.BeginPaddedChild("ChurinDNC Status", true,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            DrawRotationStatus();
            DrawQueenStepHeader();
            DrawHyperChargeStatusHeader();
            DrawCombatStatusHeader();
            DrawPotionStatusHeader();
            DisplayStatusHelper.EndPaddedChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Error displaying status: {ex.Message}");
        }
    }

    #endregion
    #region Status Window Helper Methods

    private static Vector4 GetColor(bool condition, Vector4 trueColor, Vector4 falseColor)
        => condition ? trueColor : falseColor;
    private void InitializeColorData()
    {
        var shouldUseConditions = new[]
        {
            TryUseWildfire(out _), TryUseQueen(out _), TryUseDrill(out _), TryUseChainSaw(out _), TryUseExcavator(out _), TryUseAirAnchor(out _),
            TryUseFullMetalField(out _), IsOverheated
        };
        var hyperChargeConditions = new[]
        {
            IsBurstWindow(), IsHeatOvercapRisk(), IsOptimalHeatThreshold(), AreToolsComingOffCooldown()
        };

        _shouldUseConditions = shouldUseConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _hyperChargeConditions = hyperChargeConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
    }

    #endregion
}