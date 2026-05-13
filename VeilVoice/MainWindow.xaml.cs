using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using VeilVoice.Core;
using VeilVoice.Inference;

namespace VeilVoice
{
    public partial class MainWindow : Window
    {
        // ------------------------------------------------------------------
        // Fields
        // ------------------------------------------------------------------

        private VeilVoiceAudioEngine? _engine;
        private IInferenceProvider? _provider;
        private VeilVoiceConfig _config;

        private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
        private readonly List<string> _logLines = new();
        private const int MaxLogLines = 500;

        private List<MMDevice> _inputDevices = new();
        private List<MMDevice> _outputDevices = new();

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            // Register crash handler
            CrashRecoveryService.Register();

            // Load persisted config
            _config = ConfigPersistenceService.Load();

            // Hook LogService to UI
            LogService.OnMessage += AppendLog;

            // Load devices
            RefreshDevices();

            // Check model
            CheckModelStatus();

            // UI update timer
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            AppendLog("[VeilVoice] Ready. Select devices and press START.");
        }

        // ------------------------------------------------------------------
        // Device loading
        // ------------------------------------------------------------------

        private void RefreshDevices()
        {
            try
            {
                _inputDevices = DeviceScanner.GetInputDevices();
                _outputDevices = DeviceScanner.GetOutputDevices();

                CbInput.Items.Clear();
                foreach (var d in _inputDevices)
                    CbInput.Items.Add(d.FriendlyName);

                CbOutput.Items.Clear();
                foreach (var d in _outputDevices)
                    CbOutput.Items.Add(d.FriendlyName);

                // Auto-select FIFINE
                var fifine = _inputDevices.FirstOrDefault(d =>
                    d.FriendlyName.Contains("FIFINE", StringComparison.OrdinalIgnoreCase));
                if (fifine != null)
                {
                    CbInput.SelectedIndex = _inputDevices.IndexOf(fifine);
                    TxtInputStatus.Text = "[OK] FIFINE detected";
                    TxtInputStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
                }
                else if (_inputDevices.Any())
                {
                    CbInput.SelectedIndex = 0;
                    TxtInputStatus.Text = "[!] FIFINE not found - using default";
                    TxtInputStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                }

                // Auto-select VeilVoiceOut
                var vvo = _outputDevices.FirstOrDefault(d =>
                    d.FriendlyName.Contains("VeilVoiceOut", StringComparison.OrdinalIgnoreCase));
                if (vvo != null)
                {
                    CbOutput.SelectedIndex = _outputDevices.IndexOf(vvo);
                    TxtOutputStatus.Text = "[OK] VeilVoiceOut detected";
                    TxtOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
                }
                else if (_outputDevices.Any())
                {
                    CbOutput.SelectedIndex = 0;
                    TxtOutputStatus.Text = "[!] VeilVoiceOut not found - BLOCKER-2";
                    TxtOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
                }

                AppendLog($"[Devices] Found {_inputDevices.Count} inputs, {_outputDevices.Count} outputs.");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Device refresh failed: {ex.Message}");
            }
        }

        private void CheckModelStatus()
        {
            string modelPath = Path.Combine(AppContext.BaseDirectory, "models", "beatrice_v2.onnx");

            if (!string.IsNullOrEmpty(_config.ModelPath))
                modelPath = _config.ModelPath;

            if (File.Exists(modelPath))
            {
                TxtModelStatus.Text = $"[OK] Model: {Path.GetFileName(modelPath)}";
                TxtModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
                TxtModelHash.Text = "SHA256 verification will run on load";
                TxtEngineTag.Text = "BEATRICE";
                AppendLog($"[Model] Found at {modelPath}");
            }
            else
            {
                TxtModelStatus.Text = "[!] Model absent - BLOCKER-1";
                TxtModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                TxtModelHash.Text = "Place models/beatrice_v2.onnx to enable inference";
                TxtEngineTag.Text = "NO MODEL";
                AppendLog("[Model] ABSENT. Engine will run in silent mode (BLOCKER-1 unresolved).");
            }
        }

        // ------------------------------------------------------------------
        // Start / Stop
        // ------------------------------------------------------------------

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (CbInput.SelectedIndex < 0 || CbOutput.SelectedIndex < 0)
            {
                AppendLog("[ERROR] Select input and output devices first.");
                return;
            }

            try
            {
                var inputDevice = _inputDevices[CbInput.SelectedIndex];
                var outputDevice = _outputDevices[CbOutput.SelectedIndex];

                // Save config
                _config.InputDeviceId = inputDevice.ID;
                _config.InputDeviceName = inputDevice.FriendlyName;
                _config.OutputDeviceId = outputDevice.ID;
                _config.OutputDeviceName = outputDevice.FriendlyName;
                ConfigPersistenceService.Save(_config);

                // Create inference provider
                string modelPath = string.IsNullOrEmpty(_config.ModelPath)
                    ? Path.Combine(AppContext.BaseDirectory, "models", "beatrice_v2.onnx")
                    : _config.ModelPath;

                _provider?.Dispose();
                _provider = new BeatriceOnnxProvider(modelPath, _config.ModelSha256);

                if (!_provider.IsReady)
                    AppendLog("[WARN] Beatrice not ready. Audio will be muted (TEST-PRIVACY-001). Resolve BLOCKER-1.");

                // Create and start engine
                _engine?.Dispose();
                _engine = new VeilVoiceAudioEngine(_provider);
                _engine.StatusChanged += s => Dispatcher.InvokeAsync(() => UpdateStatus(s));
                _engine.HotplugRecovered += () => Dispatcher.InvokeAsync(() => AppendLog("[Engine] Hotplug recovered!"));
                _engine.Start(inputDevice, outputDevice);

                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;

                AppendLog($"[Engine] Started. In={inputDevice.FriendlyName} | Out={outputDevice.FriendlyName}");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Start failed: {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _engine?.Dispose();
            _engine = null;

            _provider?.Dispose();
            _provider = null;

            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;

            UpdateStatus("Stopped");
            AppendLog("[Engine] Stopped.");
        }

        // ------------------------------------------------------------------
        // UI update timer
        // ------------------------------------------------------------------

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_engine == null || !_engine.IsRunning) return;

            float inLvl = _engine.InputLevel;
            float outLvl = _engine.OutputLevel;
            double p95 = _engine.GetP95LatencyMs();

            PbInput.Value = Math.Clamp(inLvl, 0, 1);
            PbOutput.Value = Math.Clamp(outLvl, 0, 1);

            TxtInLevel.Text = $"{inLvl * 100:F0}%";
            TxtOutLevel.Text = $"{outLvl * 100:F0}%";
            TxtP95.Text = p95 > 0 ? $"{p95:F1}ms" : "--";
            TxtLatency.Text = $"Latency: {_engine.LatencyMs:F1}ms";

            // p95 color (red if >= 150ms per TEST-LATENCY-001)
            TxtP95.Foreground = p95 < 150
                ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                : new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
        }

        private void UpdateStatus(string status)
        {
            TxtStatus.Text = status;
            Color dot = status switch
            {
                "Running" => Color.FromRgb(0x4A, 0xDE, 0x80),
                "Reconnecting..." => Color.FromRgb(0xFB, 0xBF, 0x24),
                _ => Color.FromRgb(0xF8, 0x71, 0x71)
            };
            StatusDot.Fill = new SolidColorBrush(dot);
        }

        // ------------------------------------------------------------------
        // Log
        // ------------------------------------------------------------------

        private void AppendLog(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                _logLines.Add(line);

                if (_logLines.Count > MaxLogLines)
                    _logLines.RemoveAt(0);

                TxtLog.Text = string.Join("\n", _logLines);
                TxtLogCount.Text = $"{_logLines.Count} lines";
                LogScroll.ScrollToEnd();
            });
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            _logLines.Clear();
            TxtLog.Text = string.Empty;
            TxtLogCount.Text = "0 lines";
        }

        // ------------------------------------------------------------------
        // Device selection events
        // ------------------------------------------------------------------

        private void CbInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbInput.SelectedIndex >= 0 && CbInput.SelectedIndex < _inputDevices.Count)
            {
                var d = _inputDevices[CbInput.SelectedIndex];
                bool isFifine = d.FriendlyName.Contains("FIFINE", StringComparison.OrdinalIgnoreCase);
                TxtInputStatus.Text = isFifine ? "[OK] FIFINE detected" : $"Selected: {d.FriendlyName}";
                TxtInputStatus.Foreground = isFifine
                    ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                    : new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
            }
        }

        private void CbOutput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbOutput.SelectedIndex >= 0 && CbOutput.SelectedIndex < _outputDevices.Count)
            {
                var d = _outputDevices[CbOutput.SelectedIndex];
                bool isVVO = d.FriendlyName.Contains("VeilVoiceOut", StringComparison.OrdinalIgnoreCase);
                TxtOutputStatus.Text = isVVO ? "[OK] VeilVoiceOut" : $"[!] {d.FriendlyName} (not VeilVoiceOut)";
                TxtOutputStatus.Foreground = isVVO
                    ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                    : new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
            }
        }

        // ------------------------------------------------------------------
        // Window chrome
        // ------------------------------------------------------------------

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _engine?.Dispose();
            _provider?.Dispose();
            ConfigPersistenceService.Save(_config);
            Close();
        }

        // ------------------------------------------------------------------
        // Override close
        // ------------------------------------------------------------------

        protected override void OnClosed(EventArgs e)
        {
            _uiTimer.Stop();
            _engine?.Dispose();
            _provider?.Dispose();
            LogService.OnMessage -= AppendLog;
            base.OnClosed(e);
        }
    }
}
