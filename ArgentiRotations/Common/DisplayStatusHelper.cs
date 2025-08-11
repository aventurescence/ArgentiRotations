using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace ArgentiRotations.Common;

internal static class DisplayStatusHelper
{
    private const ImGuiWindowFlags TooltipFlag =
        ImGuiWindowFlags.Tooltip |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.AlwaysAutoResize;

    private const string TooltipId = "Churin Tooltips";

    private static int _idScope;
    internal static float Scale => ImGuiHelpers.GlobalScale;

    internal static float GetPaddingX => ImGui.GetStyle().WindowPadding.X;

    /// <summary>
    ///     gets a unique id that can be used with ImGui.PushId() to avoid conflicts with type inspectors
    /// </summary>
    /// <returns></returns>
    internal static int GetScopeId()
    {
        return _idScope++;
    }

    internal static void TripleSpacing()
    {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
    }

    internal static void SeparatorWithSpacing()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    internal static void SmallVerticalSpace()
    {
        ImGui.Dummy(new Vector2(0, 5));
    }

    internal static void MediumVerticalSpace()
    {
        ImGui.Dummy(new Vector2(0, 10));
    }

    /// <summary>
    ///     adds a DrawList command to draw a border around the group
    /// </summary>
    public static void BeginBorderedGroup()
    {
        ImGui.BeginGroup();
    }

    public static void EndBorderedGroup()
    {
        EndBorderedGroup(new Vector2(3, 2), new Vector2(0, 3));
    }

    private static void EndBorderedGroup(Vector2 minPadding, Vector2 maxPadding = default)
    {
        ImGui.EndGroup();

        // attempt to size the border around the content to frame it
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Border];

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        max.X = min.X + ImGui.GetContentRegionAvail().X;
        ImGui.GetWindowDrawList().AddRect(min - minPadding, max + maxPadding, ImGui.ColorConvertFloat4ToU32(color));
    }


    public static void BeginPaddedChild(string strId, bool border = false, ImGuiWindowFlags flags = 0)
    {
        var padding = ImGui.GetStyle().WindowPadding.X;
        // Set cursor position with padding
        var cursorPosX = ImGui.GetCursorPosX() + padding;
        ImGui.SetCursorPosX(cursorPosX);

        // Adjust the size to account for padding
        // Get the available size and adjust it to account for padding
        var size = ImGui.GetContentRegionAvail();
        size.X -= 2 * padding;
        size.Y -= 2 * padding;

        // Begin the child window
        ImGui.BeginChild(strId, size, border, flags);
    }

    public static void EndPaddedChild()
    {
        ImGui.EndChild();
    }

    public static void SetCursorMiddle()
    {
        var cursorPosX = ImGui.GetContentRegionAvail().X / 2;
        ImGui.SetCursorPosX(cursorPosX);
    }

    internal static void DrawItemMiddle(Action? drawAction, float wholeWidth, float width, bool leftAlign = true)
    {
        if (drawAction == null) return;
        var distance = (wholeWidth - width) / 2;
        if (leftAlign) distance = MathF.Max(distance, 0);
        ImGui.SetCursorPosX(distance);
        drawAction();
    }

    public static void HoveredTooltip(string? text)
    {
        if (!ImGui.IsItemHovered()) return;

        ShowTooltip(text);
    }

    private static void ShowTooltip(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ShowTooltip(() => ImGui.TextColored(ImGuiColors.DalamudGrey2, text));
    }

    private static void ShowTooltip(Action? act)
    {
        if (act == null) return;

        ImGui.SetNextWindowBgAlpha(1);

        using var color = ImRaii.PushColor(ImGuiCol.BorderShadow, ImGuiColors.DalamudWhite);

        ImGui.SetNextWindowSizeConstraints(new Vector2(150, 0) * ImGuiHelpers.GlobalScale,
            new Vector2(1200, 1500) * ImGuiHelpers.GlobalScale);
        ImGui.SetWindowPos(TooltipId, ImGui.GetIO().MousePos);

        if (ImGui.Begin(TooltipId, TooltipFlag))
        {
            act();
            ImGui.End();
        }
    }
}