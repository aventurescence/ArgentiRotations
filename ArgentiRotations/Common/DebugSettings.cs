using System.Text.Json;
            using ArgentiRotations.Ranged;
            using Dalamud.Configuration;
            using ECommons.Logging;

            namespace ArgentiRotations.Common;

            public class DebugSettings: IDisposable
            {
                public bool AutoClearDebugLogs { get; set; } = false;
                public int DebugClearInterval { get; set; } = 5;
                public int MaxDebugEntries { get; set; } = 50;
                public bool CondenseEntries { get; set; } = true;

                private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

                public static void SaveDebugInfoAsJson()
                {
                    try
                    {
                        // Safely collect debug data
                        List<Dictionary<string, object>> debugEntries;
                        try
                        {
                            debugEntries = ChurinDNC.GetGCDMethodDebugInfo() ?? [];
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Error($"Error collecting debug data: {ex.Message}");
                            debugEntries = [];
                        }

                        // Create a timestamped filename
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var fileName = $"DNCDebug_{timestamp}.json";
                        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "FFXIVRotationLogs", fileName);

                        // Ensure directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                        // Prepare the debug data with additional metadata
                        var debugData = new Dictionary<string, object>
                        {
                            ["Timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["RotationName"] = "Churin DNC",
                            ["DebugEntries"] = debugEntries
                        };

                        // Serialize and write to file
                        var json = System.Text.Json.JsonSerializer.Serialize(debugData, JsonOptions);
                        File.WriteAllText(path, json);

                        // Provide feedback in logs
                        PluginLog.Information($"Debug info saved to {path}");
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Failed to save debug info: {ex.Message}");
                    }
                }

                public void SaveSettings()
                {
                    try
                    {
                        var settings = new Dictionary<string, object>
                        {
                            ["AutoClearDebugLogs"] = AutoClearDebugLogs,
                            ["DebugClearInterval"] = DebugClearInterval,
                            ["MaxDebugEntries"] = MaxDebugEntries,
                            ["CondenseEntries"] = CondenseEntries
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(settings, JsonOptions);
                        File.WriteAllText("DebugSettings.json", json);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Failed to save settings: {ex.Message}");
                    }
                }

                public void Dispose()
                {
                    try
                    {
                        GC.SuppressFinalize(this);
                        ChurinDNC.GCDMethodDebugInfo.Clear();
                        ChurinDNC.IsDebugTableVisible = false;
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Error during Dispose: {ex.Message}");
                    }
                }

            }