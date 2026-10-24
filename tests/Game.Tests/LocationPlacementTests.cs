using Game.App;
using Newtonsoft.Json.Linq;
using WorldGen;

public sealed class LocationPlacementTests
{
    public void RunAll()
    {
        CatalogCarriesTheBuiltInRules();
        CatalogRulesReproduceTheBuiltInWorlds();
        ValidatorRejectsInconsistentRules();
        WeightsSteerTheVariantChoice();
    }

    private static GameDataCatalog Catalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void CatalogCarriesTheBuiltInRules()
    {
        var placement = Catalog().Placement;
        AssertTrue(placement != null, "The catalog loads the location placement document");
        AssertTrue(placement!.Groups.Count == 10, "The document lists the ten placement groups");
        AssertTrue(LocationPlacementLoader.ToJson(placement) == LocationPlacementLoader.ToJson(PlacementTable.Default()), "The shipped rules equal the built-in default rules");
        var variants = placement.Groups.SelectMany(group => group.VariantRules).SelectMany(rule => rule.Variants).Distinct().Count();
        AssertTrue(variants == 30, "The groups carry the thirty generator variants, found " + variants);
    }

    private static void CatalogRulesReproduceTheBuiltInWorlds()
    {
        var placement = Catalog().Placement!;
        var index = 0;
        foreach (var testCase in WorldGeneratorPlacementTests.BuildCases())
        {
            if (index++ % 4 != 0) continue;
            var builtIn = WorldGeneratorPlacementTests.DigestFor(testCase.Seed, testCase.Width, testCase.Height, testCase.Factions, testCase.Density);
            var fromData = WorldGeneratorPlacementTests.DigestFor(testCase.Seed, testCase.Width, testCase.Height, testCase.Factions, testCase.Density, placement);
            AssertTrue(builtIn == fromData, "Rules from the document place the same locations as the built-in rules (seed " + testCase.Seed + ")");
        }
    }

    private static string Mutated(Action<JObject> change)
    {
        var document = JObject.Parse(LocationPlacementLoader.ToJson(PlacementTable.Default()));
        change(document);
        return document.ToString();
    }

    private static JObject Group(JObject document, string id) => document["groups"]!.OfType<JObject>().First(group => (string?)group["id"] == id);

    private static void ExpectFailure(string json, string expected, string message)
    {
        try
        {
            LocationPlacementLoader.LoadFromJson(new[] { json });
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains(expected), message + ": message '" + exception.Message + "' lacks '" + expected + "'");
            return;
        }

        throw new InvalidOperationException(message + ": nothing was rejected.");
    }

    private static void ValidatorRejectsInconsistentRules()
    {
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["archetype"] = "Zauberturm"), "unknown archetype", "An unknown generator archetype is rejected");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["id"] = "placement-trace-sites"), "used twice", "Group ids are unique");
        ExpectFailure(Mutated(d => ((JArray)Group(d, "placement-landmarks")["variantRules"]!).RemoveAt(0)), "at least one variant rule", "A group needs variant rules");
        ExpectFailure(Mutated(d => ((JArray)Group(d, "placement-resource-sites")["variantRules"]!).Last!["when"] = "coastal"), "end its variant rules with 'always'", "The last rule must always match");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["variantRules"]![0]!["variants"]![0]!["name"] = "Verlassenes Lager"), "belongs to two placement groups", "A variant belongs to exactly one group");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["variantRules"]![0]!["variants"]![0]!["weight"] = -1), "weight", "Weights must be positive");
        ExpectFailure(Mutated(d => Group(d, "placement-hazard-swamp")["candidateBiome"] = "Lava"), "unknown biome", "Biome names are checked");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["candidates"] = "everywhere"), "unknown candidates", "Candidate names are checked");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["count"]!["factor"] = 0), "positive factor", "A density count needs a factor");
        ExpectFailure(Mutated(d => Group(d, "placement-route-obstacles")["anchor"] = "point"), "edge anchor", "Edge groups use the edge anchor");
        ExpectFailure(Mutated(d => Group(d, "placement-landmarks")["areaScale"] = 1.0), "area scale", "An area scale needs an area anchor");
        ExpectFailure(Mutated(d => d["schemaVersion"] = 2), "schemaVersion 1", "The schema version is checked");
    }

    private static void WeightsSteerTheVariantChoice()
    {
        var table = PlacementTable.Default();
        var trace = table.Groups.First(group => group.Id == "placement-trace-sites");
        trace.VariantRules[0].Weights.AddRange(new[] { 1d, 1d, 1d, 60d });
        LocationPlacementLoader.Validate(table);

        var boatCamps = 0;
        var traces = 0;
        for (uint seed = 1; seed <= 12; seed++)
        {
            var world = new WorldGenerator().Generate(new GenerationParams { Seed = seed, MapWidth = 60, MapHeight = 45, FactionCount = 3, Placement = table });
            foreach (var special in world.Specials.Where(special => special.Archetype == "Spurenort"))
            {
                traces++;
                if (special.Variant == "Altes Bootslager") boatCamps++;
            }
        }

        AssertTrue(traces >= 12, "Weighted worlds still contain trace sites");
        AssertTrue(boatCamps * 10 >= traces * 7, "A heavily weighted variant dominates (" + boatCamps + " of " + traces + ")");

        var first = new WorldGenerator().Generate(new GenerationParams { Seed = 5, MapWidth = 60, MapHeight = 45, FactionCount = 3, Placement = table });
        var again = new WorldGenerator().Generate(new GenerationParams { Seed = 5, MapWidth = 60, MapHeight = 45, FactionCount = 3, Placement = table });
        AssertTrue(again.Specials.Select(special => special.Variant).SequenceEqual(first.Specials.Select(special => special.Variant)), "Weighted choice stays deterministic");
    }
}
