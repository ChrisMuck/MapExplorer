using Game.App;
using Game.Core;

/// <summary>Pins what the data compositions of the generator sites show for each state (V3a investigation and trace sites, V3b landmarks and resource places).</summary>
public sealed class SiteVisualTests
{
    private sealed record Case(string Archetype, string[] Interactions, string Variant, string BaseOperational, string[] Operational, int[] OperationalCounts, string BasePresence, string[] Presence, int[] PresenceCounts, int[] InteractionCounts, int Unknown);

    private static readonly Case[] Cases =
    {
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "old-ruin", "crumbling", new[] { "crumbling", "shored-up", "collapsed" }, new[] { 6, 8, 8 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 6, 7, 8 }, new[] { 7, 9, 10 }, 6),
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "stone-circle", "dormant", new[] { "dormant", "awakened", "broken" }, new[] { 8, 10, 8 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 8, 9, 10 }, new[] { 9, 11, 12 }, 8),
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "ruined-shrine", "decayed", new[] { "decayed", "tended", "desecrated" }, new[] { 5, 9, 6 }, "empty", new[] { "empty", "watched", "abandoned" }, new[] { 5, 6, 7 }, new[] { 6, 8, 9 }, 5),
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "cold-fireplace", "cold", new[] { "cold", "smoldering", "cleared" }, new[] { 9, 12, 7 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 9, 11, 13 }, new[] { 10, 12, 13 }, 6),
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "broken-cart", "wrecked", new[] { "wrecked", "looted", "salvaged" }, new[] { 7, 5, 4 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 7, 9, 11 }, new[] { 8, 10, 11 }, 3),
        new("investigation-site", new[] { "inspected", "searched", "documented" }, "old-boat-camp", "derelict", new[] { "derelict", "repaired", "in-use" }, new[] { 2, 4, 10 }, "abandoned", new[] { "abandoned", "recently-used", "watched" }, new[] { 2, 4, 6 }, new[] { 3, 5, 6 }, 1),

        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "signal-tower", "dark", new[] { "dark", "lit", "ruined" }, new[] { 3, 6, 6 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 3, 4, 5 }, new[] { 4, 5, 6 }, 3),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "colossus-statue", "standing", new[] { "standing", "cracked", "toppled" }, new[] { 6, 8, 8 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 6, 7, 8 }, new[] { 7, 8, 9 }, 6),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "old-lighthouse", "dark", new[] { "dark", "lit", "ruined" }, new[] { 4, 5, 6 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 4, 5, 6 }, new[] { 5, 6, 7 }, 3),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "wind-carved-peak", "stable", new[] { "stable", "accessible", "mapped" }, new[] { 2, 6, 8 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 2, 3, 4 }, new[] { 3, 4, 5 }, 2),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "freshwater-spring", "clear", new[] { "clear", "muddied", "dried-up" }, new[] { 8, 8, 8 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 8, 9, 10 }, new[] { 9, 10, 11 }, 8),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "fishing-grounds", "rich", new[] { "rich", "thin", "fished-out" }, new[] { 10, 8, 6 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 10, 11, 12 }, new[] { 11, 12, 13 }, 5),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "healing-herbs", "growing", new[] { "growing", "picked", "withered" }, new[] { 8, 8, 8 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 8, 9, 10 }, new[] { 9, 10, 11 }, 8),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "timber-stand", "standing", new[] { "standing", "partly-felled", "cleared" }, new[] { 5, 6, 6 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 5, 6, 7 }, new[] { 6, 7, 8 }, 5),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "salt-deposit", "exposed", new[] { "exposed", "mined", "exhausted" }, new[] { 5, 6, 2 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 5, 6, 7 }, new[] { 6, 7, 8 }, 5),
        new("natural-phenomenon", new[] { "observed", "surveyed", "documented" }, "berry-grove", "ripe", new[] { "ripe", "picked-over", "blighted" }, new[] { 12, 5, 8 }, "unknown", new[] { "unknown", "watched", "abandoned" }, new[] { 12, 13, 14 }, new[] { 13, 14, 15 }, 12),
    };

    public void RunAll()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var catalog = GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
        var resolver = new LocationVisualResolver(catalog.VisualDefinitions);
        foreach (var testCase in Cases)
        {
            var composition = catalog.VisualDefinitions.FindComposition(testCase.Archetype, testCase.Variant);
            AssertTrue(composition != null && composition.Id == "vis-" + testCase.Variant && composition.AnchorMode == "point", "Composition exists for " + testCase.Variant);
            var location = new SpecialLocationState("test-" + testCase.Variant, LocationKind.Ruin, new HexCoord(1, 1), testCase.Variant, LocationAnchor.Point(new HexCoord(1, 1)), testCase.Archetype, testCase.Variant);

            int Count(string? operational, string? presence, string? interaction) =>
                resolver.Resolve(location, new LocationVisualContext(VisualKnowledge.Confirmed, operational, presence, interaction)).ShownElements.Count();

            for (var i = 0; i < testCase.Operational.Length; i++)
            {
                AssertEqual(testCase.OperationalCounts[i], Count(testCase.Operational[i], testCase.BasePresence, "untouched"), $"{testCase.Variant}: operational '{testCase.Operational[i]}'");
                AssertEqual(testCase.PresenceCounts[i], Count(testCase.BaseOperational, testCase.Presence[i], "untouched"), $"{testCase.Variant}: presence '{testCase.Presence[i]}'");
            }

            var interactions = testCase.Interactions;
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
