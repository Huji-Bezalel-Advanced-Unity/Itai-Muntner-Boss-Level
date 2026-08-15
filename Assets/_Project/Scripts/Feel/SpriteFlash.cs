using System.Collections;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Briefly tints a sprite when its owner takes damage, so hits read clearly.
    /// </summary>
    /// <remarks>
    /// This is the placeholder version, driven through <see cref="SpriteRenderer.color"/>. The
    /// final version drives a <c>_FlashAmount</c> property on the project's Shader Graph
    /// material instead, which can whiten the sprite without washing out its own colours. Only
    /// the two lines that apply and clear the tint need to change.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private Health health;

        [Tooltip("Must differ from the sprite's own colour, or the flash is invisible. White is " +
                 "a poor default precisely because untouched sprites are already white.")]
        [SerializeField] private Color flashColour = new Color(1f, 0.35f, 0.35f);

        [SerializeField] private float flashDuration = 0.1f;

        private SpriteRenderer _renderer;
        private Color _baseColour;
        private Coroutine _running;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColour = _renderer.color;

            if (health == null)
            {
                Debug.LogError($"{nameof(SpriteFlash)} has no {nameof(Health)} assigned.", this);
                enabled = false;
                return;
            }

            // An invisible flash looks exactly like a flash that never fired, which is a
            // genuinely confusing thing to debug. Say so instead.
            if (flashColour == _baseColour)
            {
                Debug.LogWarning(
                    $"{nameof(SpriteFlash)} flash colour matches the sprite's own colour, " +
                    "so hits will produce no visible change.", this);
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;

            // Leaving mid-flash would strand the sprite on the flash colour.
            RestoreBaseColour();
        }

        private void OnDamaged(int amount)
        {
            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            _renderer.color = flashColour;

            // Unscaled, so the flash still reads during the hit stop that will accompany it.
            yield return new WaitForSecondsRealtime(flashDuration);

            RestoreBaseColour();
        }

        private void RestoreBaseColour()
        {
            if (_renderer != null)
            {
                _renderer.color = _baseColour;
            }

            _running = null;
        }
    }
}
