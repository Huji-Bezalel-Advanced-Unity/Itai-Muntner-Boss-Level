using System.Collections;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Freezes the game for a fraction of a second so an impact lands.
    /// </summary>
    /// <remarks>
    /// The cheapest piece of feel in the project and one of the most effective: a hit with no
    /// pause reads as a number changing, while the same hit with fifty milliseconds of stillness
    /// after it reads as weight. The player never consciously notices it.
    /// <para>
    /// Everything here runs on unscaled time. Waiting on scaled time while time is stopped is a
    /// coroutine that never resumes — the game would simply hang on the first hit.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class HitStop : MonoBehaviour
    {
        [Tooltip("Default freeze length. Long enough to feel, short enough not to read as a stall.")]
        [SerializeField, Range(0f, 0.3f)] private float defaultDuration = 0.05f;

        private Coroutine _running;

        /// <summary>Freezes for the default duration.</summary>
        public void Play()
        {
            Play(defaultDuration);
        }

        /// <summary>Freezes for <paramref name="duration"/> seconds of real time.</summary>
        public void Play(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            // A second hit during a freeze extends it rather than stacking, so two impacts
            // landing together cannot compound into a noticeable stall.
            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(Freeze(duration));
        }

        /// <summary>
        /// Abandons a freeze in progress without restoring time.
        /// </summary>
        /// <remarks>
        /// For the pause menu, which takes ownership of the time scale itself. Letting a freeze
        /// finish normally would set time back to one and un-pause the game a fraction of a
        /// second after the player paused it — and only sometimes, which is the worst kind of
        /// bug to be handed.
        /// </remarks>
        public void Cancel()
        {
            if (_running == null)
            {
                return;
            }

            StopCoroutine(_running);
            _running = null;
        }

        private IEnumerator Freeze(float duration)
        {
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            _running = null;
        }

        private void OnDisable()
        {
            // Being disabled mid-freeze would otherwise leave the game stopped for good.
            if (_running == null)
            {
                return;
            }

            StopCoroutine(_running);
            _running = null;
            Time.timeScale = 1f;
        }
    }
}
