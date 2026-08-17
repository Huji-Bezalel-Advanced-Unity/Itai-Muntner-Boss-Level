using System.Collections.Generic;
using BossLevel.Boss.Attacks;
using UnityEngine;

namespace BossLevel.Boss.Data
{
    /// <summary>
    /// One tier of the fight: which attacks the boss uses, and how hard it uses them.
    /// </summary>
    /// <remarks>
    /// Escalation is expressed almost entirely here rather than in code. A later phase adds
    /// attacks to its list, shortens the warning the player gets, and shortens the window in
    /// which the boss can be punished — and every attack in that phase becomes harder without
    /// any of them being rewritten.
    /// <para>
    /// The multipliers are the important part. Because <see cref="BossController"/> owns the
    /// telegraph and recovery beats, scaling them here scales them for every attack uniformly,
    /// which is why difficulty is a tuning exercise rather than an authoring one.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Phase", menuName = "Boss Level/Phase")]
    public class BossPhase : ScriptableObject
    {
        [Tooltip("Shown when the phase begins.")]
        [SerializeField] private string displayName = "Phase";

        [Tooltip("This phase begins once the boss's remaining health drops to or below this " +
                 "fraction. The first phase should be 1.")]
        [SerializeField, Range(0f, 1f)] private float healthThreshold = 1f;

        [Tooltip("Attacks available in this phase. Listing one twice makes it twice as likely.")]
        [SerializeField] private List<BossAttack> attacks = new List<BossAttack>();

        [Tooltip("Idle pause between attacks, randomised within this range.")]
        [SerializeField] private Vector2 cooldownRange = new Vector2(0.5f, 1.2f);

        [Tooltip("Scales every attack's telegraph. Below 1 means less warning — the single most " +
                 "effective way to make a phase harder.")]
        [SerializeField, Range(0.1f, 2f)] private float telegraphMultiplier = 1f;

        [Tooltip("Scales every attack's recovery. Below 1 shrinks the player's damage window.")]
        [SerializeField, Range(0.1f, 2f)] private float recoveryMultiplier = 1f;

        [Tooltip("How far ahead of the player the boss aims: 0 fires at where they are, 1 fires " +
                 "at where they will be. Raise it in later phases so the boss visibly learns to " +
                 "read the player rather than simply firing faster.")]
        [SerializeField, Range(0f, 1f)] private float aimLead = 0.5f;

        [Tooltip("Multiplied into the boss's sprite for the whole phase, so the fight escalates " +
                 "visibly and not only mechanically. White leaves it untouched.")]
        [SerializeField] private Color tint = Color.white;

        public string DisplayName => displayName;

        public float HealthThreshold => healthThreshold;

        public IReadOnlyList<BossAttack> Attacks => attacks;

        public Vector2 CooldownRange => cooldownRange;

        public float TelegraphMultiplier => telegraphMultiplier;

        public float RecoveryMultiplier => recoveryMultiplier;

        /// <summary>How far ahead of the player this phase leads its shots, from 0 to 1.</summary>
        public float AimLead => aimLead;

        /// <summary>The colour the boss takes on for this phase.</summary>
        public Color Tint => tint;

        /// <summary>
        /// Repairs a tint that Unity zero-filled when the field was introduced, which would
        /// otherwise render the boss fully transparent from the first phase change onwards.
        /// </summary>
        private void OnValidate()
        {
            if (tint.a <= 0f)
            {
                tint = Color.white;
            }
        }
    }
}
