using System.Collections;
using BossLevel.Audio;
using BossLevel.Common;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// A vent that smoulders on the ground and then erupts as a column of fire.
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
    /// The eruption itself is driven by curves rather than a single linear scale. A column that
    /// simply grows and vanishes reads as a rectangle being resized; fire does not move at a
    /// constant speed. Bursting past full height and falling back, flaring wide at the base
    /// before narrowing, swaying, and guttering out is what makes the same primitive read as
    /// something alive — and being curves, all of it is tunable by eye in the Inspector.
    /// </para>
    /// <para>
    /// Damage is resolved by repeated overlap queries against the part of the column that has
    /// actually risen, rather than by a trigger collider. A collider would have to cope with the
    /// player already standing inside it when it activates — the normal case here — and enter
    /// events do not fire for something already there.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class VolcanoHazard : MonoBehaviour, IPoolable
    {
        [Header("Timing")]
        [Tooltip("Warning before the eruption. The attack is only fair because this is long.")]
        [SerializeField, Min(2f)] private float warningDuration = 2.2f;

        [Tooltip("How long the whole eruption lasts, from first spark to guttering out.")]
        [SerializeField, Min(0.1f)] private float eruptionDuration = 0.9f;

        [Tooltip("How often the risen column checks what is inside it.")]
        [SerializeField, Min(0.02f)] private float damageInterval = 0.06f;

        [Header("Column")]
        [SerializeField, Min(0.1f)] private float columnWidth = 1.2f;
        [SerializeField, Min(0.5f)] private float columnHeight = 7f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Eruption shape")]
        [Tooltip("Height across the eruption, as a fraction of full height. Overshooting past 1 " +
                 "early and falling back is what gives the burst its kick.")]
        [SerializeField] private AnimationCurve heightCurve;

        [Tooltip("Width across the eruption. Flaring wide at the base then narrowing reads as " +
                 "pressure escaping rather than as a box being stretched.")]
        [SerializeField] private AnimationCurve widthCurve;

        [Tooltip("Opacity across the eruption, so the column gutters out instead of vanishing.")]
        [SerializeField] private AnimationCurve alphaCurve;

        [Tooltip("How far the column sways sideways.")]
        [SerializeField, Min(0f)] private float swayDistance = 0.18f;

        [Tooltip("How much it leans as it sways. A little tilt sells the movement as fluid.")]
        [SerializeField, Range(0f, 30f)] private float swayTilt = 7f;

        [SerializeField, Min(0f)] private float swaySpeed = 3.5f;

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
        private Color _columnBaseColour;
        private float _swayPhase;

        private void Awake()
        {
            if (columnVisual != null)
            {
                _columnBaseColour = columnVisual.color;
            }
        }

        /// <summary>
        /// Fills in any curve Unity zero-filled when these fields were introduced.
        /// </summary>
        /// <remarks>
        /// A newly added <see cref="AnimationCurve"/> deserialises with no keys on an
        /// already-authored prefab, and an empty curve evaluates to zero — which would render
        /// the column invisible and harmless rather than merely mis-shaped.
        /// </remarks>
        private void OnValidate()
        {
            if (heightCurve == null || heightCurve.length == 0)
            {
                heightCurve = EruptionCurves.Height();
            }

            if (widthCurve == null || widthCurve.length == 0)
            {
                widthCurve = EruptionCurves.Width();
            }

            if (alphaCurve == null || alphaCurve.length == 0)
            {
                alphaCurve = EruptionCurves.Alpha();
            }
        }

        /// <summary>Places the vent on the ground and starts its warning.</summary>
        public void Trigger(VolcanoPool owner, Vector2 groundPosition)
        {
            _owner = owner;
            transform.position = groundPosition;

            // Offset per eruption, so several vents burning at once do not sway in lockstep.
            _swayPhase = Random.Range(0f, Mathf.PI * 2f);

            _routine = StartCoroutine(Run());
        }

        public void OnSpawn()
        {
            OnValidate();

            if (warningVisual != null)
            {
                warningVisual.enabled = true;
                warningVisual.color = warningStartColour;
                warningVisual.transform.localScale = new Vector3(columnWidth * 0.5f, columnWidth * 0.25f, 1f);
            }

            ShapeColumn(0f, 0f, 0f, 0f);

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

            if (_owner != null)
            {
                _owner.NotifyErupted(transform.position);
            }

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
            var untilNextDamage = 0f;

            // Shaped every frame but damaging on an interval: the motion needs to be smooth,
            // while re-querying the physics scene sixty times a second would not make the
            // attack any more dangerous.
            while (elapsed < eruptionDuration)
            {
                var progress = Mathf.Clamp01(elapsed / eruptionDuration);

                var height = heightCurve.Evaluate(progress) * columnHeight;
                var width = widthCurve.Evaluate(progress) * columnWidth;
                var alpha = alphaCurve.Evaluate(progress);

                ShapeColumn(height, width, alpha, elapsed);

                untilNextDamage -= Time.deltaTime;

                if (untilNextDamage <= 0f)
                {
                    DamageInsideColumn(height, width);
                    untilNextDamage = damageInterval;
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            ShapeColumn(0f, 0f, 0f, elapsed);

            if (columnVisual != null)
            {
                columnVisual.enabled = false;
            }
        }

        private void ShapeColumn(float height, float width, float alpha, float elapsed)
        {
            if (columnVisual == null)
            {
                return;
            }

            var sway = Mathf.Sin(_swayPhase + elapsed * swaySpeed) * swayDistance;

            // Scaled and offset together so the column grows upward from the vent regardless of
            // where the sprite's pivot happens to be. The sway leans as well as slides, because
            // a column that only slides reads as a picture being moved.
            var columnTransform = columnVisual.transform;
            columnTransform.localScale = new Vector3(Mathf.Max(width, 0.001f), Mathf.Max(height, 0.001f), 1f);
            columnTransform.localPosition = new Vector3(sway, height * 0.5f, 0f);
            columnTransform.localRotation = Quaternion.Euler(0f, 0f, -sway * swayTilt);

            var colour = _columnBaseColour;
            colour.a = _columnBaseColour.a * alpha;
            columnVisual.color = colour;
        }

        private void DamageInsideColumn(float height, float width)
        {
            if (height <= 0.01f)
            {
                return;
            }

            var centre = (Vector2)transform.position + new Vector2(0f, height * 0.5f);
            var size = new Vector2(Mathf.Max(width, 0.01f), height);

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
