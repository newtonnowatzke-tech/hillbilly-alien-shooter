using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HillbillyAlienShooter.EditorTools
{
    /// <summary>
    /// One-stop WebGL build pipeline, usable two ways:
    ///
    ///  • Locally: Tools ▸ Hillbilly ▸ Build WebGL — one click, output in build/WebGL.
    ///  • CI (GameCI unity-builder): buildMethod
    ///    HillbillyAlienShooter.EditorTools.WebGLBuilder.BuildWebGL
    ///
    /// Because this repo ships only scripts (no serialized scene/settings), the
    /// pipeline is self-sufficient: it regenerates the farm scene, forces the
    /// New Input System on (fresh CI projects default to the legacy manager),
    /// and creates+assigns a URP pipeline asset if none exists (otherwise every
    /// material renders hot pink). Compression is Gzip WITH decompression
    /// fallback so the build runs on GitHub Pages / itch.io with zero server
    /// header configuration.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string DefaultBuildPath = "build/WebGL";
        private const string SettingsFolder = "Assets/_Project/Settings";

        [MenuItem("Tools/Hillbilly/Build WebGL", priority = 1)]
        public static void BuildWebGLMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build WebGL",
                    "Rebuilds the farm scene, applies WebGL settings, and builds the\n" +
                    "browser version into build/WebGL (takes a few minutes).\n\nContinue?",
                    "Build it!", "Cancel"))
                return;

            var report = BuildInternal(DefaultBuildPath);
            if (report != null && report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("WebGL build done!",
                    $"Output: {DefaultBuildPath}\n\n" +
                    "To playtest locally, serve the folder over HTTP (browsers block\n" +
                    "file:// WebGL), e.g.:\n\n" +
                    "    cd build/WebGL\n    python -m http.server 8000\n\n" +
                    "then open http://localhost:8000 — or upload the folder to itch.io.",
                    "Yee-haw");
                EditorUtility.RevealInFinder(DefaultBuildPath);
            }
        }

        /// <summary>CI entry point (GameCI passes -customBuildPath; exits non-zero on failure).</summary>
        public static void BuildWebGL()
        {
            string path = GetArg("-customBuildPath") ?? DefaultBuildPath;
            var report = BuildInternal(path);

            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[Hillbilly] WebGL build {(ok ? "SUCCEEDED" : "FAILED")} → {path}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        // -------------------------------------------------------------------
        // Core
        // -------------------------------------------------------------------
        private static BuildReport BuildInternal(string outputPath)
        {
            EnsureNewInputSystemEnabled();
            EnsureUrpPipelineAssigned();
            FarmSceneBuilder.BuildFarmSceneHeadless();
            ApplyWebGLPlayerSettings();

            return BuildPipeline.BuildPlayer(
                new[] { FarmSceneBuilder.ScenePath },
                outputPath,
                BuildTarget.WebGL,
                BuildOptions.None);
        }

        private static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.companyName = "Hillbilly Games";
            PlayerSettings.productName = "Hillbilly Alien Shooter";
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear; // match the URP template look

            // Gzip + fallback = works on any static host (GitHub Pages, itch.io)
            // without Content-Encoding header configuration.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
        }

        /// <summary>
        /// Fresh (CI-generated) projects default to the legacy Input Manager,
        /// which would make PlayerInputHandler throw at runtime. There's no
        /// public API for this setting, so flip it via SerializedObject:
        /// 0 = legacy, 1 = new Input System, 2 = both.
        /// </summary>
        private static void EnsureNewInputSystemEnabled()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Hillbilly] Could not load ProjectSettings.asset to check input handler.");
                return;
            }

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[Hillbilly] activeInputHandler property not found; check input settings manually.");
                return;
            }

            if (prop.intValue != 2)
            {
                prop.intValue = 2; // Both — matches SETUP.md guidance
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                Debug.Log("[Hillbilly] Enabled Input System (Active Input Handling = Both).");
            }
        }

        /// <summary>
        /// If no scriptable render pipeline is assigned (fresh CI project), create
        /// a URP asset + renderer and assign them, otherwise URP-shader materials
        /// render pink. A local project made from the URP template already has
        /// one, so this is a no-op there.
        /// </summary>
        private static void EnsureUrpPipelineAssigned()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
                return;

            FarmSceneBuilder.EnsureFolder(SettingsFolder);

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, $"{SettingsFolder}/URP_Renderer.asset");

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, $"{SettingsFolder}/URP_Pipeline.asset");

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("[Hillbilly] Created and assigned a URP pipeline asset (none was configured).");
        }

        /// <summary>Reads "-flag value" style args passed by GameCI / the command line.</summary>
        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
