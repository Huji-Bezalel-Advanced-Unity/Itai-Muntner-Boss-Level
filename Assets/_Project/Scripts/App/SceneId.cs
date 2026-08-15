namespace BossLevel.App
{
    /// <summary>
    /// The scenes the game can load, named rather than typed as strings.
    /// </summary>
    /// <remarks>
    /// A string API (<c>Load("BossLevel")</c>) would need no code change to add a scene, but it
    /// moves a typo from a compile error to a runtime one and gives the reader nothing to
    /// autocomplete against. The enum keeps call sites checked while
    /// <see cref="SceneCatalog"/> keeps the actual scene <i>names</i> as data.
    /// </remarks>
    public enum SceneId
    {
        Bootstrap,
        MainMenu,
        BossLevel,
    }
}
