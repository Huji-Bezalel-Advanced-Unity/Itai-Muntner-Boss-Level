using System.Collections;
using BossLevel.Common;
using BossLevel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossLevel.App
{
    /// <summary>
    /// The single way any scene is loaded: fade in, load in the background, fade out.
    /// </summary>
    /// <remarks>
    /// Created once by the Bootstrap scene and kept alive for the session, so the loading screen
    /// it owns survives the very transitions it is covering.
    /// </remarks>
    public class SceneLoader : PersistentSingleton<SceneLoader>
    {
        /// <summary>
        /// The largest slice of a single frame that counts towards the minimum display time.
        /// </summary>
        /// <remarks>
        /// A hitch — a domain reload, a shader compile, the first frame of play mode — can carry
        /// a delta of over a second. Counting it in full would spend the whole minimum display
        /// time in one frame, which is exactly the flash the minimum exists to prevent.
        /// </remarks>
        private const float MaxPacingStep = 0.05f;

        [SerializeField] private SceneCatalog catalog;
        [SerializeField] private LoadingScreen loadingScreen;

        [Tooltip("Shortest time the loading screen stays up. Without a floor, a fast load makes " +
                 "it flash on and straight off again, which reads as a glitch rather than a load.")]
        [SerializeField, Min(0f)] private float minimumDisplayTime = 0.75f;

        /// <summary>True while a load is in progress. Guards against overlapping requests.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Loads a scene, covering the transition with the loading screen.</summary>
        public void Load(SceneId id)
        {
            if (IsLoading)
            {
                return;
            }

            StartCoroutine(LoadRoutine(id));
        }

        private IEnumerator LoadRoutine(SceneId id)
        {
            var sceneName = catalog.NameFor(id);

            if (string.IsNullOrEmpty(sceneName))
            {
                yield break;
            }

            IsLoading = true;

            yield return loadingScreen.Show();

            var operation = SceneManager.LoadSceneAsync(sceneName);

            // Holding activation lets the loading screen stay up for its minimum time and lets
            // the bar finish travelling, rather than the new scene appearing mid-fade.
            operation.allowSceneActivation = false;

            var elapsed = 0f;

            while (operation.progress < 0.9f || elapsed < minimumDisplayTime)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxPacingStep);

                // Unity's reported progress stops at 0.9 while activation is held, so it is
                // remapped — otherwise the bar visibly stalls at ninety per cent every time.
                var loadProgress = Mathf.Clamp01(operation.progress / 0.9f);

                var timeProgress = minimumDisplayTime > 0f
                    ? Mathf.Clamp01(elapsed / minimumDisplayTime)
                    : 1f;

                // Whichever is further behind. Scenes this small finish loading within a frame
                // or two, so reporting the real figure alone would show a full bar immediately
                // and never animate at all — the time term is what makes it read as loading.
                loadingScreen.SetProgress(Mathf.Min(loadProgress, timeProgress));

                yield return null;
            }

            loadingScreen.SetProgress(1f);
            operation.allowSceneActivation = true;

            yield return operation;

            yield return loadingScreen.Hide();

            IsLoading = false;
        }
    }
}
