using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;
using VeilVoice.Core;
using VeilVoice.Core.Models;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace VeilVoice.Inference
{
    /// <summary>
    /// ONNX Runtime provider for Beatrice v2 (Contract SECTION 25, 26, 31, 32).
    /// </summary>
    public class BeatriceOnnxProvider : IInferenceProvider
    {
        private InferenceSession? _session;
        private readonly string _modelPath;
        private readonly string _expectedHash;
        private string? _inputName;
        
        public ModelManifest? Manifest { get; private set; }
        public string EngineName => "Beatrice v2 (ONNX)";
        public bool IsReady { get; private set; }
        public string StatusMessage { get; private set; } = "Initializing...";
        public int SampleRate => Manifest?.SampleRate ?? 48000;
        public int LatencySamples => 480; // Beatrice v2 standard latency

        public BeatriceOnnxProvider(ModelManifest manifest)
        {
            Manifest = manifest;
            _modelPath = manifest.ResolvedAbsolutePath ?? manifest.ModelPath;
            _expectedHash = manifest.Sha256;

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (!File.Exists(_modelPath))
                {
                    StatusMessage = $"[ERROR] Model file not found: {Path.GetFileName(_modelPath)}";
                    IsReady = false;
                    return;
                }

                // Mandatory Hash Verification (SECTION 31)
                if (!ModelManager.VerifyHash(_modelPath, _expectedHash))
                {
                    StatusMessage = "[ERROR] SHA256 mismatch (Contract SECTION 31)";
                    IsReady = false;
                    LogService.Error($"[Beatrice] Hash verification failed for {_modelPath}");
                    return;
                }

                var options = new SessionOptions();
                options.AppendExecutionProvider_CPU(); // Default for stability

                _session = new InferenceSession(_modelPath, options);
                _inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";

                // Compatibility Validation (SECTION 32)
                if (!ValidateCompatibility(out string reason))
                {
                    StatusMessage = $"[ERROR] Incompatible Model: {reason}";
                    IsReady = false;
                    return;
                }

                IsReady = true;
                StatusMessage = "Ready";
                LogService.Info($"[Beatrice] Loaded model: {Manifest?.ModelName}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"[CRASH] {ex.Message}";
                IsReady = false;
                LogService.Error($"[Beatrice] Initialization failed: {ex}");
            }
        }

        public bool ValidateCompatibility(out string reason)
        {
            reason = string.Empty;
            if (_session == null) return false;

            // Contract v4.0 REQUIREMENT: Beatrice architecture compatible
            // Beatrice v2 (ONNX) typical signature:
            // Inputs: "input" (float, [1, length]), "speaker_id" (int64/float, [1])
            // OR custom variations, but must have specific nodes for VC.

            if (!_session.InputMetadata.ContainsKey("input"))
            {
                reason = "Missing mandatory 'input' node (Beatrice architecture violation).";
                return false;
            }

            // Real Beatrice v2 models often have multiple inputs or specific output shapes.
            // For now, we enforce that it MUST NOT be a generic single-input-single-output model
            // if we want to claim "Beatrice" authenticity, unless it matches the v2 specs.
            
            bool hasSpeakerId = _session.InputMetadata.ContainsKey("speaker_id");
            bool hasSpeakerEmbed = _session.InputMetadata.ContainsKey("speaker_embedding");

            if (!hasSpeakerId && !hasSpeakerEmbed)
            {
                reason = "Model lacks Beatrice-specific control nodes (speaker_id/embedding). Likely a generic ONNX model.";
                return false;
            }

            // Check sample rate compatibility if manifest provides it
            if (Manifest != null && Manifest.SampleRate != 48000)
            {
                reason = $"Sample rate mismatch: Expected 48000, Model says {Manifest.SampleRate}.";
                return false;
            }

            return true;
        }

        public void Process(float[] input, float[] output)
        {
            if (!IsReady || _session == null || _inputName == null)
            {
                Array.Clear(output, 0, output.Length);
                return;
            }

            try
            {
                // Provenance Trace (Contract v4.0 SECTION 3)
                string executionDir = Path.Combine(AppContext.BaseDirectory, "provenance", ProvenanceService.CurrentExecutionId);
                Directory.CreateDirectory(executionDir);

                // Dump Input Tensor
                string inputPath = Path.Combine(executionDir, "tensor_input_dump.bin");
                File.WriteAllBytes(inputPath, MemoryMarshal.AsBytes(input.AsSpan()).ToArray());
                ProvenanceService.RegisterArtifact(inputPath, "tensor_input", $"Engine: {EngineName}");

                var inputTensor = new DenseTensor<float>(input, new[] { 1, input.Length });
                var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) };

                using var outputs = _session.Run(inputs);
                var result = outputs.First().AsTensor<float>().ToArray();
                Array.Copy(result, output, Math.Min(result.Length, output.Length));

                // Dump Output Tensor
                string outputPath = Path.Combine(executionDir, "tensor_output_dump.bin");
                File.WriteAllBytes(outputPath, MemoryMarshal.AsBytes(result.AsSpan()).ToArray());
                ProvenanceService.RegisterArtifact(outputPath, "tensor_output", $"Engine: {EngineName}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[Beatrice] Process error: {ex.Message}");
                Array.Clear(output, 0, output.Length);
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }
}
