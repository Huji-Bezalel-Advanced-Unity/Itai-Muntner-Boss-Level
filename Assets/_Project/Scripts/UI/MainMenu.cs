using BossLevel.App;
using UnityEngine;
using UnityEngine.UI;

namespace BossLevel.UI
{
    /// <summary>
    /// The title screen. Its only job is to start the fight.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button playButton;

        [Tooltip("Optional. Hidden automatically in a web build, where it cannot work.")]
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError($"{nameof(MainMenu)} has no play button assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            playButton.onClick.AddListener(Play);

            if (quitButton == null)
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // A page cannot close the browser tab containing it, so the button would silently do
            // nothing in the submitted build. Absent is better than broken.
            quitButton.gameObject.SetActive(false);
#else
            quitButton.onClick.AddListener(Quit);
#endif
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(Play);

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(Quit);
            }
        }

        private void Play()
        {
            SceneLoader.Instance.Load(SceneId.BossLevel);
        }

        private void Quit()
        {
            Application.Quit();
        }
    }
}
