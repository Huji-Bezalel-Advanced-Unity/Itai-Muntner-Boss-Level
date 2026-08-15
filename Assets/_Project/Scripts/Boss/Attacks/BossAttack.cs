using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// One thing the boss can do, authored as an asset rather than written into the boss.
    /// </summary>
    /// <remarks>
    /// An attack is mostly numbers — how many shots, how wide, how fast, how much warning. Those
    /// belong in an asset that can be tuned in the Inspector while the game is running, because
    /// a boss fight is made good by tuning it many times, and a recompile between every attempt
    /// quietly stops that happening.
    /// <para>
    /// Escalation across phases mostly does not need new code: duplicating this asset and
    /// retuning the numbers gives a harder variant of the same attack.
    /// </para>
    /// <para>
    /// <b>An attack asset holds configuration only, never runtime state.</b> A ScriptableObject
    /// is a single shared instance, so a field written during play keeps that value between play
    /// sessions in the editor and is shared by every boss using the asset. This is why
    /// <see cref="Execute"/> is a coroutine: its local variables live in the iterator object the
    /// compiler creates per call, so each execution gets isolated state for free.
    /// </para>
    /// </remarks>
    public abstract class BossAttack : ScriptableObject
    {
        [Tooltip("Shown in the phase banner and used when reading the attack list at a glance.")]
        [SerializeField] private string displayName = "Attack";

        [Tooltip("Visible warning before any damage. This is the player's reaction time, and " +
                 "the main lever for making a later phase harder.")]
        [SerializeField, Min(0f)] private float telegraphDuration = 0.7f;

        [Tooltip("How long the boss stays committed afterwards. This is the player's window to " +
                 "deal damage.")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.9f;

        public string DisplayName => displayName;

        public float TelegraphDuration => telegraphDuration;

        public float RecoveryDuration => recoveryDuration;

        /// <summary>
        /// Performs the attack. The telegraph has already played and the recovery window follows
        /// automatically, so this covers only the active beat — the part that actually threatens.
        /// </summary>
        public abstract IEnumerator Execute(BossContext context);
    }
}
