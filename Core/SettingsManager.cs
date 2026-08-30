using System;
using System.IO;

namespace Hermes_Executor.Core
{
    public class AppSettings
    {
        // Layout visibility settings
        public bool ShowSidebar { get; set; } = true;
        public bool ShowActivityBar { get; set; } = true;
        public bool ShowConsole { get; set; } = true;
        public bool ShowBottomPanel { get; set; } = false;

        // Panel size settings
        public double SidebarWidth { get; set; } = 240;
        public double ConsoleWidth { get; set; } = 300;
        public double BottomPanelHeight { get; set; } = 150;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Hermes-Executor",
            "settings.json"
        );

        public static AppSettings Current { get; set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                    if (loaded != null)
                    {
                        Current = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(Current, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
