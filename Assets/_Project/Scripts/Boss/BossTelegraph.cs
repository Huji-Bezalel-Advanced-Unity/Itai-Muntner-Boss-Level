using BossLevel.Feel;
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
    /// The tint goes through <see cref="SpriteEffects"/> rather than
    /// <see cref="SpriteRenderer.color"/>. That was a real bug: the damage flash wrote the same
    /// colour, so a hit landing during a wind-up was immediately overwritten and never showed.
    /// As separate shader properties the two compose — the boss can flash white while still
    /// glowing with whatever it is about to do.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossTelegraph : MonoBehaviour
    {
        [SerializeField] private SpriteEffects sprite;

        [Tooltip("What gets squashed and shaken. Usually the sprite's own transform.")]
        [SerializeField] private Transform body;

        [Tooltip("Fraction of each pulse spent swelling. The remainder snaps back.")]
        [SerializeField, Range(0.1f, 0.9f)] private float windUpFraction = 0.7f;

        [Tooltip("How jittery the shudder is. Higher is more frantic.")]
        [SerializeField, Min(1)] private int shakeVibrato = 20;

        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Sequence _sequence;

        private void Awake()
        {
            if (sprite == null || body == null)
            {
                Debug.LogError($"{nameof(BossTelegraph)} is missing a reference.", this);
                enabled = false;
                return;
            }

            _baseScale = body.localScale;
            _basePosition = body.localPosition;
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

            // Remember where the body started, because a shake interrupted partway would
            // otherwise leave the boss permanently nudged off its mark.
            _basePosition = body.localPosition;

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
                    .Append(Tint(cue.Colour, 0f, 1f, swell))
                    .Join(body.DOScale(punchedScale, swell))
                    .Append(Tint(cue.Colour, 1f, 0f, settle))
                    .Join(body.DOScale(_baseScale, settle));
            }

            if (cue.ShakeStrength > 0f)
            {
                // Inserted at zero rather than joined, so it runs across every pulse instead of
                // only alongside the last one appended.
                _sequence.Insert(0f, body.DOShakePosition(
                    duration, cue.ShakeStrength, shakeVibrato, 90f, false, true));
            }
        }

        /// <summary>Cancels whatever is playing and puts the boss back exactly as it was.</summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;

            if (sprite != null)
            {
                sprite.SetTint(Color.white, 0f);
            }

            if (body == null)
            {
                return;
            }

            body.localScale = _baseScale;
            body.localPosition = _basePosition;
        }

        /// <summary>
        /// Tweens the tint strength between two explicit values.
        /// </summary>
        /// <remarks>
        /// <c>DOVirtual</c> drives the shader property directly, because there is no component
        /// field for DOTween to animate. Both ends are stated rather than read back, since a
        /// pulse always runs from nothing to full and back.
        /// </remarks>
        private Tween Tint(Color colour, float from, float to, float duration)
        {
            return DOVirtual.Float(from, to, duration, value => sprite.SetTint(colour, value));
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
