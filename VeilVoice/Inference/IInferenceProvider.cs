using System;
using VeilVoice.Core.Models;

namespace VeilVoice.Inference
{
    public interface IInferenceProvider : IDisposable
    {
        ModelManifest? Manifest { get; }
        string EngineName { get; }
        bool IsReady { get; }
        string StatusMessage { get; }
        int SampleRate { get; }
        int LatencySamples { get; }
        void Process(float[] input, float[] output);
        bool ValidateCompatibility(out string reason);
    }
}
