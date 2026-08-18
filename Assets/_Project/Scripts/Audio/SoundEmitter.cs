using BossLevel.Common;
using UnityEngine;
using UnityEngine.Audio;

namespace BossLevel.Audio
{
    /// <summary>
    /// One pooled <see cref="AudioSource"/>, which plays a single sound and then hands itself back.
    /// </summary>
    /// <remarks>
    /// Pooled for the same reason projectiles are, and reusing the same
    /// <see cref="Pool{T}"/>: sounds happen in bursts at the busiest moments, and creating an
    /// AudioSource per shot allocates during exactly the frames that can least afford it.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class SoundEmitter : MonoBehaviour, IPoolable
    {
        /// <summary>Grace after the clip should have ended, before reclaiming the source.</summary>
        private const float ReleaseMargin = 0.1f;

        private AudioSource _source;
        private AudioService _owner;
        private float _releaseAt;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            _source.playOnAwake = false;
            _source.loop = false;
        }

        /// <summary>Plays <paramref name="sound"/> and schedules this emitter's return.</summary>
        public void Play(
            AudioService owner,
            SoundEvent sound,
            Vector3 position,
            float volumeScale,
            AudioMixerGroup mixerGroup)
        {
            var clip = sound.PickClip();

            if (clip == null)
            {
                return;
            }

            _owner = owner;
            transform.position = position;

            var pitch = sound.PickPitch();

            _source.outputAudioMixerGroup = mixerGroup;
            _source.clip = clip;
            _source.volume = sound.Volume * volumeScale;
            _source.pitch = pitch;
            _source.spatialBlend = sound.SpatialBlend;
            _source.Play();

            // Unscaled, and divided by pitch because a pitched-up clip finishes sooner. Audio is
            // not affected by time scale, so a hit stop must not hold an emitter that has
            // already finished playing.
            var playbackLength = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
            _releaseAt = Time.unscaledTime + playbackLength + ReleaseMargin;
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            _owner = null;
            _source.Stop();

            // Dropping the clip lets Unity unload it if nothing else is using it.
            _source.clip = null;
        }

        private void Update()
        {
            if (_owner == null || Time.unscaledTime < _releaseAt)
            {
                return;
            }

            var pool = _owner;
            _owner = null;
            pool.Release(this);
        }
    }
}
