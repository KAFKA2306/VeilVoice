using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using VeilVoice.Core.Models;

namespace VeilVoice.Core
{
    public static class ModelManager
    {
        private static readonly List<ModelManifest> _models = new();
        private static readonly List<string> _scanPaths = new();

        static ModelManager()
        {
            _scanPaths.Add(Path.Combine(AppContext.BaseDirectory, "models"));
            _scanPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "models"));
            string? parent = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.FullName;
            if (parent != null) _scanPaths.Add(Path.Combine(parent, "models"));
            string oneDriveDocs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "ドキュメント", "REAPER Media", "Beatrice");
            if (Directory.Exists(oneDriveDocs)) _scanPaths.Add(oneDriveDocs);
        }

        public static void AddScanPath(string path)
        {
            if (Directory.Exists(path) && !_scanPaths.Contains(path)) _scanPaths.Add(path);
        }

        public static IReadOnlyList<ModelManifest> GetAvailableModels()
        {
            Refresh();
            return _models.AsReadOnly();
        }

        public static void Refresh()
        {
            _models.Clear();
            foreach (var path in _scanPaths)
            {
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
                {
                    var manifest = JsonSerializer.Deserialize<ModelManifest>(File.ReadAllText(file));
                    if (manifest == null) continue;
                    manifest.ResolvedAbsolutePath = Path.IsPathRooted(manifest.ModelPath) 
                        ? manifest.ModelPath 
                        : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, manifest.ModelPath));

                    if (File.Exists(manifest.ResolvedAbsolutePath))
                    {
                        manifest.IsVerified = VerifyHash(manifest.ResolvedAbsolutePath, manifest.Sha256);
                        _models.Add(manifest);
                    }
                }
            }
        }

        public static bool VerifyHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash)) return false;
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var actualHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        public static ModelManifest CreateTemplate(string onnxPath)
        {
            using var sha = SHA256.Create();
            var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(onnxPath))).Replace("-", "").ToLowerInvariant();
            return new ModelManifest
            {
                ModelName = Path.GetFileNameWithoutExtension(onnxPath),
                ModelPath = onnxPath,
                ResolvedAbsolutePath = Path.GetFullPath(onnxPath),
                Sha256 = hash,
                IsVerified = true
            };
        }
    }
}
