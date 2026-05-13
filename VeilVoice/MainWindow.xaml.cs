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
using VeilVoice.Core.Models;
using VeilVoice.Inference;

namespace VeilVoice
{
    public partial class MainWindow : Window
    {
        private VeilVoiceAudioEngine? _engine;
        private IInferenceProvider? _provider;
        private VeilVoiceConfig _config;
        private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
        private readonly List<string> _logLines = new();
        private const int MaxLogLines = 500;
        private List<MMDevice> _inputDevices = new();
        private List<MMDevice> _outputDevices = new();
        private List<ModelManifest> _availableModels = new();

        public MainWindow()
        {
            InitializeComponent();
            _config = ConfigPersistenceService.Load();
            LogService.OnMessage += AppendLog;
            RefreshDevices();
            RefreshModels();
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();
            AppendLog("[VeilVoice] Ready.");
        }

        private void RefreshDevices()
        {
            _inputDevices = DeviceScanner.GetInputDevices();
            _outputDevices = DeviceScanner.GetOutputDevices();
            CbInput.Items.Clear();
            foreach (var d in _inputDevices) CbInput.Items.Add(d.FriendlyName);
            CbOutput.Items.Clear();
            foreach (var d in _outputDevices) CbOutput.Items.Add(d.FriendlyName);
            var fifine = _inputDevices.FirstOrDefault(d => d.FriendlyName.Contains("FIFINE", StringComparison.OrdinalIgnoreCase));
            if (fifine != null) CbInput.SelectedIndex = _inputDevices.IndexOf(fifine);
            var vvo = _outputDevices.FirstOrDefault(d => d.FriendlyName.Contains("VeilVoiceOut", StringComparison.OrdinalIgnoreCase));
            if (vvo != null) CbOutput.SelectedIndex = _outputDevices.IndexOf(vvo);
        }

        private void RefreshModels()
        {
            _availableModels = ModelManager.GetAvailableModels().ToList();
            CbModels.Items.Clear();
            foreach (var m in _availableModels) CbModels.Items.Add($"{m.ModelName} ({m.SpeakerId})");
            if (_availableModels.Any())
            {
                CbModels.SelectedIndex = 0;
            }
            else
            {
                TxtModelStatus.Text = "No manifests found.";
                TxtModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
            }
        }

        private void CbModels_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbModels.SelectedIndex < 0) return;
            var manifest = _availableModels[CbModels.SelectedIndex];
            UpdateModelInfoUI(manifest);
            if (_engine != null && _engine.IsRunning) TryLoadModel(manifest);
        }

        private void UpdateModelInfoUI(ModelManifest manifest)
        {
            TxtModelStatus.Text = $"{manifest.ModelName} ({manifest.Engine})";
            TxtModelHash.Text = $"SHA256: {(manifest.Sha256.Length > 16 ? manifest.Sha256.Substring(0, 16) + "..." : manifest.Sha256)}";
            BdrModelBadge.Visibility = Visibility.Visible;
            TxtModelBadge.Text = manifest.IsVerified ? "VERIFIED" : "HASH MISMATCH";
            TxtModelBadge.Foreground = new SolidColorBrush(manifest.IsVerified ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xF8, 0x71, 0x71));
        }

        private void BtnScanModels_Click(object sender, RoutedEventArgs e) => RefreshModels();

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (CbInput.SelectedIndex < 0 || CbOutput.SelectedIndex < 0) return;
            var inputDevice = _inputDevices[CbInput.SelectedIndex];
            var outputDevice = _outputDevices[CbOutput.SelectedIndex];
            if (CbModels.SelectedIndex >= 0) TryLoadModel(_availableModels[CbModels.SelectedIndex]);
            _engine?.Dispose();
            _engine = new VeilVoiceAudioEngine(_provider);
            _engine.StatusChanged += s => Dispatcher.InvokeAsync(() => UpdateStatus(s));
            _engine.Start(inputDevice, outputDevice);
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
        }

        private void TryLoadModel(ModelManifest manifest)
        {
            var nextProvider = new BeatriceOnnxProvider(manifest);
            if (nextProvider.IsReady)
            {
                if (_engine != null && _engine.IsRunning) _engine.UpdateInferenceProvider(nextProvider);
                _provider?.Dispose();
                _provider = nextProvider;
                AppendLog($"[Model] Loaded: {manifest.ModelName}");
            }
            else
            {
                AppendLog($"[ERROR] {nextProvider.StatusMessage}");
                if (_engine == null || !_engine.IsRunning) _provider = nextProvider;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _engine?.Dispose();
            _engine = null;
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            UpdateStatus("Stopped");
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_engine == null || !_engine.IsRunning) return;
            PbInput.Value = Math.Clamp(_engine.InputLevel, 0, 1);
            PbOutput.Value = Math.Clamp(_engine.OutputLevel, 0, 1);
            TxtInLevel.Text = $"{_engine.InputLevel * 100:F0}%";
            TxtOutLevel.Text = $"{_engine.OutputLevel * 100:F0}%";
            double p95 = _engine.GetP95LatencyMs();
            TxtP95.Text = p95 > 0 ? $"{p95:F1}ms" : "--";
        }

        private void UpdateStatus(string status)
        {
            TxtStatus.Text = status;
            StatusDot.Fill = new SolidColorBrush(status == "Running" ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xF8, 0x71, 0x71));
        }

        private void AppendLog(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                _logLines.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                if (_logLines.Count > MaxLogLines) _logLines.RemoveAt(0);
                TxtLog.Text = string.Join("\n", _logLines);
                LogScroll.ScrollToEnd();
            });
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) { _logLines.Clear(); TxtLog.Text = ""; }

        private void CbInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbInput.SelectedIndex >= 0 && CbInput.SelectedIndex < _inputDevices.Count)
            {
                var d = _inputDevices[CbInput.SelectedIndex];
                AppendLog($"[Devices] Input selected: {d.FriendlyName}");
            }
        }

        private void CbOutput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbOutput.SelectedIndex >= 0 && CbOutput.SelectedIndex < _outputDevices.Count)
            {
                var d = _outputDevices[CbOutput.SelectedIndex];
                AppendLog($"[Devices] Output selected: {d.FriendlyName}");
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) { _engine?.Dispose(); Close(); }
    }
}
