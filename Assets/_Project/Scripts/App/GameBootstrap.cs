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
        [SerializeField] private SceneId firstScene = SceneId.MainMenu;

        private void Start()
        {
            // Start rather than Awake: the services in this scene need their own Awake to have
            // run before anything asks them to do work.
            SceneLoader.Instance.Load(firstScene);
        }
    }
}
