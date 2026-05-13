using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;

namespace VeilVoice.Inference
{
    public class OnnxVeilVoiceProvider : IInferenceProvider
    {
        private InferenceSession _session;
        private string _modelPath;

        public string EngineName => "Beatrice";
        public int LatencySamples => 512;
        public bool IsReady => _session != null;

        public OnnxVeilVoiceProvider(string modelPath)
        {
            _modelPath = modelPath;
            
            if (!System.IO.File.Exists(modelPath))
            {
                throw new System.IO.FileNotFoundException("Beatrice ONNX model not found.", modelPath);
            }

            var options = new SessionOptions();
            options.AppendExecutionProvider_CPU(); 
            _session = new InferenceSession(modelPath, options);
        }

        public float[] Process(float[] input)
        {
            if (!IsReady) return input;

            var inputName = _session.InputMetadata.Keys.First();
            var shape = new int[] { 1, input.Length };
            var tensor = new DenseTensor<float>(input, shape);
            
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            };

            using (var results = _session.Run(inputs))
            {
                var output = results.First().AsEnumerable<float>().ToArray();
                return output;
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
