using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Player
{
    /// <summary>
    /// Turns movement intent into physics: running, jumping, double jumping and dashing.
    /// </summary>
    /// <remarks>
    /// Input edges are sampled in <see cref="Update"/>, because a button-down is only true for a
    /// single rendered frame and would be missed at the physics rate. Everything that touches the
    /// rigidbody happens in <see cref="FixedUpdate"/>, so movement does not change with frame
    /// rate.
    /// <para>
    /// This file is longer than the project's usual guideline, and deliberately so: running,
    /// jumping and dashing are one responsibility — how the player moves — and all three write
    /// to the same rigidbody in the same fixed step. Splitting them would mean separate
    /// components competing to set velocity, and working out which one won on a given frame is
    /// far harder to follow than reading them in order here.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour, ITarget
    {
        /// <summary>Below this much input, facing is left alone rather than flipped.</summary>
        private const float FacingDeadzone = 0.01f;

        [Header("Dependencies")]
        [SerializeField] private PlayerInputReader input;

        [Tooltip("Optional. Only needed so a dash can grant invulnerability.")]
        [SerializeField] private Health health;

        [Header("Running")]
        [SerializeField] private float moveSpeed = 8f;

        [Header("Jumping")]
        [Tooltip("Upward speed applied on jump. Set directly rather than as an impulse so a " +
                 "jump taken while falling reaches the same height as one from rest.")]
        [SerializeField] private float jumpSpeed = 14f;

        [Tooltip("Jumps available after leaving the ground. One gives the usual double jump.")]
        [SerializeField, Min(0)] private int airJumps = 1;

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

        [Header("Dashing")]
        [SerializeField] private float dashSpeed = 22f;

        [Tooltip("Short on purpose. A dash is a commitment to a moment, not a means of travel.")]
        [SerializeField, Min(0.02f)] private float dashDuration = 0.16f;

        [SerializeField, Min(0f)] private float dashCooldown = 0.6f;

        [Tooltip("Whether a dash passes through damage. This is what makes reading a telegraph " +
                 "correctly worth something, rather than retreating always being the safest play.")]
        [SerializeField] private bool dashGrantsInvulnerability = true;

        [Header("Ground detection")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody2D _body;

        private float _timeSinceGrounded = float.PositiveInfinity;
        private float _timeSinceJumpPressed = float.PositiveInfinity;
        private int _airJumpsUsed;
        private bool _canCutJump;

        private bool _isDashing;
        private bool _airDashUsed;
        private float _dashEndsAt;
        private float _dashReadyAt;
        private float _dashDirection = 1f;
        private bool _holdingDashInvulnerability;

        /// <summary>Whether the player is currently standing on something.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// The direction the player is facing: 1 for right, -1 for left. Retained while standing
        /// still, so aiming does not snap back to a default the moment the player stops.
        /// </summary>
        public float Facing { get; private set; } = 1f;

        /// <summary>True for the brief window of a dash. Shooting and aiming can read it.</summary>
        public bool IsDashing => _isDashing;

        /// <summary>Where the player is. Part of <see cref="ITarget"/>, read by the boss.</summary>
        public Vector2 Position => transform.position;

        /// <summary>
        /// How fast the player is moving. Part of <see cref="ITarget"/>, and what lets the boss
        /// lead its shots rather than firing at where the player already was.
        /// </summary>
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private Vector2 GroundCheckPosition => (Vector2)transform.position + groundCheckOffset;

        /// <summary>
        /// Configures sensible rigidbody defaults when this component is first added in the
        /// editor, so the player cannot tip over or tunnel through geometry at speed.
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

            if (input == null)
            {
                Debug.LogError($"{nameof(PlayerMotor)} has no {nameof(PlayerInputReader)} assigned.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            // Losing control mid-dash must not leave the player permanently untouchable.
            EndDash();
        }

        private void Update()
        {
            _timeSinceJumpPressed += Time.deltaTime;

            if (input.JumpPressed)
            {
                _timeSinceJumpPressed = 0f;
            }

            if (input.DashPressed)
            {
                TryStartDash();
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            if (_isDashing)
            {
                if (Time.time < _dashEndsAt)
                {
                    // A dash overrides gravity and steering entirely. Being unable to correct
                    // mid-dash is what makes committing to one a decision.
                    _body.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
                    return;
                }

                EndDash();
            }

            ApplyHorizontalMovement();
            ApplyJump();
            ApplyGravity();
        }

        private void UpdateGrounded()
        {
            IsGrounded = Physics2D.OverlapCircle(GroundCheckPosition, groundCheckRadius, groundLayers) != null;
            _timeSinceGrounded = IsGrounded ? 0f : _timeSinceGrounded + Time.fixedDeltaTime;

            // Checking downward velocity avoids resetting on the frame a jump starts, while the
            // ground check still overlaps what was just left.
            if (IsGrounded && _body.linearVelocity.y <= 0f)
            {
                _airJumpsUsed = 0;
                _airDashUsed = false;
                _canCutJump = false;
            }
        }

        private void ApplyHorizontalMovement()
        {
            var move = input.HorizontalMove;

            var velocity = _body.linearVelocity;
            velocity.x = move * moveSpeed;
            _body.linearVelocity = velocity;

            // A deadzone keeps analogue drift or a barely-touched key from flipping the facing.
            if (Mathf.Abs(move) > FacingDeadzone)
            {
                Facing = Mathf.Sign(move);
            }
        }

        private void ApplyJump()
        {
            var jumpWasBuffered = _timeSinceJumpPressed <= jumpBufferTime;

            if (jumpWasBuffered)
            {
                var canJumpFromGround = _timeSinceGrounded <= coyoteTime;
                var canJumpFromAir = _airJumpsUsed < airJumps;

                if (canJumpFromGround || canJumpFromAir)
                {
                    if (!canJumpFromGround)
                    {
                        _airJumpsUsed++;
                    }

                    PerformJump();
                    return;
                }
            }

            if (_canCutJump && !input.JumpHeld && _body.linearVelocity.y > 0f)
            {
                var velocity = _body.linearVelocity;
                velocity.y *= shortJumpCut;
                _body.linearVelocity = velocity;

                _canCutJump = false;
            }
        }

        private void PerformJump()
        {
            var velocity = _body.linearVelocity;
            velocity.y = jumpSpeed;
            _body.linearVelocity = velocity;

            _canCutJump = true;

            // Consume both windows so one press cannot produce two jumps.
            _timeSinceJumpPressed = float.PositiveInfinity;
            _timeSinceGrounded = float.PositiveInfinity;
        }

        private void ApplyGravity()
        {
            _body.gravityScale = _body.linearVelocity.y > 0f ? riseGravity : fallGravity;
        }

        private void TryStartDash()
        {
            if (_isDashing || Time.time < _dashReadyAt)
            {
                return;
            }

            // One dash per trip through the air, so it cannot be chained into flight.
            if (!IsGrounded)
            {
                if (_airDashUsed)
                {
                    return;
                }

                _airDashUsed = true;
            }

            var steer = input.HorizontalMove;
            _dashDirection = Mathf.Abs(steer) > FacingDeadzone ? Mathf.Sign(steer) : Facing;
            Facing = _dashDirection;

            _isDashing = true;
            _dashEndsAt = Time.time + dashDuration;
            _dashReadyAt = _dashEndsAt + dashCooldown;

            if (dashGrantsInvulnerability && health != null)
            {
                health.HoldInvulnerability();
                _holdingDashInvulnerability = true;
            }
        }

        private void EndDash()
        {
            _isDashing = false;

            if (!_holdingDashInvulnerability)
            {
                return;
            }

            // Released rather than cleared, so hit-frames running at the same time survive.
            health.ReleaseInvulnerability();
            _holdingDashInvulnerability = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GroundCheckPosition, groundCheckRadius);
        }
    }
}
