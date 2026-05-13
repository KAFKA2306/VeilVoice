using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeilVoice.Core
{




    public static class CrashRecoveryService
    {
        private static string _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VeilVoice", "logs");

        public static void Register()
        {
            Directory.CreateDirectory(_logDir);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            LogService.Info("[CrashRecovery] Registered unhandled exception handler.");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                string crashPath = Path.Combine(_logDir, $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
                string content = e.ExceptionObject?.ToString() ?? "Unknown exception";
                File.WriteAllText(crashPath, content);



                WriteCrashEndpointReport();

                LogService.Error($"[CrashRecovery] Crash dump written to {crashPath}");
            }
            catch
            {

            }
        }

        private static void WriteCrashEndpointReport()
        {
            try
            {
                using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var outputs = enumerator.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.DeviceState.Active);

                var report = new System.Collections.Generic.List<object>();
                foreach (var d in outputs)
                    report.Add(new { name = d.FriendlyName, id = d.ID, state = d.State.ToString() });

                string reportPath = Path.Combine(_logDir, "endpoint_post_crash.json");
                File.WriteAllText(reportPath,
                    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch {  }
        }
    }
}
