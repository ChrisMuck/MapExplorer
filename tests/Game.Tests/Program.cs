using Game.Core;
using Game.App;

var tests = new HexCoordTests();
tests.RunAll();
var mapTests = new HexMapStateTests();
mapTests.RunAll();
var movementTests = new MovementCostServiceTests();
movementTests.RunAll();
var knowledgeTests = new KnowledgeServiceTests();
knowledgeTests.RunAll();
var gameStateTests = new GameStateTests();
gameStateTests.RunAll();
var moveCommandTests = new MoveExpeditionCommandTests();
moveCommandTests.RunAll();
var annotationTests = new MapAnnotationCommandTests();
annotationTests.RunAll();
var scoutMissionTests = new SendScoutMissionCommandTests();
scoutMissionTests.RunAll();
var endDayCommandTests = new EndDayCommandTests();
endDayCommandTests.RunAll();

Console.WriteLine("All Game.Tests checks passed.");

internal sealed class HexCoordTests
{
    public void RunAll()
    {
        NeighborOffsetsMatchAxialDirections();
        OppositeDirectionsReturnToOrigin();
        DistanceUsesCubeCoordinateLength();
        HexCoordsCanBeDictionaryKeys();
        RectangularBoundsContainExpectedCoords();
    }

    private static void NeighborOffsetsMatchAxialDirections()
    {
        var origin = HexCoord.Zero;

        AssertEqual(new HexCoord(1, 0), origin.Neighbor(HexDirection.East), "East neighbor");
        AssertEqual(new HexCoord(1, -1), origin.Neighbor(HexDirection.NorthEast), "NorthEast neighbor");
        AssertEqual(new HexCoord(0, -1), origin.Neighbor(HexDirection.NorthWest), "NorthWest neighbor");
        AssertEqual(new HexCoord(-1, 0), origin.Neighbor(HexDirection.West), "West neighbor");
        AssertEqual(new HexCoord(-1, 1), origin.Neighbor(HexDirection.SouthWest), "SouthWest neighbor");
        AssertEqual(new HexCoord(0, 1), origin.Neighbor(HexDirection.SouthEast), "SouthEast neighbor");
        AssertEqual(6, origin.Neighbors().Count, "Neighbor count");
    }

    private static void OppositeDirectionsReturnToOrigin()
    {
        foreach (var direction in Enum.GetValues<HexDirection>())
        {
            var destination = HexCoord.Zero.Neighbor(direction).Neighbor(direction.Opposite());
            AssertEqual(HexCoord.Zero, destination, $"Opposite direction for {direction}");
        }
    }

    private static void DistanceUsesCubeCoordinateLength()
    {
        AssertEqual(0, HexCoord.Zero.DistanceTo(HexCoord.Zero), "Zero distance");
        AssertEqual(1, HexCoord.Zero.DistanceTo(new HexCoord(1, 0)), "Adjacent distance");
        AssertEqual(2, new HexCoord(0, 0).DistanceTo(new HexCoord(2, -1)), "Diagonal-ish distance");
        AssertEqual(7, new HexCoord(-2, 4).DistanceTo(new HexCoord(3, -3)), "Long distance");
    }

    private static void HexCoordsCanBeDictionaryKeys()
    {
        var tiles = new Dictionary<HexCoord, string>
        {
            [new HexCoord(4, 7)] = "forest"
        };

        AssertTrue(tiles.ContainsKey(new HexCoord(4, 7)), "Dictionary contains equivalent coord");
        AssertEqual("forest", tiles[new HexCoord(4, 7)], "Dictionary lookup with equivalent coord");
    }

    private static void RectangularBoundsContainExpectedCoords()
    {
        var bounds = new HexMapBounds(40, 30);

        AssertTrue(bounds.Contains(new HexCoord(0, 0)), "Bounds contain origin");
        AssertTrue(bounds.Contains(new HexCoord(39, 29)), "Bounds contain far corner");
        AssertFalse(bounds.Contains(new HexCoord(40, 29)), "Bounds reject q outside");
        AssertFalse(bounds.Contains(new HexCoord(39, 30)), "Bounds reject r outside");
        AssertEqual(1200, bounds.AllCoords().Count(), "Bounds coordinate count");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }
}

internal sealed class MovementCostServiceTests
{
    public void RunAll()
    {
        ClearTerrainCostsMatchInitialRules();
        DifficultTerrainCostsMatchInitialRules();
        RoadsReduceCostButNeverBelowOne();
        WaterIsBlockedForLandMovement();
        ExplicitBlockedFlagPreventsEntry();
        RequiredCostThrowsForBlockedTiles();
    }

    private static void ClearTerrainCostsMatchInitialRules()
    {
        var service = new MovementCostService();

        AssertCost(1, service.GetEntryCost(Tile(TerrainType.Grassland)), "Grassland cost");
        AssertCost(1, service.GetEntryCost(Tile(TerrainType.Coast)), "Coast cost");
        AssertCost(1, service.GetEntryCost(Tile(TerrainType.DryPlains)), "Dry plains cost");
        AssertCost(1, service.GetEntryCost(Tile(TerrainType.Desert)), "Desert cost");
    }

    private static void DifficultTerrainCostsMatchInitialRules()
    {
        var service = new MovementCostService();

        AssertCost(2, service.GetEntryCost(Tile(TerrainType.Forest)), "Forest cost");
        AssertCost(2, service.GetEntryCost(Tile(TerrainType.Hills)), "Hills cost");
        AssertCost(3, service.GetEntryCost(Tile(TerrainType.Swamp)), "Swamp cost");
        AssertCost(3, service.GetEntryCost(Tile(TerrainType.Mountain)), "Mountain cost");
        AssertCost(3, service.GetEntryCost(Tile(TerrainType.Snow)), "Snow cost");
    }

    private static void RoadsReduceCostButNeverBelowOne()
    {
        var service = new MovementCostService();

        AssertCost(1, service.GetEntryCost(Tile(TerrainType.Grassland).WithRoad("road-a")), "Road on grassland");
        AssertCost(1, service.GetEntryCost(Tile(TerrainType.Forest).WithRoad("road-b")), "Road through forest");
        AssertCost(2, service.GetEntryCost(Tile(TerrainType.Swamp).WithRoad("road-c")), "Road through swamp");
    }

    private static void WaterIsBlockedForLandMovement()
    {
        var service = new MovementCostService();
        var result = service.GetEntryCost(Tile(TerrainType.Water));

        AssertFalse(result.CanEnter, "Water is blocked");
        AssertEqual(0, result.Cost, "Blocked water cost");
        AssertTrue(result.Reason != null && result.Reason.Contains("Water"), "Water blocked reason");
    }

    private static void ExplicitBlockedFlagPreventsEntry()
    {
        var service = new MovementCostService();
        var result = service.GetEntryCost(new HexTileState(HexCoord.Zero, TerrainType.Grassland, isBlocked: true));

        AssertFalse(result.CanEnter, "Explicit blocked flag");
        AssertEqual(0, result.Cost, "Explicit blocked cost");
    }

    private static void RequiredCostThrowsForBlockedTiles()
    {
        var service = new MovementCostService();

        AssertThrows<InvalidOperationException>(
            () => service.GetRequiredEntryCost(Tile(TerrainType.Water)),
            "Required cost for blocked tile");
    }

    private static HexTileState Tile(TerrainType terrain)
    {
        return new HexTileState(HexCoord.Zero, terrain);
    }

    private static void AssertCost(int expected, MovementCostResult result, string message)
    {
        AssertTrue(result.CanEnter, message + " enterable");
        AssertEqual(expected, result.Cost, message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected {typeof(TException).Name}.");
    }
}

internal sealed class KnowledgeServiceTests
{
    public void RunAll()
    {
        RevealConfirmsOriginAndAdjacentTiles();
        RevealReportsOuterVisibleTiles();
        RevealDoesNotDowngradeConfirmedKnowledge();
        MountainsBlockSightToTilesBehindThem();
        HillsExtendReportedSightRange();
    }

    private static void RevealConfirmsOriginAndAdjacentTiles()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        var knowledge = new KnowledgeState();
        var origin = new HexCoord(2, 2);

        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);

        AssertEqual(KnowledgeLevel.Confirmed, knowledge.GetTileKnowledge(origin), "Origin confirmed");
        AssertEqual(KnowledgeLevel.Confirmed, knowledge.GetTileKnowledge(new HexCoord(3, 2)), "Adjacent tile confirmed");
    }

    private static void RevealReportsOuterVisibleTiles()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        var knowledge = new KnowledgeState();

        new KnowledgeService().RevealFromExpedition(map, knowledge, new HexCoord(2, 2));

        AssertEqual(KnowledgeLevel.Reported, knowledge.GetTileKnowledge(new HexCoord(4, 2)), "Distance two tile reported");
    }

    private static void RevealDoesNotDowngradeConfirmedKnowledge()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        var knowledge = new KnowledgeState();
        var outerTile = new HexCoord(4, 2);
        knowledge.SetTileKnowledge(outerTile, KnowledgeLevel.Confirmed);

        new KnowledgeService().RevealFromExpedition(map, knowledge, new HexCoord(2, 2));

        AssertEqual(KnowledgeLevel.Confirmed, knowledge.GetTileKnowledge(outerTile), "Confirmed knowledge is not downgraded to reported");
    }

    private static void MountainsBlockSightToTilesBehindThem()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        map.SetTile(new HexTileState(new HexCoord(1, 0), TerrainType.Mountain));
        var knowledge = new KnowledgeState();

        new KnowledgeService().RevealFromExpedition(map, knowledge, HexCoord.Zero);

        AssertEqual(KnowledgeLevel.Confirmed, knowledge.GetTileKnowledge(new HexCoord(1, 0)), "Blocking mountain itself is visible");
        AssertEqual(KnowledgeLevel.Unknown, knowledge.GetTileKnowledge(new HexCoord(2, 0)), "Tile behind mountain stays unknown");
    }

    private static void HillsExtendReportedSightRange()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(6, 6), TerrainType.Grassland);
        var origin = new HexCoord(2, 2);
        map.SetTile(new HexTileState(origin, TerrainType.Hills));
        var knowledge = new KnowledgeState();

        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);

        AssertEqual(KnowledgeLevel.Reported, knowledge.GetTileKnowledge(new HexCoord(5, 2)), "Hill origin reports distance three tile");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }
}

internal sealed class GameStateTests
{
    public void RunAll()
    {
        TutorialGameInitializesSeparatedStateRoots();
        TutorialExpeditionHasExpectedStartingTeam();
        TutorialMapContainsBiomeVariety();
        TutorialWorldContainsPathsAndLocations();
        KnowledgeDoesNotExposeWorldTruthAutomatically();
        PlayerNotesAreSeparateFromKnowledgeAndWorldState();
        WorldDayCanAdvanceWithoutChangingExpeditionDay();
    }

    private static void TutorialGameInitializesSeparatedStateRoots()
    {
        var game = TutorialGameFactory.Create();

        AssertEqual(40, game.World.Map.Bounds.Width, "Tutorial map width");
        AssertEqual(30, game.World.Map.Bounds.Height, "Tutorial map height");
        AssertEqual(new HexCoord(3, 15), game.Base.Location, "Tutorial base location");
        AssertEqual(game.Base.Location, game.Expedition.Position, "Expedition starts at base");
        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(game.Base.Location), "Base starts confirmed");
        AssertEqual(TerrainType.Coast, game.World.Map.GetTile(game.Base.Location).Terrain, "Base tile terrain");
    }

    private static void TutorialExpeditionHasExpectedStartingTeam()
    {
        var game = new GameApplication().CreateTutorialGame();

        AssertEqual(1, game.Expedition.ExpeditionNumber, "Expedition number");
        AssertEqual(8, game.Expedition.Members.Count, "Tutorial member count");
        AssertEqual(2, CountRole(game, ExpeditionMemberRole.Scout), "Scout count");
        AssertEqual(2, CountRole(game, ExpeditionMemberRole.Guard), "Guard count");
        AssertEqual(2, CountRole(game, ExpeditionMemberRole.Carrier), "Carrier count");
        AssertEqual(1, CountRole(game, ExpeditionMemberRole.Medic), "Medic count");
        AssertEqual(1, CountRole(game, ExpeditionMemberRole.Scholar), "Scholar count");
        AssertEqual(0, CountRole(game, ExpeditionMemberRole.Engineer), "Engineer count");
        AssertEqual(20, game.Expedition.Supplies, "Starting supplies");
        AssertEqual(3, game.Expedition.Medicine, "Starting medicine");
        AssertEqual(70, game.Expedition.Morale, "Starting morale");
    }

    private static void TutorialMapContainsBiomeVariety()
    {
        var game = TutorialGameFactory.Create();

        AssertTrue(CountTerrain(game, TerrainType.Water) + CountTerrain(game, TerrainType.Coast) > 20, "Tutorial map has water/coast");
        AssertTrue(CountTerrain(game, TerrainType.Forest) > 15, "Tutorial map has forests");
        AssertTrue(CountTerrain(game, TerrainType.Hills) > 20, "Tutorial map has hills");
        AssertTrue(CountTerrain(game, TerrainType.Mountain) + CountTerrain(game, TerrainType.Snow) > 20, "Tutorial map has mountains");
        AssertTrue(CountTerrain(game, TerrainType.Grassland) < game.World.Map.Count - 100, "Tutorial map is not mostly only grassland");
    }

    private static void TutorialWorldContainsPathsAndLocations()
    {
        var game = TutorialGameFactory.Create();

        AssertEqual(2, CountPathKind(game, WorldPathKind.River), "Tutorial river count");
        AssertEqual(3, CountPathKind(game, WorldPathKind.Road), "Tutorial road count");
        AssertEqual(1, CountPathKind(game, WorldPathKind.TerritoryBorder), "Tutorial border count");
        AssertEqual(1, CountPathKind(game, WorldPathKind.Wall), "Tutorial wall count");
        AssertTrue(game.World.Paths.All(path => path.Coords.Count >= 2), "All tutorial paths have at least two coords");
        AssertTrue(game.World.Paths.SelectMany(path => path.Coords).All(coord => game.World.Map.Bounds.Contains(coord)), "All tutorial path coords are in bounds");

        AssertTrue(game.World.Locations.Any(location => location.Kind == LocationKind.BaseCamp), "Tutorial has base camp location");
        AssertTrue(game.World.Locations.Count(location => location.Kind == LocationKind.Settlement) >= 5, "Tutorial has settlements");
        AssertTrue(game.World.Locations.Any(location => location.Kind == LocationKind.Watchtower), "Tutorial has watchtower");
        AssertTrue(game.World.Locations.Any(location => location.Kind == LocationKind.Mine), "Tutorial has mine");
        AssertTrue(game.World.Locations.All(location => game.World.Map.Bounds.Contains(location.Coord)), "All tutorial locations are in bounds");
    }

    private static void KnowledgeDoesNotExposeWorldTruthAutomatically()
    {
        var game = TutorialGameFactory.Create();
        var mountainCoord = new HexCoord(9, 14);

        AssertEqual(TerrainType.Mountain, game.World.Map.GetTile(mountainCoord).Terrain, "World truth has mountain");
        AssertEqual(KnowledgeLevel.Unknown, game.Knowledge.GetTileKnowledge(mountainCoord), "Knowledge starts unknown far away");

        game.Knowledge.SetTileKnowledge(mountainCoord, KnowledgeLevel.Reported);

        AssertEqual(TerrainType.Mountain, game.World.Map.GetTile(mountainCoord).Terrain, "World truth remains terrain");
        AssertEqual(KnowledgeLevel.Reported, game.Knowledge.GetTileKnowledge(mountainCoord), "Knowledge can store report");
    }

    private static void PlayerNotesAreSeparateFromKnowledgeAndWorldState()
    {
        var game = TutorialGameFactory.Create();
        var coord = new HexCoord(12, 15);

        game.PlayerNotes.AddMarker(new PlayerMapMarkerState("marker-1", coord, PlayerMapMarkerKind.Question, "Broken bridge?"));
        game.PlayerNotes.AddNote(new PlayerMapNoteState("note-1", coord, "Need an engineer later."));

        AssertEqual(1, game.PlayerNotes.Markers.Count, "Marker count");
        AssertEqual(1, game.PlayerNotes.Notes.Count, "Note count");
        AssertEqual(KnowledgeLevel.Unknown, game.Knowledge.GetTileKnowledge(coord), "Note does not reveal knowledge");
        AssertEqual("broken-ravine", game.World.Map.GetTile(coord).LocationId, "Note does not mutate world location");
    }

    private static void WorldDayCanAdvanceWithoutChangingExpeditionDay()
    {
        var game = TutorialGameFactory.Create();

        game.World.AdvanceDays(3);

        AssertEqual(4, game.World.WorldDay, "World day advanced");
        AssertEqual(1, game.Expedition.ExpeditionDay, "Expedition day unchanged");
    }

    private static int CountRole(GameState game, ExpeditionMemberRole role)
    {
        return game.Expedition.Members.Count(member => member.Role == role);
    }

    private static int CountTerrain(GameState game, TerrainType terrain)
    {
        return game.World.Map.Tiles.Count(tile => tile.Terrain == terrain);
    }

    private static int CountPathKind(GameState game, WorldPathKind kind)
    {
        return game.World.Paths.Count(path => path.Kind == kind);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }
}

internal sealed class MoveExpeditionCommandTests
{
    public void RunAll()
    {
        AdjacentEnterableTileMovesExpeditionAndSpendsMovementPoints();
        NonAdjacentTileIsRejected();
        WaterTileIsRejected();
        MovementReportsNearbyTilesWithoutRevealingTruth();
        MovementRequiresEnoughMovementPoints();
    }

    private static void AdjacentEnterableTileMovesExpeditionAndSpendsMovementPoints()
    {
        var game = CreateMovementTestGame(TerrainType.Grassland);
        var command = new MoveExpeditionCommand(new MovementCostService());
        var destination = new HexCoord(1, 0);

        var result = command.Execute(game, destination);

        AssertTrue(result.Success, "Move succeeds");
        AssertEqual(HexCoord.Zero, result.From, "Move result from");
        AssertEqual(destination, result.To, "Move result to");
        AssertEqual(1, result.Cost, "Move cost");
        AssertEqual(destination, game.Expedition.Position, "Expedition position");
        AssertEqual(2, game.Expedition.MovementPoints, "Movement points spent");
        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(destination), "Destination confirmed");
    }

    private static void NonAdjacentTileIsRejected()
    {
        var game = CreateMovementTestGame(TerrainType.Grassland);
        var command = new MoveExpeditionCommand(new MovementCostService());

        var result = command.Execute(game, new HexCoord(2, 0));

        AssertFalse(result.Success, "Non-adjacent rejected");
        AssertEqual(HexCoord.Zero, game.Expedition.Position, "Position unchanged");
    }

    private static void WaterTileIsRejected()
    {
        var game = CreateMovementTestGame(TerrainType.Water);
        var command = new MoveExpeditionCommand(new MovementCostService());

        var result = command.Execute(game, new HexCoord(1, 0));

        AssertFalse(result.Success, "Water rejected");
        AssertEqual(HexCoord.Zero, game.Expedition.Position, "Water rejection position unchanged");
    }

    private static void MovementReportsNearbyTilesWithoutRevealingTruth()
    {
        var game = CreateMovementTestGame(TerrainType.Grassland);
        var command = new MoveExpeditionCommand(new MovementCostService());

        command.Execute(game, new HexCoord(1, 0));

        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(new HexCoord(2, 0)), "Adjacent neighbor confirmed");
        AssertEqual(KnowledgeLevel.Reported, game.Knowledge.GetTileKnowledge(new HexCoord(3, 0)), "Outer visible tile reported");
        AssertEqual(TerrainType.Mountain, game.World.Map.GetTile(new HexCoord(3, 0)).Terrain, "World truth remains separate");
    }

    private static void MovementRequiresEnoughMovementPoints()
    {
        var bounds = new HexMapBounds(3, 3);
        var map = HexMapState.CreateFilled(bounds, TerrainType.Grassland);
        map.SetTile(new HexTileState(new HexCoord(1, 0), TerrainType.Swamp));
        var game = CreateMovementTestGame(map, movementPoints: 2);
        var command = new MoveExpeditionCommand(new MovementCostService());

        var result = command.Execute(game, new HexCoord(1, 0));

        AssertFalse(result.Success, "Insufficient movement rejected");
        AssertEqual(2, game.Expedition.MovementPoints, "Movement points unchanged");
    }

    private static GameState CreateMovementTestGame(TerrainType destinationTerrain)
    {
        var bounds = new HexMapBounds(4, 3);
        var map = HexMapState.CreateFilled(bounds, TerrainType.Grassland);
        map.SetTile(new HexTileState(new HexCoord(1, 0), destinationTerrain));
        map.SetTile(new HexTileState(new HexCoord(3, 0), TerrainType.Mountain));
        return CreateMovementTestGame(map, movementPoints: 3);
    }

    private static GameState CreateMovementTestGame(HexMapState map, int movementPoints)
    {
        var world = new WorldState(map);
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(HexCoord.Zero, KnowledgeLevel.Confirmed);
        var expedition = new ExpeditionState(
            1,
            HexCoord.Zero,
            new[] { new ExpeditionMemberState("scout", "Scout", ExpeditionMemberRole.Scout) },
            movementPoints: movementPoints);
        return new GameState(world, knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }
}

internal sealed class MapAnnotationCommandTests
{
    public void RunAll()
    {
        AddNoteStoresPlayerTextWithoutChangingWorldKnowledge();
        AddMarkerStoresFactionMarkerMetadata();
        UnknownTilesCannotBeAnnotated();
    }

    private static void AddNoteStoresPlayerTextWithoutChangingWorldKnowledge()
    {
        var game = CreateAnnotationTestGame(KnowledgeLevel.Confirmed);
        var command = new AddMapNoteCommand();
        var coord = new HexCoord(1, 1);

        var result = command.Execute(game, coord, "Possible ford near the river.");

        AssertTrue(result.Success, "Note added");
        AssertEqual(1, game.PlayerNotes.Notes.Count, "Note count");
        AssertEqual("Possible ford near the river.", game.PlayerNotes.Notes[0].Text, "Note text");
        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(coord), "Note does not alter knowledge");
    }

    private static void AddMarkerStoresFactionMarkerMetadata()
    {
        var game = CreateAnnotationTestGame(KnowledgeLevel.Reported);
        var command = new AddMapMarkerCommand();
        var coord = new HexCoord(1, 1);

        var result = command.Execute(game, coord, PlayerMapMarkerKind.FactionWarning, "Border Warden sign", "border-wardens");

        AssertTrue(result.Success, "Faction marker added");
        AssertEqual(1, game.PlayerNotes.Markers.Count, "Marker count");
        AssertEqual(PlayerMapMarkerKind.FactionWarning, game.PlayerNotes.Markers[0].Kind, "Marker kind");
        AssertEqual("border-wardens", game.PlayerNotes.Markers[0].FactionId, "Marker faction id");
    }

    private static void UnknownTilesCannotBeAnnotated()
    {
        var game = CreateAnnotationTestGame(KnowledgeLevel.Unknown);
        var markerCommand = new AddMapMarkerCommand();
        var noteCommand = new AddMapNoteCommand();
        var coord = new HexCoord(1, 1);

        var markerResult = markerCommand.Execute(game, coord, PlayerMapMarkerKind.Question, "Unknown thing?");
        var noteResult = noteCommand.Execute(game, coord, "Unknown note");

        AssertFalse(markerResult.Success, "Unknown marker rejected");
        AssertFalse(noteResult.Success, "Unknown note rejected");
        AssertEqual(0, game.PlayerNotes.Markers.Count, "No marker added");
        AssertEqual(0, game.PlayerNotes.Notes.Count, "No note added");
    }

    private static GameState CreateAnnotationTestGame(KnowledgeLevel knowledgeLevel)
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var knowledge = new KnowledgeState();
        if (knowledgeLevel != KnowledgeLevel.Unknown)
        {
            knowledge.SetTileKnowledge(new HexCoord(1, 1), knowledgeLevel);
        }

        var expedition = new ExpeditionState(
            1,
            HexCoord.Zero,
            new[] { new ExpeditionMemberState("scout", "Scout", ExpeditionMemberRole.Scout) },
            movementPoints: 4,
            supplies: 8,
            medicine: 1,
            morale: 70,
            capacity: 10);

        return new GameState(new WorldState(map), knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }
}

internal sealed class SendScoutMissionCommandTests
{
    public void RunAll()
    {
        ValidScoutMissionAssignsScoutAndStoresMission();
        NonScoutMemberCannotBeSent();
        AssignedScoutCannotBeSentAgain();
        ScoutMissionDurationMustBeAllowed();
        GameApplicationCanSendTutorialScoutMission();
    }

    private static void ValidScoutMissionAssignsScoutAndStoresMission()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "scout-1" },
            HexDirection.NorthEast,
            2,
            ScoutMissionFocus.FactionSigns,
            ScoutMissionBehavior.Cautious);

        AssertTrue(result.Success, "Scout mission succeeds");
        AssertEqual(1, game.Expedition.ScoutMissions.Count, "Mission count");
        AssertEqual(ExpeditionMemberStatus.Assigned, game.Expedition.FindMember("scout-1")!.Status, "Scout is assigned");
        AssertEqual(3, result.Mission!.ExpectedReturnWorldDay, "Expected return day");
        AssertEqual(HexDirection.NorthEast, result.Mission.Direction, "Mission direction");
        AssertEqual(ScoutMissionFocus.FactionSigns, result.Mission.Focus, "Mission focus");
    }

    private static void NonScoutMemberCannotBeSent()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "guard-1" },
            HexDirection.East,
            1,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Balanced);

        AssertFalse(result.Success, "Non-scout rejected");
        AssertEqual(0, game.Expedition.ScoutMissions.Count, "No mission added");
        AssertEqual(ExpeditionMemberStatus.Available, game.Expedition.FindMember("guard-1")!.Status, "Guard remains available");
    }

    private static void AssignedScoutCannotBeSentAgain()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        command.Execute(game, new[] { "scout-1" }, HexDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Balanced);
        var result = command.Execute(game, new[] { "scout-1" }, HexDirection.West, 1, ScoutMissionFocus.Route, ScoutMissionBehavior.Bold);

        AssertFalse(result.Success, "Assigned scout rejected");
        AssertEqual(1, game.Expedition.ScoutMissions.Count, "No second mission added");
    }

    private static void ScoutMissionDurationMustBeAllowed()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "scout-1" },
            HexDirection.East,
            6,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Balanced);

        AssertFalse(result.Success, "Too long mission rejected");
        AssertEqual(0, game.Expedition.ScoutMissions.Count, "No mission added");
    }

    private static void GameApplicationCanSendTutorialScoutMission()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();

        var result = app.SendScoutMission(
            game,
            new[] { "scout-1", "scout-2" },
            HexDirection.NorthWest,
            3,
            ScoutMissionFocus.Route,
            ScoutMissionBehavior.Cautious);

        AssertTrue(result.Success, "Application scout mission succeeds");
        AssertEqual(1, game.Expedition.ScoutMissions.Count, "Application mission count");
        AssertEqual(ExpeditionMemberStatus.Assigned, game.Expedition.FindMember("scout-1")!.Status, "First tutorial scout assigned");
        AssertEqual(ExpeditionMemberStatus.Assigned, game.Expedition.FindMember("scout-2")!.Status, "Second tutorial scout assigned");
    }

    private static GameState CreateScoutTestGame()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        var expedition = new ExpeditionState(
            1,
            new HexCoord(2, 2),
            new[]
            {
                new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout),
                new ExpeditionMemberState("scout-2", "Tovin", ExpeditionMemberRole.Scout),
                new ExpeditionMemberState("guard-1", "Bram", ExpeditionMemberRole.Guard)
            },
            movementPoints: 4,
            supplies: 8);

        return new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }
}

internal sealed class EndDayCommandTests
{
    public void RunAll()
    {
        EndDayAdvancesWorldAndExpeditionDay();
        EndDayResetsMovementPoints();
        EndDayConsumesSuppliesWithoutGoingBelowZero();
        GameApplicationCanEndTutorialDay();
        CautiousScoutReturnsWithReport();
        BalancedScoutCanBecomeOverdueThenReturn();
        BoldScoutCanReturnInjured();
        BoldRuinScoutCanGoMissing();
    }

    private static void EndDayAdvancesWorldAndExpeditionDay()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 1, maxMovementPoints: 4);
        var command = new EndDayCommand(suppliesPerDay: 2);

        var result = command.Execute(game);

        AssertTrue(result.Success, "End day succeeds");
        AssertEqual(2, game.World.WorldDay, "World day advanced");
        AssertEqual(2, game.Expedition.ExpeditionDay, "Expedition day advanced");
        AssertEqual(2, result.WorldDay, "Result world day");
        AssertEqual(2, result.ExpeditionDay, "Result expedition day");
    }

    private static void EndDayResetsMovementPoints()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 1, maxMovementPoints: 4);

        new EndDayCommand().Execute(game);

        AssertEqual(4, game.Expedition.MovementPoints, "Movement points reset");
    }

    private static void EndDayConsumesSuppliesWithoutGoingBelowZero()
    {
        var game = CreateEndDayTestGame(supplies: 1, movementPoints: 4, maxMovementPoints: 4);
        var result = new EndDayCommand(suppliesPerDay: 2).Execute(game);

        AssertEqual(0, game.Expedition.Supplies, "Supplies do not go below zero");
        AssertEqual(1, result.SuppliesConsumed, "Consumed available supplies only");
    }

    private static void GameApplicationCanEndTutorialDay()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var startSupplies = game.Expedition.Supplies;

        var result = app.EndDay(game);

        AssertTrue(result.Success, "Application end day succeeds");
        AssertEqual(2, game.World.WorldDay, "Application world day");
        AssertEqual(2, game.Expedition.ExpeditionDay, "Application expedition day");
        AssertEqual(game.Expedition.MaxMovementPoints, game.Expedition.MovementPoints, "Application movement reset");
        AssertEqual(startSupplies - 2, game.Expedition.Supplies, "Application supplies consumed");
    }

    private static void CautiousScoutReturnsWithReport()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, HexDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(1, result.ScoutResolutions.Count, "Scout resolution count");
        AssertEqual(ScoutMissionStatus.Returned, result.ScoutResolutions[0].Status, "Cautious scout returned");
        AssertEqual(ExpeditionMemberStatus.Available, game.Expedition.FindMember("scout")!.Status, "Returned scout available");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Scout report stored");
        AssertEqual(KnowledgeLevel.Reported, game.Knowledge.GetTileKnowledge(new HexCoord(1, 0)), "Scout report marks related hex reported");
    }

    private static void BalancedScoutCanBecomeOverdueThenReturn()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, HexDirection.East, 1, ScoutMissionFocus.Route, ScoutMissionBehavior.Balanced);

        var firstDay = new EndDayCommand().Execute(game);
        var statusAfterFirstDay = game.Expedition.FindMember("scout")!.Status;
        var secondDay = new EndDayCommand().Execute(game);

        AssertEqual(ScoutMissionStatus.Overdue, firstDay.ScoutResolutions[0].Status, "Balanced scout overdue first");
        AssertEqual(ExpeditionMemberStatus.Assigned, statusAfterFirstDay, "Scout remains unavailable while overdue");
        AssertEqual(ScoutMissionStatus.Returned, secondDay.ScoutResolutions[0].Status, "Overdue scout returns later");
        AssertEqual(ExpeditionMemberStatus.Available, game.Expedition.FindMember("scout")!.Status, "Late scout available after return");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Late scout report stored");
    }

    private static void BoldScoutCanReturnInjured()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, HexDirection.East, 1, ScoutMissionFocus.FactionSigns, ScoutMissionBehavior.Bold);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(ScoutMissionStatus.ReturnedInjured, result.ScoutResolutions[0].Status, "Bold scout injured");
        AssertEqual(ExpeditionMemberStatus.Injured, game.Expedition.FindMember("scout")!.Status, "Scout marked injured");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Injured scout report stored");
    }

    private static void BoldRuinScoutCanGoMissing()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, HexDirection.East, 1, ScoutMissionFocus.Ruins, ScoutMissionBehavior.Bold);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(ScoutMissionStatus.Missing, result.ScoutResolutions[0].Status, "Bold ruin scout missing");
        AssertEqual(ExpeditionMemberStatus.Missing, game.Expedition.FindMember("scout")!.Status, "Scout marked missing");
        AssertEqual(0, game.Knowledge.ScoutReports.Count, "Missing scout creates no report");
    }

    private static GameState CreateEndDayTestGame(int supplies, int movementPoints, int maxMovementPoints)
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(
            1,
            HexCoord.Zero,
            new[] { new ExpeditionMemberState("scout", "Scout", ExpeditionMemberRole.Scout) },
            movementPoints: movementPoints,
            maxMovementPoints: maxMovementPoints,
            supplies: supplies);
        return new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }
}

internal sealed class HexMapStateTests
{
    public void RunAll()
    {
        FilledMapCreatesOneTilePerCoordinate();
        TilesCanStoreTerrainElevationAndFeatureReferences();
        SetTileRejectsCoordinatesOutsideBounds();
        GetTileReportsMissingCoordinatesClearly();
        NeighborLookupReturnsOnlyTilesInsideMap();
    }

    private static void FilledMapCreatesOneTilePerCoordinate()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(40, 30), TerrainType.Grassland);

        AssertEqual(1200, map.Count, "Filled map tile count");
        AssertTrue(map.Contains(new HexCoord(0, 0)), "Filled map contains origin");
        AssertTrue(map.Contains(new HexCoord(39, 29)), "Filled map contains far corner");
        AssertFalse(map.Contains(new HexCoord(40, 29)), "Filled map rejects q outside");
        AssertEqual(TerrainType.Grassland, map.GetTile(new HexCoord(12, 8)).Terrain, "Filled terrain type");
    }

    private static void TilesCanStoreTerrainElevationAndFeatureReferences()
    {
        var coord = new HexCoord(3, 4);
        var map = HexMapState.CreateFilled(new HexMapBounds(8, 8), TerrainType.Grassland);
        var tile = new HexTileState(
            coord,
            TerrainType.Mountain,
            elevation: 4,
            isBlocked: true,
            roadId: "road-west-pass",
            riverId: "river-gray",
            locationId: "sealed-gate",
            ownerId: "border-wardens");

        map.SetTile(tile);
        var stored = map.GetTile(coord);

        AssertEqual(TerrainType.Mountain, stored.Terrain, "Stored terrain");
        AssertEqual(4, stored.Elevation, "Stored elevation");
        AssertTrue(stored.IsBlocked, "Stored blocked flag");
        AssertTrue(stored.HasRoad, "Stored road flag");
        AssertTrue(stored.HasRiver, "Stored river flag");
        AssertTrue(stored.HasLocation, "Stored location flag");
        AssertEqual("border-wardens", stored.OwnerId, "Stored owner");
    }

    private static void SetTileRejectsCoordinatesOutsideBounds()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(2, 2), TerrainType.Grassland);

        AssertThrows<ArgumentOutOfRangeException>(
            () => map.SetTile(new HexTileState(new HexCoord(2, 1), TerrainType.Water)),
            "Out-of-bounds SetTile");
    }

    private static void GetTileReportsMissingCoordinatesClearly()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(2, 2), TerrainType.Grassland);

        AssertThrows<KeyNotFoundException>(
            () => map.GetTile(new HexCoord(-1, 0)),
            "Missing GetTile");
    }

    private static void NeighborLookupReturnsOnlyTilesInsideMap()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);

        AssertEqual(2, map.GetNeighborTiles(new HexCoord(0, 0)).Count, "Corner neighbor count");
        AssertEqual(6, map.GetNeighborTiles(new HexCoord(1, 1)).Count, "Interior neighbor count");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected {typeof(TException).Name}.");
    }
}
