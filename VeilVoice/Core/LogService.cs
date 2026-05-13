using System;
using System.IO;

namespace VeilVoice.Core
{
    public static class LogService
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VeilVoice", "logs", $"veilvoice_{DateTime.Now:yyyyMMdd}.log");

        private static readonly object _lock = new();

        public static event Action<string>? OnMessage;

        public static void Info(string message) => Write("INFO ", message);
        public static void Warn(string message) => Write("WARN ", message);
        public static void Error(string message) => Write("ERROR", message);

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}";

            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllLines(LogPath, new[] { line });
            }

            OnMessage?.Invoke($"[{level.Trim()}] {message}");
            Console.WriteLine($"[{level}] {message}");
        }
    }
}
