#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless WebGL build for GitHub Pages.
/// Unity.exe -batchmode -nographics -quit -projectPath ... -buildTarget WebGL -executeMethod CathedralWebGlBuild.Build
/// Optional env WEBGL_OUT = absolute output folder.
/// </summary>
public static class CathedralWebGlBuild
{
    public static void Build()
    {
        try
        {
            const string scene = "Assets/Scenes/CathedralWalk.unity";
            if (!File.Exists(scene))
                throw new Exception("Missing scene: " + scene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scene, true)
            };

            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // Gzip keeps files under GitHub's 100 MB limit; Decompression Fallback works on Pages.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 512;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);

            string outDir = Environment.GetEnvironmentVariable("WEBGL_OUT");
            if (string.IsNullOrWhiteSpace(outDir))
                outDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL"));

            Directory.CreateDirectory(outDir);
            Debug.Log("[CathedralWebGlBuild] Output: " + outDir);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"[CathedralWebGlBuild] Result={summary.result} errors={summary.totalErrors} size={summary.totalSize}");

            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError("[CathedralWebGlBuild] " + msg.content);
                    }
                }
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("[CathedralWebGlBuild] " + ex);
            EditorApplication.Exit(1);
        }
    }
}
#endif
