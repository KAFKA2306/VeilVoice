using System;
using System.IO;

namespace VeilVoice.Core
{
    public static class LogService
    {
        private static string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VeilVoice.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", $"{message} {(ex != null ? ex.ToString() : "")}");
        }

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            try
            {
                File.AppendAllLines(LogPath, new[] { line });
            }
            catch
            {
                // Ignore log errors
            }
        }
    }
}
