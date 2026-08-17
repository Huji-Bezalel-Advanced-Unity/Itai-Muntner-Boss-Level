using BossLevel.Combat;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Burns a sprite away when it dies, rather than letting it simply vanish.
    /// </summary>
    /// <remarks>
    /// A boss that disappears on its last hit ends the fight without marking it. The dissolve
    /// costs a second and turns the kill into a moment, which matters more here than anywhere
    /// else in the game because it is the only time the player gets to win.
    /// <para>
    /// The pause before it starts is deliberate: the final hit should land, register, and only
    /// then start unmaking the thing it killed.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class DeathDissolve : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Health health;
        [SerializeField] private SpriteEffects sprite;

        [Header("Timing")]
        [Tooltip("Beat between the killing blow and the body starting to go.")]
        [SerializeField, Min(0f)] private float delay = 0.2f;

        [SerializeField, Min(0.1f)] private float duration = 1.2f;

        [Header("Impact (all optional)")]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField, Min(0f)] private float shakeStrength = 0.45f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.6f;

        [SerializeField] private VfxPool deathVfx;
        [SerializeField] private Color deathColour = new Color(1f, 0.5f, 0.15f);

        private Tween _dissolve;

        private void Awake()
        {
            if (health == null || sprite == null)
            {
                Debug.LogError($"{nameof(DeathDissolve)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;

            _dissolve?.Kill();
            _dissolve = null;
        }

        private void OnDied()
        {
            if (cameraShake != null)
            {
                cameraShake.Play(shakeStrength, shakeDuration);
            }

            if (deathVfx != null)
            {
                deathVfx.Play(transform.position, deathColour);
            }

            _dissolve?.Kill();

            // Unscaled so the death sequence plays out even if a hit stop is still running from
            // the blow that caused it.
            _dissolve = DOVirtual
                .Float(0f, 1f, duration, amount => sprite.SetDissolve(amount))
                .SetDelay(delay)
                .SetUpdate(true);
        }
    }
}
