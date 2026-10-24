using Game.App;
using Game.Core;

public sealed class GeneratorVariantContentTests
{
    private static readonly string[] AuthoredBefore =
    {
        "broken-bridge", "marked-grave", "abandoned-camp", "sealed-gate", "border-warning-sign", "restless-marsh", "wind-carved-peak", "first-contact"
    };

    public void RunAll()
    {
        EveryGeneratorVariantHasFindingsAndEvidence();
        FindingTablesOnlyOfferTheFindingsOfTheirVariant();
        GeneratedLocationsCarryTheirEvidenceSeedFirst();
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

    private static IEnumerable<string> NewVariants(GameDataCatalog catalog) =>
        catalog.Placement!.Groups.SelectMany(group => group.VariantRules).SelectMany(rule => rule.Variants).Distinct().Where(variant => !AuthoredBefore.Contains(variant));

    private static void EveryGeneratorVariantHasFindingsAndEvidence()
    {
        var catalog = Catalog();
        var scenarios = catalog.Authoring.ScenarioProfiles.Values.ToDictionary(scenario => scenario.VariantId);
        var variants = NewVariants(catalog).ToList();
        AssertTrue(variants.Count == 23, "Twenty-three generator variants are new, found " + variants.Count);
        foreach (var variant in variants)
        {
            var scenario = scenarios[variant];
            AssertTrue(scenario.FindingPoolIds.Count == 2 && scenario.FindingPoolIds.All(catalog.Authoring.Findings.ContainsKey), "Variant offers two defined findings: " + variant);
            AssertTrue(scenario.EvidencePoolIds.Count == 1 && catalog.CrossSystem.Evidence.Find(scenario.EvidencePoolIds[0]) != null, "Variant has a defined evidence: " + variant);
            AssertTrue(scenario.EvidencePoolIds[0] == "evidence-site-" + variant, "Evidence follows the naming scheme: " + variant);
        }
    }

    private static void FindingTablesOnlyOfferTheFindingsOfTheirVariant()
    {
        var catalog = Catalog();
        var seen = new HashSet<string>();
        foreach (var location in GeneratedLocations(catalog))
        {
            if (AuthoredBefore.Contains(location.VariantId!)) continue;
            seen.Add(location.VariantId!);
            foreach (var table in catalog.Authoring.FindingTables.Values)
            {
                foreach (var entry in table.Entries.Where(entry => entry.Matches(location) && entry.FindingId != null))
                {
                    AssertTrue(entry.FindingId!.StartsWith("finding-" + location.VariantId + "-", StringComparison.Ordinal),
                        $"Table '{table.Id}' offers '{entry.FindingId}' at a '{location.VariantId}' location");
                }
            }
        }

        AssertTrue(seen.Count >= 10, "The generated worlds contain at least ten of the new variants, found " + seen.Count);
    }

    private static void GeneratedLocationsCarryTheirEvidenceSeedFirst()
    {
        var catalog = Catalog();
        var checkedLocations = 0;
        foreach (var location in GeneratedLocations(catalog).Where(location => !AuthoredBefore.Contains(location.VariantId!)))
        {
            AssertTrue(location.EvidenceSeedIds.FirstOrDefault() == "evidence-site-" + location.VariantId, "Evidence seed of a '" + location.VariantId + "' location comes first");
            checkedLocations++;
        }

        AssertTrue(checkedLocations >= 20, "Evidence seeds were checked on many locations, found " + checkedLocations);
    }

    private static IEnumerable<SpecialLocationState> GeneratedLocations(GameDataCatalog catalog)
    {
        var application = new GameApplication(catalog);
        for (uint seed = 1; seed <= 8; seed++)
        {
            var game = application.CreateGeneratedGame(new WorldGenerationRequest { Seed = seed, Width = 60, Height = 45, FactionCount = 3 });
            foreach (var location in game.World.Locations.Where(location => location.VariantId != null)) yield return location;
        }
    }
}
