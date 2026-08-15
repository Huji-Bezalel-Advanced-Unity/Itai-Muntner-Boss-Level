using System;
using System.Collections;
using BossLevel.Boss;
using BossLevel.Combat;
using BossLevel.Player;
using UnityEngine;

namespace BossLevel.App
{
    /// <summary>The stages an encounter passes through.</summary>
    public enum GameState
    {
        /// <summary>Before the fight starts. The player cannot act.</summary>
        Intro,

        /// <summary>Normal play.</summary>
        Fighting,

        /// <summary>The boss is dead.</summary>
        Won,

        /// <summary>The player is dead.</summary>
        Lost,
    }

    /// <summary>
    /// Owns the shape of an encounter: intro, fight, and whichever ending arrives first.
    /// </summary>
    /// <remarks>
    /// It holds no gameplay logic of its own. Its job is to decide when the fight is over and to
    /// take the controls away at the two moments the player should not have them — before the
    /// fight begins, and once it has been decided.
    /// <para>
    /// Because all input is funnelled through <see cref="PlayerInputReader"/>, removing control
    /// is a single object being disabled, and nothing else in the project needs to know these
    /// states exist. The UI listens to <see cref="StateChanged"/> and reacts.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class GameStateMachine : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BossController boss;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerInputReader playerInput;

        [Header("Timing")]
        [Tooltip("How long the player waits before control is handed over. Should be shorter " +
                 "than the boss's own opening delay so the fight never starts before they can move.")]
        [SerializeField, Min(0f)] private float introDuration = 1f;

        [Tooltip("Pause after the fight is decided, before the end screen is announced, so the " +
                 "final moment is allowed to land.")]
        [SerializeField, Min(0f)] private float endingDelay = 1.5f;

        /// <summary>Raised on every state change. The UI listens to this.</summary>
        public event Action<GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Intro;

        private bool _endingStarted;

        private void Awake()
        {
            if (boss == null || playerHealth == null || playerInput == null)
            {
                Debug.LogError($"{nameof(GameStateMachine)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            boss.Defeated += OnBossDefeated;
            playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            boss.Defeated -= OnBossDefeated;
            playerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            StartCoroutine(RunIntro());
        }

        private IEnumerator RunIntro()
        {
            EnterState(GameState.Intro);
            playerInput.enabled = false;

            yield return new WaitForSeconds(introDuration);

            playerInput.enabled = true;
            EnterState(GameState.Fighting);
        }

        private void OnBossDefeated()
        {
            StartCoroutine(EndFight(GameState.Won));
        }

        private void OnPlayerDied()
        {
            // The boss does not stop on its own when the player dies, so it is told to.
            boss.StopFighting();
            StartCoroutine(EndFight(GameState.Lost));
        }

        private IEnumerator EndFight(GameState ending)
        {
            // The player and the boss can die on the same frame. Only the first ending counts,
            // and the flag is checked rather than the state because the state does not change
            // until after the delay below.
            if (_endingStarted)
            {
                yield break;
            }

            _endingStarted = true;

            // Control goes immediately; the announcement waits, so the final moment is allowed
            // to land before an end screen covers it.
            playerInput.enabled = false;

            yield return new WaitForSeconds(endingDelay);

            EnterState(ending);
        }

        private void EnterState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
