using System;
using System.IO;
using System.Linq;
using NAudio.Wave;
using VeilVoice.Inference;

namespace VeilVoice.Core
{
    public class AudioProcessor
    {
        private IInferenceProvider _inference;

        public AudioProcessor(IInferenceProvider inference)
        {
            _inference = inference;
        }

        public void ProcessFile(string inputPath, string outputPath)
        {
            using (var reader = new AudioFileReader(inputPath))
            {
                var sampleProvider = reader.ToSampleProvider();
                int sampleCount = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
                float[] buffer = new float[sampleCount];
                
                int read = sampleProvider.Read(buffer, 0, sampleCount);
                
                // Process in chunks to simulate real-time
                int chunkSize = 512;
                float[] processedBuffer = new float[read];
                
                for (int i = 0; i < read; i += chunkSize)
                {
                    int currentChunkSize = Math.Min(chunkSize, read - i);
                    float[] chunk = new float[currentChunkSize];
                    Array.Copy(buffer, i, chunk, 0, currentChunkSize);
                    
                    float[] processedChunk = _inference.Process(chunk);
                    Array.Copy(processedChunk, 0, processedBuffer, i, Math.Min(currentChunkSize, processedChunk.Length));
                }

                using (var writer = new WaveFileWriter(outputPath, new WaveFormat(reader.WaveFormat.SampleRate, 16, 1)))
                {
                    float[] finalBuffer = processedBuffer.Take(read).ToArray();
                    for (int i = 0; i < finalBuffer.Length; i++)
                    {
                        writer.WriteSample(finalBuffer[i]);
                    }
                }
            }
        }
    }
}
