using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeilVoice.Core.Models
{
    /// <summary>
    /// Represents a Beatrice voice model manifest (Contract SECTION 27).
    /// </summary>
    public class ModelManifest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("engine")]
        public string Engine { get; set; } = "Beatrice";

        [JsonPropertyName("model_path")]
        public string ModelPath { get; set; } = string.Empty;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; } = 48000;

        [JsonPropertyName("speaker_id")]
        public string SpeakerId { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;

        [JsonPropertyName("compatible_engine_versions")]
        public List<string> CompatibleEngineVersions { get; set; } = new() { "Beatrice v2" };

        [JsonIgnore]
        public string? ResolvedAbsolutePath { get; set; }

        [JsonIgnore]
        public bool IsVerified { get; set; }
    }
}
