using BossLevel.Audio;
using BossLevel.Combat;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossLevel.Player
{
    /// <summary>
    /// Fires the player's projectile at a fixed rate while the fire button is held.
    /// </summary>
    /// <remarks>
    /// Rate-limited on hold rather than one shot per press: the fight expects a steady stream,
    /// and tying fire rate to how fast the player can tap rewards the keyboard rather than the
    /// player.
    /// <para>
    /// The sound is handled in two halves, because firing sounds like two different things. A
    /// tap is a single shot; a held trigger is a continuous roar that no amount of retriggering
    /// one clip will imitate — overlapping copies of the same short sample phase against each
    /// other and turn into a rattle. So a tap plays the single-shot clip, and holding past a
    /// brief threshold hands over to a looping one which stops the moment the button is
    /// released.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerShooter : MonoBehaviour
    {
        /// <summary>Sentinel for "the fire button is not currently down".</summary>
        private const float NotHeld = -1f;

        [Header("Dependencies")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private ProjectilePool projectiles;

        [Tooltip("Where shots appear. Should sit just outside the player's own collider.")]
        [SerializeField] private Transform muzzle;

        [Header("Tuning")]
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6f;

        [Header("Sound (optional)")]
        [Tooltip("Played per shot while tapping. Silent once the sustained loop takes over.")]
        [FormerlySerializedAs("shootSound")]
        [SerializeField] private SoundEvent singleShotSound;

        [Tooltip("The continuous firing sound, looped while the button is held.")]
        [SerializeField] private LoopingSound sustainedFire;

        [Tooltip("How long the button must be held before the loop takes over. Roughly one or " +
                 "two shots — long enough that a deliberate tap stays a tap.")]
        [SerializeField, Min(0f)] private float sustainDelay = 0.22f;

        private float _nextShotTime;
        private float _heldSince = NotHeld;

        /// <summary>True once the button has been held long enough for the loop to take over.</summary>
        private bool IsSustaining => _heldSince >= 0f && Time.time - _heldSince >= sustainDelay;

        private void Awake()
        {
            if (input == null || motor == null || projectiles == null || muzzle == null)
            {
                Debug.LogError($"{nameof(PlayerShooter)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            // Control can be taken away mid-burst — at the end of the fight, or on death — and
            // the loop must not outlive the shooting it describes.
            _heldSince = NotHeld;

            if (sustainedFire != null)
            {
                sustainedFire.Stop();
            }
        }

        private void Update()
        {
            UpdateFiringSound();

            if (!input.FireHeld || Time.time < _nextShotTime)
            {
                return;
            }

            _nextShotTime = Time.time + 1f / shotsPerSecond;

            // Shots follow the way the player is facing, which the motor remembers even when
            // standing still.
            var direction = new Vector2(motor.Facing, 0f);

            projectiles.Spawn(muzzle.position, direction);

            // Only while tapping. Once the loop has taken over it carries the sound of firing by
            // itself, and layering single shots on top muddies both.
            if (!IsSustaining && singleShotSound != null)
            {
                singleShotSound.Play(muzzle.position);
            }
        }

        private void UpdateFiringSound()
        {
            if (!input.FireHeld)
            {
                _heldSince = NotHeld;

                if (sustainedFire != null)
                {
                    sustainedFire.Stop();
                }

                return;
            }

            if (_heldSince < 0f)
            {
                _heldSince = Time.time;
            }

            if (IsSustaining && sustainedFire != null && !sustainedFire.IsPlaying)
            {
                sustainedFire.Play();
            }
        }
    }
}
