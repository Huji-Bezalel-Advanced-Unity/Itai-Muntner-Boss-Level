using UnityEngine;
using BossLevel.Common;

namespace BossLevel.Combat
{
    /// <summary>
    /// A single shot that travels in a straight line, damages the first valid thing it touches,
    /// and returns itself to its pool.
    /// </summary>
    /// <remarks>
    /// Pooled rather than instantiated: a boss fight puts hundreds of these on screen, and
    /// allocating each one produces garbage collection hitches at the worst possible moments.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 18f;
        [SerializeField, Min(1)] private int damage = 5;

        [Tooltip("Seconds before the shot gives up and returns to the pool, so one that hits " +
                 "nothing cannot leak.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("What this shot is allowed to hit. Keeping it as a mask means the player's own " +
                 "bullets can ignore the player without needing a dedicated physics layer yet.")]
        [SerializeField] private LayerMask hitLayers = ~0;

        private Rigidbody2D _body;
        private ProjectilePool _owner;
        private float _timeAlive;

        /// <summary>
        /// How fast this projectile travels. Attacks read it through the pool so they can work
        /// out where a moving target will be by the time the shot arrives.
        /// </summary>
        public float Speed => speed;

        /// <summary>
        /// Configures the rigidbody and collider correctly when the component is first added,
        /// so a projectile prefab cannot be built subtly wrong.
        /// </summary>
        private void Reset()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Without this, a kinematic body only reports contacts against dynamic bodies — so
            // it would sail straight through a static target such as the boss or the ground.
            body.useFullKinematicContacts = true;

            // Collider2D is abstract, so RequireComponent cannot add one for us — the concrete
            // shape has to be added by hand. Configure it only if it is already there.
            if (TryGetComponent<Collider2D>(out var shape))
            {
                shape.isTrigger = true;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Sends this projectile on its way. Called by the pool that owns it, which passes
        /// itself so the projectile knows where to return.
        /// </summary>
        public void Launch(ProjectilePool owner, Vector2 origin, Vector2 direction)
        {
            _owner = owner;

            transform.position = origin;

            var heading = direction.normalized;

            // Point the sprite along its heading so non-round projectiles read correctly.
            transform.right = heading;

            _body.linearVelocity = heading * speed;
        }

        public void OnSpawn()
        {
            _timeAlive = 0f;
        }

        public void OnDespawn()
        {
            _body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            if (_timeAlive >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsInHitLayers(other.gameObject.layer))
            {
                return;
            }

            // Search parents too: the collider is often on a child of the object that owns the
            // health, particularly once the boss has separate body parts.
            var target = other.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage);
            }

            // Spend the shot either way — hitting a wall stops it just as surely as hitting a
            // target does.
            ReturnToPool();
        }

        private bool IsInHitLayers(int layer)
        {
            return (hitLayers.value & (1 << layer)) != 0;
        }

        private void ReturnToPool()
        {
            if (_owner == null)
            {
                // Launched outside a pool, which should not happen. Hide it rather than leaving
                // a runaway projectile in the scene.
                gameObject.SetActive(false);
                return;
            }

            _owner.Despawn(this);
        }
    }
}
