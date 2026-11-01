using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AbandonedAsylum.BuildUtils
{
    public static class BuildGame
    {
        public static string BuildOutputDir => Path.Combine(Directory.GetCurrentDirectory(), "Builds", "StandaloneWindows64");
        public const string EXE_NAME = "AbandonedAsylum.exe";
        public static string ZipOutputPath => Path.Combine(Directory.GetCurrentDirectory(), "Builds", "AbandonedAsylum_Release.zip");

        [MenuItem("Build/🚀 BUILD STANDALONE WIN64 (Ctrl+Alt+B) %&b", priority = 1)]
        public static void PerformBuildToDownloads()
        {
            BuildInternal(createZip: false);
        }

        [MenuItem("Build/📦 BUILD & NÉN FILE ZIP PHÁT HÀNH (RELEASE)", priority = 2)]
        public static void PerformBuildAndZip()
        {
            BuildInternal(createZip: true);
        }

        private static void BuildInternal(bool createZip)
        {
            string outputDir = BuildOutputDir;
            string zipPath = ZipOutputPath;

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string exePath = Path.Combine(outputDir, EXE_NAME);
            string primaryScene = "Assets/Scenes/Karlheinz_Asylum_Main.unity";
            string[] scenes = new string[] { primaryScene };

            Debug.Log($"<color=cyan>[BuildGame]</color> 🔨 Bắt đầu build Standalone Windows 64-bit...");
            Debug.Log($"<color=cyan>[BuildGame]</color> Output: {exePath}");
            Debug.Log($"<color=cyan>[BuildGame]</color> Scene: {scenes[0]}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log($"<color=green>[BuildGame] ✅ BUILD THÀNH CÔNG RỰC RỠ!</color> File: {exePath} ({sizeMb} MB) | Thời gian: {summary.totalTime.TotalSeconds:F1}s");

                if (createZip)
                {
                    try
                    {
                        if (File.Exists(zipPath)) File.Delete(zipPath);
                        Debug.Log($"<color=cyan>[BuildGame]</color> 📦 Đang nén toàn bộ thư mục game sang file zip: {zipPath}...");
                        ZipFile.CreateFromDirectory(outputDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                        long zipSizeMb = new FileInfo(zipPath).Length / (1024 * 1024);
                        Debug.Log($"<color=green>[BuildGame] ✅ ĐÃ NÉN XONG FILE ZIP ({zipSizeMb} MB)! Đường dẫn gói phát hành:</color> {zipPath}");
                        EditorUtility.RevealInFinder(zipPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[BuildGame] Lỗi khi nén file zip: {ex.Message}");
                        EditorUtility.RevealInFinder(exePath);
                    }
                }
                else
                {
                    EditorUtility.RevealInFinder(exePath);
                }
            }
            else
            {
                Debug.LogError($"<color=red>[BuildGame] ❌ BUILD THẤT BẠI!</color> Kết quả: {summary.result} ({summary.totalErrors} lỗi)");
            }
        }
    }
}
