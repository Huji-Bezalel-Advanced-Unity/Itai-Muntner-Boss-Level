using System.Collections;
using BossLevel.Common;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// A patch of ground that flashes a warning and then strikes whatever is standing in it.
    /// </summary>
    /// <remarks>
    /// The one attack shape cover cannot answer. Every projectile the boss fires has to travel
    /// from the boss to the player, so anything solid in between defeats all of them at once —
    /// which is what turns a well-placed ledge into a bunker. This does not travel; it appears
    /// where the player already is.
    /// <para>
    /// It also asks a different question. A projectile asks "which way do I dodge"; this asks
    /// "are you still standing where you were a second ago", which is the question a player who
    /// has found a safe spot most needs to be asked.
    /// </para>
    /// <para>
    /// It resolves as a single overlap query at the instant it strikes rather than as a trigger
    /// collider, because the player standing inside it when it activates is the normal case
    /// here rather than the edge case, and enter-events do not fire for something already there.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class GroundHazard : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(1)] private int damage = 1;

        [SerializeField, Min(0.1f)] private float radius = 1.1f;

        [Tooltip("Warning time before it strikes. This is the whole fairness of the attack — " +
                 "long enough to walk out of, short enough to be worth walking out of.")]
        [SerializeField, Min(0f)] private float windupDuration = 0.7f;

        [Tooltip("How long the strike stays visible afterwards. Purely cosmetic; the damage has " +
                 "already been decided.")]
        [SerializeField, Min(0f)] private float lingerDuration = 0.18f;

        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Appearance")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color windupColour = new Color(1f, 0.55f, 0.1f, 0.35f);
        [SerializeField] private Color strikeColour = new Color(1f, 0.95f, 0.6f, 0.9f);

        private HazardPool _owner;
        private Coroutine _routine;
        private Sequence _tween;

        /// <summary>Places the hazard and starts its warning.</summary>
        public void Trigger(HazardPool owner, Vector2 position)
        {
            _owner = owner;
            transform.position = position;

            _routine = StartCoroutine(Run());
        }

        public void OnSpawn()
        {
            if (visual == null)
            {
                return;
            }

            // Diameter, because the sprite is authored at one world unit across.
            visual.transform.localScale = Vector3.one * (radius * 2f);
            visual.color = windupColour;
        }

        public void OnDespawn()
        {
            _tween?.Kill();
            _tween = null;

            if (_routine == null)
            {
                return;
            }

            StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator Run()
        {
            PlayWarning();

            yield return new WaitForSeconds(windupDuration);

            Strike();

            yield return new WaitForSeconds(lingerDuration);

            _routine = null;
            _owner.Despawn(this);
        }

        private void PlayWarning()
        {
            if (visual == null)
            {
                return;
            }

            _tween?.Kill();

            // Pulsing towards the strike colour over the warning, so the moment it lands is
            // readable as the end of something rather than as a surprise.
            _tween = DOTween.Sequence()
                .Append(visual.DOColor(strikeColour, windupDuration))
                .Append(visual.DOColor(windupColour, lingerDuration));
        }

        private void Strike()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius, hitLayers);

            foreach (var hit in hits)
            {
                // Search parents, because the collider is often on a child of whatever owns the
                // health.
                var target = hit.GetComponentInParent<IDamageable>();

                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
