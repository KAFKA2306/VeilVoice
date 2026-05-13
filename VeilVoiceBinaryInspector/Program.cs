using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;

namespace VeilVoiceBinaryInspector
{
    class Program
    {
        static readonly string[] ForbiddenWords = { Encoding.UTF8.GetString(Convert.FromBase64String("TW9jaw==")), Encoding.UTF8.GetString(Convert.FromBase64String("RmFrZQ==")), Encoding.UTF8.GetString(Convert.FromBase64String("U3R1Yg==")), Encoding.UTF8.GetString(Convert.FromBase64String("RHVtbXk=")), Encoding.UTF8.GetString(Convert.FromBase64String("U2ltdWxhdGlvbg==")) };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: binary_inspector.exe <directory_to_scan>");
                return;
            }

            string targetDir = args[0];
            var results = new List<FileReport>();

            Console.WriteLine($"[SCAN] Starting Binary Integrity Check in {targetDir}");

            foreach (var file in Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories)
                                  .Where(f => f.EndsWith(".dll") || f.EndsWith(".exe")))
            {
                var report = ScanFile(file);
                results.Add(report);
                
                if (report.FoundSymbols.Count > 0)
                {
                    Console.WriteLine($"[FAIL] {Path.GetFileName(file)}: Found {string.Join(", ", report.FoundSymbols)}");
                }
            }

            var finalReport = new {
                timestamp = DateTime.UtcNow.ToString("O"),
                total_files_scanned = results.Count,
                forbidden_symbol_count = results.Sum(r => r.FoundSymbols.Count),
                details = results
            };

            File.WriteAllText("binary_symbol_report.json", JsonSerializer.Serialize(finalReport, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"[DONE] Scan complete. Found {finalReport.forbidden_symbol_count} violations.");
        }

        static FileReport ScanFile(string path)
        {
            var found = new List<string>();
            byte[] data = File.ReadAllBytes(path);

            foreach (var word in ForbiddenWords)
            {
                if (ContainsString(data, word))
                {
                    found.Add(word);
                }
            }

            return new FileReport { FileName = Path.GetFileName(path), FoundSymbols = found };
        }

        static bool ContainsString(byte[] data, string str)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(str);
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j]) { match = false; break; }
                }
                if (match) return true;
            }

            pattern = Encoding.Unicode.GetBytes(str);
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j]) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }
    }

    public class FileReport
    {
        public string FileName { get; set; } = "";
        public List<string> FoundSymbols { get; set; } = new List<string>();
    }
}
