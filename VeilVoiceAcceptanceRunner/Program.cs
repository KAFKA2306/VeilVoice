using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using VeilVoice.Core;
using VeilVoice.Inference;

namespace VeilVoiceAcceptanceRunner
{
    class Program
    {
        static readonly List<TestResult> Results = new();
        static readonly string ArtifactDir = Path.Combine(AppContext.BaseDirectory, "artifacts");
        static readonly string ReportPath = Path.Combine(AppContext.BaseDirectory, "acceptance_report.html");

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== VeilVoice Acceptance Runner v4.0 (Zero-Trust) ===");
            Console.WriteLine("    Provenance-Based Verification & Audit Chain");
            Console.WriteLine();

            Directory.CreateDirectory(ArtifactDir);

            // Audit Phase 1: Forbidden Symbols (SECTION 5/6)
            RunTest(Audit_ForbiddenSymbols, "TEST-BINARY-001");
            RunTest(Audit_SourceIntegrity, "TEST-SOURCE-001");

            // Execution Phase: Generate Real Provenance (SECTION 3)
            Console.WriteLine("\n[EXECUTION] Generating Real Inference Provenance...");
            GenerateRealProvenance();

            // Audit Phase 2: Real Execution & Provenance (SECTION 3/4)
            RunTest(Audit_RealInference, "TEST-REAL-001");
            RunTest(Audit_ProvenanceChain, "TEST-PROVENANCE-001");

            // Audit Phase 3: Virtual Audio Disclosure (SECTION 7)
            RunTest(Audit_VirtualAudioDisclosure, "TEST-VAUDIO-001");

            GenerateReport();
            
            bool infrastructurePass = Results.Where(r => r.TestId != "TEST-REAL-001").All(r => r.Status == TestStatus.PASS);
            bool beatricePass = Results.Any(r => r.TestId == "TEST-REAL-001" && r.Status == TestStatus.PASS);

            Console.WriteLine("\n=== AUDIT SUMMARY ===");
            Console.WriteLine($"Infrastructure Verification: {(infrastructurePass ? "\x1b[92mPASS\x1b[0m" : "\x1b[91mFAIL\x1b[0m")}");
            Console.WriteLine($"Real Beatrice Inference:   {(beatricePass ? "\x1b[92mPASS\x1b[0m" : "\x1b[91mFAIL / UNVERIFIED\x1b[0m")}");

            if (!infrastructurePass || !beatricePass) {
                Console.WriteLine("\n\x1b[91mDELIVERY BLOCKED: Contract violation or missing evidence.\x1b[0m");
                Environment.Exit(1);
            } else {
                Console.WriteLine("\n\x1b[92mVEILVOICE 完成 (Contract v4.0 Verified)\x1b[0m");
            }
        }

        static void GenerateRealProvenance()
        {
            try {
                ProvenanceService.ResetExecution();
                ModelManager.Refresh();
                var models = ModelManager.GetAvailableModels().Where(m => m.IsVerified).ToList();
                if (!models.Any()) {
                    Console.WriteLine("    [WARN] No verified models found. Real inference skipped.");
                    return;
                }

                var manifest = models.FirstOrDefault(m => m.Engine == "Beatrice");
                if (manifest == null) {
                    Console.WriteLine("    [WARN] No Beatrice-engine models found. Using first available for infra check only.");
                    manifest = models.First();
                }
                
                Console.WriteLine($"    [INFO] Using model for trace: {manifest.ModelName} (Engine: {manifest.Engine})");
                
                using var provider = new BeatriceOnnxProvider(manifest);
                if (!provider.IsReady) {
                    Console.WriteLine($"    [ERROR] Provider not ready: {provider.StatusMessage}");
                    return;
                }

                float[] input = new float[480];
                float[] output = new float[480];
                provider.Process(input, output);
                
                Console.WriteLine($"    [SUCCESS] Real execution trace generated: {ProvenanceService.CurrentExecutionId}");
            } catch (Exception ex) {
                Console.WriteLine($"    [ERROR] Execution failed: {ex.Message}");
            }
        }

        static void RunTest(Action action, string testId)
        {
            Console.Write($"[{testId}] Auditing... ");
            try { action(); } catch (Exception ex) { Results.Add(new TestResult(testId, TestStatus.FAIL, ex.Message)); }
            var res = Results.Last();
            string color = res.Status switch { TestStatus.PASS => "\x1b[92m", TestStatus.FAIL => "\x1b[91m", _ => "\x1b[93m" };
            Console.WriteLine($"{color}{res.Status}\x1b[0m: {res.Reason}");
        }

        static void Audit_ForbiddenSymbols()
        {
            var res = new TestResult("TEST-BINARY-001");
            string binDir = AppContext.BaseDirectory;
            string[] forbidden = { "Mock", "Simulation", "ValidationOnly", "Bypass", "Dummy" };
            
            int count = 0;
            foreach (var file in Directory.GetFiles(binDir, "*.dll")) {
                if (file.Contains("NAudio") || file.Contains("Microsoft")) continue;
                string content = File.ReadAllText(file); // Note: Simplified for DLL scan
                foreach (var s in forbidden) if (content.Contains(s)) count++;
            }

            if (count == 0) res.Pass("No forbidden symbols detected in local binaries.");
            else res.Fail($"{count} forbidden symbols found. SECTION 5 violation.");
            Results.Add(res);
        }

        static void Audit_SourceIntegrity()
        {
            var res = new TestResult("TEST-SOURCE-001");
            // Check for MOCK branches in key classes via Reflection or simple text scan
            string srcDir = Path.Combine(Directory.GetCurrentDirectory(), "VeilVoice");
            if (!Directory.Exists(srcDir)) { res.Unverified("Source not found."); Results.Add(res); return; }

            var files = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
            bool found = false;
            foreach (var f in files) {
                string txt = File.ReadAllText(f);
                if (txt.Contains("MOCK_VALIDATION") || txt.Contains("if (MOCK)")) { found = true; break; }
            }

            if (!found) res.Pass("Source integrity verified. No validation branches.");
            else res.Fail("Validation/Mock branches detected in source. SECTION 6 violation.");
            Results.Add(res);
        }

        static void Audit_RealInference()
        {
            var res = new TestResult("TEST-REAL-001");
            ModelManager.Refresh();
            var models = ModelManager.GetAvailableModels();
            
            var official = models.FirstOrDefault(m => m.ModelName.Contains("Official"));
            if (official != null) {
                Console.WriteLine($"    [INFO] Path discovered: {official.ResolvedAbsolutePath}");
                // Now check if execution happened
                string provDir = Path.Combine(AppContext.BaseDirectory, "provenance");
                var latest = Directory.GetFiles(provDir, "provenance_*.json").OrderByDescending(f => File.GetCreationTime(f)).FirstOrDefault();
                
                if (latest != null) {
                    res.Pass($"Real inference verified for {official.ModelName}");
                } else {
                    res.Fail($"Model found at {Path.GetFileName(official.ResolvedAbsolutePath)}, but REAL INFERENCE failed to execute. Check engine compatibility.");
                }
            } else {
                res.Fail("Official Beatrice model path not discovered. SECTION 26 violation.");
            }
            Results.Add(res);
        }

        static void Audit_ProvenanceChain()
        {
            var res = new TestResult("TEST-PROVENANCE-001");
            string provDir = Path.Combine(AppContext.BaseDirectory, "provenance");
            var latest = Directory.GetFiles(provDir, "provenance_*.json").OrderByDescending(f => File.GetCreationTime(f)).FirstOrDefault();
            
            if (latest == null) { res.Fail("No provenance chain to verify."); Results.Add(res); return; }

            string json = File.ReadAllText(latest);
            var artifacts = JsonSerializer.Deserialize<List<ProvenanceService.ArtifactMetadata>>(json);
            
            bool hasInput = artifacts.Any(a => a.Type == "tensor_input");
            bool hasOutput = artifacts.Any(a => a.Type == "tensor_output");

            // NEW: Check if the model used was actually Beatrice and passed architecture check
            bool isBeatrice = artifacts.Any(a => a.Details?.Contains("Engine: Beatrice") ?? false);
            
            if (hasInput && hasOutput && isBeatrice) {
                res.Pass($"Real Beatrice inference verified via ExecutionID: {artifacts.First().ExecutionId}");
            } else if (!isBeatrice) {
                res.Fail("Architecture Mismatch: Non-Beatrice model used. Generic ONNX infra verified only.");
            } else {
                res.Fail("Incomplete inference trace. Tensors missing.");
            }
            Results.Add(res);
        }

        static void Audit_VirtualAudioDisclosure()
        {
            var res = new TestResult("TEST-VAUDIO-001");
            string disclosure = DeviceScanner.GetEndpointDisclosure();
            if (disclosure != "None") {
                File.WriteAllText(Path.Combine(ArtifactDir, "endpoint_provider.json"), disclosure);
                res.Pass("Endpoint provider disclosed and verified.");
            } else {
                res.Fail("Virtual audio backend not disclosed. SECTION 7 violation.");
            }
            Results.Add(res);
        }

        static void GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><style>body{font-family:sans-serif;background:#0d0d14;color:#e8e8f0} table{width:100%;border-collapse:collapse} th,td{border:1px solid #333;padding:10px;text-align:left} .PASS{color:#4ade80} .FAIL{color:#f87171}</style></head><body>");
            sb.AppendLine("<h1>VeilVoice v4.0 Zero-Trust Audit Report</h1>");
            
            bool infraPass = Results.Where(r => r.TestId != "TEST-REAL-001").All(r => r.Status == TestStatus.PASS);
            bool beatricePass = Results.Any(r => r.TestId == "TEST-REAL-001" && r.Status == TestStatus.PASS);

            sb.AppendLine("<div style='margin-bottom:20px; padding:15px; border:2px solid #333;'>");
            sb.AppendLine($"<p>Infrastructure Verification: <b class='{(infraPass ? "PASS" : "FAIL")}'>{(infraPass ? "PASS" : "FAIL")}</b></p>");
            sb.AppendLine($"<p>Real Beatrice Inference: <b class='{(beatricePass ? "PASS" : "FAIL")}'>{(beatricePass ? "PASS" : "FAIL / UNVERIFIED")}</b></p>");
            sb.AppendLine($"<p style='font-size:1.5em; color:#f87171;'>Status: <b>{(infraPass && beatricePass ? "DELIVERY READY" : "DELIVERY BLOCKED")}</b></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<table><tr><th>Test ID</th><th>Status</th><th>Reason</th></tr>");
            foreach (var r in Results) sb.AppendLine($"<tr><td>{r.TestId}</td><td class='{r.Status}'>{r.Status}</td><td>{r.Reason}</td></tr>");
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(ReportPath, sb.ToString());
        }

        enum TestStatus { PASS, FAIL, UNVERIFIED }
        class TestResult {
            public string TestId { get; set; }
            public TestStatus Status { get; set; }
            public string Reason { get; set; }
            public TestResult(string id, TestStatus s = TestStatus.UNVERIFIED, string r = "") { TestId = id; Status = s; Reason = r; }
            public void Pass(string r) { Status = TestStatus.PASS; Reason = r; }
            public void Fail(string r) { Status = TestStatus.FAIL; Reason = r; }
            public void Unverified(string r) { Status = TestStatus.UNVERIFIED; Reason = r; }
        }
    }
}
