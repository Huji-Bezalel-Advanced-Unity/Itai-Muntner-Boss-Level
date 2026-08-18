using DG.Tweening;
using UnityEngine;

namespace BossLevel.Audio
{
    /// <summary>
    /// A sound that plays continuously for as long as something is happening, and fades out when
    /// it stops.
    /// </summary>
    /// <remarks>
    /// The counterpart to <see cref="SoundEvent"/>, which fires once and forgets. A held action —
    /// sustained fire, a charging attack — needs a source that can be started, held and stopped,
    /// so this owns its own <see cref="AudioSource"/> rather than borrowing one from the pool.
    /// A pooled emitter releases itself on a timer, which is exactly wrong for something that
    /// should last an unknown length of time.
    /// <para>
    /// <b>The fades are the point.</b> Starting a looping clip at full volume clicks, and cutting
    /// it dead clicks louder — for a sound that starts and stops several times a second while
    /// the player taps, that is the difference between a weapon and a fault. A few tens of
    /// milliseconds either side removes it entirely.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class LoopingSound : MonoBehaviour
    {
        [SerializeField] private SoundEvent sound;

        [Tooltip("Short. Long enough to remove the click, short enough that the sound feels " +
                 "immediate.")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.05f;

        [SerializeField, Min(0f)] private float fadeOutDuration = 0.09f;

        private AudioSource _source;
        private Tween _fade;
        private bool _isActive;

        /// <summary>Whether this sound is currently meant to be heard.</summary>
        /// <remarks>
        /// Tracks intent rather than <see cref="AudioSource.isPlaying"/>, which stays true
        /// throughout a fade-out and would make a restart during one look like a no-op.
        /// </remarks>
        public bool IsPlaying => _isActive;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0f;

            if (sound == null)
            {
                Debug.LogError($"{nameof(LoopingSound)} has no {nameof(SoundEvent)} assigned.", this);
                enabled = false;
                return;
            }

            _source.clip = sound.PickClip();
            _source.pitch = sound.PickPitch();
            _source.spatialBlend = sound.SpatialBlend;
            _source.outputAudioMixerGroup = AudioService.Exists
                ? AudioService.Instance.EffectsGroup
                : null;
        }

        /// <summary>Starts the loop, or fades it back in if it was still fading out.</summary>
        public void Play()
        {
            if (!enabled || _isActive)
            {
                return;
            }

            _isActive = true;

            // Resumed rather than restarted when a fade-out is still running, so tapping the
            // button quickly does not chop the loop back to its first sample every time.
            if (!_source.isPlaying)
            {
                _source.Play();
            }

            FadeTo(TargetVolume, fadeInDuration);
        }

        /// <summary>Fades the loop out and stops it.</summary>
        public void Stop()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;

            FadeTo(0f, fadeOutDuration).OnComplete(() =>
            {
                // Guarded, because Play may have been called again during the fade.
                if (!_isActive)
                {
                    _source.Stop();
                }
            });
        }

        private float TargetVolume =>
            sound.Volume * (AudioService.Exists ? AudioService.Instance.EffectsVolume : 1f);

        private Tween FadeTo(float target, float duration)
        {
            _fade?.Kill();

            // Unscaled, so a loop still fades out during a hit stop rather than hanging at full
            // volume until time resumes.
            _fade = DOVirtual
                .Float(_source.volume, target, duration, value => _source.volume = value)
                .SetUpdate(true);

            return _fade;
        }

        private void OnDisable()
        {
            // Losing control mid-loop must not leave the sound running with nothing driving it.
            _fade?.Kill();
            _fade = null;

            _isActive = false;
            _source.volume = 0f;
            _source.Stop();
        }
    }
}
