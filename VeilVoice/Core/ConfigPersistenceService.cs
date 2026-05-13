using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeilVoice.Core
{
    public class VeilVoiceConfig
    {
        [JsonPropertyName("input_device_id")]
        public string? InputDeviceId { get; set; }

        [JsonPropertyName("input_device_name")]
        public string? InputDeviceName { get; set; }

        [JsonPropertyName("output_device_id")]
        public string? OutputDeviceId { get; set; }

        [JsonPropertyName("output_device_name")]
        public string? OutputDeviceName { get; set; }

        [JsonPropertyName("model_path")]
        public string ModelPath { get; set; } = string.Empty;

        [JsonPropertyName("model_sha256")]
        public string? ModelSha256 { get; set; }

        [JsonPropertyName("input_gain")]
        public float InputGain { get; set; } = 1.0f;

        [JsonPropertyName("output_gain")]
        public float OutputGain { get; set; } = 1.0f;

        [JsonPropertyName("app_version")]
        public string AppVersion { get; set; } = "2.0.0";

        [JsonPropertyName("saved_at")]
        public string SavedAt { get; set; } = DateTime.UtcNow.ToString("O");
    }





    public static class ConfigPersistenceService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VeilVoice");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static VeilVoiceConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var cfg = JsonSerializer.Deserialize<VeilVoiceConfig>(json, JsonOpts);
                    if (cfg != null)
                    {
                        LogService.Info($"[Config] Loaded from {ConfigPath}");
                        return cfg;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Warn($"[Config] Load failed: {ex.Message}. Using defaults.");
            }

            return new VeilVoiceConfig();
        }

        public static void Save(VeilVoiceConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                config.SavedAt = DateTime.UtcNow.ToString("O");
                string json = JsonSerializer.Serialize(config, JsonOpts);
                File.WriteAllText(ConfigPath, json);
                LogService.Info($"[Config] Saved to {ConfigPath}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[Config] Save failed: {ex.Message}");
            }
        }




        public static string ConfigFilePath => ConfigPath;
    }
}
