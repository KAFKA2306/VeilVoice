using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeilVoice.Core.Models
{
    public class ModelManifest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; init; } = string.Empty;

        [JsonPropertyName("engine")]
        public string Engine { get; init; } = "Beatrice";

        [JsonPropertyName("model_path")]
        public string ModelPath { get; init; } = string.Empty;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; init; } = 48000;

        [JsonPropertyName("speaker_id")]
        public string SpeakerId { get; init; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; init; } = string.Empty;

        [JsonPropertyName("compatible_engine_versions")]
        public List<string> CompatibleEngineVersions { get; init; } = new() { "Beatrice v2" };

        [JsonIgnore]
        public string? ResolvedAbsolutePath { get; set; }

        [JsonIgnore]
        public bool IsVerified { get; set; }
    }
}
