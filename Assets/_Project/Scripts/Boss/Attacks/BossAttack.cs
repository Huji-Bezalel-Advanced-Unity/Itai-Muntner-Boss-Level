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

        [Tooltip("What this attack's wind-up looks like. Give every attack a distinct one — a " +
                 "warning the player can identify is worth far more than one they can only count.")]
        [SerializeField] private TelegraphCue telegraphCue = TelegraphCue.Default;

        public string DisplayName => displayName;

        public float TelegraphDuration => telegraphDuration;

        public float RecoveryDuration => recoveryDuration;

        /// <summary>How this attack announces itself before it lands.</summary>
        public TelegraphCue TelegraphCue => telegraphCue;

        /// <summary>
        /// Repairs a cue that Unity zero-filled.
        /// </summary>
        /// <remarks>
        /// When a struct field is added to a type, Unity fills it on already-authored assets
        /// with zeroes rather than running the C# initialiser — which would leave a transparent
        /// tell with no pulses, i.e. no visible warning at all. Rather than making every asset
        /// be fixed by hand, they repair themselves on import.
        /// </remarks>
        private void OnValidate()
        {
            if (!telegraphCue.IsConfigured)
            {
                telegraphCue = TelegraphCue.Default;
            }
        }

        /// <summary>
        /// Performs the attack. The telegraph has already played and the recovery window follows
        /// automatically, so this covers only the active beat — the part that actually threatens.
        /// </summary>
        public abstract IEnumerator Execute(BossContext context);

        /// <summary>
        /// How well this attack fits the situation right now, from 0 (pointless) to 1 (ideal).
        /// </summary>
        /// <remarks>
        /// This is where the boss's judgement lives. Cycling attacks at random makes a boss that
        /// is merely busy; choosing the one that answers what the player is currently doing —
        /// punishing a camper with something pinpoint, covering a runner's escape routes,
        /// declining to send a shockwave along the floor at somebody already in the air — is
        /// what makes it read as a fight rather than as a sprinkler.
        /// <para>
        /// The default is deliberately middling, so an attack that has no opinion competes on
        /// even terms rather than never being picked.
        /// </para>
        /// </remarks>
        public virtual float Suitability(BossContext context) => 0.5f;
    }
}
