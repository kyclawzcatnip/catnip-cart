using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CatnipCart.Editor
{
    public class WebGLBuilder
    {
        [MenuItem("Build/Build WebGL")]
        public static void Build()
        {
            Debug.Log("Starting WebGL Build...");

            // Always ensure shader materials exist and regenerate scene
            // so it references them — prevents texture variant stripping
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            AutoSceneSetup.CreateRaceScene();

            // Set up player settings for WebGL
            // Disable compression to ensure it runs on GitHub pages without needing special server configs
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            
            // Build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { scenePath };
            buildPlayerOptions.locationPathName = "docs"; // GitHub Pages uses the 'docs' folder
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            Debug.Log($"Build ended with result: {report.summary.result}");
            
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("WebGL Build completed successfully!");
            }
            else
            {
                Debug.LogError("WebGL Build failed!");
            }
        }
    }
}
