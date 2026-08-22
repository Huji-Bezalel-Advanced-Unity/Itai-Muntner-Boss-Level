using BossLevel.App;
using BossLevel.Audio;
using BossLevel.Feel;
using BossLevel.Player;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// Freezes the fight and offers a way out of it.
    /// </summary>
    /// <remarks>
    /// Pausing sets the time scale to zero, which stops everything at once — physics,
    /// coroutines, projectiles mid-flight, a vent halfway through its warning — because all of
    /// gameplay is driven by scaled time. Only the interface runs unscaled, which is what lets
    /// this menu still fade in while the world behind it is stopped dead.
    /// <para>
    /// Player input is switched off as well as frozen. A zero time scale does not stop
    /// <c>Update</c> running, so a dash begun while paused would start and never reach its end
    /// time, leaving the player stuck mid-dash the moment they resumed.
    /// </para>
    /// <para>
    /// Pause input is read directly rather than through <see cref="PlayerInputReader"/>, which
    /// is the one deliberate exception to routing input through a single place. Pausing has to
    /// keep working precisely when gameplay input has been taken away, so it cannot depend on
    /// the component that gets disabled to take it away.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Used to refuse pausing once the fight has been decided.")]
        [SerializeField] private GameStateMachine game;

        [SerializeField] private PlayerInputReader playerInput;

        [Tooltip("Optional, but assign it if the scene has one — see the class remarks.")]
        [SerializeField] private HitStop hitStop;

        [Header("Layout")]
        [Tooltip("The panel in the middle. Scales up as the menu appears.")]
        [SerializeField] private RectTransform panel;

        [SerializeField, Range(0.5f, 1f)] private float panelStartScale = 0.9f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Sound (optional)")]
        [SerializeField] private SoundEvent pauseSound;
        [SerializeField] private SoundEvent resumeSound;

        private CanvasGroup _group;
        private Tween _fade;
        private Tween _panelScale;
        private bool _isPaused;
        private bool _restoreInputOnResume;

        /// <summary>Whether the game is currently paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Pausing is refused once the fight is over — there is nothing left to pause.</summary>
        private bool CanPause => game == null || game.State == GameState.Fighting;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitToMenu);
            }
        }

        private void OnDisable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitToMenu);
            }

            _fade?.Kill();
            _panelScale?.Kill();

            // Being disabled while paused would otherwise leave the game frozen with nothing
            // able to unfreeze it.
            if (_isPaused)
            {
                RestoreTime();
            }
        }

        private void Update()
        {
            if (!WasPausePressed())
            {
                return;
            }

            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private static bool WasPausePressed()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return false;
            }

            return keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame;
        }

        private void Pause()
        {
            if (!CanPause)
            {
                return;
            }

            _isPaused = true;

            // Told to stand down rather than stopped: a hit stop finishing normally sets time
            // back to one, which would un-pause the game on its own a moment later.
            if (hitStop != null)
            {
                hitStop.Cancel();
            }

            Time.timeScale = 0f;

            if (playerInput != null)
            {
                // Remembered rather than assumed, so resuming cannot hand control back during a
                // moment the game had already taken it away.
                _restoreInputOnResume = playerInput.enabled;
                playerInput.enabled = false;
            }

            pauseSound?.Play();
            Show();
        }

        private void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            RestoreTime();

            if (playerInput != null && _restoreInputOnResume)
            {
                playerInput.enabled = true;
            }

            resumeSound?.Play();
            Hide();
        }

        private void Restart()
        {
            LeaveForAnotherScene();

            if (SceneLoader.Exists)
            {
                SceneLoader.Instance.Load(SceneId.BossLevel);
                return;
            }

            // Playing the fight scene directly, without Bootstrap, is a normal way to work on
            // it, so restart reloads in place rather than failing on a service that only exists
            // in a full run.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitToMenu()
        {
            LeaveForAnotherScene();

            if (!SceneLoader.Exists)
            {
                Debug.LogWarning(
                    "No SceneLoader — start from the Bootstrap scene to reach the menu.", this);
                return;
            }

            SceneLoader.Instance.Load(SceneId.MainMenu);
        }

        /// <summary>
        /// Unfreezes and hides the menu before a scene change.
        /// </summary>
        /// <remarks>
        /// Restoring time first is essential: a scene loaded while the time scale is zero opens
        /// frozen, with no menu left to unfreeze it.
        /// </remarks>
        private void LeaveForAnotherScene()
        {
            RestoreTime();
            Hide();
        }

        private void RestoreTime()
        {
            _isPaused = false;
            Time.timeScale = 1f;
        }

        private void Show()
        {
            _group.blocksRaycasts = true;
            _group.interactable = true;

            _fade?.Kill();

            // Unscaled throughout, because the whole point is that everything else is stopped.
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

        private void Hide()
        {
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _fade?.Kill();
            _fade = _group.DOFade(0f, fadeDuration).SetUpdate(true);
        }
    }
}
