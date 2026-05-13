using System;
using System.Collections.Generic;

namespace VeilVoice.Inference
{
    public interface IInferenceProvider
    {
        /// <summary>
        /// Processes a chunk of audio data.
        /// </summary>
        /// <param name="input">Input buffer (PCM Float)</param>
        /// <returns>Processed buffer (PCM Float)</returns>
        float[] Process(float[] input);
        
        int LatencySamples { get; }
    }

    public class MockVeilVoiceProvider : IInferenceProvider
    {
        public int LatencySamples => 512;

        public float[] Process(float[] input)
        {
            // Just a placeholder: in a real product, this would be the VeilVoice ML model.
            // For now, let's just pass through with a tiny bit of gain reduction to simulate "processing".
            float[] output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = input[i] * 0.95f; 
            }
            return output;
        }
    }
}
