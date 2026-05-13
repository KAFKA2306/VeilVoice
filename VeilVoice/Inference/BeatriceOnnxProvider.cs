using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using VeilVoice.Core;

namespace VeilVoice.Inference
{











    public class BeatriceOnnxProvider : IInferenceProvider
    {





        public const int ModelSampleRate = 24000;






        public const int ChunkSamples = 512;





        private InferenceSession? _session;
        private readonly string _modelPath;
        private readonly string? _expectedSha256;
        private string? _actualSha256;

        private bool _disposed;





        public string EngineName => "Beatrice";

        public int LatencySamples => ChunkSamples;

        public bool IsReady => _session != null && !_disposed;





        public string? ModelSha256 => _actualSha256;













        public BeatriceOnnxProvider(string modelPath, string? expectedSha256 = null)
        {
            _modelPath = modelPath;
            _expectedSha256 = expectedSha256;

            TryLoadModel();
        }





        private void TryLoadModel()
        {
            if (!File.Exists(_modelPath))
            {
                LogService.Warn($"[BeatriceOnnxProvider] Model file not found: {_modelPath}");
                LogService.Warn("[BeatriceOnnxProvider] IsReady = false. TEST-ENGINE-001 = UNVERIFIED.");
                return;
            }


            _actualSha256 = ComputeSha256(_modelPath);
            LogService.Info($"[BeatriceOnnxProvider] Model SHA256: {_actualSha256}");

            if (_expectedSha256 != null)
            {
                if (!string.Equals(_actualSha256, _expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    LogService.Error($"[BeatriceOnnxProvider] SHA256 MISMATCH. Expected={_expectedSha256} Got={_actualSha256}");
                    LogService.Error("[BeatriceOnnxProvider] TEST-MODEL-001 = FAIL.");
                    return;
                }
                LogService.Info("[BeatriceOnnxProvider] SHA256 verified OK.");
            }
            else
            {
                LogService.Warn("[BeatriceOnnxProvider] No expected SHA256 provided. TEST-MODEL-001 = UNVERIFIED.");
            }


            try
            {
                var options = new SessionOptions();
                options.InterOpNumThreads = 2;
                options.IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2);
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;


                _session = new InferenceSession(_modelPath, options);

                LogDiagnostics();
                LogService.Info("[BeatriceOnnxProvider] ONNX session initialized. IsReady = true.");
            }
            catch (Exception ex)
            {
                LogService.Error($"[BeatriceOnnxProvider] Failed to create ONNX session: {ex.Message}");
                _session = null;
            }
        }

        private void LogDiagnostics()
        {
            if (_session == null) return;
            LogService.Info("[BeatriceOnnxProvider] Input nodes:");
            foreach (var node in _session.InputMetadata)
                LogService.Info($"  {node.Key}: shape=[{string.Join(",", node.Value.Dimensions)}] type={node.Value.ElementType}");
            LogService.Info("[BeatriceOnnxProvider] Output nodes:");
            foreach (var node in _session.OutputMetadata)
                LogService.Info($"  {node.Key}: shape=[{string.Join(",", node.Value.Dimensions)}] type={node.Value.ElementType}");
        }





        public float[] Process(float[] input)
        {
            if (!IsReady || _session == null)
            {


                return new float[input.Length];
            }

            try
            {
                return RunBeatriceInference(input);
            }
            catch (Exception ex)
            {
                LogService.Error($"[BeatriceOnnxProvider] Inference error: {ex.Message}");
                return new float[input.Length]; 
            }
        }











        private float[] RunBeatriceInference(float[] input)
        {

            float[] padded = new float[ChunkSamples];
            int copyLen = Math.Min(input.Length, ChunkSamples);
            Array.Copy(input, padded, copyLen);



            var inputNodeName = _session!.InputMetadata.Keys.First();
            var tensor = new DenseTensor<float>(padded, new[] { 1, ChunkSamples });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputNodeName, tensor)
            };

            using var results = _session.Run(inputs);


            var outputTensor = results.First().AsTensor<float>();
            float[] output = outputTensor.ToArray();


            if (output.Length >= input.Length)
                return output.Take(input.Length).ToArray();


            float[] finalOutput = new float[input.Length];
            Array.Copy(output, finalOutput, output.Length);
            return finalOutput;
        }





        private static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(filePath);
            byte[] hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }





        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session?.Dispose();
            _session = null;
        }
    }
}
