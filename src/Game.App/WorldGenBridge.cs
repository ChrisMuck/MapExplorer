#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using WorldGen;

namespace Game.App
{

/// <summary>Player-facing campaign choices. Technical generator tuning remains in editor-only settings.</summary>
public sealed class WorldGenerationRequest
{
    public uint Seed { get; init; } = 42;
    public int Width { get; init; } = 40;
    public int Height { get; init; } = 30;
    public int FactionCount { get; init; } = 3;
    public string FactionMood { get; init; } = "Gemischt";
    public int FactionSalt { get; init; }
    public int LocationSalt { get; init; }

    public GenerationParams ToGeneratorParams()
    {
        return new GenerationParams
        {
            Seed = Seed,
            MapWidth = Width,
            MapHeight = Height,
            FactionCount = FactionCount,
            FactionMood = FactionMood,
            FactionSalt = FactionSalt,
            LocationSalt = LocationSalt
        };
    }
}

/// <summary>Snapshot produced by the bridge; campaign composition is intentionally left to Game.App.</summary>
public sealed class WorldGenerationBridgeResult
{
    public WorldGenerationBridgeResult(WorldState world, HexCoord baseLocation, IReadOnlyList<FactionState> factions)
    {
        World = world;
        BaseLocation = baseLocation;
        Factions = factions;
    }

    public WorldState World { get; }
    public HexCoord BaseLocation { get; }
    public IReadOnlyList<FactionState> Factions { get; }
}

/// <summary>Translates pure WorldGen output into immutable-at-the-boundary Game.Core state.</summary>
public sealed class WorldGenBridge
{
    public WorldGenerationBridgeResult Generate(WorldGenerationRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return Bridge(new WorldGenerator().Generate(request.ToGeneratorParams()));
    }

    public WorldGenerationBridgeResult Bridge(GeneratedWorld generated)
    {
        if (generated == null) throw new ArgumentNullException(nameof(generated));

        var qOffset = (generated.Grid.H - 1) >> 1;
        HexCoord ToCore(HexCell cell) => new(cell.AX + qOffset, cell.AZ);
        var factionIds = generated.Factions.ToDictionary(faction => faction.Id, faction => $"faction-{faction.Id}");
        var locations = BuildLocations(generated, ToCore, factionIds);
        var locationByCell = locations
            .Where(location => location.Anchor.Kind == LocationAnchorKind.Point)
            .ToDictionary(location => location.Coord, location => location.Id);
        var roadByCell = PathIds(generated.Roads, "road", ToCore);
        var riverByCell = RiverIds(generated.Rivers, ToCore);
        var tiles = generated.Grid.Cells.Select(cell => new HexTileState(
            ToCore(cell),
            ToTerrain(cell),
            elevation: Math.Max(0, (int)Math.Round(cell.Elevation)),
            isBlocked: cell.IsUnder,
            roadId: roadByCell.TryGetValue(ToCore(cell), out var roadId) ? roadId : null,
            riverId: riverByCell.TryGetValue(ToCore(cell), out var riverId) ? riverId : null,
            locationId: locationByCell.TryGetValue(ToCore(cell), out var locationId) ? locationId : null,
            ownerId: cell.Faction >= 0 && factionIds.TryGetValue(cell.Faction, out var ownerId) ? ownerId : null));
        var bounds = new HexMapBounds(generated.Grid.W + qOffset, generated.Grid.H);
        var paths = BuildPaths(generated, ToCore);
        var world = new WorldState(new HexMapState(bounds, tiles), paths, locations);
        var factions = generated.Factions
            .Select(faction => new FactionState(factionIds[faction.Id], faction.Name ?? factionIds[faction.Id]))
            .ToList();
        return new WorldGenerationBridgeResult(world, ToCore(generated.Base), factions);
    }

    private static IReadOnlyList<SpecialLocationState> BuildLocations(
        GeneratedWorld world,
        Func<HexCell, HexCoord> toCore,
        IReadOnlyDictionary<int, string> factionIds)
    {
        var results = new List<SpecialLocationState>();
        for (var index = 0; index < world.Specials.Count; index++)
        {
            var source = world.Specials[index];
            var sourceAnchor = source.Anchor;
            var anchor = sourceAnchor.Kind switch
            {
                AnchorKind.Edge when sourceAnchor.Cells.Count >= 2 => LocationAnchor.Edge(toCore(sourceAnchor.Cells[0]), toCore(sourceAnchor.Cells[1])),
                AnchorKind.Area when sourceAnchor.Cells.Count > 0 => LocationAnchor.Area(sourceAnchor.Cells.Select(toCore)),
                _ => LocationAnchor.Point(toCore(source.Cell))
            };
            var relations = Relations(source, factionIds);
            results.Add(new SpecialLocationState(
                $"worldgen-location-{index + 1}",
                KindFor(source.Archetype),
                toCore(source.Cell),
                source.Variant,
                anchor,
                ArchetypeIdFor(source.Archetype),
                VariantIdFor(source.Variant),
                source.Modifiers.Select(ModifierIdFor),
                factionRelations: relations));
        }

        return results;
    }

    private static IEnumerable<LocationFactionRelationState> Relations(SpecialLocation source, IReadOnlyDictionary<int, string> factionIds)
    {
        if (source.Owner >= 0 && factionIds.TryGetValue(source.Owner, out var owner))
        {
            yield return new LocationFactionRelationState(owner, LocationFactionRelationKind.Claimed);
        }
        if (source.WatchedBy >= 0 && factionIds.TryGetValue(source.WatchedBy, out var watcher))
        {
            yield return new LocationFactionRelationState(watcher, LocationFactionRelationKind.Watched);
        }
    }

    private static Dictionary<HexCoord, string> PathIds(IEnumerable<List<HexCell>> paths, string prefix, Func<HexCell, HexCoord> toCore)
    {
        var result = new Dictionary<HexCoord, string>();
        var index = 0;
        foreach (var path in paths)
        {
            index++;
            foreach (var cell in path) result[toCore(cell)] = $"{prefix}-{index}";
        }
        return result;
    }

    private static Dictionary<HexCoord, string> RiverIds(IEnumerable<River> rivers, Func<HexCell, HexCoord> toCore)
    {
        var result = new Dictionary<HexCoord, string>();
        var index = 0;
        foreach (var river in rivers)
        {
            index++;
            foreach (var cell in river.Cells) result[toCore(cell)] = $"river-{index}";
        }
        return result;
    }

    private static IReadOnlyList<WorldPathState> BuildPaths(GeneratedWorld world, Func<HexCell, HexCoord> toCore)
    {
        var paths = new List<WorldPathState>();
        for (var index = 0; index < world.Roads.Count; index++) paths.Add(new WorldPathState($"road-{index + 1}", WorldPathKind.Road, world.Roads[index].Select(toCore)));
        for (var index = 0; index < world.Rivers.Count; index++) paths.Add(new WorldPathState($"river-{index + 1}", WorldPathKind.River, world.Rivers[index].Cells.Select(toCore)));
        return paths;
    }

    private static TerrainType ToTerrain(HexCell cell) => cell.Biome switch
    {
        (int)Biome.Ocean or (int)Biome.Lake => TerrainType.Water,
        (int)Biome.Coast or (int)Biome.Riverlands => TerrainType.Coast,
        (int)Biome.Forest or (int)Biome.DeepForest or (int)Biome.Jungle => TerrainType.Forest,
        (int)Biome.Swamp => TerrainType.Swamp,
        (int)Biome.Desert or (int)Biome.Wasteland => TerrainType.Desert,
        (int)Biome.Mountains or (int)Biome.Volcanic => TerrainType.Mountain,
        (int)Biome.Highlands or (int)Biome.Tundra => TerrainType.Hills,
        _ => TerrainType.Grassland
    };

    private static LocationKind KindFor(string archetype) => ArchetypeIdFor(archetype) switch
    {
        "route-obstacle" => LocationKind.BrokenRavine,
        "investigation-site" or "containment-site" => LocationKind.Ruin,
        "trace-site" => LocationKind.AbandonedCamp,
        _ => LocationKind.Landmark
    };

    private static string ArchetypeIdFor(string archetype) => archetype switch
    {
        "Wegehindernis" => "route-obstacle",
        "Untersuchungsort" => "investigation-site",
        "Verwahrungsort" => "containment-site",
        "Spurenort" => "trace-site",
        "Ressourcenort" => "resource-site",
        "Gefahrenzone" => "hazard-zone",
        "Landmarke" => "landmark-site",
        "Grenzzeichen" => "territorial-marker",
        _ => "landmark-site"
    };

    private static string VariantIdFor(string variant) => variant switch
    {
        "ZerstÃ¶rte BrÃ¼cke" => "broken-bridge",
        "Vergessenes Grab" => "marked-grave",
        "Verlassenes Lager" => "abandoned-camp",
        "Versiegeltes Tor" => "sealed-gate",
        _ => $"worldgen-{Normalize(variant)}"
    };

    private static string ModifierIdFor(string modifier) => modifier switch
    {
        "FactionOwned" => "modifier-faction-owned",
        "Sacred" => "modifier-sacred",
        "Watched" => "modifier-watched",
        _ => $"modifier-worldgen-{Normalize(modifier)}"
    };

    private static string Normalize(string value)
    {
        return new string((value ?? "unknown").ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
    }
}
}
