using UnityEngine;

namespace BossLevel.Audio
{
    /// <summary>
    /// Asks for this scene's music when the scene starts.
    /// </summary>
    /// <remarks>
    /// Each scene declaring its own track keeps the decision next to the thing it describes,
    /// rather than in a table somewhere that has to be kept in step with the scene list. If two
    /// scenes name the same clip, <see cref="AudioService"/> leaves it playing rather than
    /// restarting it.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SceneMusic : MonoBehaviour
    {
        [Tooltip("Leave empty to fade the music out for this scene.")]
        [SerializeField] private AudioClip track;

        private void Start()
        {
            // Expected to be absent when a scene is played directly rather than through
            // Bootstrap, which is a normal way to work on it.
            if (!AudioService.Exists)
            {
                return;
            }

            if (track == null)
            {
                AudioService.Instance.StopMusic();
                return;
            }

            AudioService.Instance.PlayMusic(track);
        }
    }
}
