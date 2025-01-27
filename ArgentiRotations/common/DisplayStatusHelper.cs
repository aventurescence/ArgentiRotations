using Dalamud.Interface.Utility;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

namespace ArgentiRotations.Common;

internal static class DisplayStatusHelper
{
    internal static float Scale => ImGuiHelpers.GlobalScale;

    static int _idScope;

    /// <summary>
    /// gets a unique id that can be used with ImGui.PushId() to avoid conflicts with type inspectors
    /// </summary>
    /// <returns></returns>
    internal static int GetScopeId() => _idScope++;

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

    internal static void SmallVerticalSpace() => ImGui.Dummy(new Vector2(0, 5));

    internal static void MediumVerticalSpace() => ImGui.Dummy(new Vector2(0, 10));

    internal static float GetPaddingX => ImGui.GetStyle().WindowPadding.X;

    /// <summary>
    /// adds a DrawList command to draw a border around the group
    /// </summary>
    public static void BeginBorderedGroup()
    {
        ImGui.BeginGroup();
    }

    public static void EndBorderedGroup() => EndBorderedGroup(new Vector2(3, 2), new Vector2(0, 3));

    public static void EndBorderedGroup(Vector2 minPadding, Vector2 maxPadding = default(Vector2))
    {
        ImGui.EndGroup();

        // attempt to size the border around the content to frame it
        var color = ImGui.GetStyle().Colors[(int) ImGuiCol.Border];

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        max.X = min.X + ImGui.GetContentRegionAvail().X;
        ImGui.GetWindowDrawList().AddRect(min - minPadding, max + maxPadding, ImGui.ColorConvertFloat4ToU32(color));

        // this fits just the content, not the full width
        //ImGui.GetWindowDrawList().AddRect( ImGui.GetItemRectMin() - padding, ImGui.GetItemRectMax() + padding, packedColor );
    }


    public static bool BeginPaddedChild(string str_id, bool border = false, ImGuiWindowFlags flags = 0)
    {
        float padding = ImGui.GetStyle().WindowPadding.X;
        // Set cursor position with padding
        float cursorPosX = ImGui.GetCursorPosX() + padding;
        ImGui.SetCursorPosX(cursorPosX);

        // Adjust the size to account for padding
        // Get the available size and adjust it to account for padding
        Vector2 size = ImGui.GetContentRegionAvail();
        size.X -= 2 * padding;
        size.Y -= 2 * padding;

        // Begin the child window
        return ImGui.BeginChild(str_id, size, border, flags);
    }

    public static void EndPaddedChild()
    {
        ImGui.EndChild();
    }

    public static void SetCursorMiddle()
    {
        float cursorPosX = ImGui.GetContentRegionAvail().X / 2;
        ImGui.SetCursorPosX(cursorPosX);
    }

    internal static void DrawItemMiddle(Action drawAction, float wholeWidth, float width, bool leftAlign = true)
    {
        if (drawAction == null) return;
        var distance = (wholeWidth - width) / 2;
        if (leftAlign) distance = MathF.Max(distance, 0);
        ImGui.SetCursorPosX(distance);
        drawAction();
    }
    const ImGuiWindowFlags TOOLTIP_FLAG =
          ImGuiWindowFlags.Tooltip |
          ImGuiWindowFlags.NoMove |
          ImGuiWindowFlags.NoSavedSettings |
          ImGuiWindowFlags.NoBringToFrontOnFocus |
          ImGuiWindowFlags.NoDecoration |
          ImGuiWindowFlags.NoInputs |
          ImGuiWindowFlags.AlwaysAutoResize;

    const string TOOLTIP_ID = "Churin Tooltips";

    public static void HoveredTooltip(string? text)
    {
        if (!ImGui.IsItemHovered())
        {
            return;
        }

        ShowTooltip(text);
    }

    public static void ShowTooltip(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        ///ShowTooltip(() => ImGui.Text(text));
        ShowTooltip(() => ImGui.TextColored(ImGuiColors.DalamudGrey2, text));
    }

    public static void ShowTooltip(Action act)
    {
        if (act == null)
        {
            return;
        }

        ImGui.SetNextWindowBgAlpha(1);

        using ImRaii.Color color = ImRaii.PushColor(ImGuiCol.BorderShadow, ImGuiColors.DalamudWhite);

        ImGui.SetNextWindowSizeConstraints(new Vector2(150, 0) * ImGuiHelpers.GlobalScale, new Vector2(1200, 1500) * ImGuiHelpers.GlobalScale);
        ImGui.SetWindowPos(TOOLTIP_ID, ImGui.GetIO().MousePos);

        if (ImGui.Begin(TOOLTIP_ID, TOOLTIP_FLAG))
        {
            act();
            ImGui.End();
        }
    }


}
