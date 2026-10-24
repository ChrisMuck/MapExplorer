using Game.App;

public sealed class LocationTextKeyTests
{
    public void RunAll()
    {
        ContentProfilesNameTheirWordsByTextId();
        LoaderResolvesTextIdsAndRejectsMissingCatalogs();
        TutorialInstancesTakeTheirNamesFromTexts();
    }

    private static string DataRoot() => Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");

    private static GameDataCatalog Catalog() => GameDataCatalog.LoadFromDirectory(DataRoot()) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void ContentProfilesNameTheirWordsByTextId()
    {
        var raw = File.ReadAllText(Path.Combine(DataRoot(), "Locations", "ContentProfiles", "content-profiles.json"));
        foreach (var literal in new[] { "\"title\"", "\"subtitle\"", "\"shortDescription\"", "\"description\"", "\"discovered\"", "\"resolved\"" })
        {
            AssertTrue(!raw.Contains(literal), "Content profiles carry no literal text field " + literal);
        }

        var catalog = Catalog();
        foreach (var scenario in catalog.Authoring.ScenarioProfiles.Values)
        {
            var profile = catalog.Locations.Definitions.FindContentProfile(scenario.ContentProfileId);
            AssertTrue(profile != null && profile.Title.Length > 0 && profile.Description != null && profile.JournalDiscovered != null, "Content profile resolves its texts: " + scenario.ContentProfileId);
        }

        AssertTrue(catalog.Scenes.Texts.Resolve("location.old-lighthouse.title") == "Alter Leuchtturm", "A location title is an ordinary localized text");
    }

    private static void LoaderResolvesTextIdsAndRejectsMissingCatalogs()
    {
        const string document = """
            { "documentType": "location-content-profiles", "schemaVersion": 1, "items": [
              { "id": "content-test", "titleTextId": "test.title", "subtitle": "Literal subtitle", "imageId": "placeholder-camp" } ] }
            """;
        var bundle = LocationDataLoader.LoadFromJson(new[] { document }, id => id == "test.title" ? "Titel aus Text" : throw new LocationDataException("unknown text " + id));
        var profile = bundle.Definitions.FindContentProfile("content-test");
        AssertTrue(profile != null && profile.Title == "Titel aus Text" && profile.Subtitle == "Literal subtitle", "A text id is resolved and a literal text still works");

        try
        {
            LocationDataLoader.LoadFromJson(new[] { document });
            throw new InvalidOperationException("A text id without a text catalog was accepted.");
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains("no text catalog"), "Missing text catalog is reported: " + exception.Message);
        }

        try
        {
            LocationDataLoader.LoadFromJson(new[] { document.Replace("test.title", "test.missing") }, id => throw new LocationDataException("Scene text '" + id + "' does not exist."));
            throw new InvalidOperationException("An unknown text id was accepted.");
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains("test.missing"), "Unknown text id is reported: " + exception.Message);
        }
    }

    private static void TutorialInstancesTakeTheirNamesFromTexts()
    {
        var catalog = Catalog();
        var names = catalog.Locations.Instances.ToDictionary(instance => instance.Id, instance => instance.Name);
        AssertTrue(names["broken-ravine"] == "Zerstörte Brücke" && names["coastal-landing"] == "Uferzeichen", "Tutorial instance names come from the localized texts");
    }
}
