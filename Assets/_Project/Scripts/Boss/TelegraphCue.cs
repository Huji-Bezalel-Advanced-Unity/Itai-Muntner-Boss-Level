using System;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// How a particular wind-up looks. Each attack carries its own.
    /// </summary>
    /// <remarks>
    /// One shared tell for every attack reduces the telegraph to a countdown: the player learns
    /// that <i>something</i> is coming but not what, so the only available response is to keep
    /// moving and hope. Giving each attack a distinct colour and motion turns the warning into
    /// information — the player can start moving in the right direction before the first
    /// projectile exists. That is the difference between a fight that is hard and one that is
    /// merely fast.
    /// <para>
    /// Public fields rather than serialized private ones, because Unity serializes fields on a
    /// plain data struct and properties would never appear in the Inspector. This is the one
    /// deliberate exception to the project's field convention.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct TelegraphCue
    {
        [Tooltip("Colour the boss shifts towards. Make these obviously different per attack — " +
                 "this is the part the player actually reads.")]
        public Color Colour;

        [Tooltip("How many times the tell pulses across the telegraph. Several sharp pulses " +
                 "read as a burst and are recognisably not one long swell.")]
        [Min(1)] public int Pulses;

        [Tooltip("Per-axis swell. Squashing on Y while stretching on X reads as winding up for " +
                 "something that hits the ground.")]
        public Vector2 ScalePunch;

        [Tooltip("Shudder, running across the whole telegraph. Reserve it for the heaviest " +
                 "attacks so that it keeps meaning something.")]
        [Min(0f)] public float ShakeStrength;

        /// <summary>A neutral tell, used for an attack that has not been given its own look.</summary>
        public static TelegraphCue Default => new TelegraphCue
        {
            Colour = new Color(1f, 0.8f, 0.35f),
            Pulses = 1,
            ScalePunch = new Vector2(0.08f, 0.08f),
            ShakeStrength = 0f,
        };

        /// <summary>
        /// The phase change, which must not be mistakable for an attack.
        /// </summary>
        /// <remarks>
        /// Deliberately unlike any attack tell: white rather than a hue, several rapid pulses
        /// rather than one swell, and a strong shudder. A phase change means the rules just
        /// changed, and it should not look like the boss merely winding up again.
        /// </remarks>
        public static TelegraphCue PhaseTransition => new TelegraphCue
        {
            Colour = Color.white,
            Pulses = 6,
            ScalePunch = new Vector2(0.2f, 0.2f),
            ShakeStrength = 0.35f,
        };

        /// <summary>
        /// Whether this cue has actually been configured.
        /// </summary>
        /// <remarks>
        /// Unity fills a newly added struct field on an existing asset with zeroes rather than
        /// running the C# initialiser, which would leave an invisible tell — transparent, with
        /// no pulses. Assets repair themselves against this in their <c>OnValidate</c>.
        /// </remarks>
        public bool IsConfigured => Pulses >= 1 && Colour.a > 0f;
    }
}
