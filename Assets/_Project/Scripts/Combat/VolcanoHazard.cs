using System.Collections;
using BossLevel.Audio;
using BossLevel.Common;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// A vent that smoulders on the ground and then erupts as a column of fire straight upwards.
    /// </summary>
    /// <remarks>
    /// The one attack shape cover and distance cannot answer. Everything else the boss does
    /// travels from the boss to the player and can be blocked or outrun; this appears where the
    /// player already is and only asks that they leave.
    /// <para>
    /// The long warning is the whole design. Two seconds is far more than any projectile gives,
    /// which is what makes an attack that cannot be dodged sideways or jumped over fair: the
    /// player is never surprised by it, only caught standing still by it.
    /// </para>
    /// <para>
    /// Damage is resolved by repeated overlap queries against the part of the column that has
    /// actually risen so far, rather than by a trigger collider. A collider would have to cope
    /// with the player already standing inside it when it activates — the normal case here —
    /// and enter events do not fire for something already there.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class VolcanoHazard : MonoBehaviour, IPoolable
    {
        [Header("Timing")]
        [Tooltip("Warning before the eruption. The attack is only fair because this is long.")]
        [SerializeField, Min(2f)] private float warningDuration = 2.2f;

        [Tooltip("How long the column takes to reach full height. Short — the warning has " +
                 "already been given, so the eruption itself should feel sudden.")]
        [SerializeField, Min(0.02f)] private float riseDuration = 0.12f;

        [SerializeField, Min(0f)] private float activeDuration = 0.5f;

        [Tooltip("How often the risen column checks what is inside it.")]
        [SerializeField, Min(0.02f)] private float damageInterval = 0.06f;

        [Header("Column")]
        [SerializeField, Min(0.1f)] private float columnWidth = 1.2f;
        [SerializeField, Min(0.5f)] private float columnHeight = 7f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Appearance")]
        [Tooltip("The smouldering patch on the ground during the warning.")]
        [SerializeField] private SpriteRenderer warningVisual;

        [Tooltip("The column itself. Grown from the base, so a centred sprite is fine.")]
        [SerializeField] private SpriteRenderer columnVisual;

        [SerializeField] private Color warningStartColour = new Color(1f, 0.45f, 0.1f, 0.25f);
        [SerializeField] private Color warningPeakColour = new Color(1f, 0.9f, 0.4f, 0.85f);

        [Header("Sound (optional)")]
        [Tooltip("A rumble as the vent opens. This one earns its place more than most — a vent " +
                 "opens underfoot rather than at the boss, so the player may never see it start.")]
        [SerializeField] private SoundEvent warningSound;

        [SerializeField] private SoundEvent eruptSound;

        private VolcanoPool _owner;
        private Coroutine _routine;
        private Sequence _warningTween;

        /// <summary>Places the vent on the ground and starts its warning.</summary>
        public void Trigger(VolcanoPool owner, Vector2 groundPosition)
        {
            _owner = owner;
            transform.position = groundPosition;

            _routine = StartCoroutine(Run());
        }

        public void OnSpawn()
        {
            if (warningVisual != null)
            {
                warningVisual.enabled = true;
                warningVisual.color = warningStartColour;
                warningVisual.transform.localScale = new Vector3(columnWidth * 0.5f, columnWidth * 0.25f, 1f);
            }

            SetColumnHeight(0f);

            if (columnVisual != null)
            {
                columnVisual.enabled = false;
            }
        }

        public void OnDespawn()
        {
            _warningTween?.Kill();
            _warningTween = null;

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
            warningSound?.Play(transform.position);

            yield return new WaitForSeconds(warningDuration);

            eruptSound?.Play(transform.position);

            yield return Erupt();

            _routine = null;
            _owner.Despawn(this);
        }

        private void PlayWarning()
        {
            if (warningVisual == null)
            {
                return;
            }

            _warningTween?.Kill();

            // Two signals at once: a pulse that says "active", and a steady swell that says
            // "and getting closer". The swell is what lets the player judge how long is left
            // rather than only that something is happening.
            _warningTween = DOTween.Sequence();

            _warningTween.Append(
                warningVisual.DOColor(warningPeakColour, warningDuration * 0.25f)
                    .SetLoops(4, LoopType.Yoyo));

            _warningTween.Insert(0f, warningVisual.transform
                .DOScaleX(columnWidth, warningDuration)
                .SetEase(Ease.InQuad));
        }

        private IEnumerator Erupt()
        {
            if (warningVisual != null)
            {
                warningVisual.enabled = false;
            }

            if (columnVisual != null)
            {
                columnVisual.enabled = true;
            }

            var elapsed = 0f;
            var totalDuration = riseDuration + activeDuration;

            while (elapsed < totalDuration)
            {
                // Only the part of the column that has actually risen can burn anything, so a
                // player above it is safe until it reaches them — the column reads as travelling
                // rather than simply appearing.
                var risen = Mathf.Min(1f, elapsed / riseDuration) * columnHeight;

                SetColumnHeight(risen);
                DamageInsideColumn(risen);

                yield return new WaitForSeconds(damageInterval);
                elapsed += damageInterval;
            }
        }

        private void SetColumnHeight(float height)
        {
            if (columnVisual == null)
            {
                return;
            }

            // Scaled and offset together so the column grows upward from the vent regardless of
            // where the sprite's pivot happens to be.
            columnVisual.transform.localScale = new Vector3(columnWidth, Mathf.Max(height, 0.001f), 1f);
            columnVisual.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        }

        private void DamageInsideColumn(float height)
        {
            if (height <= 0f)
            {
                return;
            }

            var centre = (Vector2)transform.position + new Vector2(0f, height * 0.5f);
            var size = new Vector2(columnWidth, height);

            var hits = Physics2D.OverlapBoxAll(centre, size, 0f, hitLayers);

            foreach (var hit in hits)
            {
                // Search parents, because the collider is often on a child of whatever owns the
                // health. Repeat hits across intervals are absorbed by the target's own
                // invulnerability frames rather than being tracked here.
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
            Gizmos.DrawWireCube(
                transform.position + new Vector3(0f, columnHeight * 0.5f, 0f),
                new Vector3(columnWidth, columnHeight, 0f));
        }
    }
}
