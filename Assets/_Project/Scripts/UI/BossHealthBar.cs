using BossLevel.Combat;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// Shows the boss's remaining health as a bar, with a second bar trailing behind it.
    /// </summary>
    /// <remarks>
    /// The trailing "chip" bar is not decoration. A single bar that snaps to its new value makes
    /// a small hit nearly invisible and a large one ambiguous — the player sees where the health
    /// is but not what just happened to it. A second bar that catches up a moment later shows
    /// exactly how much the last hit took, which is the part they actually want to know.
    /// <para>
    /// Read-only observer: it subscribes to <see cref="Health"/> and holds nothing the fight
    /// depends on, so the fight runs perfectly well in a scene with no canvas at all.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private Health bossHealth;

        [Tooltip("Filled Image that tracks health immediately.")]
        [SerializeField] private Image fill;

        [Tooltip("Optional. Filled Image drawn behind the first, which catches up a moment later.")]
        [SerializeField] private Image chip;

        [SerializeField, Min(0f)] private float fillDuration = 0.12f;
        [SerializeField, Min(0f)] private float chipDelay = 0.35f;
        [SerializeField, Min(0f)] private float chipDuration = 0.45f;

        private Tween _fillTween;
        private Tween _chipTween;

        private void Awake()
        {
            if (bossHealth == null || fill == null)
            {
                Debug.LogError($"{nameof(BossHealthBar)} is missing a reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            bossHealth.Changed += OnHealthChanged;

            // The boss sets its health from its definition during Awake, which happens before
            // this subscription exists — so the starting value is read rather than waited for.
            SetImmediately(bossHealth.Fraction);
        }

        private void OnDisable()
        {
            bossHealth.Changed -= OnHealthChanged;

            _fillTween?.Kill();
            _chipTween?.Kill();
        }

        private void OnHealthChanged(int current, int max)
        {
            var fraction = max > 0 ? (float)current / max : 0f;

            _fillTween?.Kill();
            _fillTween = fill.DOFillAmount(fraction, fillDuration);

            if (chip == null)
            {
                return;
            }

            _chipTween?.Kill();
            _chipTween = chip.DOFillAmount(fraction, chipDuration).SetDelay(chipDelay);
        }

        private void SetImmediately(float fraction)
        {
            fill.fillAmount = fraction;

            if (chip != null)
            {
                chip.fillAmount = fraction;
            }
        }
    }
}
