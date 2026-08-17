using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BossLevel.Editor
{
    /// <summary>
    /// Configures and produces the WebGL build, and checks the things that quietly break it.
    /// </summary>
    /// <remarks>
    /// A WebGL build fails in ways that are invisible until it is hosted — a compression format
    /// the server cannot describe, a scene missing from the list, a first scene that is not the
    /// bootstrap. Encoding the settings here means they are applied identically every time and
    /// are reviewable as source, rather than being a checklist someone has to remember.
    /// <para>
    /// It builds into <c>docs/</c> because GitHub Pages can serve that folder from the default
    /// branch with no extra configuration, which is the shortest path from a commit to a
    /// playable link.
    /// </para>
    /// </remarks>
    public static class WebGlBuildTool
    {
        /// <summary>Where the build lands. GitHub Pages serves this folder directly.</summary>
        private const string OutputDirectory = "docs";

        /// <summary>The scene the game must start from, since it creates the services.</summary>
        private const string RequiredFirstScene = "Bootstrap";

        [MenuItem("Boss Level/Build WebGL", priority = 100)]
        public static void Build()
        {
            var scenes = EnabledScenePaths();

            if (scenes.Length == 0)
            {
                Debug.LogError("No scenes are enabled in Build Settings, so there is nothing to build.");
                return;
            }

            if (!Validate(scenes))
            {
                return;
            }

            ApplyWebGlSettings();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"WebGL build {summary.result} after {summary.totalErrors} errors.");
                return;
            }

            WriteNoJekyllMarker();

            var megabytes = summary.totalSize / (1024f * 1024f);

            Debug.Log(
                $"WebGL build succeeded in {summary.totalTime.TotalSeconds:F0}s, " +
                $"{megabytes:F1} MB, written to {OutputDirectory}/. " +
                "Commit it and enable GitHub Pages on this branch with /docs as the source.");
        }

        /// <summary>
        /// Applies the player settings a hosted WebGL build needs.
        /// </summary>
        /// <remarks>
        /// Exposed separately so the settings can be applied and inspected without waiting for a
        /// full build to finish.
        /// </remarks>
        [MenuItem("Boss Level/Apply WebGL Settings", priority = 101)]
        public static void ApplyWebGlSettings()
        {
            // Gzip with a decompression fallback, and the fallback is the important half. A
            // static host such as GitHub Pages cannot send the Content-Encoding header that
            // compressed Unity builds normally rely on, so without the fallback the loader
            // fails outright and the page shows nothing but an error.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Caches the build in the browser, so a second visit does not download it again.
            PlayerSettings.WebGL.dataCaching = true;

            // Full exception support costs both size and speed. Explicitly thrown exceptions are
            // enough to diagnose anything this game does.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // Download size is the whole player experience on the web — nobody waits.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);

            AssetDatabase.SaveAssets();

            Debug.Log("WebGL player settings applied: gzip with decompression fallback, " +
                      "data caching on, explicit exceptions only, high stripping.");
        }

        /// <summary>Checks the mistakes that only show up once the build is hosted.</summary>
        private static bool Validate(IReadOnlyList<string> scenes)
        {
            var firstScene = Path.GetFileNameWithoutExtension(scenes[0]);

            if (firstScene == RequiredFirstScene)
            {
                return true;
            }

            // Starting anywhere else means the persistent services are never created, so the
            // first attempt to change scene fails — in a build, with no console to explain it.
            Debug.LogError(
                $"The first enabled scene is '{firstScene}', but it must be '{RequiredFirstScene}'. " +
                "Reorder them in File ▸ Build Profiles.");

            return false;
        }

        /// <summary>
        /// Stops GitHub Pages running the build through Jekyll, which skips files and folders
        /// beginning with an underscore — several of Unity's.
        /// </summary>
        private static void WriteNoJekyllMarker()
        {
            File.WriteAllText(Path.Combine(OutputDirectory, ".nojekyll"), string.Empty);
        }

        private static string[] EnabledScenePaths()
        {
            var paths = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    paths.Add(scene.path);
                }
            }

            return paths.ToArray();
        }
    }
}
