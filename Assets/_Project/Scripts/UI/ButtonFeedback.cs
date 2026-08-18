using BossLevel.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// Makes a button answer back: it grows under the cursor and kicks when pressed.
    /// </summary>
    /// <remarks>
    /// A button's built-in colour tint tells the player it is interactive; movement and sound
    /// tell them it worked. That second half matters most on a web build, where a click can be
    /// swallowed by a page that has not taken focus yet and the player is left wondering whether
    /// they missed.
    /// <para>
    /// Everything runs on unscaled time, because menus can appear while the game is paused or
    /// frozen and a button that will not animate looks broken.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("What moves. Defaults to this object's own transform.")]
        [SerializeField] private Transform target;

        [Header("Hover")]
        [SerializeField, Min(1f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

        [Header("Press")]
        [SerializeField, Min(0f)] private float pressPunch = 0.14f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.22f;

        [Header("Sound (optional)")]
        [SerializeField] private SoundEvent hoverSound;
        [SerializeField] private SoundEvent clickSound;

        private Button _button;
        private Vector3 _baseScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (target == null)
            {
                target = transform;
            }

            _baseScale = target.localScale;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);

            _scaleTween?.Kill();
            _scaleTween = null;

            // Leaving while hovered would strand the button enlarged.
            target.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable)
            {
                return;
            }

            ScaleTo(_baseScale * hoverScale, hoverDuration);

            if (hoverSound != null)
            {
                hoverSound.Play();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(_baseScale, hoverDuration);
        }

        private void OnClicked()
        {
            _scaleTween?.Kill();

            // Punch from the resting size rather than the hovered one, so the kick is the same
            // whether the button was clicked with a mouse or activated from the keyboard.
            target.localScale = _baseScale;

            _scaleTween = target
                .DOPunchScale(Vector3.one * pressPunch, pressDuration)
                .SetUpdate(true);

            if (clickSound != null)
            {
                clickSound.Play();
            }
        }

        private void ScaleTo(Vector3 scale, float duration)
        {
            _scaleTween?.Kill();
            _scaleTween = target.DOScale(scale, duration).SetUpdate(true);
        }
    }
}
