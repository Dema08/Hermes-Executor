using System;
using System.IO;
using System.Text.Json;

namespace Hermes_Executor.Core {
    public class SettingsManager {
        private static SettingsManager? _instance;
        private static readonly object _lock = new object();
        private const string SettingsFile = "settings.json";

        public static SettingsManager Current {
            get {
                lock (_lock) {
                    _instance ??= Load();
                    return _instance;
                }
            }
            set {
                lock (_lock) {
                    _instance = value;
                }
            }
        }

        // Properti Settings
        public double SidebarWidth { get; set; } = 240;
        public double ConsoleWidth { get; set; } = 400;
        public double BottomPanelHeight { get; set; } = 200;
        public bool ShowSidebar { get; set; } = true;
        public bool ShowActivityBar { get; set; } = true;
        public bool ShowConsole { get; set; } = true;
        public bool ShowBottomPanel { get; set; } = true;
        public string? LastScriptPath { get; set; }
        public string? Theme { get; set; } = "Dark";

        public static SettingsManager Load() {
            try {
                if (File.Exists(SettingsFile)) {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<SettingsManager>(json) ?? new SettingsManager();
                }
            } catch (Exception ex) {
                Console.WriteLine($"Failed to load settings: {ex.Message}");
            }
            return new SettingsManager();
        }

        public void SaveToFile() {
            try {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            } catch (Exception ex) {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static void Save() {
            Current.SaveToFile();
        }

        public static void SaveCurrent() {
            Save();
        }
    }
}
