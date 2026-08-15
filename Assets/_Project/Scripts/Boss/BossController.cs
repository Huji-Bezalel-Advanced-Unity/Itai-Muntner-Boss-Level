using System;
using System.Collections;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// Runs the boss fight: warn, strike, recover, pause, repeat, until the boss dies.
    /// </summary>
    /// <remarks>
    /// This milestone carries a single hardcoded attack. That is deliberate — the four-beat
    /// rhythm around the attack is the part that matters, and building one real attack first
    /// means the ScriptableObject attack system that replaces it is shaped by an actual case
    /// rather than a guess at one. Only the middle beat changes.
    /// <para>
    /// Telegraph and recovery live here rather than inside the attack itself, so the fairness
    /// contract holds for every attack ever added: the player always gets a visible warning
    /// before damage, and always gets a window in which the boss is committed and can be
    /// punished. Stated once here, it cannot be quietly forgotten by an attack written later.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Health health;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform player;

        [Tooltip("Optional. Without it the fight still runs, just with no visible wind-up.")]
        [SerializeField] private BossTelegraph telegraph;

        [Header("Attack rhythm")]
        [Tooltip("Breathing room before the first attack, so the fight does not open mid-swing.")]
        [SerializeField] private float openingDelay = 1.5f;

        [Tooltip("Visible warning before damage. This is the player's reaction time — shortening " +
                 "it is the main lever for making a later phase harder.")]
        [SerializeField] private float telegraphDuration = 0.7f;

        [Tooltip("The boss is committed and cannot act. This is the player's damage window.")]
        [SerializeField] private float recoveryDuration = 0.9f;

        [Tooltip("Idle pause before the next attack, randomised so the fight does not feel " +
                 "metronomic.")]
        [SerializeField] private Vector2 cooldownRange = new Vector2(0.5f, 1.2f);

        [Header("Spread shot")]
        [SerializeField, Min(1)] private int bulletCount = 5;

        [Tooltip("Total width of the fan, centred on the player.")]
        [SerializeField, Range(0f, 360f)] private float arcDegrees = 60f;

        /// <summary>
        /// Raised once, when the boss is defeated. The game state machine will listen to this to
        /// drive the win sequence.
        /// </summary>
        public event Action Defeated;

        private Coroutine _fightLoop;

        private void Awake()
        {
            if (health == null || projectiles == null || muzzle == null || player == null)
            {
                Debug.LogError($"{nameof(BossController)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Died += OnDied;
            _fightLoop = StartCoroutine(FightLoop());
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
            StopFightLoop();
        }

        private IEnumerator FightLoop()
        {
            yield return new WaitForSeconds(openingDelay);

            while (health.IsAlive)
            {
                // Telegraph — the warning. No damage happens during this window.
                telegraph?.Play(telegraphDuration);
                yield return new WaitForSeconds(telegraphDuration);

                // Active — the threat. Instantaneous for this attack; later ones will hold here.
                FireSpread();

                // Recovery — the boss is committed. This is when the player answers back.
                yield return new WaitForSeconds(recoveryDuration);

                // Idle — pacing, so attacks do not run together into a wall.
                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(cooldownRange.x, cooldownRange.y));
            }
        }

        /// <summary>
        /// Fires a fan of projectiles centred on wherever the player is standing at the moment
        /// the shot goes off — aiming at the telegraph's end, not its start, so backing away
        /// during the wind-up does not make the attack miss for free.
        /// </summary>
        private void FireSpread()
        {
            var origin = (Vector2)muzzle.position;
            var toPlayer = (Vector2)player.position - origin;

            var centreAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            var startAngle = centreAngle - arcDegrees * 0.5f;

            // With a single bullet there is no gap to divide, so fire it straight down the middle.
            var angleStep = bulletCount > 1 ? arcDegrees / (bulletCount - 1) : 0f;
            var firstAngle = bulletCount > 1 ? startAngle : centreAngle;

            for (var i = 0; i < bulletCount; i++)
            {
                var angle = (firstAngle + angleStep * i) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                projectiles.Spawn(origin, direction);
            }
        }

        private void OnDied()
        {
            StopFightLoop();
            telegraph?.Stop();

            // Clear the arena so the player cannot be killed by shots from a boss that is
            // already dead — which would otherwise turn a win into a draw.
            projectiles.DespawnAll();

            Defeated?.Invoke();
        }

        private void StopFightLoop()
        {
            if (_fightLoop == null)
            {
                return;
            }

            StopCoroutine(_fightLoop);
            _fightLoop = null;
        }
    }
}
