using System;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// Hit points for anything that can be damaged and killed.
    /// </summary>
    /// <remarks>
    /// Views observe the events below rather than polling, and nothing here knows what a health
    /// bar is — gameplay code contains no reference to UI, which is what lets the fight run in a
    /// scene with no canvas at all.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        [Tooltip("Driven by code, not set by hand. Serialized only so remaining health can be " +
                 "watched in the Inspector while playing.")]
        [SerializeField] private int currentHealth;

        /// <summary>Raised on every change, with (current, max). Health bars listen to this.</summary>
        public event Action<int, int> Changed;

        /// <summary>Raised when damage lands, with the amount actually applied after clamping.</summary>
        public event Action<int> Damaged;

        /// <summary>Raised exactly once, the moment health first reaches zero.</summary>
        public event Action Died;

        private int _invulnerabilityHolds;

        public int Current => currentHealth;

        public int Max => maxHealth;

        public bool IsAlive => Current > 0;

        /// <summary>Remaining health from 0 to 1. Convenient for bars and for phase thresholds.</summary>
        public float Fraction => maxHealth <= 0 ? 0f : (float)Current / maxHealth;

        /// <summary>Whether damage is currently being ignored.</summary>
        public bool IsInvulnerable => _invulnerabilityHolds > 0;

        /// <summary>
        /// Adds one reason to ignore damage. Every call must be matched by
        /// <see cref="ReleaseInvulnerability"/>.
        /// </summary>
        /// <remarks>
        /// Counted rather than a simple flag because more than one system can want
        /// invulnerability at the same moment and none of them knows about the others. A player
        /// hit at the start of a dash holds it twice — once for the dash, once for the hit
        /// frames — and with a flag whichever finished first would strip the protection the
        /// other was still relying on, producing a rare unfair death that is near impossible to
        /// reproduce on purpose.
        /// </remarks>
        public void HoldInvulnerability()
        {
            _invulnerabilityHolds++;
        }

        /// <summary>Removes one reason to ignore damage.</summary>
        public void ReleaseInvulnerability()
        {
            _invulnerabilityHolds = Mathf.Max(0, _invulnerabilityHolds - 1);
        }

        private void Awake()
        {
            ResetTo(maxHealth);
        }

        /// <summary>
        /// Keeps the Inspector value meaningful outside play mode.
        /// </summary>
        /// <remarks>
        /// Because current health is serialized for visibility, leaving play mode restores
        /// whatever the last session ended on — usually zero, which reads as a corpse sitting in
        /// the scene even though <see cref="Awake"/> refills it the instant play resumes. Syncing
        /// to the maximum while not playing keeps the editor honest.
        /// </remarks>
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
            }
        }

        /// <summary>
        /// Sets the maximum and refills to full.
        /// </summary>
        /// <remarks>
        /// The boss calls this with the value from its definition asset, so its health lives in
        /// one place as data instead of being duplicated on a component in the scene.
        /// </remarks>
        public void ResetTo(int newMax)
        {
            maxHealth = Mathf.Max(1, newMax);
            currentHealth = maxHealth;
            _invulnerabilityHolds = 0;
            Changed?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            // Refusing damage once dead is what keeps Died a once-only event. Without it, two
            // projectiles landing on the same frame would each fire the death sequence.
            if (!IsAlive || IsInvulnerable || amount <= 0)
            {
                return;
            }

            var applied = Mathf.Min(amount, currentHealth);
            currentHealth -= applied;

            Damaged?.Invoke(applied);
            Changed?.Invoke(currentHealth, maxHealth);

            if (currentHealth == 0)
            {
                Died?.Invoke();
            }
        }

        /// <summary>Restores health, never above the maximum and never resurrecting the dead.</summary>
        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Changed?.Invoke(currentHealth, maxHealth);
        }
    }
}
