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

        public string DisplayName => displayName;

        public float HealthThreshold => healthThreshold;

        public IReadOnlyList<BossAttack> Attacks => attacks;

        public Vector2 CooldownRange => cooldownRange;

        public float TelegraphMultiplier => telegraphMultiplier;

        public float RecoveryMultiplier => recoveryMultiplier;
    }
}
