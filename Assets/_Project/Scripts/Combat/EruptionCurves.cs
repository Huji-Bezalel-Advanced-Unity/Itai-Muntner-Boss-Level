using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// The default shape of an eruption, expressed as curves over its normalised duration.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="VolcanoHazard"/> because these describe what an eruption
    /// should look like, which is a different question from how a vent behaves. They are only
    /// starting points — the curves serialized on the prefab are what actually run, and are
    /// meant to be pushed around by eye.
    /// </remarks>
    public static class EruptionCurves
    {
        /// <summary>
        /// Bursts past full height, falls back, holds, then drops away.
        /// </summary>
        /// <remarks>
        /// The overshoot is the important part. Rising straight to full height and stopping
        /// reads as a bar being filled; going too far and settling back reads as pressure
        /// escaping.
        /// </remarks>
        public static AnimationCurve Height()
        {
            return Smoothed(
                new Keyframe(0f, 0f),
                new Keyframe(0.13f, 1.15f),
                new Keyframe(0.28f, 0.92f),
                new Keyframe(0.62f, 1f),
                new Keyframe(0.82f, 0.8f),
                new Keyframe(1f, 0f));
        }

        /// <summary>Flares wide as the pressure escapes, then narrows as it burns down.</summary>
        public static AnimationCurve Width()
        {
            return Smoothed(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.09f, 1.3f),
                new Keyframe(0.32f, 0.85f),
                new Keyframe(0.7f, 0.72f),
                new Keyframe(1f, 0.15f));
        }

        /// <summary>Holds solid, then gutters out rather than blinking off.</summary>
        public static AnimationCurve Alpha()
        {
            return Smoothed(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.72f, 1f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve Smoothed(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);

            // Default tangents are flat, which produces visible steps between keys. Smoothing
            // every key is what turns a handful of points into a continuous motion.
            for (var i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }
    }
}
