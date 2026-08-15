using DG.Tweening;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// Plays the boss's wind-up tells — the visible warnings that something is about to happen.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="BossController"/> so how a warning looks can change without
    /// touching the rhythm of the fight. The controller owns the timing, because a telegraph's
    /// length is a fairness guarantee rather than a decoration; this component is only asked to
    /// fill that window with something the player can read.
    /// <para>
    /// What it fills it with comes from the caller as a <see cref="TelegraphCue"/>, so each
    /// attack looks like itself rather than every attack looking the same.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossTelegraph : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        [Tooltip("Fraction of each pulse spent swelling. The remainder snaps back.")]
        [SerializeField, Range(0.1f, 0.9f)] private float windUpFraction = 0.7f;

        [Tooltip("How jittery the shudder is. Higher is more frantic.")]
        [SerializeField, Min(1)] private int shakeVibrato = 20;

        private Color _baseColour;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Sequence _sequence;

        private void Awake()
        {
            if (target == null)
            {
                Debug.LogError($"{nameof(BossTelegraph)} has no target sprite assigned.", this);
                enabled = false;
                return;
            }

            _baseColour = target.color;
            _baseScale = target.transform.localScale;
            _basePosition = target.transform.localPosition;
        }

        /// <summary>
        /// Plays <paramref name="cue"/> across exactly <paramref name="duration"/> seconds,
        /// replacing anything already running.
        /// </summary>
        public void Play(TelegraphCue cue, float duration)
        {
            if (!enabled || duration <= 0f)
            {
                return;
            }

            Stop();

            // Remember where the sprite started, because a shake that is interrupted partway
            // would otherwise leave the boss permanently nudged off its mark.
            _basePosition = target.transform.localPosition;

            var pulses = Mathf.Max(1, cue.Pulses);
            var pulseDuration = duration / pulses;
            var swell = pulseDuration * windUpFraction;
            var settle = pulseDuration - swell;

            var punchedScale = new Vector3(
                _baseScale.x * (1f + cue.ScalePunch.x),
                _baseScale.y * (1f + cue.ScalePunch.y),
                _baseScale.z);

            _sequence = DOTween.Sequence();

            for (var pulse = 0; pulse < pulses; pulse++)
            {
                _sequence
                    .Append(target.DOColor(cue.Colour, swell))
                    .Join(target.transform.DOScale(punchedScale, swell))
                    .Append(target.DOColor(_baseColour, settle))
                    .Join(target.transform.DOScale(_baseScale, settle));
            }

            if (cue.ShakeStrength > 0f)
            {
                // Inserted at zero rather than joined, so it runs across every pulse instead of
                // only alongside the last one appended.
                _sequence.Insert(0f, target.transform.DOShakePosition(
                    duration, cue.ShakeStrength, shakeVibrato, 90f, false, true));
            }
        }

        /// <summary>Cancels whatever is playing and puts the sprite back exactly as it was.</summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;

            if (target == null)
            {
                return;
            }

            target.color = _baseColour;
            target.transform.localScale = _baseScale;
            target.transform.localPosition = _basePosition;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            // A tween outliving the object it drives would throw on its next frame.
            _sequence?.Kill();
        }
    }
}
