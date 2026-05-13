using System;
using System.Linq;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using VeilVoice.Inference;

namespace VeilVoice.Core
{
    public class VeilVoiceAudioEngine : IDisposable
    {
        private WasapiCapture _capture;
        private WasapiOut _render;
        private BufferedWaveProvider _bufferedProvider;
        private IInferenceProvider _inference;
        
        // Stats
        public float InputLevel { get; private set; }
        public float OutputLevel { get; private set; }
        public double LatencyMs { get; private set; }
        
        public bool IsRunning { get; private set; }
        
        public event Action<float[]> DataReceived;
        public event Action<float[]> DataProcessed;

        public VeilVoiceAudioEngine(IInferenceProvider inference)
        {
            _inference = inference;
        }

        public void Start(MMDevice inputDevice, MMDevice outputDevice)
        {
            if (IsRunning) return;

            LogService.Info($"Starting Engine: In={inputDevice.FriendlyName}, Out={outputDevice.FriendlyName}");

            // 1. Setup Capture (Physical Mic) - Try 48kHz, fallback to device format
            _capture = new WasapiCapture(inputDevice);
            // Auto-resampling is handled if we use a MediaFoundationResampler, 
            // but for low latency we prefer matching formats.
            
            _capture.DataAvailable += OnCaptureDataAvailable;

            // 2. Setup Render (Virtual VeilVoiceOut)
            // Using EventSync for lowest latency
            _render = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 20); 
            
            // 3. Setup Buffer with Resampling support if needed
            // (Simplification: Assuming 48kHz for now, in production we'd use WdlResampler)
            _bufferedProvider = new BufferedWaveProvider(_capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferLength = _capture.WaveFormat.AverageBytesPerSecond * 1 // 1 sec buffer max
            };

            _render.Init(_bufferedProvider);
            
            _capture.StartRecording();
            _render.Play();
            
            IsRunning = true;
            LatencyMs = 20 + _inference.LatencySamples * 1000.0 / _capture.WaveFormat.SampleRate;
        }

        private void OnCaptureDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;

            // Convert to samples
            float[] samples;
            using (var ms = new System.IO.MemoryStream(e.Buffer, 0, e.BytesRecorded))
            using (var rs = new RawSourceWaveStream(ms, _capture.WaveFormat))
            {
                var sampleProvider = rs.ToSampleProvider();
                samples = new float[e.BytesRecorded / (_capture.WaveFormat.BitsPerSample / 8)];
                sampleProvider.Read(samples, 0, samples.Length);
            }

            InputLevel = samples.Max(Math.Abs);
            DataReceived?.Invoke(samples);

            // Process with VeilVoice
            float[] processed = _inference.Process(samples);
            
            OutputLevel = processed.Max(Math.Abs);
            DataProcessed?.Invoke(processed);

            // Convert back to bytes
            byte[] outBuffer = new byte[processed.Length * 2];
            for (int i = 0; i < processed.Length; i++)
            {
                short sample = (short)Math.Clamp(processed[i] * 32767, -32768, 32767);
                byte[] bytes = BitConverter.GetBytes(sample);
                outBuffer[i * 2] = bytes[0];
                outBuffer[i * 2 + 1] = bytes[1];
            }

            _bufferedProvider.AddSamples(outBuffer, 0, outBuffer.Length);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _capture?.StopRecording();
            _render?.Stop();
            
            LogService.Info("Engine Stopped.");
            
            _capture?.Dispose();
            _render?.Dispose();
            
            _capture = null;
            _render = null;
            
            IsRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
