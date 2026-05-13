using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using VeilVoice.Core;
using VeilVoice.Inference;

namespace VeilVoice
{
    public partial class MainWindow : Window
    {
        private VeilVoiceAudioEngine _engine;
        private DispatcherTimer _metricsTimer;
        private PerformanceCounter _cpuCounter;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new VeilVoiceAudioEngine(new MockVeilVoiceProvider());
            
            _metricsTimer = new DispatcherTimer();
            _metricsTimer.Interval = TimeSpan.FromMilliseconds(100);
            _metricsTimer.Tick += UpdateUI;
            _metricsTimer.Start();

            try {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            } catch {
                // Performance counters might not be available in all environments
            }

            RefreshDeviceDisplay();
        }

        private void RefreshDeviceDisplay()
        {
            var mic = DeviceScanner.FindBestInputDevice();
            var vmic = DeviceScanner.FindVBCableInput();

            MicName.Text = mic?.FriendlyName ?? "No Microphone Found";
            VirtualMicName.Text = vmic?.FriendlyName ?? "VB-CABLE Not Installed";

            if (vmic == null)
            {
                StatusText.Text = "MISSING DRIVER";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            // Meters
            InputMeter.Value = _engine.InputLevel * 100;
            OutputMeter.Value = _engine.OutputLevel * 100;

            // Metrics
            if (_engine.IsRunning)
            {
                LatencyText.Text = $"{_engine.LatencyMs:F0} ms";
                
                if (_cpuCounter != null)
                {
                    // Just a mock CPU usage for the app itself if needed, or total system
                    float cpu = _cpuCounter.NextValue();
                    // We'll just show status instead of overloading with system CPU
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsRunning)
            {
                var mic = DeviceScanner.FindBestInputDevice();
                var vmic = DeviceScanner.FindVBCableInput();

                if (mic != null && vmic != null)
                {
                    try
                    {
                        _engine.Start(mic, vmic);
                        StartButton.Content = "STOP ENGINE";
                        StartButton.Background = System.Windows.Media.Brushes.DarkRed;
                        StatusText.Text = "ACTIVE";
                        StatusText.Foreground = System.Windows.Media.Brushes.Cyan;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Engine Error: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Please check audio device connections (VB-CABLE required).");
                }
            }
            else
            {
                _engine.Stop();
                StartButton.Content = "START ENGINE";
                StartButton.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00f2ff"));
                StatusText.Text = "READY";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
                LatencyText.Text = "-- ms";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _engine.Stop();
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
