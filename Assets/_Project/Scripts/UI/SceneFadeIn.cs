using DG.Tweening;
using UnityEngine;

namespace BossLevel.UI
{
    /// <summary>
    /// Fades a scene up from black when it starts.
    /// </summary>
    /// <remarks>
    /// The loading screen already covers a transition made through <see cref="App.SceneLoader"/>,
    /// but not every entry into a scene goes through it — the fight can be started directly from
    /// the editor, and retry falls back to reloading in place when no loader exists. Putting the
    /// fade in the scene itself means it opens the same way however it was reached.
    /// <para>
    /// It never blocks raycasts, even while fully opaque, so it cannot swallow a click during
    /// the fade.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class SceneFadeIn : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.45f;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void Start()
        {
            // Unscaled, so the scene still opens if it begins paused or mid-freeze.
            _group
                .DOFade(0f, duration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
