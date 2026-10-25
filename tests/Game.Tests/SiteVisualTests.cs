using Game.App;
using Game.Core;

/// <summary>Pins what the data compositions of the generator sites show for each state (V3a investigation and trace sites, V3b landmarks and resource places, V3c containment sites and territorial markers, V3d hazard zones, S3 settlements).</summary>
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

        new("containment-site", new[] { "inspected", "investigated", "secured", "documented" }, "sealed-cave", "sealed", new[] { "sealed", "opened", "collapsed" }, new[] { 5, 6, 7 }, "unknown", new[] { "unknown", "watched", "guarded", "abandoned" }, new[] { 5, 6, 7, 7 }, new[] { 6, 8, 7, 7 }, 4),
        new("containment-site", new[] { "inspected", "investigated", "secured", "documented" }, "warded-threshold", "warded", new[] { "warded", "weakened", "broken" }, new[] { 8, 6, 6 }, "unknown", new[] { "unknown", "watched", "guarded", "abandoned" }, new[] { 8, 9, 10, 10 }, new[] { 9, 11, 10, 10 }, 4),
        new("territorial-marker", new[] { "observed", "interpreted", "documented" }, "border-stones", "standing", new[] { "standing", "respected", "violated" }, new[] { 7, 10, 8 }, "unknown", new[] { "unknown", "watched", "guarded" }, new[] { 7, 8, 9 }, new[] { 8, 9, 10 }, 7),
        new("territorial-marker", new[] { "observed", "interpreted", "documented" }, "skull-marks", "standing", new[] { "standing", "respected", "violated" }, new[] { 4, 7, 5 }, "unknown", new[] { "unknown", "watched", "guarded" }, new[] { 4, 5, 6 }, new[] { 5, 6, 7 }, 4),
        new("territorial-marker", new[] { "observed", "interpreted", "documented" }, "wind-ribbons", "standing", new[] { "standing", "respected", "violated" }, new[] { 12, 15, 13 }, "unknown", new[] { "unknown", "watched", "guarded" }, new[] { 12, 13, 14 }, new[] { 13, 14, 15 }, 12),

        new("hazard-site", new[] { "assessed", "contained", "documented" }, "restless-marsh", "active", new[] { "active", "avoided", "traversed", "restricted", "contained" }, new[] { 11, 13, 13, 13, 13 }, "unknown", new[] { "unknown", "spreading", "watched" }, new[] { 11, 13, 12 }, new[] { 12, 14, 13 }, 10),
        new("hazard-site", new[] { "assessed", "contained", "documented" }, "volcanic-vent", "dormant", new[] { "dormant", "smoldering", "erupting", "avoided" }, new[] { 2, 4, 9, 5 }, "unknown", new[] { "unknown", "spreading", "watched" }, new[] { 2, 4, 3 }, new[] { 3, 5, 4 }, 2),
        new("hazard-site", new[] { "assessed", "contained", "documented" }, "dead-zone", "lifeless", new[] { "lifeless", "avoided", "traversed", "contained" }, new[] { 7, 9, 11, 10 }, "unknown", new[] { "unknown", "spreading", "watched" }, new[] { 7, 8, 8 }, new[] { 8, 10, 9 }, 7),

        new("contact-site", new[] { "approached", "contacted", "withdrawn" }, "faction-capital", "closed", new[] { "closed", "available", "invited" }, new[] { 33, 36, 39 }, "unknown", new[] { "unknown", "present", "guarded" }, new[] { 33, 35, 39 }, new[] { 34, 35, 35 }, 33),
        new("contact-site", new[] { "approached", "contacted", "withdrawn" }, "faction-town", "closed", new[] { "closed", "available", "invited" }, new[] { 29, 31, 34 }, "unknown", new[] { "unknown", "present", "guarded" }, new[] { 29, 31, 35 }, new[] { 30, 31, 31 }, 29),
        new("contact-site", new[] { "approached", "contacted", "withdrawn" }, "faction-village", "closed", new[] { "closed", "available", "invited" }, new[] { 16, 17, 20 }, "unknown", new[] { "unknown", "present", "guarded" }, new[] { 16, 18, 22 }, new[] { 17, 18, 18 }, 16),
        new("contact-site", new[] { "approached", "contacted", "withdrawn" }, "fishing-village", "closed", new[] { "closed", "available", "invited" }, new[] { 12, 13, 16 }, "unknown", new[] { "unknown", "present", "guarded" }, new[] { 12, 14, 18 }, new[] { 13, 14, 14 }, 12),
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
            }

            for (var i = 0; i < testCase.Presence.Length; i++)
            {
                AssertEqual(testCase.PresenceCounts[i], Count(testCase.BaseOperational, testCase.Presence[i], "untouched"), $"{testCase.Variant}: presence '{testCase.Presence[i]}'");
            }

            for (var i = 0; i < testCase.Interactions.Length; i++)
            {
                AssertEqual(testCase.InteractionCounts[i], Count(testCase.BaseOperational, testCase.BasePresence, testCase.Interactions[i]), $"{testCase.Variant}: interaction '{testCase.Interactions[i]}'");
            }

            AssertEqual(testCase.Unknown, Count(null, null, null), $"{testCase.Variant}: an unobserved site shows only its neutral parts");
        }

        // The expedition base has no archetype in the catalog; its picture is found through the pseudo archetype "base-camp".
        var baseCamp = new SpecialLocationState("base-camp", LocationKind.BaseCamp, new HexCoord(1, 1), "Basislager");
        var basePicture = resolver.Resolve(baseCamp, new LocationVisualContext(VisualKnowledge.Confirmed, null));
        AssertTrue(basePicture.CompositionId == "vis-expedition-base" && basePicture.MarkerColorHex == "#3D8BD9", "The base has its own composition and marker");
        AssertEqual(30, basePicture.ShownElements.Count(), "The base shows tents, store, flag, fire and palisade");
        AssertTrue(basePicture.ShownElements.Any(element => element.Kind == VisualElementKind.Light), "The base fire gives light");
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
