using BossLevel.App;
using UnityEngine;

namespace BossLevel.Audio
{
    /// <summary>
    /// Changes what the game sounds like once the fight has been decided.
    /// </summary>
    /// <remarks>
    /// The two endings want opposite treatments, and the difference is the point. Victory cuts
    /// the music and lets a single sound land in the silence, which is far more emphatic than
    /// adding something on top of a track that is still going. Defeat replaces the music
    /// instead, because a loss should sit with the player rather than being punctuated and
    /// released.
    /// <para>
    /// Separate from <see cref="UI.EndScreen"/> so the sound of an ending is not tangled up with
    /// the drawing of one — either can be changed without touching the other.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class OutcomeAudio : MonoBehaviour
    {
        [SerializeField] private GameStateMachine game;

        [Header("Victory")]
        [Tooltip("Silences the music so the victory sound lands in the quiet.")]
        [SerializeField] private bool silenceMusicOnVictory = true;

        [SerializeField] private SoundEvent victorySound;

        [Header("Defeat")]
        [Tooltip("Replaces the music entirely. Leave empty to simply stop it.")]
        [SerializeField] private AudioClip defeatMusic;

        [SerializeField] private SoundEvent defeatSound;

        private void Awake()
        {
            if (game == null)
            {
                Debug.LogError($"{nameof(OutcomeAudio)} has no {nameof(GameStateMachine)} assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            game.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (!AudioService.Exists)
            {
                return;
            }

            switch (state)
            {
                case GameState.Won:
                    OnVictory();
                    break;

                case GameState.Lost:
                    OnDefeat();
                    break;
            }
        }

        private void OnVictory()
        {
            if (silenceMusicOnVictory)
            {
                AudioService.Instance.StopMusic();
            }

            victorySound?.Play();
        }

        private void OnDefeat()
        {
            if (defeatMusic != null)
            {
                // Restarted deliberately: the defeat theme should open at its beginning even if
                // the player has just lost twice in a row.
                AudioService.Instance.PlayMusic(defeatMusic, restartIfAlreadyPlaying: true);
            }
            else
            {
                AudioService.Instance.StopMusic();
            }

            defeatSound?.Play();
        }
    }
}
