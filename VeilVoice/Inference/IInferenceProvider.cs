using System;
using VeilVoice.Core.Models;

namespace VeilVoice.Inference
{
    /// <summary>
    /// Core interface for voice inference engines.
    /// Supports runtime model management and manifest-based verification.
    /// </summary>
    public interface IInferenceProvider : IDisposable
    {
        /// <summary>The manifest associated with this provider instance.</summary>
        ModelManifest? Manifest { get; }

        /// <summary>Human-readable name of the engine (e.g. "Beatrice v2").</summary>
        string EngineName { get; }

        /// <summary>True if the model is loaded and verified.</summary>
        bool IsReady { get; }

        /// <summary>Current status or error message.</summary>
        string StatusMessage { get; }

        /// <summary>Processing sample rate (typically 48000).</summary>
        int SampleRate { get; }

        /// <summary>Latency in samples introduced by the engine.</summary>
        int LatencySamples { get; }

        /// <summary>Processes audio block. input/output must have the same length.</summary>
        void Process(float[] input, float[] output);

        /// <summary>
        /// Validates that the loaded model's tensor shapes match engine requirements.
        /// (Contract SECTION 32)
        /// </summary>
        bool ValidateCompatibility(out string reason);
    }
}
