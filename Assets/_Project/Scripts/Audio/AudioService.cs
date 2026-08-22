using System.Collections.Generic;
using BossLevel.Common;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace BossLevel.Audio
{
    /// <summary>
    /// Plays every sound in the game: pooled one-shots, and one crossfading music track.
    /// </summary>
    /// <remarks>
    /// Created once in the Bootstrap scene and kept for the session, because music has to
    /// survive the scene changes it is playing across — a track that restarted every time the
    /// player pressed retry would make the fight feel like it was stuttering rather than
    /// continuing.
    /// <para>
    /// Callers do not talk to this directly. They hold a <see cref="SoundEvent"/> and call
    /// <c>Play</c> on it, which keeps call sites reading as <i>what</i> should be heard rather
    /// than as how it gets played.
    /// </para>
    /// </remarks>
    public class AudioService : PersistentSingleton<AudioService>
    {
        [Header("Sound effects")]
        [SerializeField] private SoundEmitter emitterPrefab;

        [Tooltip("Sources created up front. The pool grows if a moment needs more.")]
        [SerializeField, Min(1)] private int initialEmitters = 8;

        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.9f;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Tooltip("Total length of a crossfade between two tracks.")]
        [SerializeField, Min(0f)] private float crossfadeDuration = 0.9f;

        [Header("Mixing (optional)")]
        [Tooltip("Assign if the project gains an AudioMixer. Everything works without one.")]
        [SerializeField] private AudioMixerGroup effectsGroup;

        [SerializeField] private AudioMixerGroup musicGroup;

        private Pool<SoundEmitter> _emitters;
        private Tween _musicFade;

        /// <summary>
        /// The overall level for sound effects, so sounds that own their own source — such as
        /// <see cref="LoopingSound"/> — are mixed alongside the pooled ones rather than beside
        /// them.
        /// </summary>
        public float EffectsVolume => effectsVolume;

        /// <summary>The mixer group effects route through, or null if the project has no mixer.</summary>
        public AudioMixerGroup EffectsGroup => effectsGroup;

        /// <summary>
        /// When each event was last heard, so <see cref="SoundEvent.MinimumInterval"/> can be
        /// honoured.
        /// </summary>
        /// <remarks>
        /// The interval is configuration and lives on the asset; the timestamp is runtime state
        /// and therefore lives here. Writing it back to the asset would persist it between play
        /// sessions in the editor and share it across every user of that sound.
        /// </remarks>
        private readonly Dictionary<SoundEvent, float> _lastPlayed = new Dictionary<SoundEvent, float>();

        protected override void Awake()
        {
            base.Awake();

            // A duplicate has already destroyed itself by this point and must not go on to build
            // a pool it will never use.
            if (Instance != this)
            {
                return;
            }

            if (emitterPrefab == null)
            {
                Debug.LogError($"{nameof(AudioService)} has no emitter prefab assigned.", this);
                enabled = false;
                return;
            }

            _emitters = new Pool<SoundEmitter>(emitterPrefab, transform, initialEmitters);

            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.outputAudioMixerGroup = musicGroup;
                musicSource.volume = 0f;
            }
        }

        /// <summary>Plays a sound effect. Called by <see cref="SoundEvent.Play()"/>.</summary>
        public void Play(SoundEvent sound, Vector3 position)
        {
            if (sound == null || !sound.HasClips || !enabled)
            {
                return;
            }

            if (!HasWaitedLongEnough(sound))
            {
                return;
            }

            _lastPlayed[sound] = Time.unscaledTime;

            var emitter = _emitters.Get();
            emitter.Play(this, sound, position, effectsVolume, effectsGroup);
        }

        /// <summary>Returns a finished emitter to the pool. Called by the emitter itself.</summary>
        public void Release(SoundEmitter emitter)
        {
            _emitters.Return(emitter);
        }

        /// <summary>
        /// Crossfades to a track.
        /// </summary>
        /// <param name="track">The clip to play.</param>
        /// <param name="restartIfAlreadyPlaying">
        /// True to start the track over even if it is the one currently playing. False leaves it
        /// running, which is what lets two scenes share music without it restarting on every
        /// transition.
        /// </param>
        public void PlayMusic(AudioClip track, bool restartIfAlreadyPlaying = false)
        {
            if (musicSource == null || track == null)
            {
                return;
            }

            if (!restartIfAlreadyPlaying && musicSource.clip == track && musicSource.isPlaying)
            {
                return;
            }

            _musicFade?.Kill();

            if (!musicSource.isPlaying)
            {
                StartTrack(track);
                _musicFade = FadeMusicTo(musicVolume, crossfadeDuration);
                return;
            }

            // Out, swap, in — half the crossfade each way.
            var half = crossfadeDuration * 0.5f;

            _musicFade = FadeMusicTo(0f, half).OnComplete(() =>
            {
                StartTrack(track);
                _musicFade = FadeMusicTo(musicVolume, half);
            });
        }

        /// <summary>Fades the music out and stops it.</summary>
        public void StopMusic()
        {
            if (musicSource == null || !musicSource.isPlaying)
            {
                return;
            }

            _musicFade?.Kill();
            _musicFade = FadeMusicTo(0f, crossfadeDuration * 0.5f).OnComplete(musicSource.Stop);
        }

        private bool HasWaitedLongEnough(SoundEvent sound)
        {
            if (!_lastPlayed.TryGetValue(sound, out var lastTime))
            {
                return true;
            }

            return Time.unscaledTime - lastTime >= sound.MinimumInterval;
        }

        private void StartTrack(AudioClip track)
        {
            musicSource.clip = track;
            musicSource.volume = 0f;
            musicSource.Play();
        }

        private Tween FadeMusicTo(float target, float duration)
        {
            // Unscaled, so music keeps moving through a hit stop rather than freezing with it.
            return DOVirtual
                .Float(musicSource.volume, target, duration, value => musicSource.volume = value)
                .SetUpdate(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _musicFade?.Kill();
            _musicFade = null;
        }
    }
}
