using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Sparks and a jolt when a vent opens and when it erupts.
    /// </summary>
    /// <remarks>
    /// Listens to the pool rather than living on the vent prefab, because a prefab cannot hold a
    /// reference to a scene object and the effect pools are scene objects. Bracketing at the
    /// pool solves that and keeps the vent concerned only with burning things.
    /// <para>
    /// The shake on eruption is what sells the column as force rather than as a shape appearing.
    /// It is deliberately smaller than the phase-change shake — a vent is a threat, not a change
    /// of rules.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class VolcanoFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VolcanoPool pool;
        [SerializeField] private VfxPool burstVfx;
        [SerializeField] private CameraShake cameraShake;

        [Header("Opening")]
        [SerializeField] private Color emberColour = new Color(1f, 0.55f, 0.15f);

        [Header("Erupting")]
        [SerializeField] private Color eruptionColour = new Color(1f, 0.85f, 0.35f);
        [SerializeField, Min(0f)] private float eruptionShakeStrength = 0.16f;
        [SerializeField, Min(0f)] private float eruptionShakeDuration = 0.25f;

        private void Awake()
        {
            if (pool == null)
            {
                Debug.LogError($"{nameof(VolcanoFeedback)} has no {nameof(VolcanoPool)} assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            pool.Opened += OnOpened;
            pool.Erupted += OnErupted;
        }

        private void OnDisable()
        {
            pool.Opened -= OnOpened;
            pool.Erupted -= OnErupted;
        }

        private void OnOpened(Vector2 position)
        {
            if (burstVfx != null)
            {
                burstVfx.Play(position, emberColour);
            }
        }

        private void OnErupted(Vector2 position)
        {
            if (burstVfx != null)
            {
                burstVfx.Play(position, eruptionColour);
            }

            if (cameraShake != null)
            {
                cameraShake.Play(eruptionShakeStrength, eruptionShakeDuration);
            }
        }
    }
}
