using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using VeilVoice.Core.Models;

namespace VeilVoice.Core
{
    /// <summary>
    /// Manages model manifests, scanning, and path resolution (Contract SECTION 26, 27, 30).
    /// </summary>
    public static class ModelManager
    {
        private static readonly List<ModelManifest> _models = new();
        private static readonly List<string> _scanPaths = new();

        static ModelManager()
        {
            // Default scan paths
            _scanPaths.Add(Path.Combine(AppContext.BaseDirectory, "models"));
            _scanPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "models"));
            
            // Try parent directory if we are in bin/Debug
            string? parent = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.FullName;
            if (parent != null) _scanPaths.Add(Path.Combine(parent, "models"));
            
            // Add OneDrive/Documents path found during research (Contract SECTION 26: Japanese path support)
            string oneDriveDocs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "ドキュメント", "REAPER Media", "Beatrice");
            if (Directory.Exists(oneDriveDocs))
                _scanPaths.Add(oneDriveDocs);
        }

        public static void AddScanPath(string path)
        {
            if (Directory.Exists(path) && !_scanPaths.Contains(path))
            {
                _scanPaths.Add(path);
                LogService.Info($"[ModelManager] Added scan path: {path}");
            }
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

                // Look for .json or .toml files (v3.1 allows manifest management)
                // We prioritize .json for our native manifests
                var manifestFiles = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in manifestFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var manifest = JsonSerializer.Deserialize<ModelManifest>(json);
                        if (manifest != null)
                        {
                            // Resolve model path relative to manifest file if it's not absolute
                            if (Path.IsPathRooted(manifest.ModelPath))
                            {
                                manifest.ResolvedAbsolutePath = manifest.ModelPath;
                            }
                            else
                            {
                                manifest.ResolvedAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, manifest.ModelPath));
                            }

                            // Verify existence and hash
                            if (File.Exists(manifest.ResolvedAbsolutePath))
                            {
                                manifest.IsVerified = VerifyHash(manifest.ResolvedAbsolutePath, manifest.Sha256);
                                _models.Add(manifest);
                                LogService.Info($"[ModelManager] Found model: {manifest.ModelName} at {manifest.ResolvedAbsolutePath} (Verified={manifest.IsVerified})");
                            }
                            else
                            {
                                LogService.Warn($"[ModelManager] Model file NOT found: {manifest.ResolvedAbsolutePath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Warn($"[ModelManager] Failed to load manifest {file}: {ex.Message}");
                    }
                }
            }
        }

        public static bool VerifyHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash)) return false;

            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha.ComputeHash(stream);
                var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>
        /// Creates a template manifest for a raw ONNX file.
        /// </summary>
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
