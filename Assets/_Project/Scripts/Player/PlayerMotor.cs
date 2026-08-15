using System.Collections;
using UnityEngine;

namespace BossLevel.Player
{
    /// <summary>
    /// Turns movement intent into physics: running, jumping, and dropping through one-way
    /// platforms.
    /// </summary>
    /// <remarks>
    /// Input edges are sampled in <see cref="Update"/>, because a button-down is only true for a
    /// single rendered frame and would be missed at the physics rate. Everything that touches the
    /// rigidbody happens in <see cref="FixedUpdate"/>, so movement does not change with frame
    /// rate.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerInputReader input;

        [Header("Running")]
        [SerializeField] private float moveSpeed = 8f;

        [Header("Jumping")]
        [Tooltip("Upward speed applied on jump. Set directly rather than as an impulse so a " +
                 "jump taken while falling reaches the same height as one from rest.")]
        [SerializeField] private float jumpSpeed = 14f;

        [Tooltip("Gravity while rising. Lower than falling gravity, which makes the jump feel " +
                 "floaty at the top and weighty on the way down.")]
        [SerializeField] private float riseGravity = 3f;

        [SerializeField] private float fallGravity = 5f;

        [Tooltip("Upward speed is multiplied by this when jump is released early, so a tap is a " +
                 "hop and a hold is a full jump.")]
        [SerializeField, Range(0f, 1f)] private float shortJumpCut = 0.5f;

        [Tooltip("How long after leaving a ledge a jump is still accepted.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Tooltip("How long before landing a jump press is remembered and fired on touchdown.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Ground detection")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Dropping through platforms")]
        [SerializeField] private float dropThroughProbeDistance = 1f;

        [Tooltip("Safety net only. Collision normally returns as soon as the player is clear.")]
        [SerializeField] private float dropThroughTimeout = 0.6f;

        private Rigidbody2D _body;
        private Collider2D _collider;

        private float _timeSinceGrounded = float.PositiveInfinity;
        private float _timeSinceJumpPressed = float.PositiveInfinity;
        private bool _isJumping;
        private Collider2D _platformBeingDroppedThrough;

        /// <summary>Whether the player is currently standing on something.</summary>
        public bool IsGrounded { get; private set; }

        private Vector2 GroundCheckPosition => (Vector2)transform.position + groundCheckOffset;

        /// <summary>
        /// Configures sensible rigidbody defaults when this component is first added in the
        /// editor, so the player cannot tip over or tunnel through platforms at speed.
        /// </summary>
        private void Reset()
        {
            var body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = fallGravity;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            if (input == null)
            {
                Debug.LogError($"{nameof(PlayerMotor)} has no {nameof(PlayerInputReader)} assigned.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            // If control is taken away mid-drop, restore the collision we suppressed. Otherwise
            // the player would keep falling through that platform once re-enabled.
            RestoreDropThroughCollision();
        }

        private void Update()
        {
            _timeSinceJumpPressed += Time.deltaTime;

            if (input.JumpPressed)
            {
                _timeSinceJumpPressed = 0f;
            }

            if (input.DropThroughPressed)
            {
                TryDropThrough();
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded();
            ApplyHorizontalMovement();
            ApplyJump();
            ApplyGravity();
        }

        private void UpdateGrounded()
        {
            IsGrounded = Physics2D.OverlapCircle(GroundCheckPosition, groundCheckRadius, groundLayers) != null;
            _timeSinceGrounded = IsGrounded ? 0f : _timeSinceGrounded + Time.fixedDeltaTime;

            // Landing ends the jump. Checking downward velocity avoids ending it on the frame
            // the jump starts, while the ground check still overlaps.
            if (IsGrounded && _body.linearVelocity.y <= 0f)
            {
                _isJumping = false;
            }
        }

        private void ApplyHorizontalMovement()
        {
            var velocity = _body.linearVelocity;
            velocity.x = input.HorizontalMove * moveSpeed;
            _body.linearVelocity = velocity;
        }

        private void ApplyJump()
        {
            var withinCoyoteTime = _timeSinceGrounded <= coyoteTime;
            var jumpWasBuffered = _timeSinceJumpPressed <= jumpBufferTime;

            if (withinCoyoteTime && jumpWasBuffered && !_isJumping)
            {
                var velocity = _body.linearVelocity;
                velocity.y = jumpSpeed;
                _body.linearVelocity = velocity;

                _isJumping = true;

                // Consume both windows so one press cannot produce two jumps.
                _timeSinceJumpPressed = float.PositiveInfinity;
                _timeSinceGrounded = float.PositiveInfinity;
                return;
            }

            if (_isJumping && !input.JumpHeld && _body.linearVelocity.y > 0f)
            {
                var velocity = _body.linearVelocity;
                velocity.y *= shortJumpCut;
                _body.linearVelocity = velocity;

                // Clearing the flag also means the cut happens once, not every frame of the rise.
                _isJumping = false;
            }
        }

        private void ApplyGravity()
        {
            _body.gravityScale = _body.linearVelocity.y > 0f ? riseGravity : fallGravity;
        }

        private void TryDropThrough()
        {
            if (!IsGrounded || _platformBeingDroppedThrough != null)
            {
                return;
            }

            var hit = Physics2D.Raycast(
                GroundCheckPosition, Vector2.down, dropThroughProbeDistance, groundLayers);

            if (hit.collider == null)
            {
                return;
            }

            // Only one-way platforms can be dropped through, and a PlatformEffector2D is what
            // makes a platform one-way. Testing for it means solid ground is rejected without
            // needing a dedicated layer or a marker component.
            if (!hit.collider.TryGetComponent<PlatformEffector2D>(out _))
            {
                return;
            }

            StartCoroutine(DropThrough(hit.collider));
        }

        private IEnumerator DropThrough(Collider2D platform)
        {
            _platformBeingDroppedThrough = platform;

            // Suppressing this one pair leaves the platform solid for everything else, unlike
            // disabling its collider.
            Physics2D.IgnoreCollision(_collider, platform, true);

            var elapsed = 0f;

            // Position is the real condition: hold the suppression until the player is fully
            // below the platform, so collision cannot be restored while still overlapping it.
            // The timeout is only a safety net against a missed frame leaving the player able to
            // fall through the world forever.
            while (elapsed < dropThroughTimeout && _collider.bounds.max.y > platform.bounds.min.y)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreDropThroughCollision();
        }

        private void RestoreDropThroughCollision()
        {
            if (_platformBeingDroppedThrough == null)
            {
                return;
            }

            Physics2D.IgnoreCollision(_collider, _platformBeingDroppedThrough, false);
            _platformBeingDroppedThrough = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GroundCheckPosition, groundCheckRadius);
        }
    }
}
