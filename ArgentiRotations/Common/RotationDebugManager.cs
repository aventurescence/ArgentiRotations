using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;

namespace ArgentiRotations.Common;

public static class RotationDebugManager
{
    private static Dictionary<string, Dictionary<string, string>> DebugInfo { get; } = new();
    private static DateTime _lastDebugUpdateTime = DateTime.MinValue;
    private static DateTime _lastDebugClear = DateTime.MinValue;

    public static bool IsDebugTableVisible { get; set; }
    private static int MaxDebugEntries { get; set; } = 50;
    private static bool AutoClearDebugLogs { get; set; } = true;
    private static float DebugClearInterval { get; set; } = 30f;

    public static string CurrentGCDEvaluation { get; set; } = "No GCD Found";

    public static void CheckAndClearLogs()
    {
        if (AutoClearDebugLogs && (DateTime.Now - _lastDebugClear).TotalSeconds > DebugClearInterval)
        {
            DebugInfo.Clear();
            _lastDebugClear = DateTime.Now;
        }
    }

    // Use this in your Try* methods to wrap the method call
    public static bool Debug(Func<IAction?, bool> method, out IAction? act,
        [CallerMemberName] string methodName = "")
    {
        // Skip if debugging is disabled
        if (!IsDebugTableVisible && (DateTime.Now - _lastDebugUpdateTime).TotalSeconds > 0.5)
        {
            act = null;
            return method.Invoke(act);
        }

        var debug = new Dictionary<string, string>();
        try
        {
            // Update current evaluation
            CurrentGCDEvaluation = $"Trying: {methodName}";

            // Execute the method
            act = null;
            var startTime = DateTime.Now;
            var result = method.Invoke(act);
            var elapsed = DateTime.Now - startTime;

            // Log information
            debug["Method"] = methodName;
            debug["Result"] = result.ToString();
            debug["Action"] = act?.ToString() ?? "null";
            debug["Time"] = $"{elapsed.TotalMilliseconds:F2}ms";
            debug["Timestamp"] = DateTime.Now.ToString("HH:mm:ss.fff");

            if (result)
                CurrentGCDEvaluation = $"Using: {methodName}";

            // Update debug info
            if (DebugInfo.Count >= MaxDebugEntries && !DebugInfo.ContainsKey(methodName))
            {
                var oldestEntry = DebugInfo.Keys.FirstOrDefault();
                if (oldestEntry != null)
                    DebugInfo.Remove(oldestEntry);
            }

            DebugInfo[methodName] = debug;
            _lastDebugUpdateTime = DateTime.Now;

            return result;
        }
        catch (Exception ex)
        {
            debug["Method"] = methodName;
            debug["Error"] = ex.Message;
            debug["Timestamp"] = DateTime.Now.ToString("HH:mm:ss.fff");
            DebugInfo[methodName] = debug;
            act = null;
            return false;
        }
    }

    public static void AddExtraDebugInfo(string methodName, string key, string value)
    {
        if (!DebugInfo.ContainsKey(methodName))
        {
            DebugInfo[methodName] = new Dictionary<string, string>();
        }

        DebugInfo[methodName][key] = value;
    }

   public static void DrawGCDMethodDebugTable()
   {
       if (!IsDebugTableVisible) return;

       ImGui.TextColored(ImGuiColors.DalamudYellow, "Current GCD Path: " + CurrentGCDEvaluation);

       if (ImGui.BeginTable("##DebugMethodTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
       {
           ImGui.TableSetupColumn("Method", ImGuiTableColumnFlags.WidthFixed, 200);
           ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthFixed, 150);
           ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
           ImGui.TableHeadersRow();

           foreach (var (methodName, debug) in DebugInfo)
           {
               ImGui.TableNextRow();
               ImGui.TableNextColumn();
               var headerOpen = ImGui.TreeNode(methodName);
               ImGui.TableNextColumn();
               ImGui.Text("");
               ImGui.TableNextColumn();
               ImGui.Text("");

               if (headerOpen)
               {
                   foreach (var (key, value) in debug)
                   {
                       ImGui.TableNextRow();
                       ImGui.TableNextColumn();
                       ImGui.Text(""); // Empty method column for detail row
                       ImGui.TableNextColumn();
                       ImGui.Text(key);
                       ImGui.TableNextColumn();
                       ImGui.Text(value);
                   }
                   ImGui.TreePop();
               }
           }
           ImGui.EndTable();
       }
   }
}