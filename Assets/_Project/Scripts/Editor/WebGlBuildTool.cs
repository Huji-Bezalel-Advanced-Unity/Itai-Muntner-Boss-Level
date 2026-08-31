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

        /// <summary>
        /// Canvas size written into the generated page, and the size the itch.io embed must be
        /// set to.
        /// </summary>
        /// <remarks>
        /// 16:9, matching the aspect the arena is composed for. Unity's default of 960 by 600 is
        /// 8:5, and with a fixed orthographic camera a narrower aspect simply shows less of the
        /// arena horizontally — the edges of the fight are cropped, which is not a cosmetic
        /// problem in a game about dodging sideways.
        /// </remarks>
        private const int CanvasWidth = 1280;

        private const int CanvasHeight = 720;

        /// <summary>Where the loader config begins in Unity's generated page.</summary>
        private const string ConfigOpening = "var config = {";

        /// <summary>Injected straight after <see cref="ConfigOpening"/>. See PatchLoaderCacheControl.</summary>
        private const string CacheControlOverride = @"
        // Added by the Boss Level build tool. Unity's built-in cacheControl assumes it is
        // always handed a defined URL and calls .match on it, which throws on the loading
        // bar when it is not — this config declares no symbolsUrl, so it is not. Anything
        // defined here overrides the loader's own version.
        cacheControl: function (url) {
          if (!url) {
            return ""no-store"";
          }

          return url === config.dataUrl || url.match(/\.bundle/)
            ? ""must-revalidate""
            : ""no-store"";
        },";

        [MenuItem("Boss Level/Build WebGL", priority = 100)]
        public static void Build()
        {
            Build(diagnostic: false);
        }

        /// <summary>
        /// Builds with every diagnostic aid turned on: full exceptions with stack traces, and
        /// almost no code stripping.
        /// </summary>
        /// <remarks>
        /// A WebGL build that hangs on the loading bar usually says nothing at all, because the
        /// settings that make a build small are the same ones that make it silent. Stripping
        /// removes types only reached by reflection, and limiting exception support means a null
        /// reference does not throw so much as stop. This build is bigger and slower and will
        /// tell you what went wrong.
        /// </remarks>
        [MenuItem("Boss Level/Build WebGL (Diagnostic)", priority = 101)]
        public static void BuildDiagnostic()
        {
            Build(diagnostic: true);
        }

        private static void Build(bool diagnostic)
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

            ApplyWebGlSettings(diagnostic);

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
                LogBuildErrors(report);
                return;
            }

            WriteNoJekyllMarker();
            PatchLoaderCacheControl();
            WarnIfLoaderExpectsAMissingWorker();

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
        [MenuItem("Boss Level/Apply WebGL Settings", priority = 110)]
        public static void ApplyWebGlSettings()
        {
            ApplyWebGlSettings(diagnostic: false);
        }

        private static void ApplyWebGlSettings(bool diagnostic)
        {
            // Gzip, and deliberately WITHOUT the decompression fallback.
            //
            // The fallback decompresses in JavaScript so that a host which cannot send the
            // Content-Encoding header — GitHub Pages, for one — can still serve a compressed
            // build. It sounds like the safer choice and was, until it wasn't: the fallback does
            // its work in a web worker, so the loader unconditionally downloads a worker script,
            // and this Unity version emits neither the file nor a workerUrl in the generated
            // page. The result is a build that hangs on its loading bar with a type error and no
            // clue as to why.
            //
            // Without the fallback the browser decompresses using Content-Encoding as normal, no
            // worker is involved, and the build starts. That means it needs a host that sends
            // those headers — itch.io does. If the build must also run from GitHub Pages, set
            // the format to Disabled instead and accept a much larger download.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;

            // Caches the build in the browser, so a second visit does not download it again.
            PlayerSettings.WebGL.dataCaching = true;

            // The canvas the generated page is built around. Whatever this is, the itch.io embed
            // has to be set to the same numbers, or the page crops what the game is drawing.
            PlayerSettings.defaultWebScreenWidth = CanvasWidth;
            PlayerSettings.defaultWebScreenHeight = CanvasHeight;

            // Full exception support costs size and speed, but limiting it means a null
            // reference does not throw so much as stop — the build simply hangs, with nothing in
            // the console. That is worth paying for while diagnosing and not otherwise.
            PlayerSettings.WebGL.exceptionSupport = diagnostic
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // Download size is the whole player experience on the web — nobody waits. But High
            // strips types that are only ever reached by reflection, and DOTween in particular
            // is reflection-heavy, so Low is the setting that is actually safe to ship.
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL,
                diagnostic ? ManagedStrippingLevel.Minimal : ManagedStrippingLevel.Low);

            AssetDatabase.SaveAssets();

            Debug.Log(diagnostic
                ? $"WebGL settings applied for DIAGNOSIS: full exceptions with stack traces, " +
                  $"minimal stripping, canvas {CanvasWidth}x{CanvasHeight}. Bigger and slower, " +
                  "but it will say what went wrong."
                : $"WebGL settings applied: gzip without decompression fallback, data caching " +
                  $"on, explicit exceptions only, low stripping, canvas {CanvasWidth}x{CanvasHeight}. " +
                  $"Set the itch.io embed to {CanvasWidth} by {CanvasHeight} to match.");
        }

        /// <summary>Checks the mistakes that only show up once the build is attempted or hosted.</summary>
        private static bool Validate(IReadOnlyList<string> scenes)
        {
            if (!IsWebGlSupportInstalled())
            {
                // Unity's own message for this is "Error building player because build target
                // was unsupported", which names neither the module nor where to get it.
                Debug.LogError(
                    "WebGL Build Support is not installed for this version of Unity. " +
                    "Install it from Unity Hub — Installs, the gear icon on this version, " +
                    "Add modules — then switch platform in File ▸ Build Profiles before building.");

                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                // Not fatal — the build will switch by itself — but the switch reimports every
                // asset in the project, which is a long wait to hit unannounced mid-build.
                Debug.LogWarning(
                    "The active build target is not WebGL. Building will switch it, which " +
                    "reimports the whole project first. Switching in File ▸ Build Profiles " +
                    "beforehand makes that wait visible rather than mysterious.");
            }

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

        /// <summary>Whether this Unity installation can build for WebGL at all.</summary>
        /// <remarks>
        /// The playback engine directory is empty — or the call throws, depending on version —
        /// when the platform module is missing, which is the cheapest reliable way to ask.
        /// </remarks>
        private static bool IsWebGlSupportInstalled()
        {
            try
            {
                var engineDirectory =
                    BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.WebGL, BuildOptions.None);

                return !string.IsNullOrEmpty(engineDirectory);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Repeats the build's own error messages, which otherwise sit above a summary that
        /// says only how many there were.
        /// </summary>
        private static void LogBuildErrors(BuildReport report)
        {
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"Build error in '{step.name}': {message.content}");
                    }
                }
            }
        }

        /// <summary>
        /// Stops GitHub Pages running the build through Jekyll, which skips files and folders
        /// beginning with an underscore — several of Unity's.
        /// </summary>
        private static void WriteNoJekyllMarker()
        {
            File.WriteAllText(Path.Combine(OutputDirectory, ".nojekyll"), string.Empty);
        }

        /// <summary>
        /// Adds a null-safe <c>cacheControl</c> to the generated page's loader config.
        /// </summary>
        /// <remarks>
        /// Unity's built-in implementation assumes it is always handed a defined URL and calls
        /// <c>.match</c> on it. The generated config declares no <c>symbolsUrl</c>, so it is
        /// handed <c>undefined</c> and throws — leaving the game stuck on its loading bar with
        /// nothing but a type error to show for it. Anything defined in the config overrides the
        /// loader's own version, so guarding it here is enough.
        /// <para>
        /// Applied after every build because Unity regenerates the page each time, which would
        /// otherwise quietly undo the fix.
        /// </para>
        /// </remarks>
        private static void PatchLoaderCacheControl()
        {
            var pagePath = Path.Combine(OutputDirectory, "index.html");

            if (!File.Exists(pagePath))
            {
                Debug.LogWarning($"No index.html at {pagePath} to patch.");
                return;
            }

            var page = File.ReadAllText(pagePath);

            if (page.Contains("cacheControl"))
            {
                return;
            }

            var configStart = page.IndexOf(ConfigOpening, System.StringComparison.Ordinal);

            if (configStart < 0)
            {
                Debug.LogWarning(
                    "Could not find the loader config in index.html, so cacheControl was not " +
                    "guarded. If the build hangs on its loading bar, that is why.");

                return;
            }

            page = page.Insert(configStart + ConfigOpening.Length, CacheControlOverride);

            File.WriteAllText(pagePath, page);

            Debug.Log("Patched index.html with a null-safe cacheControl.");
        }

        /// <summary>
        /// Warns when the loader will try to download a worker script the build did not produce.
        /// </summary>
        /// <remarks>
        /// The decompression fallback decompresses in a web worker, so the loader downloads one
        /// unconditionally — but Unity does not always emit the file or declare a
        /// <c>workerUrl</c> in the generated page. When that happens the build hangs on its
        /// loading bar with nothing but a type error, which is a miserable thing to diagnose from
        /// a hosted page. Cheaper to notice it here, where the files are still in front of us.
        /// </remarks>
        private static void WarnIfLoaderExpectsAMissingWorker()
        {
            var buildDirectory = Path.Combine(OutputDirectory, "Build");

            if (!Directory.Exists(buildDirectory))
            {
                return;
            }

            var loader = Directory.GetFiles(buildDirectory, "*.loader.js");

            if (loader.Length == 0)
            {
                return;
            }

            var wantsWorker = File.ReadAllText(loader[0]).Contains("workerUrl");
            var hasWorker = Directory.GetFiles(buildDirectory, "*worker*").Length > 0;

            if (!wantsWorker || hasWorker)
            {
                return;
            }

            Debug.LogError(
                "This build's loader downloads a worker script, but no worker file was produced. " +
                "It will hang on the loading bar. Turn off Decompression Fallback in Player " +
                "Settings — the fallback is what needs the worker.");
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
