using Game.App;
using Game.Core;
using Newtonsoft.Json.Linq;
using WorldGen;

/// <summary>Capitals, towns and villages of the generator become places of the game (docs/world_generator_settlements_concept.md, S1).</summary>
public sealed class SettlementTests
{
    private static readonly string[] SettlementVariants = { "faction-capital", "faction-town", "faction-village", "fishing-village" };

    public void RunAll()
    {
        SettlementsBecomeOwnedContactPlaces();
        VillagesStayFewAndFishingVillagesSitAtWater();
        GeneratedLocationsStartInValidStates();
        RoadsEndAtSettlements();
        SettlementRulesAreValidated();
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

    private static IEnumerable<(uint Seed, int Width, int Height, int Factions)> Worlds()
    {
        uint seed = 1;
        foreach (var size in new[] { (30, 22), (40, 30), (60, 45) })
        {
            foreach (var factions in new[] { 2, 3, 4 })
            {
                for (var repeat = 0; repeat < 2; repeat++) yield return (seed++, size.Item1, size.Item2, factions);
            }
        }
    }

    private static GameState Generate(GameApplication application, (uint Seed, int Width, int Height, int Factions) world) =>
        application.CreateGeneratedGame(new WorldGenerationRequest { Seed = world.Seed, Width = world.Width, Height = world.Height, FactionCount = world.Factions });

    private static void SettlementsBecomeOwnedContactPlaces()
    {
        var catalog = Catalog();
        var application = new GameApplication(catalog);
        var villages = 0;
        foreach (var world in Worlds())
        {
            var game = Generate(application, world);
            var settlements = game.World.Locations.Where(location => location.Kind == LocationKind.Settlement).ToList();
            AssertTrue(settlements.Count >= world.Factions, $"Every faction has at least its capital (seed {world.Seed})");
            AssertTrue(settlements.All(location => location.ArchetypeId == "contact-site" && SettlementVariants.Contains(location.VariantId!)), "Settlements are contact places of the four settlement variants");
            AssertTrue(settlements.All(location => location.ProperName != null && location.ProperName != location.Name), "The own name is kept apart from the designation the player sees");
            AssertTrue(settlements.All(location => location.FactionRelations.Any(relation => relation.Kind == LocationFactionRelationKind.Claimed)), "A settlement is claimed by its faction");
            AssertTrue(settlements.Count(location => location.VariantId == "faction-capital") == world.Factions, $"One capital per faction (seed {world.Seed})");
            AssertTrue(settlements.Select(location => location.Id).Distinct().Count() == settlements.Count, "Settlement ids are unique");
            var baseCamp = game.World.Locations.SingleOrDefault(location => location.Kind == LocationKind.BaseCamp);
            AssertTrue(baseCamp != null && baseCamp.Coord.Equals(game.Expedition.Position), $"The base is a place at the base coordinate (seed {world.Seed})");
            villages += settlements.Count(location => location.VariantId == "faction-village" || location.VariantId == "fishing-village");
        }

        AssertTrue(villages > 0, "Some of the generated worlds contain villages");
    }

    private static void VillagesStayFewAndFishingVillagesSitAtWater()
    {
        var rules = new SettlementRules();
        var fishing = 0;
        foreach (var world in Worlds())
        {
            var generated = new WorldGenerator().Generate(new GenerationParams { Seed = world.Seed, MapWidth = world.Width, MapHeight = world.Height, FactionCount = world.Factions });
            var all = new List<HexCell>();
            foreach (var faction in generated.Factions)
            {
                if (faction.Seed != null) all.Add(faction.Seed);
                all.AddRange(faction.Towns);
            }

            all.Add(generated.Base);
            foreach (var faction in generated.Factions)
            {
                var cap = Math.Min(rules.MaxVillagesPerFaction, (int)Math.Floor(faction.Towns.Count * rules.VillagesPerTown) + 1);
                AssertTrue(faction.Villages.Count <= cap, $"Faction {faction.Id} has {faction.Villages.Count} villages, at most {cap} (seed {world.Seed})");
                AssertTrue(faction.Villages.Count(village => village.Fishing) <= (int)Math.Ceiling(Math.Max(1, faction.Villages.Count) * rules.MaxFishingShare), "Fishing villages are at most half of the villages");
                foreach (var village in faction.Villages)
                {
                    AssertTrue(village.Cell.Special == null && !village.Cell.IsUnder, "A village does not sit on a special location or in water");
                    AssertTrue(all.All(other => generated.Grid.Dist(village.Cell, other) >= rules.MinSpacing), $"A village keeps its distance to other settlements (seed {world.Seed})");
                    AssertTrue(!string.IsNullOrWhiteSpace(village.Name), "A village has a name");
                    if (village.Fishing)
                    {
                        fishing++;
                        var atWater = village.Cell.HasRiver || village.Cell.Biome == (int)Biome.Coast;
                        for (var direction = 0; direction < 6 && !atWater; direction++)
                        {
                            var neighbour = generated.Grid.Step(village.Cell, direction);
                            atWater = neighbour != null && (neighbour.IsOcean || neighbour.IsLake || neighbour.HasRiver);
                        }

                        AssertTrue(atWater, $"A fishing village sits at water (seed {world.Seed})");
                    }

                    all.Add(village.Cell);
                }
            }
        }

        AssertTrue(fishing > 0, "At least one fishing village appears in the test worlds");
    }

    private static void GeneratedLocationsStartInValidStates()
    {
        var catalog = Catalog();
        var application = new GameApplication(catalog);
        var resolver = new LocationScenarioActionResolver(catalog.Authoring);
        var checkedLocations = 0;
        foreach (var world in Worlds())
        {
            foreach (var location in Generate(application, world).World.Locations.Where(location => location.ArchetypeId != null))
            {
                resolver.ValidateRuntimeState(location);
                checkedLocations++;
            }
        }

        AssertTrue(checkedLocations > 100, "Generated locations were checked, found " + checkedLocations);
    }

    private static void RoadsEndAtSettlements()
    {
        var application = new GameApplication(Catalog());
        var checkedRoads = 0;
        foreach (var world in Worlds())
        {
            var game = Generate(application, world);
            var settlementCoords = game.World.Locations.Where(location => location.Kind == LocationKind.Settlement).Select(location => location.Coord).ToHashSet();
            foreach (var road in game.World.Paths.Where(path => path.Kind == WorldPathKind.Road))
            {
                AssertTrue(settlementCoords.Contains(road.Coords[0]) && settlementCoords.Contains(road.Coords[road.Coords.Count - 1]), $"A road runs from settlement to settlement (seed {world.Seed})");
                checkedRoads++;
            }
        }

        AssertTrue(checkedRoads > 0, "Roads were checked");
    }

    private static void SettlementRulesAreValidated()
    {
        var catalog = Catalog();
        var rules = catalog.Placement!.Settlements;
        AssertTrue(rules.CapitalVariant == "faction-capital" && rules.FishingVillageVariant == "fishing-village" && rules.MaxVillagesPerFaction == 4, "The shipped settlement rules are loaded");

        void Reject(Action<JObject> change, string expected)
        {
            var document = JObject.Parse(LocationPlacementLoader.ToJson(catalog.Placement!));
            change((JObject)document["settlements"]!);
            try
            {
                LocationPlacementLoader.LoadFromJson(new[] { document.ToString() });
            }
            catch (LocationDataException exception)
            {
                AssertTrue(exception.Message.Contains(expected), "Unexpected message: " + exception.Message);
                return;
            }

            throw new InvalidOperationException("Invalid settlement rules were accepted: " + expected);
        }

        Reject(settlements => settlements["minSpacing"] = 0, "minSpacing >= 1");
        Reject(settlements => settlements["maxFishingShare"] = 1.5, "maxFishingShare");
        Reject(settlements => settlements["villagesPerTown"] = -1, "villagesPerTown >= 0");
        Reject(settlements => settlements["townVariant"] = "faction-capital", "four different variant ids");

        var shipped = catalog.Placement!;
        var original = shipped.Settlements.VillageVariant;
        shipped.Settlements.VillageVariant = "flying-village";
        try
        {
            LocationPlacementLoader.ValidateAgainstCatalog(shipped, catalog.Authoring);
            throw new InvalidOperationException("An unknown settlement variant was accepted.");
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains("no contact-site scenario profile"), "Unknown settlement variant is reported: " + exception.Message);
        }
        finally
        {
            shipped.Settlements.VillageVariant = original;
        }
    }
}
