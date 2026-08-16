using System.Collections;
using UnityEngine;

namespace BossLevel.App
{
    /// <summary>
    /// The game's entry point. Lives in the Bootstrap scene, which contains nothing else.
    /// </summary>
    /// <remarks>
    /// Splitting the entry point from the menu gives the persistent services exactly one
    /// creation site and one lifetime. The alternative — creating them lazily wherever they are
    /// first touched — makes their startup order depend on which screen the player happened to
    /// open, which is the kind of bug that only appears in a build.
    /// </remarks>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Frames to let pass before the first load is requested.
        /// </summary>
        /// <remarks>
        /// The first frame after entering play mode carries the whole of start-up in its delta —
        /// often more than a second. Requesting a load during it means the loading screen's
        /// minimum display time is already spent before it has drawn once, so the bar appears
        /// full and the transition cuts straight through. Letting a couple of honest frames pass
        /// first costs nothing and makes the very first transition behave like every other one.
        /// </remarks>
        private const int SettleFrames = 2;

        [SerializeField] private SceneId firstScene = SceneId.MainMenu;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < SettleFrames; frame++)
            {
                yield return null;
            }

            SceneLoader.Instance.Load(firstScene);
        }
    }
}
