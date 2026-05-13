using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeilVoice.Core
{
    public class VeilVoiceConfig
    {
        [JsonPropertyName("input_device_id")]
        public string? InputDeviceId { get; init; }

        [JsonPropertyName("input_device_name")]
        public string? InputDeviceName { get; init; }

        [JsonPropertyName("output_device_id")]
        public string? OutputDeviceId { get; init; }

        [JsonPropertyName("output_device_name")]
        public string? OutputDeviceName { get; init; }

        [JsonPropertyName("model_path")]
        public string ModelPath { get; init; } = string.Empty;

        [JsonPropertyName("model_sha256")]
        public string? ModelSha256 { get; init; }

        [JsonPropertyName("input_gain")]
        public float InputGain { get; init; } = 1.0f;

        [JsonPropertyName("output_gain")]
        public float OutputGain { get; init; } = 1.0f;

        [JsonPropertyName("app_version")]
        public string AppVersion { get; init; } = "2.0.0";

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
            if (!File.Exists(ConfigPath)) return new VeilVoiceConfig();
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<VeilVoiceConfig>(json, JsonOpts) ?? new VeilVoiceConfig();
        }

        public static void Save(VeilVoiceConfig config)
        {
            Directory.CreateDirectory(ConfigDir);
            config.SavedAt = DateTime.UtcNow.ToString("O");
            string json = JsonSerializer.Serialize(config, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }

        public static string ConfigFilePath => ConfigPath;
    }
}
