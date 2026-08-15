using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// Covers a scene transition and reports how far along it is.
    /// </summary>
    /// <remarks>
    /// The object stays active for the whole session and hides itself by fading its
    /// <see cref="CanvasGroup"/> rather than by deactivating. Deactivating would stop
    /// <see cref="Awake"/> ever running on the first show, which is a lifecycle trap worth
    /// avoiding for the cost of one always-present canvas.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("Optional. A filled Image whose fillAmount tracks load progress.")]
        [SerializeField] private Image progressFill;

        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        private CanvasGroup _group;
        private Tween _fade;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            SetProgress(0f);
        }

        /// <summary>Fades the screen in. Yieldable, so a loader can wait for it.</summary>
        public IEnumerator Show()
        {
            SetProgress(0f);

            // Blocking raycasts immediately stops the player clicking the menu behind a screen
            // that is still fading in.
            _group.blocksRaycasts = true;

            yield return FadeTo(1f);
        }

        /// <summary>Fades the screen back out.</summary>
        public IEnumerator Hide()
        {
            yield return FadeTo(0f);

            _group.blocksRaycasts = false;
        }

        /// <summary>Sets the progress bar, from 0 to 1.</summary>
        public void SetProgress(float normalised)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(normalised);
            }
        }

        private IEnumerator FadeTo(float target)
        {
            _fade?.Kill();

            // Unscaled, because a load can begin while the game is paused or mid-hit-stop, and a
            // loading screen that will not fade because time is stopped is a hang.
            _fade = _group.DOFade(target, fadeDuration).SetUpdate(true);

            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        private void OnDestroy()
        {
            _fade?.Kill();
        }
    }
}
