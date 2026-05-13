using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using VeilVoice.Inference;
using System.IO;

namespace VeilVoice.Core
{
    public class VeilVoiceAudioEngine : IDisposable
    {
        private WasapiCapture? _capture;
        private WasapiOut? _render;
        private BufferedWaveProvider? _bufferedProvider;
        private IInferenceProvider? _inference;
        private readonly object _inferenceLock = new();
        private MMDevice? _inputDevice;
        private MMDevice? _outputDevice;
        private readonly MMDeviceEnumerator _enumerator = new();
        private readonly double[] _latencyRing = new double[100];
        private int _latencyIdx = 0;
        private long _totalSamples = 0;

        public float InputLevel { get; private set; }
        public float OutputLevel { get; private set; }
        public double LatencyMs { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action<float[]>? DataReceived;
        public event Action<float[]>? DataProcessed;
        public event Action<string>? StatusChanged;

        public VeilVoiceAudioEngine(IInferenceProvider? initialInference = null)
        {
            _inference = initialInference;
        }

        public void UpdateInferenceProvider(IInferenceProvider newProvider)
        {
            lock (_inferenceLock)
            {
                _inference = newProvider;
            }
        }

        public void Start(MMDevice inputDevice, MMDevice outputDevice)
        {
            if (IsRunning) return;
            _inputDevice = inputDevice;
            _outputDevice = outputDevice;
            StartInternal(inputDevice, outputDevice);
        }

        private void StartInternal(MMDevice inputDevice, MMDevice outputDevice)
        {
            _capture = new WasapiCapture(inputDevice);
            _capture.DataAvailable += OnCaptureDataAvailable;
            _render = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 20);
            _bufferedProvider = new BufferedWaveProvider(_capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferLength = _capture.WaveFormat.AverageBytesPerSecond * 2
            };
            _render.Init(_bufferedProvider);
            _capture.StartRecording();
            _render.Play();
            IsRunning = true;
            int infLatency = 0;
            lock(_inferenceLock) { infLatency = _inference?.LatencySamples ?? 0; }
            LatencyMs = 20.0 + (double)infLatency * 1000.0 / _capture.WaveFormat.SampleRate;
            StatusChanged?.Invoke("Running");
        }

        private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;
            var sw = Stopwatch.GetTimestamp();
            var format = _capture?.WaveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            float[] samples = BytesToFloat(e.Buffer, e.BytesRecorded, format);
            InputLevel = samples.Length > 0 ? samples.Max(Math.Abs) : 0f;
            DataReceived?.Invoke(samples);
            float[] processed = new float[samples.Length];

            lock (_inferenceLock)
            {
                if (_inference != null && _inference.IsReady)
                {
                    _inference.Process(samples, processed);
                }
                else
                {
                    Array.Clear(processed, 0, processed.Length);
                }
            }

            OutputLevel = processed.Length > 0 ? processed.Max(Math.Abs) : 0f;
            DataProcessed?.Invoke(processed);
            _bufferedProvider?.AddSamples(FloatToBytes(processed), 0, processed.Length * 2);
            double elapsedMs = (Stopwatch.GetTimestamp() - sw) * 1000.0 / Stopwatch.Frequency;
            _latencyRing[_latencyIdx++ % _latencyRing.Length] = elapsedMs + LatencyMs;
            _totalSamples++;
        }

        public double GetP95LatencyMs()
        {
            int count = (int)Math.Min(_totalSamples, _latencyRing.Length);
            if (count == 0) return 0;
            var sorted = _latencyRing.Take(count).OrderBy(x => x).ToArray();
            int idx = (int)Math.Ceiling(count * 0.95) - 1;
            return sorted[Math.Clamp(idx, 0, sorted.Length - 1)];
        }

        public void Stop() => StopInternal();

        private void StopInternal()
        {
            if (_capture != null)
            {
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }
            if (_render != null)
            {
                _render.Stop();
                _render.Dispose();
                _render = null;
            }
            _bufferedProvider = null;
            IsRunning = false;
            StatusChanged?.Invoke("Stopped");
        }

        private static float[] BytesToFloat(byte[] buffer, int bytesRecorded, WaveFormat format)
        {
            int bytesPerSample = format.BitsPerSample / 8;
            int sampleCount = bytesRecorded / bytesPerSample;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int offset = i * bytesPerSample;
                if (format.BitsPerSample == 16)
                    samples[i] = BitConverter.ToInt16(buffer, offset) / 32768f;
                else if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)
                    samples[i] = BitConverter.ToSingle(buffer, offset);
            }
            return samples;
        }

        private static byte[] FloatToBytes(float[] samples)
        {
            byte[] buffer = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short s = (short)Math.Clamp(samples[i] * 32767f, -32768f, 32767f);
                buffer[i * 2] = (byte)(s & 0xff);
                buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff);
            }
            return buffer;
        }

        public void Dispose()
        {
            Stop();
            _enumerator.Dispose();
        }
    }
}
