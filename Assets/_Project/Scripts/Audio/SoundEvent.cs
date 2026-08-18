using UnityEngine;

namespace BossLevel.Audio
{
    /// <summary>
    /// A sound the game can make, authored as an asset rather than as a clip reference.
    /// </summary>
    /// <remarks>
    /// The same reasoning as <see cref="Boss.Attacks.BossAttack"/>: a sound is mostly numbers —
    /// which clip, how loud, how much the pitch wanders — and those want tuning while the game
    /// is running rather than through a recompile. It also puts a layer between the code and the
    /// clip, so swapping a placeholder for a real recording is a change to one asset instead of
    /// a change to whichever component happened to reference it.
    /// <para>
    /// Several clips can be listed for one event. Repetition is what makes game audio grating,
    /// and a gun that fires four times a second is the worst offender in this project — picking
    /// a random clip and wandering the pitch slightly is most of the cure.
    /// </para>
    /// <para>
    /// <b>Configuration only, never runtime state.</b> A ScriptableObject is a single shared
    /// asset, so the throttle interval lives here but the record of when it last played lives in
    /// <see cref="AudioService"/>.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Sound", menuName = "Boss Level/Sound Event")]
    public class SoundEvent : ScriptableObject
    {
        [Tooltip("One is fine. Several are better for anything that repeats often — the game " +
                 "picks between them at random.")]
        [SerializeField] private AudioClip[] clips;

        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        [Tooltip("Pitch is randomised within this range each time, so repeats do not sound " +
                 "mechanically identical.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Tooltip("0 plays flat in both ears, 1 places the sound in the world. Interface sounds " +
                 "want 0; things happening in the arena can want more.")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend;

        [Tooltip("Shortest gap between two plays of this event. Stops a rapid-fire weapon " +
                 "stacking a dozen identical clips into a single frame of noise.")]
        [SerializeField, Min(0f)] private float minimumInterval = 0.04f;

        public float Volume => volume;

        public float SpatialBlend => spatialBlend;

        public float MinimumInterval => minimumInterval;

        public bool HasClips => clips != null && clips.Length > 0;

        /// <summary>Plays flat, without a position. Right for interface and player sounds.</summary>
        public void Play()
        {
            Play(Vector3.zero);
        }

        /// <summary>Plays at a point in the world, audible through this event's spatial blend.</summary>
        public void Play(Vector3 position)
        {
            // Silence rather than an error when no service exists: playing a gameplay scene
            // directly instead of through Bootstrap is a normal way to work on it, and warning
            // on every shot would drown the console.
            if (!AudioService.Exists)
            {
                return;
            }

            AudioService.Instance.Play(this, position);
        }

        /// <summary>Picks one of the clips at random.</summary>
        public AudioClip PickClip()
        {
            if (!HasClips)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>Picks a pitch from this event's range.</summary>
        public float PickPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }
}
