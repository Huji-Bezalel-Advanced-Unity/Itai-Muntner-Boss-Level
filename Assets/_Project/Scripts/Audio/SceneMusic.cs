using UnityEngine;

namespace BossLevel.Audio
{
    /// <summary>
    /// Asks for this scene's music when the scene starts.
    /// </summary>
    /// <remarks>
    /// Each scene declaring its own track keeps the decision next to the thing it describes,
    /// rather than in a table somewhere that has to be kept in step with the scene list.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SceneMusic : MonoBehaviour
    {
        [Tooltip("Leave empty to fade the music out for this scene.")]
        [SerializeField] private AudioClip track;

        [Tooltip("Start the track over even if it is already playing. On for the fight, so a " +
                 "retry opens from the top rather than halfway through the previous attempt; " +
                 "off if two scenes should share one continuous track.")]
        [SerializeField] private bool restartFromTheBeginning = true;

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

            AudioService.Instance.PlayMusic(track, restartFromTheBeginning);
        }
    }
}
