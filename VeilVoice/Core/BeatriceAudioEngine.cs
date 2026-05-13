using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using VeilVoice.Inference;

namespace VeilVoice.Core
{










    public class VeilVoiceAudioEngine : IDisposable
    {




        private WasapiCapture? _capture;
        private WasapiOut? _render;
        private BufferedWaveProvider? _bufferedProvider;
        private readonly IInferenceProvider _inference;

        private MMDevice? _inputDevice;
        private MMDevice? _outputDevice;


        private readonly MMDeviceEnumerator _enumerator = new();
        private readonly SemaphoreSlim _restartLock = new(1, 1);
        private CancellationTokenSource? _hotplugCts;


        private readonly double[] _latencyRing = new double[100];
        private int _latencyIdx = 0;
        private long _totalSamples = 0;





        public float InputLevel { get; private set; }
        public float OutputLevel { get; private set; }
        public double LatencyMs { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsHotplugging { get; private set; }

        public event Action<float[]>? DataReceived;
        public event Action<float[]>? DataProcessed;
        public event Action<string>? StatusChanged;
        public event Action? HotplugRecovered;





        public VeilVoiceAudioEngine(IInferenceProvider inference)
        {
            _inference = inference ?? throw new ArgumentNullException(nameof(inference));
        }





        public void Start(MMDevice inputDevice, MMDevice outputDevice)
        {
            if (IsRunning)
            {
                LogService.Warn("[Engine] Start called while already running.");
                return;
            }

            _inputDevice = inputDevice;
            _outputDevice = outputDevice;

            StartInternal(inputDevice, outputDevice);


            _hotplugCts = new CancellationTokenSource();
            _ = HotplugWatchdogAsync(_hotplugCts.Token);
        }

        private void StartInternal(MMDevice inputDevice, MMDevice outputDevice)
        {
            LogService.Info($"[Engine] Starting  EIn: {inputDevice.FriendlyName} | Out: {outputDevice.FriendlyName}");

            try
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
                LatencyMs = 20.0 + (double)_inference.LatencySamples * 1000.0 / _capture.WaveFormat.SampleRate;

                LogService.Info($"[Engine] Running. Estimated latency: {LatencyMs:F1} ms");
                StatusChanged?.Invoke("Running");
            }
            catch (Exception ex)
            {
                LogService.Error($"[Engine] StartInternal failed: {ex.Message}");
                StopInternal();
                throw;
            }
        }





        private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0 || _bufferedProvider == null) return;

            var sw = Stopwatch.GetTimestamp();


            float[] samples = BytesToFloat(e.Buffer, e.BytesRecorded, _capture!.WaveFormat);

            InputLevel = samples.Length > 0 ? samples.Max(Math.Abs) : 0f;
            DataReceived?.Invoke(samples);


            float[] processed = _inference.IsReady
                ? _inference.Process(samples)
                : new float[samples.Length]; 

            OutputLevel = processed.Length > 0 ? processed.Max(Math.Abs) : 0f;
            DataProcessed?.Invoke(processed);


            byte[] outBuffer = FloatToBytes(processed);
            _bufferedProvider.AddSamples(outBuffer, 0, outBuffer.Length);


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





        private async Task HotplugWatchdogAsync(CancellationToken ct)
        {
            LogService.Info("[Engine] Hotplug watchdog started.");

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(500, ct).ConfigureAwait(false);

                if (!IsRunning) continue;

                bool captureAlive = _capture != null &&
                    _capture.CaptureState == CaptureState.Capturing;

                if (!captureAlive && !IsHotplugging)
                {
                    _ = TryHotplugRecoveryAsync();
                }
            }
        }

        private async Task TryHotplugRecoveryAsync()
        {
            if (!await _restartLock.WaitAsync(100).ConfigureAwait(false)) return;

            try
            {
                IsHotplugging = true;
                LogService.Warn("[Engine] Input device disconnected. Attempting hotplug recovery...");
                StatusChanged?.Invoke("Reconnecting...");

                var sw = Stopwatch.StartNew();


                while (sw.Elapsed.TotalSeconds < 5.0)
                {
                    StopInternal();
                    var input = DeviceScanner.FindBestInputDevice();
                    var output = DeviceScanner.FindVBCableInput() ?? _outputDevice;

                    if (input != null && output != null)
                    {
                        try
                        {
                            StartInternal(input, output);
                            LogService.Info($"[Engine] Hotplug recovery SUCCESS in {sw.Elapsed.TotalSeconds:F1}s");
                            HotplugRecovered?.Invoke();
                            return;
                        }
                        catch {  }
                    }

                    await Task.Delay(200).ConfigureAwait(false);
                }

                LogService.Error("[Engine] Hotplug recovery FAILED within 5s. TEST-HOTPLUG-001 = FAIL.");
                StatusChanged?.Invoke("Error: Device lost");
            }
            finally
            {
                IsHotplugging = false;
                _restartLock.Release();
            }
        }





        public void Stop()
        {
            _hotplugCts?.Cancel();
            StopInternal();
        }

        private void StopInternal()
        {
            if (_capture != null)
            {
                try { _capture.StopRecording(); } catch { }
                try { _capture.Dispose(); } catch { }
                _capture = null;
            }

            if (_render != null)
            {
                try { _render.Stop(); } catch { }
                try { _render.Dispose(); } catch { }
                _render = null;
            }

            _bufferedProvider = null;
            IsRunning = false;
            LogService.Info("[Engine] Stopped.");
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
                {
                    samples[i] = BitConverter.ToInt16(buffer, offset) / 32768f;
                }
                else if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    samples[i] = BitConverter.ToSingle(buffer, offset);
                }
                else
                {
                    samples[i] = 0f;
                }
            }

            return samples;
        }

        private static byte[] FloatToBytes(float[] samples)
        {
            byte[] buffer = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short s = (short)Math.Clamp(samples[i] * 32767f, -32768f, 32767f);
                byte[] b = BitConverter.GetBytes(s);
                buffer[i * 2] = b[0];
                buffer[i * 2 + 1] = b[1];
            }
            return buffer;
        }





        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _restartLock.Dispose();
            _enumerator.Dispose();
        }
    }
}
