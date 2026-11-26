using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Underpin.SlotMachine.EditorTools
{
    /// <summary>
    /// One-click (or command line) WebGL build into /Build/WebGL.
    ///
    /// Command line:
    ///   Unity -batchmode -quit -projectPath . -executeMethod Underpin.SlotMachine.EditorTools.WebGLBuilder.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuilder
    {
        public const string OutputPath = "Build/WebGL";

        [MenuItem("Underpin/Build WebGL", priority = 20)]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            bool success = Build();
            EditorApplication.Exit(success ? 0 : 1);
        }

        private static bool Build()
        {
            if (!File.Exists(SlotSceneBuilder.ScenePath))
            {
                SlotSceneBuilder.BuildScene();
            }

            PlayerSettings.companyName = "Amartya Vikram Singh";
            PlayerSettings.productName = "Slot Machine";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            // No compression: the build then runs from any static host (GitHub Pages included)
            // without special Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SlotSceneBuilder.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log(ok
                ? $"[WebGLBuilder] Build succeeded: {report.summary.totalSize / (1024 * 1024)} MB in {OutputPath}"
                : $"[WebGLBuilder] Build failed: {report.summary.result}");
            return ok;
        }
    }
}
