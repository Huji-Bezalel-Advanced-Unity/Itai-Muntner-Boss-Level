using BossLevel.Combat;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Everything that happens when something takes a hit: the flash, the freeze, the shake and
    /// the burst.
    /// </summary>
    /// <remarks>
    /// Gathered into one component rather than scattered across the things that cause damage,
    /// because feedback belongs to whatever is <i>being</i> hit. A projectile should not have to
    /// know whether its target shakes the screen; it only has to land.
    /// <para>
    /// Every part except the flash is optional, so the same component fits the boss — which
    /// deserves the full treatment — and a minion, which should not stop the game every time it
    /// is grazed.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class DamageFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Health health;
        [SerializeField] private SpriteEffects sprite;

        [Header("Flash")]
        [SerializeField] private Color flashColour = Color.white;

        [Tooltip("Short. The flash is a punctuation mark, not an effect.")]
        [SerializeField, Min(0.01f)] private float flashDuration = 0.09f;

        [Header("Impact (all optional)")]
        [SerializeField] private HitStop hitStop;
        [SerializeField, Min(0f)] private float hitStopDuration = 0.045f;

        [SerializeField] private CameraShake cameraShake;
        [SerializeField, Min(0f)] private float shakeStrength = 0.1f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.16f;

        [SerializeField] private VfxPool impactVfx;
        [SerializeField] private Color impactColour = new Color(1f, 0.8f, 0.4f);

        private Tween _flash;

        private void Awake()
        {
            if (health == null || sprite == null)
            {
                Debug.LogError($"{nameof(DamageFeedback)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;

            _flash?.Kill();
            _flash = null;

            // Leaving mid-flash would strand the sprite whitened.
            sprite.SetFlash(flashColour, 0f);
        }

        private void OnDamaged(int amount)
        {
            PlayFlash();

            if (hitStop != null)
            {
                hitStop.Play(hitStopDuration);
            }

            if (cameraShake != null)
            {
                cameraShake.Play(shakeStrength, shakeDuration);
            }

            if (impactVfx != null)
            {
                impactVfx.Play(transform.position, impactColour);
            }
        }

        private void PlayFlash()
        {
            _flash?.Kill();

            // Unscaled, so the flash still animates through the hit stop that accompanies it —
            // on scaled time it would freeze at full white and only finish once time resumed.
            _flash = DOVirtual
                .Float(1f, 0f, flashDuration, amount => sprite.SetFlash(flashColour, amount))
                .SetUpdate(true);
        }
    }
}
