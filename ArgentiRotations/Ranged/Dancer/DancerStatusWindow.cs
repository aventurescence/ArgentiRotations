using ArgentiRotations.Common;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Ranged;

public sealed partial class ChurinDNC
{
    #region Fields
    private Vector4[] _oddPotionColors = [];
    private Vector4[] _enablePotionColors = [];
    private Vector4[] _usedPotionColors = [];
    private Vector4[] _conditionColors = [];
    private Vector4[] _shouldUseConditions = [];
    private Vector4[] _statusConditions = [];
    private Vector4[] _timeColors = [];
    private Vector4[] _oddPotionConditionColors = [];
    private Vector4[] _evenPotionConditionColors = [];
    private bool[] _isOddPotions = [];
    private bool[] _timeConditions = [];
    private bool[] _conditionValues = [];
    private bool[] _isOddConditionsList = [];
    private bool[] _isEvenConditionsList = [];



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
            InitializeColorData();
            if (ImGui.TreeNode("Should Use Booleans"))
            {
                ImGui.Columns(2, "CombatStatusColumns", false);

                // Column headers
                ImGui.Text("Status"); ImGui.NextColumn();
                ImGui.Text("Value"); ImGui.NextColumn();
                ImGui.Separator();

                ImGui.TextColored(_shouldUseConditions[0],"Should Use Tech Step?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[0],ShouldUseTechStep.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[1],"Should Use Standard Step?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[1],ShouldUseStandardStep.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[2],"Should Use Saber Dance?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[2],ShouldUseSaberDance.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[3],"Should Use Starfall Dance?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[3],ShouldUseStarfallDance.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[4],"Should Use Last Dance?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[4],ShouldUseLastDance.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[5],"Should Use Flourish?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[5],ShouldUseFlourish.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[6],"Should Hold For Tech Step?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[6],ShouldHoldForTechStep.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_shouldUseConditions[7],"Should Hold For Standard Step?"); ImGui.NextColumn();
                ImGui.TextColored(_shouldUseConditions[7],ShouldHoldForStandard.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[0],"In Burst:"); ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[0],DanceDance.ToString()); ImGui.NextColumn();

                ImGui.TextColored(_statusConditions[1],"Is Dancing:"); ImGui.NextColumn();
                ImGui.TextColored(_statusConditions[1],IsDancing.ToString()); ImGui.NextColumn();

                // Reset columns
                ImGui.Columns(1);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Potion Status"))
            {
                var text = "Combat Time: " + CombatTime.ToString("F");
                var textSize = ImGui.CalcTextSize(text).X;
                DisplayStatusHelper.DrawItemMiddle(() => { ImGui.TextColored(ImGuiColors.ParsedPink, text); },
                    ImGui.GetWindowWidth(), textSize);
                var text2 = "Last Potion Used:" + LastPotionUsed.ToString("F1");
                var textSize2 = ImGui.CalcTextSize(text2).X;
                DisplayStatusHelper.DrawItemMiddle(() => { ImGui.TextColored(ImGuiColors.ParsedPink, text2); },
                    ImGui.GetWindowWidth(), textSize2);
                var text3 = "Can Use Potion: " + UseBurstMedicine(out _);
                var textSize3 = ImGui.CalcTextSize(text3).X;
                DisplayStatusHelper.DrawItemMiddle(() => { ImGui.TextColored(ImGuiColors.ParsedPink, text3); },
                    ImGui.GetWindowWidth(), textSize3);
                // First Potion
                if (ImGui.TreeNode($"First Potion Status:{FirstPot(out _).ToString()}"))
                {
                    ImGui.Columns(2, "FirstPotionColumns", false);
                    ImGui.Text("Condition"); ImGui.NextColumn();
                    ImGui.Text("Value"); ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.TextColored(_enablePotionColors[0], "First Potion Enabled"); ImGui.NextColumn();
                    ImGui.TextColored(_enablePotionColors[0], EnableFirstPotion.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[0], "First Potion Used"); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[0], FirstPotionUsed.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[0], "First Potion Timing"); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[0], IsOdd(FirstPotionTime) ? "Odd :" : "Even :"); ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedOrange, $"{FirstPotionTime * 60} seconds");ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[0], "Is Time Condition Met:"); ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[0], _timeConditions[0].ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[0], "Condition:"); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[0], _isOddPotions[0].ToString()); ImGui.NextColumn();
                    DrawParityConditions(IsOdd(FirstPotionTime), _oddPotionConditionColors, _evenPotionConditionColors,
                        _isOddConditionsList, _isEvenConditionsList);
                    ImGui.Columns(1);
                    ImGui.TreePop();
                }

                // Second Potion
                if (ImGui.TreeNode($"Second Potion Status:{SecondPot(out _).ToString()}"))
                {
                    ImGui.Columns(2, "SecondPotionColumns", false);
                    ImGui.Text("Condition"); ImGui.NextColumn();
                    ImGui.Text("Value"); ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.TextColored(_enablePotionColors[1], "Second Potion Enabled"); ImGui.NextColumn();
                    ImGui.TextColored(_enablePotionColors[1], EnableSecondPotion.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[1], "Second Potion Used"); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[1], SecondPotionUsed.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[1], "Second Potion Timing"); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[1], IsOdd(SecondPotionTime) ? "Odd :" : "Even :"); ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedOrange, $"{SecondPotionTime * 60} seconds");ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[1], "Is Time Condition Met:"); ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[1], _timeConditions[1].ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[1], "Condition:"); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[1], _isOddPotions[1].ToString()); ImGui.NextColumn();
                    DrawParityConditions(IsOdd(SecondPotionTime), _oddPotionConditionColors, _evenPotionConditionColors,
                        _isOddConditionsList, _isEvenConditionsList);
                    ImGui.Columns(1);
                    ImGui.TreePop();
                }

                // Third Potion
                if (ImGui.TreeNode($"Third Potion Status:{ThirdPot(out _).ToString()}"))
                {
                    ImGui.Columns(2, "ThirdPotionColumns", false);
                    ImGui.Text("Condition"); ImGui.NextColumn();
                    ImGui.Text("Value"); ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.TextColored(_enablePotionColors[2], "Third Potion Enabled"); ImGui.NextColumn();
                    ImGui.TextColored(_enablePotionColors[2], EnableThirdPotion.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[2], "Third Potion Used"); ImGui.NextColumn();
                    ImGui.TextColored(_usedPotionColors[2], ThirdPotionUsed.ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[2], "Third Potion Timing"); ImGui.NextColumn();
                    ImGui.TextColored(_oddPotionColors[2], IsOdd(ThirdPotionTime) ? "Odd :" : "Even :"); ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedOrange, $"{ThirdPotionTime * 60} seconds");ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[2], "Is Time Condition Met:"); ImGui.NextColumn();
                    ImGui.TextColored(_timeColors[2], _timeConditions[2].ToString()); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[2], "Condition:"); ImGui.NextColumn();
                    ImGui.TextColored(_conditionColors[2], _isOddPotions[2].ToString()); ImGui.NextColumn();
                    DrawParityConditions(IsOdd(ThirdPotionTime), _oddPotionConditionColors, _evenPotionConditionColors,
                        _isOddConditionsList, _isEvenConditionsList);
                    ImGui.Columns(1);
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }
            ImGui.TreePop();
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

    #region Status Window Helper Methods

    private static Vector4 GetColor(bool condition, Vector4 trueColor, Vector4 falseColor)
        => condition ? trueColor : falseColor;
    private void InitializeColorData()
    {
        var potionTimes = new[] { FirstPotionTime, SecondPotionTime, ThirdPotionTime };
        var enablePotions = new[] { EnableFirstPotion, EnableSecondPotion, EnableThirdPotion };
        var usedPotions = new[] { FirstPotionUsed, SecondPotionUsed, ThirdPotionUsed };
        var shouldUseConditions = new [] { ShouldUseTechStep, ShouldUseStandardStep, ShouldUseSaberDance,
            ShouldUseStarfallDance, ShouldUseLastDance, ShouldUseFlourish, ShouldHoldForTechStep, ShouldHoldForStandard };
        var statusConditions = new[] { DanceDance, IsDancing };
        var oddPotionConditionList = new[]
            { FlourishPvE.Cooldown is { IsCoolingDown: true, RecastTimeElapsed: >= 50 }, FlourishPvE.Cooldown.IsCoolingDown && FlourishPvE.Cooldown.WillHaveOneCharge(5) };
        var evenPotionConditionList = new[]
            { IsLastGCD(ActionID.TechnicalStepPvE), HasTechnicalStep && IsDancing && CompletedSteps is 0 or 3 or 4, HasTechnicalStep };

        _oddPotionColors = potionTimes.Select(IsOdd)
            .Select(isOdd => GetColor(isOdd, ImGuiColors.HealerGreen, ImGuiColors.TankBlue)).ToArray();
        _enablePotionColors = enablePotions
            .Select(enabled => GetColor(enabled, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _usedPotionColors = usedPotions
            .Select(used => GetColor(used, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _shouldUseConditions = shouldUseConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _statusConditions = statusConditions
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _oddPotionConditionColors = oddPotionConditionList
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
        _evenPotionConditionColors = evenPotionConditionList
            .Select(condition => GetColor(condition, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();

        var oddPotionCondition = OddPotionCondition;
        var evenPotionCondition = EvenPotionCondition;

        _isOddPotions = potionTimes.Select(IsOdd).ToArray();
        _isOddConditionsList = oddPotionConditionList.Select(condition => condition).ToArray();
        _isEvenConditionsList = evenPotionConditionList.Select(condition => condition).ToArray();
        _conditionValues = _isOddPotions.Select(isOdd => isOdd ? oddPotionCondition : evenPotionCondition).ToArray();
        _conditionColors = _conditionValues
            .Select(val => GetColor(val, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();

        var combatTimes = new[] { FirstPotionTime * 60, SecondPotionTime * 60, ThirdPotionTime * 60 };
        _timeConditions =
        [
            CombatTime >= combatTimes[0] || combatTimes[0] == 0,
            CombatTime >= combatTimes[1] && CombatTime <= combatTimes[1] + 10,
            CombatTime >= combatTimes[2] && CombatTime <= combatTimes[2] + 10
        ];
        _timeColors = _timeConditions.Select(val => GetColor(val, ImGuiColors.HealerGreen, ImGuiColors.DalamudRed)).ToArray();
    }

    private static void DrawParityConditions(bool isOdd, Vector4[] oddColors, Vector4[] evenColors,bool[] oddList, bool[] evenList)
    {
        if (isOdd)
        {
            ImGui.TextColored(oddColors[0], "Flourish Recast Elapsed is more than 50 seconds"); ImGui.NextColumn();
            ImGui.TextColored(oddColors[0], oddList[0].ToString()); ImGui.NextColumn();
            ImGui.TextColored(oddColors[1], "Flourish Will Have One Charge in  5 seconds"); ImGui.NextColumn();
            ImGui.TextColored(oddColors[1], oddList[1].ToString());
        }
        else
        {
            ImGui.TextColored(evenColors[0], "Last GCD was Technical Step"); ImGui.NextColumn();
            ImGui.TextColored(evenColors[0], evenList[0].ToString()); ImGui.NextColumn();
            ImGui.TextColored(evenColors[1], "Has Technical Step and Is Dancing and Completed Steps is 0, 3, or 4"); ImGui.NextColumn();
            ImGui.TextColored(evenColors[1], evenList[1].ToString()); ImGui.NextColumn();
            ImGui.TextColored(evenColors[2], "Has Technical Step"); ImGui.NextColumn();
            ImGui.TextColored(evenColors[2], evenList[2].ToString());
        }

        ImGui.NextColumn();
    }

    #endregion
}