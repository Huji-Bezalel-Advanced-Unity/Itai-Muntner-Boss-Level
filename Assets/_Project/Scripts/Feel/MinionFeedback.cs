using BossLevel.Audio;
using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Puffs and sounds for minions arriving and dying.
    /// </summary>
    /// <remarks>
    /// Listens to the pool rather than living on the minion prefab, for a practical reason: a
    /// prefab cannot hold a reference to a scene object, so a minion could never point at the
    /// effect pools itself. Bracketing spawn and death at the pool solves that and keeps the
    /// minion concerned only with hunting.
    /// <para>
    /// Different colours for the two ends, because arriving and dying should not look alike —
    /// the player needs to know at a glance whether the arena is filling or clearing.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class MinionFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MinionPool pool;
        [SerializeField] private VfxPool burstVfx;

        [Header("Appearance")]
        [SerializeField] private Color spawnColour = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color deathColour = new Color(1f, 0.45f, 0.45f);

        [Header("Sound (optional)")]
        [SerializeField] private SoundEvent spawnSound;
        [SerializeField] private SoundEvent deathSound;

        private void Awake()
        {
            if (pool == null)
            {
                Debug.LogError($"{nameof(MinionFeedback)} has no {nameof(MinionPool)} assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            pool.Spawned += OnSpawned;
            pool.Despawned += OnDespawned;
        }

        private void OnDisable()
        {
            pool.Spawned -= OnSpawned;
            pool.Despawned -= OnDespawned;
        }

        private void OnSpawned(Vector2 position)
        {
            Announce(position, spawnColour, spawnSound);
        }

        private void OnDespawned(Vector2 position)
        {
            Announce(position, deathColour, deathSound);
        }

        private void Announce(Vector2 position, Color colour, SoundEvent sound)
        {
            if (burstVfx != null)
            {
                burstVfx.Play(position, colour);
            }

            if (sound != null)
            {
                sound.Play(position);
            }
        }
    }
}
