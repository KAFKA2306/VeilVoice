using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace VeilVoice.Core
{
    public class ProvenanceService
    {
        public static string CurrentExecutionId { get; private set; } = Guid.NewGuid().ToString();
        private static readonly List<ArtifactMetadata> _artifacts = new();
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "provenance");

        static ProvenanceService() => Directory.CreateDirectory(LogDir);

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
                ThreadId = Environment.CurrentManagedThreadId,
                GitCommit = GetGitCommit(),
                Details = details
            };
            _artifacts.Add(meta);
            SaveGraph();
        }

        private static string GetGitCommit()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "git_commit.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "unknown_development_build";
        }

        private static string CalculateHash(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void SaveGraph()
        {
            var graphPath = Path.Combine(LogDir, $"provenance_{CurrentExecutionId}.json");
            File.WriteAllText(graphPath, JsonSerializer.Serialize(_artifacts, new JsonSerializerOptions { WriteIndented = true }));
        }

        public class ArtifactMetadata
        {
            public string ExecutionId { get; init; } = string.Empty;
            public string FileName { get; init; } = string.Empty;
            public string Type { get; init; } = string.Empty;
            public string Timestamp { get; init; } = string.Empty;
            public string Sha256 { get; init; } = string.Empty;
            public string MachineId { get; init; } = string.Empty;
            public int ProcessId { get; init; }
            public int ThreadId { get; init; }
            public string GitCommit { get; init; } = string.Empty;
            public string Details { get; init; } = string.Empty;
        }
    }
}
