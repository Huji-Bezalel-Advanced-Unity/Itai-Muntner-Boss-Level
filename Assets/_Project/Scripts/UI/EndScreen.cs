using BossLevel.App;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// The screen shown once the fight is decided, offering another attempt.
    /// </summary>
    /// <remarks>
    /// Hidden by <see cref="CanvasGroup"/> alpha rather than by deactivating the object, so its
    /// <see cref="Awake"/> is guaranteed to have run before the fight can end.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private GameStateMachine game;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [Tooltip("Optional. The panel holding the title and buttons — it scales up as the " +
                 "screen fades in, so the result arrives rather than simply being there.")]
        [SerializeField] private RectTransform panel;

        [SerializeField, Range(0.5f, 1f)] private float panelStartScale = 0.85f;

        [SerializeField] private string wonTitle = "VICTORY";
        [SerializeField] private string lostTitle = "DEFEATED";

        [SerializeField, Min(0f)] private float fadeDuration = 0.4f;

        private CanvasGroup _group;
        private Tween _fade;
        private Tween _panelScale;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            if (game == null)
            {
                Debug.LogError($"{nameof(EndScreen)} has no {nameof(GameStateMachine)} assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(ReturnToMenu);
            }
        }

        private void OnDisable()
        {
            game.StateChanged -= OnStateChanged;

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ReturnToMenu);
            }

            _fade?.Kill();
            _panelScale?.Kill();
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Won && state != GameState.Lost)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = state == GameState.Won ? wonTitle : lostTitle;
            }

            Show();
        }

        private void Show()
        {
            _group.blocksRaycasts = true;

            _fade?.Kill();

            // Unscaled throughout: the fight may still be frozen by the hit stop from the blow
            // that ended it, and an end screen that will not appear until time resumes reads as
            // the game having hung.
            _fade = _group.DOFade(1f, fadeDuration).SetUpdate(true);

            if (panel == null)
            {
                return;
            }

            _panelScale?.Kill();

            panel.localScale = Vector3.one * panelStartScale;

            _panelScale = panel
                .DOScale(Vector3.one, fadeDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void Retry()
        {
            if (SceneLoader.Exists)
            {
                SceneLoader.Instance.Load(SceneId.BossLevel);
                return;
            }

            // Playing the fight scene on its own, without going through Bootstrap, is the normal
            // way to iterate on it. Reloading in place keeps Retry working there instead of
            // failing on a service that only exists in a full run.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToMenu()
        {
            if (!SceneLoader.Exists)
            {
                Debug.LogWarning(
                    "No SceneLoader — start from the Bootstrap scene to reach the menu.", this);
                return;
            }

            SceneLoader.Instance.Load(SceneId.MainMenu);
        }
    }
}
