using DG.Tweening;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// The boss's wind-up tell — the visible warning that an attack is about to land.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="BossController"/> so how the warning *looks* can change
    /// without touching the rhythm of the fight. The controller owns the timing, because a
    /// telegraph's length is a fairness guarantee rather than a decoration; this component is
    /// only asked to make that window visible for exactly as long as it lasts.
    /// <para>
    /// Only DOTween's core API is used. DOTween ships its shortcut extensions (<c>DOColor</c>,
    /// <c>DOScale</c> and friends) as loose scripts under <c>Assets/Plugins</c>, which Unity
    /// compiles into a predefined assembly — and an assembly definition cannot reference those.
    /// <c>DOTween.To</c> lives inside <c>DOTween.dll</c> and is always reachable.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossTelegraph : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        [SerializeField] private Color telegraphColour = new Color(1f, 0.8f, 0.35f);

        [Tooltip("How much the boss swells during the wind-up, as a fraction of its normal size.")]
        [SerializeField, Range(0f, 0.5f)] private float scalePunch = 0.08f;

        [Tooltip("Fraction of the telegraph spent winding up. The remainder snaps back.")]
        [SerializeField, Range(0.1f, 0.9f)] private float windUpFraction = 0.7f;

        private Color _baseColour;
        private Vector3 _baseScale;
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
        }

        /// <summary>
        /// Plays a tell lasting exactly <paramref name="duration"/> seconds, replacing any tell
        /// already running.
        /// </summary>
        public void Play(float duration)
        {
            if (!enabled || duration <= 0f)
            {
                return;
            }

            Stop();

            var windUp = duration * windUpFraction;
            var snapBack = duration - windUp;

            _sequence = DOTween.Sequence()
                .Append(TweenColour(telegraphColour, windUp))
                .Join(TweenScale(_baseScale * (1f + scalePunch), windUp))
                .Append(TweenColour(_baseColour, snapBack))
                .Join(TweenScale(_baseScale, snapBack));
        }

        /// <summary>Cancels any tell in progress and puts the sprite back as it was.</summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;

            if (target != null)
            {
                target.color = _baseColour;
                target.transform.localScale = _baseScale;
            }
        }

        private Tween TweenColour(Color to, float duration)
        {
            return DOTween.To(() => target.color, value => target.color = value, to, duration);
        }

        private Tween TweenScale(Vector3 to, float duration)
        {
            return DOTween.To(
                () => target.transform.localScale,
                value => target.transform.localScale = value,
                to,
                duration);
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
