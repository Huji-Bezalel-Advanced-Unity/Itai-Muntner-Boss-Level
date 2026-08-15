using BossLevel.Boss;
using BossLevel.Boss.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BossLevel.UI
{
    /// <summary>
    /// Announces each new phase with a banner that slides in, holds, and slides out.
    /// </summary>
    /// <remarks>
    /// The boss's own phase-change tell says <i>something changed</i>; this says <i>what</i>.
    /// Between them the player gets both an immediate signal and a nameable one, which is what
    /// turns a difficulty increase into a moment rather than a confusion.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PhaseBanner : MonoBehaviour
    {
        [SerializeField] private BossController boss;

        [Tooltip("The banner itself. Slides horizontally; its resting position is where it " +
                 "should sit while visible.")]
        [SerializeField] private RectTransform panel;

        [SerializeField] private TMP_Text label;

        [Tooltip("How far off-screen the banner waits. Must clear the screen edge.")]
        [SerializeField] private float offscreenDistance = 1400f;

        [SerializeField, Min(0f)] private float slideDuration = 0.35f;
        [SerializeField, Min(0f)] private float holdDuration = 1.1f;

        private Vector2 _restingPosition;
        private Sequence _sequence;

        private void Awake()
        {
            if (boss == null || panel == null)
            {
                Debug.LogError($"{nameof(PhaseBanner)} is missing a reference.", this);
                enabled = false;
                return;
            }

            _restingPosition = panel.anchoredPosition;
            MoveOffscreen();
        }

        private void OnEnable()
        {
            boss.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            boss.PhaseChanged -= OnPhaseChanged;

            _sequence?.Kill();
            _sequence = null;

            MoveOffscreen();
        }

        private void OnPhaseChanged(BossPhase phase, int index)
        {
            if (label != null)
            {
                label.text = phase.DisplayName;
            }

            _sequence?.Kill();

            _sequence = DOTween.Sequence()
                .Append(panel.DOAnchorPosX(_restingPosition.x, slideDuration).SetEase(Ease.OutCubic))
                .AppendInterval(holdDuration)
                .Append(panel.DOAnchorPosX(OffscreenX, slideDuration).SetEase(Ease.InCubic));
        }

        private float OffscreenX => _restingPosition.x + offscreenDistance;

        private void MoveOffscreen()
        {
            panel.anchoredPosition = new Vector2(OffscreenX, _restingPosition.y);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
}
