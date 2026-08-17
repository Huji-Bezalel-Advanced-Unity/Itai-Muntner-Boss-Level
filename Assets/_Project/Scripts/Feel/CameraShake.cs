using DG.Tweening;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Shakes the camera to give an impact somewhere to land.
    /// </summary>
    /// <remarks>
    /// Strength is meant to be spent carefully. If every hit shakes the screen hard then nothing
    /// does, so ordinary damage gets a nudge and the moments that actually change the fight — a
    /// phase turning over, the boss dying — get the rest. The scale is the message.
    /// <para>
    /// Runs on unscaled time so it keeps moving during a hit stop, which is precisely when it is
    /// most visible.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("The camera to move. Defaults to this object's own transform.")]
        [SerializeField] private Transform target;

        [SerializeField, Min(0f)] private float defaultStrength = 0.15f;
        [SerializeField, Min(0.01f)] private float defaultDuration = 0.2f;

        [Tooltip("How jittery the shake is. Higher reads as sharper, lower as a heavier roll.")]
        [SerializeField, Min(1)] private int vibrato = 18;

        private Vector3 _restingPosition;
        private Tween _shake;

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }

            _restingPosition = target.localPosition;
        }

        /// <summary>Shakes by the default amount.</summary>
        public void Play()
        {
            Play(defaultStrength, defaultDuration);
        }

        /// <summary>Shakes by a specific strength and duration.</summary>
        public void Play(float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f)
            {
                return;
            }

            _shake?.Kill();

            // Reset first: a shake interrupted partway leaves the camera off its mark, and
            // starting the next one from there would drift it further every time.
            target.localPosition = _restingPosition;

            _shake = target
                .DOShakePosition(duration, strength, vibrato, 90f, false, true)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            _shake?.Kill();
            _shake = null;

            if (target != null)
            {
                target.localPosition = _restingPosition;
            }
        }
    }
}
