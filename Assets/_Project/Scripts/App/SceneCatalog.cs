using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossLevel.App
{
    /// <summary>
    /// Maps each <see cref="SceneId"/> onto the scene file that implements it.
    /// </summary>
    /// <remarks>
    /// Adding a scene means adding an enum entry and a row here — no loading code changes. It
    /// also means renaming a scene file is a one-line edit in an asset rather than a hunt for
    /// string literals.
    /// </remarks>
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "Boss Level/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public SceneId Id;

            [Tooltip("Must match the scene's file name, and the scene must be in Build Settings.")]
            public string SceneName;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        /// <summary>The scene name for an id, or null with an error logged if it is not listed.</summary>
        public string NameFor(SceneId id)
        {
            foreach (var entry in entries)
            {
                if (entry.Id == id)
                {
                    return entry.SceneName;
                }
            }

            Debug.LogError($"{name} has no scene listed for {id}.", this);
            return null;
        }
    }
}
