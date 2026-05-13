using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NAudio.Wave;
using VeilVoice.Core;
using VeilVoice.Core.Models;
using VeilVoice.Inference;

namespace VeilVoiceAcceptanceRunner
{
    class Program
    {
        static readonly List<TestResult> Results = new();
        static readonly string ArtifactDir = Path.Combine(AppContext.BaseDirectory, "artifacts");
        static readonly string ReportPath = Path.Combine(AppContext.BaseDirectory, "acceptance_report.html");

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Directory.CreateDirectory(ArtifactDir);
            RunTest(Audit_ForbiddenSymbols, "TEST-BINARY-001");
            RunTest(Audit_SourceIntegrity, "TEST-SOURCE-001");
            GenerateRealProvenance();
            RunTest(Audit_RealInference, "TEST-REAL-001");
            RunTest(Audit_ProvenanceChain, "TEST-PROVENANCE-001");
            RunTest(Audit_VirtualAudioDisclosure, "TEST-VAUDIO-001");
            GenerateReport();
            bool pass = Results.All(r => r.Status == TestStatus.PASS);
            if (!pass) Environment.Exit(1);
        }

        static void GenerateRealProvenance()
        {
            ProvenanceService.ResetExecution();
            ModelManager.Refresh();
            var models = ModelManager.GetAvailableModels();
            if (models.Count == 0) return;
            ModelManifest? bestModel = null;
            foreach (var m in models)
            {
                using var testProvider = new BeatriceOnnxProvider(m);
                if (testProvider.IsReady && testProvider.IsBeatriceCompatible) { bestModel = m; break; }
            }
            if (bestModel == null)
            {
                foreach (var m in models)
                {
                    using var testProvider = new BeatriceOnnxProvider(m);
                    if (testProvider.IsReady) { bestModel = m; break; }
                }
            }
            if (bestModel == null) return;
            using var provider = new BeatriceOnnxProvider(bestModel);
            string fixturePath = Path.Combine(AppContext.BaseDirectory, "fixture_input.wav");
            CreateWavFixture(fixturePath);
            string outputPath = Path.Combine(AppContext.BaseDirectory, "fixture_output.wav");
            new AudioProcessor(provider).ProcessFile(fixturePath, outputPath);
        }

        static void CreateWavFixture(string path)
        {
            var format = new WaveFormat(48000, 16, 1);
            using var writer = new WaveFileWriter(path, format);
            float[] samples = new float[4800];
            var rand = new Random();
            for (int i = 0; i < samples.Length; i++) samples[i] = (float)(rand.NextDouble() * 0.1 - 0.05);
            writer.WriteSamples(samples, 0, samples.Length);
        }

        static void RunTest(Action action, string testId)
        {
            action();
            var res = Results.Last();
            Console.WriteLine($"[{testId}] {res.Status}: {res.Reason}");
        }

        static void Audit_ForbiddenSymbols()
        {
            var res = new TestResult("TEST-BINARY-001");
            string[] forbidden = { "Mock", "Simulation", "ValidationOnly", "Bypass", "Dummy" };
            int count = 0;
            foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                if (file.Contains("NAudio") || file.Contains("Microsoft")) continue;
                byte[] content = File.ReadAllBytes(file);
                foreach (var s in forbidden) if (ContainsBytePattern(content, s)) count++;
            }
            if (count == 0) res.Pass("No forbidden symbols detected.");
            else res.Fail($"{count} forbidden symbols found.");
            Results.Add(res);
        }

        static bool ContainsBytePattern(byte[] data, string s)
        {
            byte[] pattern = Encoding.UTF8.GetBytes(s);
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++) if (data[i + j] != pattern[j]) { match = false; break; }
                if (match) return true;
            }
            return false;
        }

        static void Audit_SourceIntegrity()
        {
            var res = new TestResult("TEST-SOURCE-001");
            string srcDir = Path.Combine(Directory.GetCurrentDirectory(), "VeilVoice");
            if (!Directory.Exists(srcDir)) { res.Unverified("Source not found."); Results.Add(res); return; }
            bool found = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("MOCK_VALIDATION") || File.ReadAllText(f).Contains("if (MOCK)"));
            if (!found) res.Pass("Source integrity verified.");
            else res.Fail("Validation/Mock branches detected.");
            Results.Add(res);
        }

        static void Audit_RealInference()
        {
            var res = new TestResult("TEST-REAL-001");
            string provDir = Path.Combine(AppContext.BaseDirectory, "provenance");
            var latestProvFile = Directory.GetFiles(provDir, "provenance_*.json").OrderByDescending(f => File.GetCreationTime(f)).FirstOrDefault();
            if (latestProvFile == null) { res.Fail("No provenance manifest found."); Results.Add(res); return; }
            var artifacts = JsonSerializer.Deserialize<List<ProvenanceService.ArtifactMetadata>>(File.ReadAllText(latestProvFile));
            string[] required = { "tensor_input", "tensor_output", "runtime_execution_id", "backend_runtime_trace", "raw_input", "processed_output" };
            var missing = required.Where(t => artifacts == null || !artifacts.Any(a => a.Type == t)).ToList();
            bool isBeatrice = artifacts != null && artifacts.Any(a => a.Details?.Contains("BeatriceCompatible: True") ?? false);
            if (missing.Count == 0 && isBeatrice) res.Pass("Real Beatrice inference verified.");
            else if (!isBeatrice) res.Fail("Architecture Mismatch.");
            else res.Fail($"Incomplete trace. Missing: {string.Join(", ", missing)}");
            Results.Add(res);
        }

        static void Audit_ProvenanceChain()
        {
            var res = new TestResult("TEST-PROVENANCE-001");
            string provDir = Path.Combine(AppContext.BaseDirectory, "provenance");
            var latestProvFile = Directory.GetFiles(provDir, "provenance_*.json").OrderByDescending(f => File.GetCreationTime(f)).FirstOrDefault();
            if (latestProvFile == null) { res.Fail("No provenance chain."); Results.Add(res); return; }
            var artifacts = JsonSerializer.Deserialize<List<ProvenanceService.ArtifactMetadata>>(File.ReadAllText(latestProvFile));
            if (artifacts == null || artifacts.Count == 0) { res.Fail("Empty manifest."); Results.Add(res); return; }
            string execId = artifacts.First().ExecutionId;
            bool valid = artifacts.All(a => a.ExecutionId == execId && !string.IsNullOrEmpty(a.Sha256) && !string.IsNullOrEmpty(a.MachineId));
            if (valid) res.Pass("Provenance chain verified.");
            else res.Fail("Chain invalid.");
            Results.Add(res);
        }

        static void Audit_VirtualAudioDisclosure()
        {
            var res = new TestResult("TEST-VAUDIO-001");
            string disclosure = DeviceScanner.GetEndpointDisclosure();
            if (disclosure != "None")
            {
                File.WriteAllText(Path.Combine(ArtifactDir, "endpoint_provider.json"), disclosure);
                res.Pass("Endpoint provider disclosed.");
            }
            else res.Fail("Virtual audio backend not disclosed.");
            Results.Add(res);
        }

        static void GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body><h1>Audit Report</h1><table>");
            foreach (var r in Results) sb.AppendLine($"<tr><td>{r.TestId}</td><td>{r.Status}</td><td>{r.Reason}</td></tr>");
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(ReportPath, sb.ToString());
        }

        enum TestStatus { PASS, FAIL, UNVERIFIED }
        class TestResult
        {
            public string TestId { get; init; }
            public TestStatus Status { get; private set; }
            public string Reason { get; private set; }
            public TestResult(string id) { TestId = id; Status = TestStatus.UNVERIFIED; Reason = ""; }
            public void Pass(string r) { Status = TestStatus.PASS; Reason = r; }
            public void Fail(string r) { Status = TestStatus.FAIL; Reason = r; }
            public void Unverified(string r) { Status = TestStatus.UNVERIFIED; Reason = r; }
        }
    }
}
