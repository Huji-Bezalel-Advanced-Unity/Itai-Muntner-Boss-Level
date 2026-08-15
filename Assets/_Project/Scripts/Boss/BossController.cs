using System;
using System.Collections;
using BossLevel.Boss.Attacks;
using BossLevel.Boss.Data;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// Runs the boss fight: choose an attack, warn, strike, recover, pause, repeat — escalating
    /// through phases as the boss loses health.
    /// </summary>
    /// <remarks>
    /// The controller owns the rhythm; the attack assets own their shape; the phase assets own
    /// the difficulty. Telegraph and recovery are sequenced here rather than inside each attack,
    /// so the fairness contract holds for every attack ever authored — the player always gets a
    /// visible warning before damage, and always gets a window in which the boss is committed
    /// and can be punished.
    /// <para>
    /// That split is what makes escalation data rather than code: a later phase scales those two
    /// beats, and every attack it contains becomes harder without being touched.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BossDefinition definition;
        [SerializeField] private Health health;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform player;

        [Tooltip("Optional. Without it the fight still runs, just with no visible wind-up.")]
        [SerializeField] private BossTelegraph telegraph;

        [Header("Rhythm")]
        [Tooltip("Breathing room before the first attack, so the fight does not open mid-swing.")]
        [SerializeField, Min(0f)] private float openingDelay = 1.5f;

        [Tooltip("How long the boss is untouchable while changing phase. Long enough to read as " +
                 "a beat of rest, short enough not to stall the fight.")]
        [SerializeField, Min(0f)] private float phaseTransitionDuration = 1.5f;

        /// <summary>Raised once, when the boss is defeated.</summary>
        public event Action Defeated;

        /// <summary>Raised when a new phase begins, with the phase and its zero-based index.</summary>
        public event Action<BossPhase, int> PhaseChanged;

        /// <summary>The phase currently in effect.</summary>
        public BossPhase CurrentPhase => definition.Phases[_phaseMachine.CurrentIndex];

        /// <summary>The attack being telegraphed or executed, or null between attacks.</summary>
        public BossAttack CurrentAttack { get; private set; }

        private BossContext _context;
        private BossPhaseMachine _phaseMachine;
        private AttackSelector _selector;
        private Coroutine _fightLoop;

        private void Awake()
        {
            if (definition == null || health == null || projectiles == null
                || muzzle == null || player == null)
            {
                Debug.LogError($"{nameof(BossController)} is missing a reference.", this);
                enabled = false;
                return;
            }

            if (definition.Phases.Count == 0)
            {
                Debug.LogError($"{definition.name} has no phases.", this);
                enabled = false;
                return;
            }

            // Health comes from the definition so the boss is described in one place rather than
            // half in an asset and half on a component in the scene.
            health.ResetTo(definition.MaxHealth);

            _context = new BossContext(transform, muzzle, player, projectiles);
            _phaseMachine = new BossPhaseMachine(definition.GetPhaseThresholds());

            RebuildSelectorForCurrentPhase();
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

        /// <summary>
        /// Ends the fight early and clears the arena — used when the player dies, so a boss that
        /// has already won does not keep shooting at a corpse.
        /// </summary>
        public void StopFighting()
        {
            StopFightLoop();
            telegraph?.Stop();
            CurrentAttack = null;
            projectiles.DespawnAll();
        }

        private IEnumerator FightLoop()
        {
            yield return new WaitForSeconds(openingDelay);

            while (health.IsAlive)
            {
                // Phase changes are handled between attacks, never during one. Interrupting an
                // attack halfway would let the player be killed by a strike that was cancelled,
                // which reads as the fight cheating.
                //
                // The loop matters: one large hit can cross two thresholds at once, and the
                // player is owed both transitions rather than a silent jump to the last phase.
                while (_phaseMachine.TryAdvance(health.Fraction))
                {
                    yield return PlayPhaseTransition();
                }

                if (!health.IsAlive)
                {
                    yield break;
                }

                var phase = CurrentPhase;
                var attack = _selector.Next();
                CurrentAttack = attack;

                // Telegraph — the warning. Nothing damaging happens during this window.
                var telegraphDuration = attack.TelegraphDuration * phase.TelegraphMultiplier;
                telegraph?.Play(telegraphDuration);
                yield return new WaitForSeconds(telegraphDuration);

                // Active — the threat. Yielding the attack's own coroutine runs it inline, so
                // stopping this loop also stops whatever the attack was in the middle of.
                yield return attack.Execute(_context);

                // Recovery — the boss is committed. This is when the player answers back.
                yield return new WaitForSeconds(attack.RecoveryDuration * phase.RecoveryMultiplier);

                CurrentAttack = null;

                // Idle — pacing, so attacks do not run together into an unbroken wall.
                var cooldown = phase.CooldownRange;
                yield return new WaitForSeconds(UnityEngine.Random.Range(cooldown.x, cooldown.y));
            }
        }

        private IEnumerator PlayPhaseTransition()
        {
            // Untouchable for the duration, so the player cannot burn through the next phase
            // during a moment when the boss cannot fight back.
            health.IsInvulnerable = true;

            // Clearing the arena is the point of the pause: it stops shots authored for the
            // phase that just ended from landing during the phase that has not started.
            projectiles.DespawnAll();
            telegraph?.Stop();

            RebuildSelectorForCurrentPhase();

            var phase = CurrentPhase;
            PhaseChanged?.Invoke(phase, _phaseMachine.CurrentIndex);

            // Reusing the wind-up as a "powering up" swell. Milestone 8 replaces this with the
            // shader's phase tint, which can run alongside the damage flash instead of fighting
            // it for the sprite's colour.
            telegraph?.Play(phaseTransitionDuration);
            yield return new WaitForSeconds(phaseTransitionDuration);

            health.IsInvulnerable = false;
        }

        private void RebuildSelectorForCurrentPhase()
        {
            var phase = CurrentPhase;

            if (phase == null || phase.Attacks.Count == 0)
            {
                Debug.LogError($"Boss phase {_phaseMachine.CurrentIndex} has no attacks.", this);
                enabled = false;
                return;
            }

            _selector = new AttackSelector(phase.Attacks);
        }

        private void OnDied()
        {
            StopFighting();
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
