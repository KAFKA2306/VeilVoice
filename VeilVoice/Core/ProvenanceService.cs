using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VeilVoice.Core
{
    public class ProvenanceService
    {
        public static string CurrentExecutionId { get; private set; } = Guid.NewGuid().ToString();
        private static List<ArtifactMetadata> _artifacts = new List<ArtifactMetadata>();
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "provenance");

        static ProvenanceService()
        {
            Directory.CreateDirectory(LogDir);
        }

        public static void ResetExecution()
        {
            CurrentExecutionId = Guid.NewGuid().ToString();
            _artifacts.Clear();
        }

        public static void RegisterArtifact(string filePath, string type, string details = "")
        {
            if (!File.Exists(filePath)) return;

            var meta = new ArtifactMetadata
            {
                ExecutionId = CurrentExecutionId,
                FileName = Path.GetFileName(filePath),
                Type = type,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Sha256 = CalculateHash(filePath),
                MachineId = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                Details = details
            };

            _artifacts.Add(meta);
            SaveGraph();
        }

        private static string CalculateHash(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void SaveGraph()
        {
            var graphPath = Path.Combine(LogDir, $"provenance_{CurrentExecutionId}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(graphPath, JsonSerializer.Serialize(_artifacts, options));
        }

        public class ArtifactMetadata
        {
            public string ExecutionId { get; set; }
            public string FileName { get; set; }
            public string Type { get; set; }
            public string Timestamp { get; set; }
            public string Sha256 { get; set; }
            public string MachineId { get; set; }
            public int ProcessId { get; set; }
            public string Details { get; set; }
        }
    }
}
