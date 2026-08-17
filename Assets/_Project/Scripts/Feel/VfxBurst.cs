using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// A one-shot particle burst that returns itself to its pool once it has finished playing.
    /// </summary>
    /// <remarks>
    /// Uses the built-in Particle System rather than VFX Graph, and that is not a preference:
    /// VFX Graph needs compute shader support, which WebGL does not have. Effects built in it
    /// would silently do nothing in the submitted build.
    /// </remarks>
    [RequireComponent(typeof(ParticleSystem))]
    [DisallowMultipleComponent]
    public class VfxBurst : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particles;
        private VfxPool _owner;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
        }

        /// <summary>Places the burst and plays it.</summary>
        public void Play(VfxPool owner, Vector2 position, Color colour)
        {
            _owner = owner;
            transform.position = position;

            var main = _particles.main;
            main.startColor = colour;

            // Stop-and-clear first, because a pooled instance may still be showing the tail of
            // its previous burst somewhere else entirely.
            _particles.Clear(true);
            _particles.Play(true);
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            // IsAlive is the only honest signal that a burst has finished: its duration does not
            // account for particle lifetime, so returning on duration alone cuts the tail off.
            if (_owner == null || _particles.IsAlive(true))
            {
                return;
            }

            var pool = _owner;
            _owner = null;
            pool.Despawn(this);
        }
    }
}
