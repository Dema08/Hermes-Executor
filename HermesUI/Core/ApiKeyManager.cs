using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Hermes_Executor.Core
{
    public class ApiKeyManager
    {
        private const string REGISTRY_PATH = @"Software\Hermes-Executor";
        private const string REGISTRY_KEY = "RScriptApiKey";
        private const string CONFIG_FILE = "config.json";
        
        private const string HARDCODED_KEY = "rsc_live_o3J13DXjzec6b5EzEeglbuZq2a-uaol0";
        
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Hermes-Executor",
            CONFIG_FILE
        );

        public static void SaveApiKey(string apiKey)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
                {
                    key?.SetValue(REGISTRY_KEY, apiKey, RegistryValueKind.String);
                }
                SaveToFile(apiKey);
                Environment.SetEnvironmentVariable("RSCRIPT_API_KEY", apiKey, EnvironmentVariableTarget.Process);
            }
            catch { }
        }

        public static string LoadApiKey()
        {
            // 🔥 PRIORITAS 1: HARDCODE KEY (UNTUK TESTING)
            if (!string.IsNullOrEmpty(HARDCODED_KEY))
            {
                Environment.SetEnvironmentVariable("RSCRIPT_API_KEY", HARDCODED_KEY, EnvironmentVariableTarget.Process);
                return HARDCODED_KEY;
            }

            // PRIORITAS 2: Registry
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        var val = key.GetValue(REGISTRY_KEY) as string;
                        if (!string.IsNullOrEmpty(val))
                        {
                            Environment.SetEnvironmentVariable("RSCRIPT_API_KEY", val, EnvironmentVariableTarget.Process);
                            return val;
                        }
                    }
                }
            }
            catch { }

            // PRIORITAS 3: Config file
            var fileKey = LoadFromFile();
            if (!string.IsNullOrEmpty(fileKey))
            {
                Environment.SetEnvironmentVariable("RSCRIPT_API_KEY", fileKey, EnvironmentVariableTarget.Process);
                return fileKey;
            }

            // PRIORITAS 4: Environment
            var envKey = Environment.GetEnvironmentVariable("RSCRIPT_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                return envKey;
            }

            return null;
        }

        public static async Task<bool> ValidateApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return false;
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    var response = await client.GetAsync("https://api.rscript.org/v1/validate");
                    return response.IsSuccessStatusCode;
                }
            }
            catch { return false; }
        }

        private static void SaveToFile(string apiKey)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var config = new { ApiKey = apiKey, LastUpdated = DateTime.Now };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        private static string LoadFromFile()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    return config?.TryGetValue("ApiKey", out var key) == true ? key.ToString() : null;
                }
            }
            catch { }
            return null;
        }

        public static void DeleteApiKey()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, true))
                {
                    key?.DeleteValue(REGISTRY_KEY);
                }
                if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
                Environment.SetEnvironmentVariable("RSCRIPT_API_KEY", null, EnvironmentVariableTarget.Process);
            }
            catch { }
        }

        public static bool HasApiKey()
        {
            return !string.IsNullOrEmpty(LoadApiKey());
        }
    }
}
