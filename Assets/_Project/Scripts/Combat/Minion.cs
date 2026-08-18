using BossLevel.Common;
using DG.Tweening;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// A small enemy that drifts steadily towards the player and bursts on contact.
    /// </summary>
    /// <remarks>
    /// Every other threat in the fight is momentary — it arrives, it resolves, it is gone.
    /// Minions persist, which changes what the player is managing: they cannot simply dodge and
    /// wait, because ignoring one costs them the arena a piece at a time. It is also the only
    /// threat that competes with the boss for the player's shots, so time spent clearing them
    /// is time the boss is not taking damage.
    /// <para>
    /// Deliberately slow. The pressure is meant to accumulate, not to ambush.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class Minion : MonoBehaviour, IPoolable
    {
        [SerializeField] private Health health;

        [Tooltip("Well below the player's running speed — this should be outrunnable, and a " +
                 "problem only if left alone.")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.2f;

        [SerializeField, Min(1)] private int contactDamage = 1;

        [Tooltip("What counts as the player. Set this to the Player layer.")]
        [SerializeField] private LayerMask playerLayers;

        [Tooltip("Safety net so a minion whose target vanishes cannot haunt the arena forever.")]
        [SerializeField, Min(1f)] private float lifetime = 30f;

        [Tooltip("How long the minion takes to swell to full size when it appears. Long enough " +
                 "to notice one arriving, short enough that it is a real threat immediately.")]
        [SerializeField, Min(0.05f)] private float spawnPopDuration = 0.25f;

        private Rigidbody2D _body;
        private Vector3 _baseScale;
        private Tween _spawnPop;
        private MinionPool _owner;
        private ITarget _target;
        private float _timeAlive;
        private bool _isDying;

        /// <summary>
        /// Configures the rigidbody and collider when the component is added, so a minion prefab
        /// cannot be built subtly wrong.
        /// </summary>
        private void Reset()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            // Without this a kinematic body only reports contacts against dynamic bodies, and
            // the minion would drift straight through anything static without a word.
            body.useFullKinematicContacts = true;

            if (TryGetComponent<Collider2D>(out var shape))
            {
                shape.isTrigger = true;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _baseScale = transform.localScale;

            if (health == null)
            {
                Debug.LogError($"{nameof(Minion)} has no {nameof(Health)} assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Died += Die;
        }

        private void OnDisable()
        {
            health.Died -= Die;
        }

        /// <summary>Places the minion and sets it hunting.</summary>
        public void Launch(MinionPool owner, ITarget target, Vector2 position)
        {
            _owner = owner;
            _target = target;

            transform.position = position;

            // Pooled instances keep whatever health they died with, so it is restored here
            // rather than relying on Awake, which only ever runs once.
            health.ResetTo(health.Max);
        }

        public void OnSpawn()
        {
            _timeAlive = 0f;
            _isDying = false;

            // Swelling into existence rather than simply being there. The brief moment at zero
            // size also means a minion cannot be hit on the frame it appears, which reads as
            // fair rather than as a gap in the collision.
            _spawnPop?.Kill();
            transform.localScale = Vector3.zero;
            _spawnPop = transform.DOScale(_baseScale, spawnPopDuration).SetEase(Ease.OutBack);
        }

        public void OnDespawn()
        {
            _spawnPop?.Kill();
            _spawnPop = null;

            // Restored, because a pooled instance is handed out again exactly as it was left.
            transform.localScale = _baseScale;

            _body.linearVelocity = Vector2.zero;
            _target = null;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            if (_timeAlive >= lifetime)
            {
                Die();
            }
        }

        private void FixedUpdate()
        {
            if (_target == null)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }

            var toTarget = _target.Position - (Vector2)transform.position;
            _body.linearVelocity = toTarget.normalized * moveSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            var target = other.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(contactDamage);
            }

            // Spent either way. If the player was invulnerable — mid-dash, or still in hit
            // frames — the minion is destroyed for nothing, which is a deliberate reward for
            // dashing through one rather than shooting it.
            Die();
        }

        private void Die()
        {
            if (_isDying)
            {
                return;
            }

            _isDying = true;
            _owner.Despawn(this);
        }
    }
}
