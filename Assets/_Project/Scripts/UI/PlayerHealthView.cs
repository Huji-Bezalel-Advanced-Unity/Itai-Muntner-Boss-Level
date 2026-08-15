using System.Collections.Generic;
using BossLevel.Combat;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// Shows the player's remaining health as a row of discrete hearts.
    /// </summary>
    /// <remarks>
    /// Discrete rather than a bar, because the player needs to know exactly how many more hits
    /// they can take — "about a third left" is not a number anyone can plan around mid-dodge.
    /// <para>
    /// One heart is one hit point, so the player's maximum health should equal the number of
    /// hearts listed here, and the boss's projectile damage should be 1.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerHealthView : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;

        [Tooltip("One per hit point, in order. Count should match the player's maximum health.")]
        [SerializeField] private List<Image> hearts = new List<Image>();

        [SerializeField] private Color emptyColour = new Color(1f, 1f, 1f, 0.15f);

        [SerializeField, Min(0f)] private float punchStrength = 0.4f;
        [SerializeField, Min(0f)] private float punchDuration = 0.3f;

        private readonly List<Color> _fullColours = new List<Color>();

        private int _heartsShown;

        private void Awake()
        {
            if (playerHealth == null || hearts.Count == 0)
            {
                Debug.LogError($"{nameof(PlayerHealthView)} is missing a reference.", this);
                enabled = false;
                return;
            }

            // Each heart keeps its own authored colour, so a designer can tint them individually
            // without this component flattening them all to one value on the first hit.
            foreach (var heart in hearts)
            {
                _fullColours.Add(heart != null ? heart.color : Color.white);
            }
        }

        private void OnEnable()
        {
            playerHealth.Changed += OnHealthChanged;

            if (playerHealth.Max != hearts.Count)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerHealthView)} has {hearts.Count} hearts but the player has " +
                    $"{playerHealth.Max} health, so the display will not match.", this);
            }

            Refresh(playerHealth.Current);
        }

        private void OnDisable()
        {
            playerHealth.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            var lostAHeart = current < _heartsShown;

            Refresh(current);

            if (!lostAHeart)
            {
                return;
            }

            // Punch the heart that just emptied rather than the whole row, so the player's eye
            // goes to what changed.
            var index = Mathf.Clamp(current, 0, hearts.Count - 1);
            var emptied = hearts[index];

            if (emptied != null)
            {
                emptied.transform.DOPunchScale(Vector3.one * punchStrength, punchDuration);
            }
        }

        private void Refresh(int current)
        {
            for (var i = 0; i < hearts.Count; i++)
            {
                if (hearts[i] == null)
                {
                    continue;
                }

                hearts[i].color = i < current ? _fullColours[i] : emptyColour;
            }

            _heartsShown = current;
        }
    }
}
