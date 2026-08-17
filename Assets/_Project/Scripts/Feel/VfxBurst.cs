using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// A one-shot particle burst that configures itself, plays once, and returns to its pool.
    /// </summary>
    /// <remarks>
    /// It sets up the entire particle system in <see cref="Awake"/> rather than relying on how
    /// the prefab was authored. That is unusual, and deliberate: a Particle System has dozens of
    /// interacting modules whose defaults suit a 3D scene — the default Cone shape fires along
    /// +Z, into the screen — so an unconfigured one in a 2D game produces something that looks
    /// broken rather than something that looks plain. Owning the settings here means the burst
    /// is described in one readable place, and a prefab cannot be silently wrong.
    /// <para>
    /// Uses the built-in Particle System rather than VFX Graph, and that is not a preference:
    /// VFX Graph needs compute shader support, which WebGL does not have. Effects built in it
    /// would silently do nothing in the submitted build.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(ParticleSystem))]
    [DisallowMultipleComponent]
    public class VfxBurst : MonoBehaviour, IPoolable
    {
        /// <summary>Grace after the last particle should have died, before returning to the pool.</summary>
        private const float DespawnMargin = 0.15f;

        [Header("Shape")]
        [SerializeField, Min(1)] private int particleCount = 14;

        [Tooltip("Particles are emitted from a disc this wide, so a hit reads as coming from a " +
                 "body rather than from a single point in its centre.")]
        [SerializeField, Min(0f)] private float emissionRadius = 0.35f;

        [Header("Motion")]
        [SerializeField, Min(0.02f)] private float lifetime = 0.35f;
        [SerializeField, Min(0f)] private float speed = 5f;
        [SerializeField, Min(0.005f)] private float size = 0.16f;

        [Tooltip("Slight downward pull, so debris falls away instead of hanging in the air.")]
        [SerializeField] private float gravity = 0.7f;

        [Header("Rendering")]
        [Tooltip("Required. The built-in particle material renders magenta under URP.")]
        [SerializeField] private Material particleMaterial;

        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("High, so bursts draw in front of the sprites they came from.")]
        [SerializeField] private int sortingOrder = 100;

        private ParticleSystem _particles;
        private VfxPool _owner;
        private float _despawnAt;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();

            Configure();

            // Whatever the prefab was doing on its own, stop it.
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Places the burst and plays it.</summary>
        public void Play(VfxPool owner, Vector2 position, Color colour)
        {
            _owner = owner;
            transform.position = position;

            var main = _particles.main;
            main.startColor = colour;

            // Cleared first, because a pooled instance may still be showing the tail of its
            // previous burst somewhere else entirely.
            _particles.Clear(true);
            _particles.Play(true);

            // A timer rather than IsAlive: immediately after Play the system has not simulated a
            // step yet, so IsAlive can read false for one frame and return the burst before a
            // single particle exists. Scaled time, so this pauses with the particles during a
            // hit stop instead of expiring behind them.
            _despawnAt = Time.time + lifetime + DespawnMargin;
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            _owner = null;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (_owner == null || Time.time < _despawnAt)
            {
                return;
            }

            var pool = _owner;
            _owner = null;
            pool.Despawn(this);
        }

        private void Configure()
        {
            var main = _particles.main;
            main.duration = lifetime;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.maxParticles = particleCount * 2;
            main.stopAction = ParticleSystemStopAction.None;

            // World space, so particles stay where they were thrown rather than being dragged
            // along when this pooled object is repositioned for the next burst.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _particles.emission;
            emission.enabled = true;

            // No continuous emission — one burst and done. A non-zero rate is what makes a
            // hit effect spray forever from wherever it happened.
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            var shape = _particles.shape;
            shape.enabled = true;

            // Circle rather than the default Cone. A cone fires along +Z, which in a 2D scene
            // is straight into the camera; a circle throws particles outwards across the screen.
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(emissionRadius, 0.01f);
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.rotation = Vector3.zero;

            var sizeOverLifetime = _particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var colourOverLifetime = _particles.colorOverLifetime;
            colourOverLifetime.enabled = true;
            colourOverLifetime.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

            ConfigureRenderer();
        }

        private void ConfigureRenderer()
        {
            var particleRenderer = GetComponent<ParticleSystemRenderer>();

            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingLayerName = sortingLayerName;
            particleRenderer.sortingOrder = sortingOrder;

            if (particleMaterial != null)
            {
                particleRenderer.sharedMaterial = particleMaterial;
                return;
            }

            Debug.LogWarning(
                $"{nameof(VfxBurst)} has no particle material assigned. Unity's built-in one " +
                "renders magenta under URP.", this);
        }

        /// <summary>
        /// White fading to transparent. White because this multiplies with the start colour, so
        /// the tint passed to <see cref="Play"/> survives the fade.
        /// </summary>
        private static Gradient FadeOutGradient()
        {
            var gradient = new Gradient();

            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });

            return gradient;
        }
    }
}
