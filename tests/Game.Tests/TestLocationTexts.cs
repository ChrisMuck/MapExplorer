using Game.App;

/// <summary>Location content names its words by text id; tests that read the location folder alone resolve them through the game-data catalog.</summary>
public static class TestLocationTexts
{
    public static Func<string, string> For(string locationsRoot)
    {
        var root = Directory.GetParent(locationsRoot)!.FullName;
        var catalog = GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
        return textId => catalog.Scenes.Texts.Resolve(textId);
    }
}
