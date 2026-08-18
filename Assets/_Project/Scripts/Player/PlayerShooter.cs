using BossLevel.Audio;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Player
{
    /// <summary>
    /// Fires the player's projectile at a fixed rate while the fire button is held.
    /// </summary>
    /// <remarks>
    /// Rate-limited on hold rather than one shot per press: the fight expects a steady stream,
    /// and tying fire rate to how fast the player can tap rewards the keyboard rather than the
    /// player.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private ProjectilePool projectiles;

        [Tooltip("Where shots appear. Should sit just outside the player's own collider.")]
        [SerializeField] private Transform muzzle;

        [Header("Tuning")]
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6f;

        [Tooltip("Optional. The most repeated sound in the game, so it wants several clips and " +
                 "a little pitch variation.")]
        [SerializeField] private SoundEvent shootSound;

        private float _nextShotTime;

        private void Awake()
        {
            if (input == null || motor == null || projectiles == null || muzzle == null)
            {
                Debug.LogError($"{nameof(PlayerShooter)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!input.FireHeld || Time.time < _nextShotTime)
            {
                return;
            }

            _nextShotTime = Time.time + 1f / shotsPerSecond;

            // Shots follow the way the player is facing, which the motor remembers even when
            // standing still.
            var direction = new Vector2(motor.Facing, 0f);

            projectiles.Spawn(muzzle.position, direction);

            if (shootSound != null)
            {
                shootSound.Play(muzzle.position);
            }
        }
    }
}
