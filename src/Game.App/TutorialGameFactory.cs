using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public static class TutorialGameFactory
{
    public static GameState Create()
    {
        return Create(null, null);
    }

    /// <summary>
    /// Creates the player-facing tutorial entry state. The first expedition has not departed yet:
    /// the base screen selects its team/loadout and the ordinary departure command creates
    /// Expedition 1. <see cref="Create"/> remains the active-expedition fixture used by focused
    /// Core tests and development scenarios.
    /// </summary>
    public static GameState CreateAtBasePreparation(
        IReadOnlyList<SpecialLocationState>? interactionInstances,
        TutorialCampaignDefinition? tutorial = null)
    {
        var game = Create(interactionInstances, tutorial);

        // The initial roster is fully rested: the player should learn composition before the
        // expedition introduces injuries/exhaustion through normal outcomes.
        foreach (var member in game.Roster.Members)
        {
            if (!member.IsAvailable)
            {
                member.Heal();
            }
        }

        game.SetExpedition(new ExpeditionState(
            expeditionNumber: 0,
            position: game.Base.Location,
            members: Array.Empty<ExpeditionMemberState>(),
            movementPoints: 0,
            maxMovementPoints: 0,
            supplies: 0,
            medicine: 0,
            morale: 70,
            capacity: 0,
            status: ExpeditionStatus.Returned));
        game.Base.AddArchiveEntry("The coastal base is prepared for Expedition 1.");
        return game;
    }

    /// <summary>
    /// Builds the tutorial world. When <paramref name="interactionInstances"/> is supplied (from the
    /// JSON location data), those archetype-driven locations are used; otherwise the in-code fallback
    /// instances are built. Plain map markers (settlements, base, watchtower, mine) are always in code.
    /// </summary>
    public static GameState Create(IReadOnlyList<SpecialLocationState>? interactionInstances, TutorialCampaignDefinition? tutorial = null)
    {
        var bounds = new HexMapBounds(40, 30);
        var map = GenerateTutorialMap(bounds);
        var baseCoord = BaseCoord();

        map.SetTile(new HexTileState(baseCoord, TerrainType.Coast, locationId: "base-camp"));
        ApplyTutorialRouteTerrain(map);
        map.SetTile(new HexTileState(new HexCoord(7, 14), TerrainType.Hills));
        map.SetTile(new HexTileState(new HexCoord(8, 14), TerrainType.Mountain, elevation: 3));
        map.SetTile(new HexTileState(new HexCoord(9, 14), TerrainType.Mountain, elevation: 4, isBlocked: true));
        map.SetTile(new HexTileState(new HexCoord(6, 16), TerrainType.Swamp, riverId: "gray-river"));
        // The optional grave sits beside the old route on open ground; the main-route landmarks
        // are already normalized by ApplyTutorialRouteTerrain.
        map.SetTile(map.GetTile(new HexCoord(12, 18)).WithTerrain(TerrainType.Grassland, elevation: 1, isBlocked: false));
        map.SetTile(new HexTileState(ViewCoord(5, -2), TerrainType.Hills));

        // Keep the ancient wall on open plains so it reads clearly instead of vanishing in the range.
        foreach (var coord in ViewPath(AncientWallRoute))
        {
            if (map.TryGetTile(coord, out var tile) && tile != null)
            {
                map.SetTile(new HexTileState(coord, TerrainType.Grassland));
            }
        }

        ApplyFactionTerritories(map);

        var locations = CreateTutorialLocations(interactionInstances).ToList();
        RegisterTutorialLocationAnchors(map, locations);
        var world = new WorldState(map, CreateTutorialPaths(), locations);
        var knowledge = new KnowledgeState();
        new KnowledgeService().RevealFromExpedition(map, knowledge, baseCoord);

        var notes = new PlayerNotesState();
        var members = CreateTutorialMembers().ToList();
        var expedition = new ExpeditionState(
            expeditionNumber: 1,
            position: baseCoord,
            members: members,
            movementPoints: 4,
            supplies: 20,
            medicine: 3,
            morale: 70,
            capacity: 20);
        var baseState = new BaseState(
            baseCoord,
            new BaseUpgradesState(CreateTutorialUpgrades()),
            new EvaluationQueueState(),
            CreateTutorialUnitStock());
        baseState.AddArchiveEntry("First expedition prepared at the coastal base.");
        baseState.MarkExpeditionDepartureArchivePoint();

        // Seed the persistent roster with rich profiles (matching the active members' ids/status)
        // so returning members reconcile into the pool instead of duplicating.
        var roster = new BaseRosterState(CreateTutorialRoster());

        var factions = CreateTutorialFactions().ToList();
        ApplyTutorialRelations(locations, factions, tutorial);
        return new GameState(world, knowledge, notes, expedition, baseState, factions: factions, roster: roster);
    }

    /// <summary>
    /// Creates a playable campaign shell from a generated world. Expedition composition is intentionally
    /// still the tutorial roster until campaign preparation receives its own command flow.
    /// </summary>
    public static GameState CreateGenerated(WorldGenerationBridgeResult generated)
    {
        if (generated == null)
        {
            throw new ArgumentNullException(nameof(generated));
        }

        var knowledge = new KnowledgeState();
        new KnowledgeService().RevealFromExpedition(generated.World.Map, knowledge, generated.BaseLocation);
        var expedition = new ExpeditionState(
            expeditionNumber: 1,
            position: generated.BaseLocation,
            members: CreateTutorialMembers().ToList(),
            movementPoints: 4,
            supplies: 20,
            medicine: 3,
            morale: 70,
            capacity: 20);
        var baseState = new BaseState(
            generated.BaseLocation,
            new BaseUpgradesState(CreateTutorialUpgrades()),
            new EvaluationQueueState(),
            CreateTutorialUnitStock());
        baseState.AddArchiveEntry("First expedition prepared at the generated coastal base.");
        baseState.MarkExpeditionDepartureArchivePoint();

        return new GameState(
            generated.World,
            knowledge,
            new PlayerNotesState(),
            expedition,
            baseState,
            factions: generated.Factions,
            roster: new BaseRosterState(CreateTutorialRoster()));
    }

    private static HexMapState GenerateTutorialMap(HexMapBounds bounds)
    {
        var tiles = new List<HexTileState>();

        foreach (var coord in bounds.AllCoords())
        {
            var view = ToCenteredViewCoord(coord, bounds);
            var terrain = PickTerrain(view);
            var elevation = terrain == TerrainType.Snow ? 4 :
                terrain == TerrainType.Mountain ? 3 :
                terrain == TerrainType.Hills ? 2 :
                terrain == TerrainType.Water ? 0 :
                1;

            tiles.Add(new HexTileState(coord, terrain, elevation, terrain == TerrainType.Water));
        }

        return new HexMapState(bounds, tiles);
    }

    private static IEnumerable<WorldPathState> CreateTutorialPaths()
    {
        return new[]
        {
            new WorldPathState("river-gray", WorldPathKind.River, TutorialRavineRoute),
            new WorldPathState("river-north", WorldPathKind.River, ViewPath(
                (-2, -6), (-1, -6), (0, -6), (0, -5), (1, -5), (1, -4), (2, -4), (2, -3),
                (3, -3), (4, -4))),
            new WorldPathState("road-main", WorldPathKind.Road, TutorialOldRoute),
            new WorldPathState("road-east-branch", WorldPathKind.Road, ViewPath(
                (-1, 0), (-1, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 3))),
            new WorldPathState("road-south-branch", WorldPathKind.Road, ViewPath(
                (-5, 2), (-5, 3), (-4, 4), (-3, 5))),
            new WorldPathState("border-wardens", WorldPathKind.TerritoryBorder, ViewPath(
                (-6, 3), (-5, 2), (-4, 2), (-3, 1), (-2, 1), (-1, 0), (0, 0), (1, -1),
                (2, -1), (3, -2), (4, -2))),
            new WorldPathState("ancient-wall", WorldPathKind.Wall, ViewPath(AncientWallRoute))
        };
    }

    // Ancient wall route across the eastern plains — deliberately clear of the mountain range.
    private static readonly (int Q, int R)[] AncientWallRoute =
    {
        (5, 3), (6, 3), (7, 3), (8, 3), (9, 3), (10, 3), (11, 2), (12, 2)
    };

    // The old survey route is deliberately continuous: the contact, warning, abandoned camp and
    // broken crossing read as one journey instead of disconnected prototype landmarks.
    private static readonly HexCoord[] TutorialOldRoute =
    {
        new(1, 15), new(2, 15), new(3, 15), new(4, 15), new(5, 15), new(6, 15), new(7, 15), new(8, 15),
        new(9, 15), new(9, 16), new(10, 16), new(11, 16), new(12, 16), new(12, 17), new(13, 17), new(14, 17), new(15, 17), new(16, 17),
        new(17, 17), new(18, 16), new(19, 15), new(20, 15)
    };

    // The ravine approaches the route at the broken edge. It is a traceable visual path; movement
    // access itself remains governed by the normal route-obstacle action/result contract.
    private static readonly HexCoord[] TutorialRavineRoute =
    {
        new(17, 8), new(17, 9), new(18, 10), new(18, 11), new(18, 12), new(18, 13), new(18, 14),
        new(18, 15), new(17, 16), new(17, 17), new(18, 18), new(19, 19)
    };

    private static IEnumerable<SpecialLocationState> CreateTutorialLocations(IReadOnlyList<SpecialLocationState>? interactionInstances)
    {
        // Plain map markers with no archetype-driven interaction stay in code.
        var locations = new List<SpecialLocationState>
        {
            new SpecialLocationState("base-camp", LocationKind.BaseCamp, BaseCoord(), "Coastal Base"),
            new SpecialLocationState("settlement-west", LocationKind.Settlement, new HexCoord(5, 17), "Coastal Outpost"),
            new SpecialLocationState("settlement-crossing", LocationKind.Settlement, ViewCoord(-1, 0), "River Crossing"),
            new SpecialLocationState("settlement-east", LocationKind.Settlement, ViewCoord(3, -2), "Eastern Hamlet"),
            new SpecialLocationState("settlement-north", LocationKind.Settlement, ViewCoord(4, 3), "Northern Village"),
            new SpecialLocationState("settlement-south", LocationKind.Settlement, ViewCoord(-3, 5), "Foothill Camp"),
            new SpecialLocationState("watchtower", LocationKind.Watchtower, ViewCoord(9, 2), "Old Watchtower"),
            new SpecialLocationState("mine", LocationKind.Mine, ViewCoord(-10, -2), "Abandoned Mine")
        };

        // Archetype-driven interaction locations. Placement is code-driven for the slice (until the
        // §18 procedural generator); definitions come from the JSON registries. A future generator can
        // inject placed instances via interactionInstances instead.
        locations.AddRange(interactionInstances is { Count: > 0 }
            ? interactionInstances
            : CreateSliceInteractionLocations());

        return locations;
    }

    private static IEnumerable<SpecialLocationState> CreateSliceInteractionLocations()
    {
        return new[]
        {
            new SpecialLocationState(
                "coastal-landing",
                LocationKind.Settlement,
                new HexCoord(8, 15),
                "Uferzeichen",
                LocationAnchor.Point(new HexCoord(8, 15)),
                "contact-site",
                "first-contact",
                new[] { "modifier-invitation", "modifier-guarded" },
                contentProfileId: "content-first-contact",
                interactionStateId: "unapproached",
                operationalStateId: "available",
                presenceStateId: "present"),
            new SpecialLocationState(
                "broken-ravine",
                LocationKind.BrokenRavine,
                new HexCoord(17, 17),
                "Zerstoerte Bruecke",
                LocationAnchor.Edge(new HexCoord(16, 17), new HexCoord(17, 17)),
                "route-obstacle",
                "broken-bridge",
                new[] { "modifier-repairable", "modifier-unstable", "modifier-watched" },
                contentProfileId: "content-old-trade-road-bridge",
                operationalStateId: LocationStateIds.Operational.Blocked,
                presenceStateId: LocationStateIds.Presence.Watched),
            new SpecialLocationState(
                "marked-grave",
                LocationKind.MarkedGrave,
                new HexCoord(12, 18),
                "Markiertes Grab",
                LocationAnchor.Point(new HexCoord(12, 18)),
                "investigation-site",
                "marked-grave",
                new[] { "modifier-sacred", "modifier-faction-owned" },
                contentProfileId: "content-marked-grave",
                operationalStateId: LocationStateIds.Operational.Sealed,
                presenceStateId: LocationStateIds.Presence.Empty,
                factionIds: new[] { "border-wardens" }),
            new SpecialLocationState(
                "abandoned-camp",
                LocationKind.AbandonedCamp,
                new HexCoord(14, 17),
                "Verlassenes Lager",
                LocationAnchor.Point(new HexCoord(14, 17)),
                "investigation-site",
                "abandoned-camp",
                new[] { "modifier-searchable", "modifier-campable" },
                contentProfileId: "content-abandoned-camp",
                interactionStateId: LocationStateIds.Interaction.Untouched,
                presenceStateId: LocationStateIds.Presence.Abandoned),
            new SpecialLocationState(
                "border-warning",
                LocationKind.Landmark,
                new HexCoord(12, 16),
                "Grenzwarnung",
                LocationAnchor.Point(new HexCoord(12, 16)),
                "territorial-marker",
                "border-warning-sign",
                new[] { "modifier-faction-owned", "modifier-watched" },
                contentProfileId: "content-border-warning",
                operationalStateId: "standing",
                presenceStateId: LocationStateIds.Presence.Watched,
                factionIds: new[] { "border-wardens" }),
            new SpecialLocationState(
                "sealed-gate",
                LocationKind.Ruin,
                ViewCoord(5, -2),
                "Versiegeltes Tor",
                LocationAnchor.Point(ViewCoord(5, -2)),
                "containment-site",
                "sealed-gate",
                new[] { "modifier-sealed", "modifier-guarded" },
                contentProfileId: "content-sealed-gate",
                operationalStateId: LocationStateIds.Operational.Sealed,
                presenceStateId: LocationStateIds.Presence.Unknown)
        };
    }

    private static IEnumerable<FactionState> CreateTutorialFactions()
    {
        return new[]
        {
            new FactionState(
                "coastal-people",
                "Coastal People",
                FactionContactStatus.Contacted,
                trust: 18,
                anger: 0,
                fear: 12,
                reactionProfileId: "welcoming",
                memories: new[] { "Coastal People warned the expedition not to camp beyond the black stones." }),
            new FactionState(
                "border-wardens",
                "Border Wardens",
                FactionContactStatus.Rumored,
                trust: 0,
                anger: 8,
                fear: 20,
                reactionProfileId: "neutral-cautious",
                warningZones: CreateBorderWardenWarningZones(),
                memories: new[] { "Their borders are inferred from graves, carved posts and renewed warning markers." }),
            new FactionState(
                "hidden-ones",
                "Hidden Ones",
                FactionContactStatus.Rumored,
                trust: 0,
                anger: 12,
                fear: 35,
                reactionProfileId: "hostile",
                warningZones: CreateHiddenOnesWarningZones(),
                memories: new[] { "Scouts speak of erased tracks and silent forest markers in the northwest." })
        };
    }

    private static void RegisterTutorialLocationAnchors(HexMapState map, IEnumerable<SpecialLocationState> locations)
    {
        foreach (var location in locations)
        {
            foreach (var coord in location.Anchor.Coords)
            {
                if (!map.TryGetTile(coord, out var tile) || tile == null)
                {
                    throw new InvalidOperationException($"Tutorial location '{location.Id}' references missing hex '{coord}'.");
                }

                if (!string.IsNullOrWhiteSpace(tile.LocationId) &&
                    !string.Equals(tile.LocationId, location.Id, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Tutorial location '{location.Id}' conflicts with '{tile.LocationId}' at hex '{coord}'.");
                }

                map.SetTile(tile.WithLocation(location.Id));
            }
        }
    }

    private static void ApplyTutorialRouteTerrain(HexMapState map)
    {
        foreach (var coord in TutorialOldRoute)
        {
            var tile = map.GetTile(coord);
            var terrain = coord.Q <= 8 ? TerrainType.Coast : TerrainType.Grassland;
            map.SetTile(tile.WithTerrain(terrain, elevation: 1, isBlocked: false).WithRoad("old-survey-route"));
        }

        foreach (var coord in TutorialRavineRoute)
        {
            var tile = map.GetTile(coord);
            map.SetTile(tile.WithRiver("gray-ravine"));
        }
    }

    private static void ApplyTutorialRelations(
        IEnumerable<SpecialLocationState> locations,
        IEnumerable<FactionState> factions,
        TutorialCampaignDefinition? tutorial)
    {
        if (tutorial == null)
        {
            return;
        }

        var locationsById = locations.ToDictionary(location => location.Id, StringComparer.Ordinal);
        var factionIds = factions.Select(faction => faction.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var relation in tutorial.LocationRelations)
        {
            if (!locationsById.TryGetValue(relation.LocationId, out var location))
            {
                throw new InvalidOperationException($"Tutorial relation references unavailable location '{relation.LocationId}'.");
            }
            if (!factionIds.Contains(relation.FactionId))
            {
                throw new InvalidOperationException($"Tutorial relation references unavailable faction '{relation.FactionId}'.");
            }
            location.AddFactionRelation(new LocationFactionRelationState(relation.FactionId, relation.Kind, relation.ContextTags));
        }
    }

    private static void ApplyFactionTerritories(HexMapState map)
    {
        foreach (var coord in CreateCoastalPeopleTerritory(map.Bounds))
        {
            SetOwnerIfPlayable(map, coord, "coastal-people", overwriteExisting: false);
        }

        foreach (var coord in CreateBorderWardenTerritory(map.Bounds))
        {
            SetOwnerIfPlayable(map, coord, "border-wardens", overwriteExisting: true);
        }

        foreach (var coord in CreateBorderWardenWarningZones())
        {
            SetOwnerIfPlayable(map, coord, "border-wardens", overwriteExisting: true);
        }

        foreach (var coord in CreateHiddenOnesTerritory(map.Bounds))
        {
            SetOwnerIfPlayable(map, coord, "hidden-ones", overwriteExisting: true);
        }

        foreach (var coord in CreateHiddenOnesWarningZones())
        {
            SetOwnerIfPlayable(map, coord, "hidden-ones", overwriteExisting: true);
        }
    }

    private static IEnumerable<HexCoord> CreateCoastalPeopleTerritory(HexMapBounds bounds)
    {
        var coords = new HashSet<HexCoord>();
        AddRadius(coords, new HexCoord(8, 15), 3, bounds);
        AddRadius(coords, new HexCoord(6, 16), 2, bounds);
        return coords;
    }

    private static IEnumerable<HexCoord> CreateBorderWardenTerritory(HexMapBounds bounds)
    {
        var coords = new HashSet<HexCoord>();
        foreach (var center in new[] { new HexCoord(12, 16), new HexCoord(14, 17), new HexCoord(16, 17), new HexCoord(18, 16) })
        {
            AddRadius(coords, center, 3, bounds);
        }
        return coords;
    }

    private static IEnumerable<HexCoord> CreateHiddenOnesTerritory(HexMapBounds bounds)
    {
        var coords = new HashSet<HexCoord>();
        AddRadius(coords, ViewCoord(15, -13), 3, bounds);
        AddRadius(coords, ViewCoord(18, -12), 3, bounds);
        AddRadius(coords, ViewCoord(16, -10), 3, bounds);
        AddRadius(coords, ViewCoord(19, -9), 2, bounds);
        return coords;
    }

    private static void AddRadius(HashSet<HexCoord> coords, HexCoord center, int radius, HexMapBounds bounds)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = Math.Max(-radius, -dq - radius);
            var maxDr = Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var coord = new HexCoord(center.Q + dq, center.R + dr);
                if (bounds.Contains(coord))
                {
                    coords.Add(coord);
                }
            }
        }
    }

    private static void SetOwnerIfPlayable(HexMapState map, HexCoord coord, string ownerId, bool overwriteExisting)
    {
        if (!map.TryGetTile(coord, out var tile) || tile == null)
        {
            return;
        }

        if (tile.Terrain == TerrainType.Water)
        {
            return;
        }

        if (!overwriteExisting && !string.IsNullOrWhiteSpace(tile.OwnerId))
        {
            return;
        }

        map.SetTile(tile.WithOwner(ownerId));
    }

    private static IEnumerable<HexCoord> CreateBorderWardenWarningZones()
    {
        return new[]
        {
            new HexCoord(11, 16),
            new HexCoord(12, 16),
            new HexCoord(12, 17),
            new HexCoord(13, 17)
        };
    }

    private static IEnumerable<HexCoord> CreateHiddenOnesWarningZones()
    {
        return new[]
        {
            ViewCoord(16, -13),
            ViewCoord(17, -12),
            ViewCoord(18, -11),
            ViewCoord(19, -10)
        };
    }

    private static HexCoord BaseCoord()
    {
        return ViewCoord(-19, 0);
    }

    private static IEnumerable<HexCoord> ViewPath(params (int Q, int R)[] coords)
    {
        foreach (var coord in coords)
        {
            yield return ViewCoord(coord.Q, coord.R);
        }
    }

    private static HexCoord ViewCoord(int q, int r)
    {
        var row = r + 15;
        var centeredColumn = q + (r - (r & 1)) / 2;
        var column = centeredColumn + 20;
        return new HexCoord(column, row);
    }

    private static HexCoord ToCenteredViewCoord(HexCoord coord, HexMapBounds bounds)
    {
        var centeredRow = coord.R - bounds.Height / 2;
        var centeredColumn = coord.Q - bounds.Width / 2;
        var q = centeredColumn - (centeredRow - (centeredRow & 1)) / 2;
        return new HexCoord(q, centeredRow);
    }

    private static TerrainType PickTerrain(HexCoord view)
    {
        var water = WaterStrength(view);
        var mountain = MountainStrength(view);
        var forest = ForestStrength(view);
        var dry = DryPlainsStrength(view);

        if (water > 0.74)
        {
            return TerrainType.Water;
        }

        if (water > 0.52)
        {
            return TerrainType.Coast;
        }

        if (mountain > 0.88)
        {
            return Hash01(view.Q, view.R, 701) > 0.72 ? TerrainType.Snow : TerrainType.Mountain;
        }

        if (mountain > 0.64)
        {
            return TerrainType.Mountain;
        }

        if (mountain > 0.42)
        {
            return TerrainType.Hills;
        }

        if (forest > 0.58)
        {
            return TerrainType.Forest;
        }

        if (forest > 0.43)
        {
            return TerrainType.Hills;
        }

        if (dry > 0.68)
        {
            return TerrainType.DryPlains;
        }

        return TerrainType.Grassland;
    }

    private static double WaterStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, 13.5, 4.0, 5.2, 3.6));
        strength = Math.Max(strength, Blob(view, -14.0, -5.0, 4.2, 3.4));
        strength = Math.Max(strength, Blob(view, 16.0, -8.0, 5.0, 4.2));
        strength = Math.Max(strength, Blob(view, -6.0, -11.5, 4.4, 2.8));
        strength = Math.Max(strength, Blob(view, 6.0, 10.5, 3.8, 2.6));
        return strength + (Hash01(view.Q, view.R, 91) - 0.5) * 0.18;
    }

    private static double MountainStrength(HexCoord view)
    {
        if (view.Q < -16 || view.Q > 15)
        {
            return 0.0;
        }

        var ridge = 7.2 - view.Q * 0.18 + Math.Sin((view.Q + 4.0) * 0.55) * 1.0;
        var distance = Math.Abs(view.R - ridge);
        var pass = Math.Abs(view.Q + 4) < 1.25 || Math.Abs(view.Q - 7) < 1.1 ? 0.24 : 0.0;
        return Math.Max(0.0, 1.0 - distance / 3.1 + (Hash01(view.Q, view.R, 311) - 0.5) * 0.24 - pass);
    }

    private static double ForestStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, -9.0, 2.5, 4.8, 3.0));
        strength = Math.Max(strength, Blob(view, -15.0, 9.0, 3.4, 2.8));
        strength = Math.Max(strength, Blob(view, 8.0, -3.5, 4.0, 3.2));
        strength = Math.Max(strength, Blob(view, 12.0, 9.0, 3.8, 2.8));
        strength = Math.Max(strength, Blob(view, -2.0, -8.0, 3.4, 2.6));
        return strength + (Hash01(view.Q, view.R, 173) - 0.5) * 0.16;
    }

    private static double DryPlainsStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, -7.0, -2.0, 4.5, 3.4));
        strength = Math.Max(strength, Blob(view, 4.0, -6.5, 5.0, 3.6));
        strength = Math.Max(strength, Blob(view, 12.0, 0.0, 3.6, 2.8));
        return strength + (Hash01(view.Q, view.R, 419) - 0.5) * 0.12;
    }

    private static double Blob(HexCoord view, double centerQ, double centerR, double radiusQ, double radiusR)
    {
        var q = (view.Q - centerQ) / radiusQ;
        var r = (view.R - centerR) / radiusR;
        return Math.Max(0.0, 1.0 - Math.Sqrt(q * q + r * r));
    }

    private static double Hash01(int q, int r, int seed)
    {
        unchecked
        {
            var n = q * 374761393 + r * 668265263 + seed * 2147483647;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647.0;
        }
    }

    private static IEnumerable<ExpeditionMemberState> CreateTutorialMembers()
    {
        return new[]
        {
            new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("scout-2", "Tovin", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("guard-1", "Bram", ExpeditionMemberRole.Guard),
            new ExpeditionMemberState("guard-2", "Ilyra", ExpeditionMemberRole.Guard, ExpeditionMemberStatus.Injured),
            new ExpeditionMemberState("carrier-1", "Nessa", ExpeditionMemberRole.Carrier),
            new ExpeditionMemberState("carrier-2", "Oren", ExpeditionMemberRole.Carrier, ExpeditionMemberStatus.Exhausted),
            new ExpeditionMemberState("medic-1", "Sela", ExpeditionMemberRole.Medic),
            new ExpeditionMemberState("scholar-1", "Rook", ExpeditionMemberRole.Scholar)
        };
    }

    private static BaseUnitStockState CreateTutorialUnitStock()
    {
        // Porter stock reflects the pre-built Trägerunterkünfte; Soldaten grow when Baracken is built.
        var stock = new BaseUnitStockState();
        stock.Add(BaseUnitKind.Porter, 4);
        stock.Add(BaseUnitKind.Porter, 2, exhausted: true);
        stock.Add(BaseUnitKind.Soldier, 1);
        stock.Add(BaseUnitKind.Soldier, 1, exhausted: true);
        return stock;
    }

    private static IEnumerable<EvaluationItemState> CreateTutorialEvaluationItems()
    {
        return new[]
        {
            new EvaluationItemState("eval-pfaehle", "Geschnitzte Pfähle", "Feld 14 / 08 · Expedition 1", 1, 3,
                "Die Pfähle markieren das Revier der Grenzwächter. Ihr Betreten gilt als Provokation — Umgehung oder Tribut empfohlen.",
                progressDays: 1),
            new EvaluationItemState("eval-saat", "Fremde Saatkörner", "Verlassenes Lager · Jonas", 3, 4,
                "Die Saatkörner gedeihen selbst in kargem Boden. Eine verlässliche Nahrungsquelle für längere Züge."),
            new EvaluationItemState("eval-karte", "Bruchstück einer Karte", "Kammland-Nord · Mara", 4, 5,
                "Das Kartenfragment zeigt einen alten Pfad durch den Kamm nach Osten — er umgeht das Gebiet der Grenzwächter."),
            new EvaluationItemState("eval-metall", "Unbekanntes Metall", "Aschegilde · Handel", 5, 4,
                "Ein ungewöhnlich leichtes, hartes Metall. Die Aschegilde hätte sicher Interesse an der Quelle.")
        };
    }

    private static IEnumerable<BaseUpgradeState> CreateTutorialUpgrades()
    {
        const string medicine = "Medizin & Pflege";
        const string workshop = "Werkstatt";
        const string cartography = "Kartografie";
        const string supplies = "Vorräte";
        const string housing = "Unterkünfte";

        return new[]
        {
            new BaseUpgradeState("feldlazarett", medicine, "Feldlazarett", "Verwundete erholen sich zwischen den Expeditionen deutlich schneller.", 0, BaseUpgradeEffect.None, isBuilt: true),
            new BaseUpgradeState("kraeuterkunde", medicine, "Kräuterkunde", "Heilmittel aus lokal gesammelten Pflanzen — Heilen im Lager kostet weniger Wissen.", 4, BaseUpgradeEffect.CheaperHealing),
            new BaseUpgradeState("quarantaenezelt", medicine, "Quarantänezelt", "Verhindert die Ausbreitung von Krankheiten im Lager.", 5, BaseUpgradeEffect.None, new[] { "feldlazarett" }),

            new BaseUpgradeState("schmiede", workshop, "Schmiede", "Reparatur und Aufwertung von Ausrüstung im Lager.", 0, BaseUpgradeEffect.None, isBuilt: true),
            new BaseUpgradeState("gerberei", workshop, "Gerberei", "Fertigt Rüstungen und Behälter aus erbeuteten Häuten.", 3),
            new BaseUpgradeState("praezisionswerkbank", workshop, "Präzisionswerkbank", "Feinmechanik — schaltet fortgeschrittene Werkzeuge frei.", 6, BaseUpgradeEffect.None, new[] { "schmiede" }),

            new BaseUpgradeState("kartentisch", cartography, "Kartentisch", "Späher-Berichte werden genauer und decken mehr Felder auf.", 0, BaseUpgradeEffect.None, isBuilt: true),
            new BaseUpgradeState("signalturm", cartography, "Signalturm", "Erhöht die Reichweite ausgesandter Späher um ein Feld.", 5, BaseUpgradeEffect.ScoutRangePlus),
            new BaseUpgradeState("sternkarten", cartography, "Sternkarten", "Ermöglicht Nachtmärsche ohne Moralverlust.", 4),

            new BaseUpgradeState("vorratskeller", supplies, "Vorratskeller", "Erhöht die maximale Lagerkapazität für Rationen.", 0, BaseUpgradeEffect.HigherSupplyCap, isBuilt: true),
            new BaseUpgradeState("raeucherei", supplies, "Räucherei", "Konserviert Nahrung — Vorräte verderben langsamer.", 4, BaseUpgradeEffect.SlowerSpoilage),
            new BaseUpgradeState("brunnen", supplies, "Brunnen", "Sichere Wasserversorgung senkt das Krankheitsrisiko.", 5, BaseUpgradeEffect.None, new[] { "vorratskeller" }),

            new BaseUpgradeState("traegerunterkuenfte", housing, "Trägerunterkünfte", "Vergrößert den Pool verfügbarer Träger für Expeditionen.", 0, BaseUpgradeEffect.GrowPorterStock, isBuilt: true),
            new BaseUpgradeState("baracken", housing, "Baracken", "Erhöht die Zahl ausgebildeter Soldaten im Bestand.", 4, BaseUpgradeEffect.GrowSoldierStock),
            new BaseUpgradeState("ausbildungsplatz", housing, "Ausbildungsplatz", "Erschöpfte Einheiten erholen sich schneller zwischen den Zügen.", 5, BaseUpgradeEffect.None, new[] { "baracken" })
        };
    }

    private static IEnumerable<BaseMemberState> CreateTutorialRoster()
    {
        return new[]
        {
            new BaseMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout, ExpeditionMemberStatus.Available, level: 4,
                bio: "Kennt das Kammland wie ihre Westentasche. Läuft voraus, wo andere zögern.",
                skills: new[] { new MemberSkill("Wahrnehmung", 5), new MemberSkill("Ausdauer", 4), new MemberSkill("Tarnung", 4), new MemberSkill("Medizin", 2) },
                traits: new[] { "Ortskundig", "Nachtsichtig" },
                gear: new[] { new MemberGear("Werkzeug", "Fernglas"), new MemberGear("Waffe", "Leichter Bogen"), new MemberGear("Ausrüstung", "Kletterseil") }),
            new BaseMemberState("scout-2", "Tovin", ExpeditionMemberRole.Scout, ExpeditionMemberStatus.Available, level: 3,
                bio: "Jung und ungeduldig — aber niemand liest Spuren so schnell wie er.",
                skills: new[] { new MemberSkill("Wahrnehmung", 4), new MemberSkill("Ausdauer", 4), new MemberSkill("Tarnung", 3), new MemberSkill("Verhandlung", 2) },
                traits: new[] { "Flink", "Übermütig" },
                gear: new[] { new MemberGear("Waffe", "Wurfmesser"), new MemberGear("Werkzeug", "Kompass") }),
            new BaseMemberState("guard-1", "Bram", ExpeditionMemberRole.Guard, ExpeditionMemberStatus.Available, level: 3,
                bio: "Steht die erste und die letzte Wache. Schläft, sagt man, mit offenen Augen.",
                skills: new[] { new MemberSkill("Stärke", 4), new MemberSkill("Ausdauer", 4), new MemberSkill("Wahrnehmung", 3), new MemberSkill("Handwerk", 2) },
                traits: new[] { "Wachsam", "Sturköpfig" },
                gear: new[] { new MemberGear("Waffe", "Speer"), new MemberGear("Rüstung", "Lederharnisch") }),
            new BaseMemberState("guard-2", "Ilyra", ExpeditionMemberRole.Guard, ExpeditionMemberStatus.Injured, level: 3,
                bio: "Ruhig im Gefecht, unerschütterlich im Rückzug. Zahlt jeden Sieg mit einer Narbe.",
                skills: new[] { new MemberSkill("Stärke", 4), new MemberSkill("Ausdauer", 3), new MemberSkill("Wahrnehmung", 3), new MemberSkill("Handwerk", 1) },
                traits: new[] { "Entschlossen", "Narbig" },
                gear: new[] { new MemberGear("Waffe", "Kurzschwert"), new MemberGear("Rüstung", "Rundschild") }),
            new BaseMemberState("carrier-1", "Nessa", ExpeditionMemberRole.Carrier, ExpeditionMemberStatus.Available, level: 2,
                bio: "Trägt die doppelte Last ohne Klage — solange die Rationen stimmen.",
                skills: new[] { new MemberSkill("Stärke", 5), new MemberSkill("Ausdauer", 5), new MemberSkill("Handwerk", 2), new MemberSkill("Wahrnehmung", 1) },
                traits: new[] { "Bärenkraft", "Langsam" },
                gear: new[] { new MemberGear("Ausrüstung", "Großer Rucksack") }),
            new BaseMemberState("carrier-2", "Oren", ExpeditionMemberRole.Carrier, ExpeditionMemberStatus.Exhausted, level: 2,
                bio: "Verlässlich bis zum Umfallen — und im Moment nah dran.",
                skills: new[] { new MemberSkill("Stärke", 4), new MemberSkill("Ausdauer", 4), new MemberSkill("Handwerk", 2), new MemberSkill("Wahrnehmung", 1) },
                traits: new[] { "Zäh", "Wortkarg" },
                gear: new[] { new MemberGear("Ausrüstung", "Traggestell") }),
            new BaseMemberState("medic-1", "Sela", ExpeditionMemberRole.Medic, ExpeditionMemberStatus.Available, level: 5,
                bio: "Ruhige Hände, nüchterner Blick. Hat mehr Wunden genäht, als sie zählen mag.",
                skills: new[] { new MemberSkill("Medizin", 5), new MemberSkill("Wissen", 4), new MemberSkill("Wahrnehmung", 3), new MemberSkill("Ausdauer", 2) },
                traits: new[] { "Gelehrt", "Fürsorglich" },
                gear: new[] { new MemberGear("Werkzeug", "Arzttasche"), new MemberGear("Ausrüstung", "Kräuterbeutel") }),
            new BaseMemberState("scholar-1", "Rook", ExpeditionMemberRole.Scholar, ExpeditionMemberStatus.Available, level: 4,
                bio: "Liest jede Ruine wie ein offenes Buch — und vergisst darüber das Abendessen.",
                skills: new[] { new MemberSkill("Wissen", 5), new MemberSkill("Verhandlung", 3), new MemberSkill("Wahrnehmung", 3), new MemberSkill("Ausdauer", 2) },
                traits: new[] { "Belesen", "Zerstreut" },
                gear: new[] { new MemberGear("Werkzeug", "Feldbuch"), new MemberGear("Werkzeug", "Lupe") })
        };
    }
}
}
