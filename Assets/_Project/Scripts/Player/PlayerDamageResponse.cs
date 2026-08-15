using System.Collections;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Player
{
    /// <summary>
    /// Gives the player a moment of invulnerability after being hit, and blinks to show it.
    /// </summary>
    /// <remarks>
    /// Without this, standing in a stream of projectiles drains the whole health bar in a
    /// fraction of a second, so any hit at all is effectively fatal and the fight cannot be
    /// balanced. Invulnerability frames turn a hit into a cost the player can recover from and
    /// make health a resource rather than a formality.
    /// <para>
    /// The blink is not decoration either — it is the only way the player knows the rules are
    /// temporarily different.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerDamageResponse : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("How long the player is untouchable after a hit.")]
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 1.2f;

        [Tooltip("How fast the sprite blinks while invulnerable.")]
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.08f;

        private Coroutine _invulnerability;

        private void Awake()
        {
            if (health == null || sprite == null)
            {
                Debug.LogError($"{nameof(PlayerDamageResponse)} is missing a reference.", this);
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

            // Leaving mid-blink would strand the player invisible and permanently untouchable.
            StopInvulnerability();
        }

        private void OnDamaged(int amount)
        {
            // A killing blow needs no grace period, and blinking a corpse looks like a bug.
            if (!health.IsAlive)
            {
                return;
            }

            if (_invulnerability != null)
            {
                StopCoroutine(_invulnerability);
            }

            _invulnerability = StartCoroutine(Invulnerability());
        }

        private IEnumerator Invulnerability()
        {
            health.IsInvulnerable = true;

            var elapsed = 0f;

            while (elapsed < invulnerabilityDuration)
            {
                sprite.enabled = !sprite.enabled;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            StopInvulnerability();
        }

        private void StopInvulnerability()
        {
            _invulnerability = null;

            if (sprite != null)
            {
                sprite.enabled = true;
            }

            if (health != null)
            {
                health.IsInvulnerable = false;
            }
        }
    }
}
