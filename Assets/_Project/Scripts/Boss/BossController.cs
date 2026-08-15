using System;
using System.Collections;
using System.Collections.Generic;
using BossLevel.Boss.Attacks;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// Runs the boss fight: choose an attack, warn, strike, recover, pause, repeat.
    /// </summary>
    /// <remarks>
    /// The controller owns the rhythm; the attack assets own their own shape. That split is the
    /// point of the design. Telegraph and recovery are sequenced here rather than inside each
    /// attack, so the fairness contract holds for every attack ever authored — the player always
    /// gets a visible warning before damage, and always gets a window where the boss is
    /// committed and can be punished. An attack written later cannot quietly omit either.
    /// <para>
    /// It also means difficulty scaling is data, not code: a later phase shortens the telegraph
    /// and recovery, and every attack in that phase becomes harder without being rewritten.
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

        [Header("Attacks")]
        [Tooltip("The attacks this boss can use. Listing one twice makes it twice as likely.")]
        [SerializeField] private List<BossAttack> attacks = new List<BossAttack>();

        [Header("Rhythm")]
        [Tooltip("Breathing room before the first attack, so the fight does not open mid-swing.")]
        [SerializeField, Min(0f)] private float openingDelay = 1.5f;

        [Tooltip("Idle pause between attacks, randomised within this range so the fight does " +
                 "not feel metronomic.")]
        [SerializeField] private Vector2 cooldownRange = new Vector2(0.5f, 1.2f);

        /// <summary>
        /// Raised once, when the boss is defeated. The game state machine listens to this to
        /// drive the win sequence.
        /// </summary>
        public event Action Defeated;

        /// <summary>The attack currently being telegraphed or executed, or null between attacks.</summary>
        public BossAttack CurrentAttack { get; private set; }

        private BossContext _context;
        private AttackSelector _selector;
        private Coroutine _fightLoop;

        private void Awake()
        {
            if (health == null || projectiles == null || muzzle == null || player == null)
            {
                Debug.LogError($"{nameof(BossController)} is missing a reference.", this);
                enabled = false;
                return;
            }

            // An empty slot in the list is easy to leave behind while authoring and would
            // otherwise surface much later as a null attack mid-fight.
            var usableAttacks = attacks.FindAll(attack => attack != null);

            if (usableAttacks.Count == 0)
            {
                Debug.LogError($"{nameof(BossController)} has no attacks assigned.", this);
                enabled = false;
                return;
            }

            _context = new BossContext(transform, muzzle, player, projectiles);
            _selector = new AttackSelector(usableAttacks);
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
                var attack = _selector.Next();
                CurrentAttack = attack;

                // Telegraph — the warning. Nothing damaging happens during this window.
                telegraph?.Play(attack.TelegraphDuration);
                yield return new WaitForSeconds(attack.TelegraphDuration);

                // Active — the threat. Yielding the attack's own coroutine runs it inline, so
                // stopping this loop also stops whatever the attack was in the middle of.
                yield return attack.Execute(_context);

                // Recovery — the boss is committed. This is when the player answers back.
                yield return new WaitForSeconds(attack.RecoveryDuration);

                CurrentAttack = null;

                // Idle — pacing, so attacks do not run together into an unbroken wall.
                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(cooldownRange.x, cooldownRange.y));
            }
        }

        private void OnDied()
        {
            StopFightLoop();
            telegraph?.Stop();
            CurrentAttack = null;

            // Clear the arena so the player cannot be killed by shots from a boss that is already
            // dead, which would otherwise turn a win into a draw.
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
