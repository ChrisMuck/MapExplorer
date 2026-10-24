using Game.App;
using Game.Core;

/// <summary>Pins what the data compositions of the generator sites show for each state (V3a: investigation and trace sites).</summary>
public sealed class SiteVisualTests
{
    private sealed record Case(string Variant, string BaseOperational, string[] Operational, int[] OperationalCounts, string BasePresence, string[] Presence, int[] PresenceCounts, int[] InteractionCounts, int Unknown);

    private static readonly Case[] Cases =
    {
        new("old-ruin", "crumbling", new[] { "crumbling", "shored-up", "collapsed" }, new[] { 6, 8, 8 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 6, 7, 8 }, new[] { 7, 9, 10 }, 6),
        new("stone-circle", "dormant", new[] { "dormant", "awakened", "broken" }, new[] { 8, 10, 8 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 8, 9, 10 }, new[] { 9, 11, 12 }, 8),
        new("ruined-shrine", "decayed", new[] { "decayed", "tended", "desecrated" }, new[] { 5, 9, 6 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 5, 6, 7 }, new[] { 6, 8, 9 }, 5),
        new("cold-fireplace", "cold", new[] { "cold", "smoldering", "cleared" }, new[] { 9, 12, 7 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 9, 11, 13 }, new[] { 10, 12, 13 }, 6),
        new("broken-cart", "wrecked", new[] { "wrecked", "looted", "salvaged" }, new[] { 7, 5, 4 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 7, 9, 11 }, new[] { 8, 10, 11 }, 3),
        new("old-boat-camp", "derelict", new[] { "derelict", "repaired", "in-use" }, new[] { 2, 4, 10 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 2, 4, 6 }, new[] { 3, 5, 6 }, 1),
    };

    public void RunAll()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var catalog = GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
        var resolver = new LocationVisualResolver(catalog.VisualDefinitions);
        foreach (var testCase in Cases)
        {
            var composition = catalog.VisualDefinitions.FindComposition("investigation-site", testCase.Variant);
            AssertTrue(composition != null && composition.Id == "vis-" + testCase.Variant && composition.AnchorMode == "point", "Composition exists for " + testCase.Variant);
            var location = new SpecialLocationState("test-" + testCase.Variant, LocationKind.Ruin, new HexCoord(1, 1), testCase.Variant, LocationAnchor.Point(new HexCoord(1, 1)), "investigation-site", testCase.Variant);

            int Count(string? operational, string? presence, string? interaction) =>
                resolver.Resolve(location, new LocationVisualContext(VisualKnowledge.Confirmed, operational, presence, interaction)).ShownElements.Count();

            for (var i = 0; i < testCase.Operational.Length; i++)
            {
                AssertEqual(testCase.OperationalCounts[i], Count(testCase.Operational[i], testCase.BasePresence, "untouched"), $"{testCase.Variant}: operational '{testCase.Operational[i]}'");
                AssertEqual(testCase.PresenceCounts[i], Count(testCase.BaseOperational, testCase.Presence[i], "untouched"), $"{testCase.Variant}: presence '{testCase.Presence[i]}'");
            }

            var interactions = new[] { "inspected", "searched", "documented" };
            for (var i = 0; i < interactions.Length; i++)
            {
                AssertEqual(testCase.InteractionCounts[i], Count(testCase.BaseOperational, testCase.BasePresence, interactions[i]), $"{testCase.Variant}: interaction '{interactions[i]}'");
            }

            AssertEqual(testCase.Unknown, Count(null, null, null), $"{testCase.Variant}: an unobserved site shows only its neutral parts");
        }
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
