using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;
using VeilVoice.Core;
using VeilVoice.Core.Models;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.Text.Json;

namespace VeilVoice.Inference
{
    public class BeatriceOnnxProvider : IInferenceProvider
    {
        private InferenceSession? _session;
        private readonly string _modelPath;
        private readonly string _expectedHash;
        private string? _inputName;
        private bool _auditDumpDone = false;
        
        public ModelManifest? Manifest { get; private set; }
        public string EngineName => "Beatrice v2 (ONNX)";
        public bool IsReady { get; private set; }
        public bool IsBeatriceCompatible { get; private set; }
        public string StatusMessage { get; private set; } = "Initializing...";
        public int SampleRate => Manifest?.SampleRate ?? 48000;
        public int LatencySamples => 480;

        public BeatriceOnnxProvider(ModelManifest manifest)
        {
            Manifest = manifest;
            _modelPath = manifest.ResolvedAbsolutePath ?? manifest.ModelPath;
            _expectedHash = manifest.Sha256;
            Initialize();
        }

        private void Initialize()
        {
            if (!File.Exists(_modelPath))
            {
                StatusMessage = $"[ERROR] Model file not found: {Path.GetFileName(_modelPath)}";
                IsReady = false;
                return;
            }
            if (!ModelManager.VerifyHash(_modelPath, _expectedHash))
            {
                StatusMessage = "[ERROR] SHA256 mismatch";
                IsReady = false;
                return;
            }
            _session = new InferenceSession(_modelPath, new SessionOptions());
            _inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";
            IsBeatriceCompatible = ValidateCompatibility(out string reason);
            IsReady = true;
            StatusMessage = IsBeatriceCompatible ? "Ready" : $"Ready ({reason})";
            GenerateSessionArtifacts();
        }

        private void GenerateSessionArtifacts()
        {
            string executionId = ProvenanceService.CurrentExecutionId;
            string executionDir = Path.Combine(AppContext.BaseDirectory, "provenance", executionId);
            Directory.CreateDirectory(executionDir);
            string idPath = Path.Combine(executionDir, "runtime_execution_id.txt");
            File.WriteAllText(idPath, executionId);
            ProvenanceService.RegisterArtifact(idPath, "runtime_execution_id");
            string tracePath = Path.Combine(executionDir, "backend_runtime_trace.json");
            var trace = new
            {
                execution_id = executionId,
                engine = EngineName,
                model_name = Manifest?.ModelName,
                is_beatrice_compatible = IsBeatriceCompatible,
                sample_rate = SampleRate,
                latency_samples = LatencySamples,
                onnx_version = "1.16.0",
                input_node = _inputName
            };
            File.WriteAllText(tracePath, JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true }));
            ProvenanceService.RegisterArtifact(tracePath, "backend_runtime_trace");
        }

        public bool ValidateCompatibility(out string reason)
        {
            reason = string.Empty;
            if (_session == null) return false;
            if (!_session.InputMetadata.ContainsKey("input"))
            {
                reason = "Missing 'input' node";
                return false;
            }
            if (!_session.InputMetadata.ContainsKey("speaker_id") && !_session.InputMetadata.ContainsKey("speaker_embedding"))
            {
                reason = "Lacks Beatrice control nodes";
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
            var inputTensor = new DenseTensor<float>(input, new[] { 1, input.Length });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) };
            using var outputs = _session.Run(inputs);
            var result = outputs.First().AsTensor<float>().ToArray();
            Array.Copy(result, output, Math.Min(result.Length, output.Length));

            if (!_auditDumpDone)
            {
                AuditDump(input, result);
                _auditDumpDone = true;
            }
        }

        private void AuditDump(float[] input, float[] output)
        {
            string executionId = ProvenanceService.CurrentExecutionId;
            string executionDir = Path.Combine(AppContext.BaseDirectory, "provenance", executionId);
            Directory.CreateDirectory(executionDir);
            string inputPath = Path.Combine(executionDir, "tensor_input_dump.bin");
            File.WriteAllBytes(inputPath, MemoryMarshal.AsBytes(input.AsSpan()).ToArray());
            ProvenanceService.RegisterArtifact(inputPath, "tensor_input", $"BeatriceCompatible: {IsBeatriceCompatible}");
            string outputPath = Path.Combine(executionDir, "tensor_output_dump.bin");
            File.WriteAllBytes(outputPath, MemoryMarshal.AsBytes(output.AsSpan()).ToArray());
            ProvenanceService.RegisterArtifact(outputPath, "tensor_output", $"BeatriceCompatible: {IsBeatriceCompatible}");
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }
}
