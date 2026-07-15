using Game.Core;
using Game.App;

try
{
var tests = new HexCoordTests();
tests.RunAll();
var mapTests = new HexMapStateTests();
mapTests.RunAll();
var movementTests = new MovementCostServiceTests();
movementTests.RunAll();
var knowledgeTests = new KnowledgeServiceTests();
knowledgeTests.RunAll();
var locationConditionKnowledgeTests = new LocationConditionKnowledgeTests();
locationConditionKnowledgeTests.RunAll();
var knowledgeRuntimeSnapshotTests = new KnowledgeRuntimeSnapshotTests();
knowledgeRuntimeSnapshotTests.RunAll();
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
var completeExpeditionTests = new CompleteExpeditionCommandTests();
completeExpeditionTests.RunAll();
var expeditionLifecycleTests = new ExpeditionLifecycleCommandTests();
expeditionLifecycleTests.RunAll();
var baseGameplayTests = new BaseGameplayCommandTests();
baseGameplayTests.RunAll();
var baseUpgradeTests = new BaseUpgradeCommandTests();
baseUpgradeTests.RunAll();
var evaluationQueueTests = new EvaluationQueueCommandTests();
evaluationQueueTests.RunAll();
var baseLoadoutTests = new BaseLoadoutCommandTests();
baseLoadoutTests.RunAll();
var archiveTests = new ArchiveCommandTests();
archiveTests.RunAll();
var inspectLocationTests = new InspectLocationCommandTests();
inspectLocationTests.RunAll();
var locationInteractionTests = new LocationInteractionFrameworkTests();
locationInteractionTests.RunAll();
var scenarioProfileInteractionTests = new ScenarioProfileInteractionTests();
scenarioProfileInteractionTests.RunAll();
var findingLifecycleTests = new FindingLifecycleTests();
findingLifecycleTests.RunAll();
var locationDataJsonTests = new LocationDataJsonTests();
locationDataJsonTests.RunAll();
var eventQueueTests = new EventQueueCommandTests();
eventQueueTests.RunAll();
var factionTests = new FactionPresenceTests();
factionTests.RunAll();
var crossSystemStateTests = new CrossSystemStateTests();
crossSystemStateTests.RunAll();
var simulationRuntimeStateTests = new SimulationRuntimeStateTests();
simulationRuntimeStateTests.RunAll();
var worldRuntimeSnapshotTests = new WorldRuntimeSnapshotTests();
worldRuntimeSnapshotTests.RunAll();
var scoutLocationSurroundingsTests = new ScoutLocationSurroundingsTests();
scoutLocationSurroundingsTests.RunAll();
var worldPhaseDataTests = new WorldPhaseDataTests();
worldPhaseDataTests.RunAll();
var worldProcessEffectTests = new WorldProcessEffectTests();
worldProcessEffectTests.RunAll();
var simulationSessionTests = new SimulationSessionTests();
simulationSessionTests.RunAll();
var simulationBatchRunnerTests = new SimulationBatchRunnerTests();
simulationBatchRunnerTests.RunAll();
var factionReactionDataTests = new FactionReactionDataTests();
factionReactionDataTests.RunAll();
var factionTerritorialPolicyTests = new FactionTerritorialPolicyTests();
factionTerritorialPolicyTests.RunAll();
var crossSystemIntegrationProofTests = new CrossSystemIntegrationProofTests();
crossSystemIntegrationProofTests.RunAll();
var crossSystemContentValidationTests = new CrossSystemContentValidationTests();
crossSystemContentValidationTests.RunAll();
var gameDataCatalogTests = new GameDataCatalogTests();
gameDataCatalogTests.RunAll();
var crossSystemAuthoringDataTests = new CrossSystemAuthoringDataTests();
crossSystemAuthoringDataTests.RunAll();
var archetypeFlowResolverTests = new ArchetypeFlowResolverTests();
archetypeFlowResolverTests.RunAll();
var worldGenerationPresetTests = new WorldGenerationPresetTests();
worldGenerationPresetTests.RunAll();
var worldGenBridgeTests = new WorldGenBridgeTests();
worldGenBridgeTests.RunAll();

Console.WriteLine("All Game.Tests checks passed.");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Game.Tests failed: {exception}");
    Environment.ExitCode = 1;
}

internal sealed class HexCoordTests
{
    public void RunAll()
    {
        NeighborOffsetsMatchAxialDirections();
        ScoutDirectionsCoverCompassOffsets();
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

    private static void ScoutDirectionsCoverCompassOffsets()
    {
        AssertEqual(new HexCoord(0, -1), ScoutDirection.North.ToScoutOffset(), "Scout north offset");
        AssertEqual(new HexCoord(1, -1), ScoutDirection.NorthEast.ToScoutOffset(), "Scout northeast offset");
        AssertEqual(new HexCoord(1, 0), ScoutDirection.East.ToScoutOffset(), "Scout east offset");
        AssertEqual(new HexCoord(1, 1), ScoutDirection.SouthEast.ToScoutOffset(), "Scout southeast offset");
        AssertEqual(new HexCoord(0, 1), ScoutDirection.South.ToScoutOffset(), "Scout south offset");
        AssertEqual(new HexCoord(-1, 1), ScoutDirection.SouthWest.ToScoutOffset(), "Scout southwest offset");
        AssertEqual(new HexCoord(-1, 0), ScoutDirection.West.ToScoutOffset(), "Scout west offset");
        AssertEqual(new HexCoord(-1, -1), ScoutDirection.NorthWest.ToScoutOffset(), "Scout northwest offset");
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

internal sealed class CrossSystemStateTests
{
    public void RunAll()
    {
        EvidenceStaysInKnowledgeStateAndCanBeUpdated();
        GeneratedFactionRelationsRemainRuntimeState();
        TriggersAndConsequencesArePersistedByWorldState();
    }

    private static void EvidenceStaysInKnowledgeStateAndCanBeUpdated()
    {
        var knowledge = new KnowledgeState();
        var evidence = new EvidenceState(
            "evidence-bridge-cuts",
            "structural-cuts",
            EvidenceSourceKind.LocationInspection,
            EvidenceKnowledgeState.Reported,
            "Mehrere TrÃ¤ger wurden gezielt entfernt.",
            subjectLocationId: "bridge-1",
            confidence: 55);

        AssertTrue(knowledge.AddEvidence(evidence), "First evidence item is stored");
        AssertFalse(knowledge.AddEvidence(evidence), "Evidence id is not duplicated");
        evidence.UpdateKnowledge(EvidenceKnowledgeState.Confirmed, 90);

        var stored = knowledge.FindEvidence("evidence-bridge-cuts");
        AssertTrue(stored != null, "Evidence can be found by stable id");
        AssertEqual(EvidenceKnowledgeState.Confirmed, stored!.KnowledgeState, "Evidence keeps player knowledge state");
        AssertEqual(90, stored.Confidence, "Evidence confidence can improve without changing World Truth");
    }

    private static void GeneratedFactionRelationsRemainRuntimeState()
    {
        var relation = new LocationFactionRelationState(
            "border-wardens",
            LocationFactionRelationKind.Watched,
            new[] { "deliberate-destruction", "quarantine" });
        var location = new SpecialLocationState(
            "bridge-1",
            LocationKind.BrokenRavine,
            HexCoord.Zero,
            "Broken Bridge",
            LocationAnchor.Point(HexCoord.Zero),
            archetypeId: "route-obstacle",
            variantId: "broken-bridge",
            factionRelations: new[] { relation });

        AssertTrue(location.FactionIds.Contains("border-wardens"), "Legacy linked-faction view includes generated relation faction");
        AssertEqual(1, location.FactionRelations.Count, "Generated relation is stored on location instance");
        AssertTrue(location.FactionRelations[0].HasContextTag("quarantine"), "Generated context tags are preserved");
    }

    private static void TriggersAndConsequencesArePersistedByWorldState()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(2, 2), TerrainType.Grassland);
        var world = new WorldState(map);
        var trigger = new WorldTriggerState(
            "trigger-1",
            "location-seal-broken",
            world.WorldDay,
            new[] { "open", "disturb" },
            sourceLocationId: "crypt-1");
        var consequence = new ScheduledConsequenceState(
            "consequence-1",
            "seal-broken",
            "crypt-1",
            dueWorldDay: world.WorldDay + 3,
            resolvedEffectIds: new[] { "create-local-hazard" });

        world.QueueWorldTrigger(trigger);
        world.ScheduleConsequence(consequence);
        world.AdvanceDays(3);

        AssertEqual(1, world.WorldTriggers.Count, "World stores neutral trigger instances");
        AssertEqual(1, world.ScheduledConsequences.Count, "World stores resolved consequence instances");
        AssertTrue(consequence.IsDue(world.WorldDay), "Consequence becomes due from world time without rerolling");
        trigger.MarkResolved();
        consequence.MarkApplied();
        AssertTrue(trigger.IsResolved, "Trigger can be marked resolved by world phase");
        AssertFalse(consequence.IsDue(world.WorldDay), "Applied consequence cannot run twice");
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

internal sealed class WorldGenBridgeTests
{
    public void RunAll()
    {
        GeneratedWorldBridgesToCoreWithoutLosingAnchors();
        SameRequestProducesSameCoreWorldLayout();
        TerritorialLocationsCanRemainUnclaimedAndCarryGeneratedEvidenceSeeds();
        GeneratedWorldCanStartAnExpedition();
        GeneratedFactionsReceiveStableHiddenSignatureProfiles();
        GeneratedWorldSeedsLaterSimulation();
        GeneratedSliceHasOptionalSoftConnectionsWithoutStaticFactionAssignment();
    }

    private static void GeneratedWorldBridgesToCoreWithoutLosingAnchors()
    {
        var result = new WorldGenBridge().Generate(Request());

        AssertTrue(result.World.Map.Contains(result.BaseLocation), "Generated base exists on bridged map");
        AssertTrue(result.World.Locations.Count > 0, "Generated specials bridge into Core locations");
        AssertTrue(result.Factions.Count == 3, "Generated factions bridge into Core faction state");
        foreach (var location in result.World.Locations)
        {
            foreach (var coord in location.Anchor.Coords)
            {
                AssertTrue(result.World.Map.Contains(coord), $"Location anchor {location.Id} exists on bridged map");
            }
        }
    }

    private static void SameRequestProducesSameCoreWorldLayout()
    {
        var bridge = new WorldGenBridge();
        var first = bridge.Generate(Request());
        var second = bridge.Generate(Request());

        AssertEqual(first.BaseLocation, second.BaseLocation, "Same request keeps generated base stable");
        AssertEqual(first.World.Locations.Count, second.World.Locations.Count, "Same request keeps location count stable");
        AssertEqual(first.World.Paths.Count, second.World.Paths.Count, "Same request keeps path count stable");
    }

    private static void GeneratedWorldCanStartAnExpedition()
    {
        var game = new GameApplication().CreateGeneratedGame(Request());

        AssertEqual(game.Base.Location, game.Expedition.Position, "Generated expedition starts at generated base");
        AssertTrue(game.Knowledge.GetTileKnowledge(game.Base.Location) == KnowledgeLevel.Confirmed, "Generated base starts confirmed");
        AssertEqual(3, game.Factions.Count, "Generated campaign carries generated factions");
    }

    private static void GeneratedFactionsReceiveStableHiddenSignatureProfiles()
    {
        var first = new GameApplication().CreateGeneratedGame(Request());
        var second = new GameApplication().CreateGeneratedGame(Request());

        AssertTrue(first.Factions.All(faction => faction.SignatureProfileId != "unassigned"), "Generated factions receive a signature profile from content");
        AssertEqual(
            string.Join("|", first.Factions.Select(faction => faction.SignatureProfileId)),
            string.Join("|", second.Factions.Select(faction => faction.SignatureProfileId)),
            "Same generation request keeps hidden signature assignments stable");
    }

    private static void GeneratedWorldSeedsLaterSimulation()
    {
        var result = new WorldGenBridge().Generate(Request());
        AssertEqual(Request().Seed, result.World.Random.Seed, "Generated campaign carries its world seed into later simulation");
    }

    private static void TerritorialLocationsCanRemainUnclaimedAndCarryGeneratedEvidenceSeeds()
    {
        var result = new WorldGenBridge().Generate(Request());
        var territorialLocations = result.World.Locations
            .Where(location => result.World.Map.GetTile(location.Coord).OwnerId != null)
            .ToList();

        AssertTrue(territorialLocations.Count > 0, "Generated world has locations inside faction territory");
        AssertTrue(territorialLocations.Any(location => location.FactionRelations.Count == 0), "Territory does not automatically claim every location");
        AssertTrue(result.World.Locations.Any(location => location.EvidenceSeedIds.Count > 0), "Generated locations carry neutral evidence seeds");
    }

    private static void GeneratedSliceHasOptionalSoftConnectionsWithoutStaticFactionAssignment()
    {
        var result = new WorldGenBridge().Generate(Request());
        AssertTrue(result.World.Connections.Count >= 2, "Generated Slice provides at least two optional soft connections");
        AssertTrue(result.World.Connections.All(connection => connection.Tags.Contains("optional")), "Generated connections are optional world context, not quest steps");
        AssertTrue(result.World.Connections.All(connection => !connection.Tags.Any(tag => tag.StartsWith("faction-", StringComparison.Ordinal))), "Generated connections carry no static faction assignment");
    }

    private static WorldGenerationRequest Request()
    {
        return new WorldGenerationRequest { Seed = 20260712, Width = 24, Height = 18, FactionCount = 3 };
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
        AssertEqual(new HexCoord(1, 15), game.Base.Location, "Tutorial base location");
        AssertEqual(game.Base.Location, game.Expedition.Position, "Expedition starts at base");
        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(game.Base.Location), "Base starts confirmed");
        AssertEqual(TerrainType.Coast, game.World.Map.GetTile(game.Base.Location).Terrain, "Base tile terrain");
        AssertEqual(0, game.Base.EvaluationQueue.Items.Count, "Expedition starts without base evaluation items");
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
        var coord = game.World.Locations.First(location => location.Id == "marked-grave").Coord;

        game.PlayerNotes.AddMarker(new PlayerMapMarkerState("marker-1", coord, PlayerMapMarkerKind.Question, "Broken bridge?"));
        game.PlayerNotes.AddNote(new PlayerMapNoteState("note-1", coord, "Need an engineer later."));

        AssertEqual(1, game.PlayerNotes.Markers.Count, "Marker count");
        AssertEqual(1, game.PlayerNotes.Notes.Count, "Note count");
        AssertEqual(KnowledgeLevel.Unknown, game.Knowledge.GetTileKnowledge(coord), "Note does not reveal knowledge");
        AssertEqual("marked-grave", game.World.Map.GetTile(coord).LocationId, "Note does not mutate world location");
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
        AssertEqual(1, game.Expedition.UnsecuredKnowledge, "New confirmed destination adds unsecured knowledge");
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
        InactiveExpeditionCannotSendScouts();
        GameApplicationCanSendTutorialScoutMission();
    }

    private static void ValidScoutMissionAssignsScoutAndStoresMission()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "scout-1" },
            ScoutDirection.NorthEast,
            2,
            ScoutMissionFocus.FactionSigns,
            ScoutMissionBehavior.Cautious);

        AssertTrue(result.Success, "Scout mission succeeds");
        AssertEqual(1, game.Expedition.ScoutMissions.Count, "Mission count");
        AssertEqual(ExpeditionMemberStatus.Assigned, game.Expedition.FindMember("scout-1")!.Status, "Scout is assigned");
        AssertEqual(3, result.Mission!.ExpectedReturnWorldDay, "Expected return day");
        AssertEqual(ScoutDirection.NorthEast, result.Mission.Direction, "Mission direction");
        AssertEqual(ScoutMissionFocus.FactionSigns, result.Mission.Focus, "Mission focus");
    }

    private static void NonScoutMemberCannotBeSent()
    {
        var game = CreateScoutTestGame();
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "guard-1" },
            ScoutDirection.East,
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

        command.Execute(game, new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Balanced);
        var result = command.Execute(game, new[] { "scout-1" }, ScoutDirection.West, 1, ScoutMissionFocus.Route, ScoutMissionBehavior.Bold);

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
            ScoutDirection.East,
            6,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Balanced);

        AssertFalse(result.Success, "Too long mission rejected");
        AssertEqual(0, game.Expedition.ScoutMissions.Count, "No mission added");
    }

    private static void InactiveExpeditionCannotSendScouts()
    {
        var game = CreateScoutTestGame();
        game.Expedition.SetStatus(ExpeditionStatus.Returned);
        var command = new SendScoutMissionCommand();

        var result = command.Execute(
            game,
            new[] { "scout-1" },
            ScoutDirection.East,
            1,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Balanced);

        AssertFalse(result.Success, "Inactive expedition scout mission rejected");
        AssertEqual(ExpeditionMemberStatus.Available, game.Expedition.FindMember("scout-1")!.Status, "Scout remains available in base");
        AssertEqual(0, game.Expedition.ScoutMissions.Count, "No mission added");
    }

    private static void GameApplicationCanSendTutorialScoutMission()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();

        var result = app.SendScoutMission(
            game,
            new[] { "scout-1", "scout-2" },
            ScoutDirection.NorthWest,
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
        EndDayAtBaseConsumesNoSuppliesWhileScoutsResolve();
        GameApplicationCanEndTutorialDay();
        CautiousScoutReturnsWithReport();
        RepeatedScoutRouteDoesNotFarmUnsecuredKnowledge();
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
        AssertEqual(startSupplies, game.Expedition.Supplies, "Application supplies are not consumed at base");
        AssertEqual(0, result.SuppliesConsumed, "Application consumed no supplies at base");
    }

    private static void EndDayAtBaseConsumesNoSuppliesWhileScoutsResolve()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4, expeditionAtBase: true);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);

        var result = new EndDayCommand(suppliesPerDay: 2).Execute(game);

        AssertEqual(10, game.Expedition.Supplies, "Base day consumes no supplies");
        AssertEqual(0, result.SuppliesConsumed, "Base day reports no consumed supplies");
        AssertEqual(1, result.ScoutResolutions.Count, "Base day still resolves scouts");
        AssertEqual(ScoutMissionStatus.Returned, result.ScoutResolutions[0].Status, "Scout can return while expedition waits at base");
    }

    private static void CautiousScoutReturnsWithReport()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(1, result.ScoutResolutions.Count, "Scout resolution count");
        AssertEqual(ScoutMissionStatus.Returned, result.ScoutResolutions[0].Status, "Cautious scout returned");
        AssertEqual(ExpeditionMemberStatus.Available, game.Expedition.FindMember("scout")!.Status, "Returned scout available");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Scout report stored");
        AssertTrue(game.Knowledge.ScoutReports[0].Leads.Any(lead => lead.Scope == ScoutLeadScope.Directional), "Scout report carries an approximate directional lead");
        AssertEqual(KnowledgeLevel.Unknown, game.Knowledge.GetTileKnowledge(new HexCoord(2, 0)), "Scout report does not reveal objective map knowledge");
        AssertEqual(0, game.PlayerNotes.Notes.Count, "Scout report never creates an exact automatic map note");
        AssertEqual(3, game.Expedition.UnsecuredKnowledge, "Returned scout report adds unsecured knowledge");
    }

    private static void RepeatedScoutRouteDoesNotFarmUnsecuredKnowledge()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        var sendFirst = new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        var firstDay = new EndDayCommand().Execute(game);
        var sendSecond = new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        var secondDay = new EndDayCommand().Execute(game);

        AssertTrue(sendFirst.Success, "First scout mission sent");
        AssertTrue(firstDay.Success, "First scout mission resolves");
        AssertTrue(sendSecond.Success, "Second scout mission sent");
        AssertTrue(secondDay.Success, "Second scout mission resolves");
        AssertEqual(2, game.Knowledge.ScoutReports.Count, "Repeated route can still produce a report");
        AssertEqual(3, game.Expedition.UnsecuredKnowledge, "Repeated known route does not farm unsecured knowledge");
    }

    private static void BalancedScoutCanBecomeOverdueThenReturn()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Route, ScoutMissionBehavior.Balanced);

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
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.FactionSigns, ScoutMissionBehavior.Bold);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(ScoutMissionStatus.ReturnedInjured, result.ScoutResolutions[0].Status, "Bold scout injured");
        AssertEqual(ExpeditionMemberStatus.Injured, game.Expedition.FindMember("scout")!.Status, "Scout marked injured");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Injured scout report stored");
    }

    private static void BoldRuinScoutCanGoMissing()
    {
        var game = CreateEndDayTestGame(supplies: 10, movementPoints: 4, maxMovementPoints: 4);
        new SendScoutMissionCommand().Execute(game, new[] { "scout" }, ScoutDirection.East, 1, ScoutMissionFocus.Ruins, ScoutMissionBehavior.Bold);

        var result = new EndDayCommand().Execute(game);

        AssertEqual(ScoutMissionStatus.Missing, result.ScoutResolutions[0].Status, "Bold ruin scout missing");
        AssertEqual(ExpeditionMemberStatus.Missing, game.Expedition.FindMember("scout")!.Status, "Scout marked missing");
        AssertEqual(0, game.Knowledge.ScoutReports.Count, "Missing scout creates no report");
        AssertEqual(0, game.Expedition.UnsecuredKnowledge, "Missing scout adds no unsecured knowledge");
    }

    private static GameState CreateEndDayTestGame(int supplies, int movementPoints, int maxMovementPoints, bool expeditionAtBase = false)
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expeditionPosition = expeditionAtBase ? HexCoord.Zero : new HexCoord(1, 0);
        var expedition = new ExpeditionState(
            1,
            expeditionPosition,
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

internal sealed class CompleteExpeditionCommandTests
{
    public void RunAll()
    {
        CompletingAwayFromBaseIsRejected();
        CompletingAtBaseArchivesAndReturnsExpedition();
        CompletingWithActiveScoutsIsRejected();
    }

    private static void CompletingAwayFromBaseIsRejected()
    {
        var game = TutorialGameFactory.Create();
        game.Expedition.SetPosition(new HexCoord(2, 15));

        var result = new CompleteExpeditionCommand().Execute(game);

        AssertFalse(result.Success, "Completing away from base rejected");
        AssertEqual(ExpeditionStatus.Active, game.Expedition.Status, "Expedition remains active away from base");
    }

    private static void CompletingAtBaseArchivesAndReturnsExpedition()
    {
        var game = TutorialGameFactory.Create();
        var archiveCount = game.Base.ArchiveEntries.Count;
        game.Expedition.AddUnsecuredKnowledge(12);

        var result = new CompleteExpeditionCommand().Execute(game);
        var moveAfterReturn = new MoveExpeditionCommand(new MovementCostService()).Execute(game, new HexCoord(2, 15));
        var endDayAfterReturn = new EndDayCommand().Execute(game);

        AssertTrue(result.Success, "Completing at base succeeds");
        AssertEqual(ExpeditionStatus.Returned, game.Expedition.Status, "Expedition is returned");
        AssertEqual(ExpeditionStatus.Returned, game.Base.LastExpeditionOutcome, "Returned outcome stored");
        AssertEqual(game.World.WorldDay + CompleteExpeditionCommand.NormalPreparationDays, game.Base.NextExpeditionAvailableWorldDay, "Returned expedition schedules short base phase");
        AssertEqual(12, result.SecuredKnowledge, "Result reports secured knowledge");
        AssertEqual(12, game.Base.KnowledgePoints, "Returned expedition converts unsecured knowledge to base points");
        AssertEqual(0, game.Expedition.UnsecuredKnowledge, "Returned expedition clears unsecured knowledge");
        AssertEqual(archiveCount + 1, game.Base.ArchiveEntries.Count, "Completion archives a summary");
        AssertTrue(game.Base.ArchiveEntries[game.Base.ArchiveEntries.Count - 1].Contains("returned"), "Completion archive text");
        AssertFalse(moveAfterReturn.Success, "Returned expedition cannot move");
        AssertFalse(endDayAfterReturn.Success, "Returned expedition cannot advance day");
    }

    private static void CompletingWithActiveScoutsIsRejected()
    {
        var game = TutorialGameFactory.Create();
        var send = new SendScoutMissionCommand().Execute(
            game,
            new[] { "scout-1" },
            ScoutDirection.East,
            1,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Cautious);
        AssertTrue(send.Success, "Scout mission sent");

        var result = new CompleteExpeditionCommand().Execute(game);

        AssertFalse(result.Success, "Completing with active scout rejected");
        AssertEqual(ExpeditionStatus.Active, game.Expedition.Status, "Expedition remains active with scout away");
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

internal sealed class ExpeditionLifecycleCommandTests
{
    public void RunAll()
    {
        ReturnedExpeditionStartsNextAfterShortBaseTimeAndKeepsKnowledge();
        LostExpeditionClearsExpeditionKnowledgeAndNeedsLongBaseTime();
        LaterExpeditionCanRecoverPartOfLostKnowledge();
        EndDayWithoutFoodLosesExpedition();
    }

    private static void ReturnedExpeditionStartsNextAfterShortBaseTimeAndKeepsKnowledge()
    {
        var game = TutorialGameFactory.Create();
        var location = game.World.Locations.First(locationState => locationState.Id == "abandoned-camp");
        location.Inspect(game.World.WorldDay);
        game.Knowledge.AddScoutReport(new ScoutReportState(
            "report-test",
            "mission-test",
            "Smoke beyond the ridge",
            "A scout returned with useful faction notes.",
            70,
            new[] { location.Coord },
            new[] { "Smoke east" }));
        game.PlayerNotes.AddNote(new PlayerMapNoteState("note-test", location.Coord, "Possible safe camp."));
        game.Base.AddArchiveEntry("Field archive: useful faction notes secured by the returning expedition.");
        game.Expedition.AddUnsecuredKnowledge(12);

        var complete = new CompleteExpeditionCommand().Execute(game);
        var prepare = new PrepareSuppliesWithKnowledgeCommand().Execute(game);
        var earlyStart = new StartNewExpeditionCommand().Execute(game);
        var baseTime = new AdvanceBaseTimeCommand().Execute(game, CompleteExpeditionCommand.NormalPreparationDays);
        var start = new StartNewExpeditionCommand().Execute(game);

        AssertTrue(complete.Success, "Return completes");
        AssertTrue(prepare.Success, "Knowledge can buy supply preparation after return");
        AssertEqual(2, game.Base.KnowledgePoints, "Knowledge spending leaves remaining points");
        AssertFalse(earlyStart.Success, "Next expedition waits for base preparation");
        AssertTrue(baseTime.Success, "Base time advances");
        AssertTrue(start.Success, "Next expedition starts after short base time");
        AssertEqual(2, game.Expedition.ExpeditionNumber, "Second expedition number");
        AssertEqual(game.Base.Location, game.Expedition.Position, "Second expedition starts at base");
        AssertEqual(40, game.Expedition.Supplies, "Second expedition starts with prepared supplies");
        AssertEqual(0, game.Base.PendingSupplyBonus, "Supply preparation is consumed on expedition start");
        AssertEqual(1, game.Knowledge.ScoutReports.Count, "Returned scout reports persist");
        AssertEqual(1, game.PlayerNotes.Notes.Count, "Returned notes persist");
        AssertTrue(game.Base.ArchiveEntries.Any(entry => entry.Contains("useful faction notes")), "Returned field archive entries persist");
        AssertTrue(location.IsInspected, "Returned special location knowledge persists");
    }

    private static void LostExpeditionClearsExpeditionKnowledgeAndNeedsLongBaseTime()
    {
        var game = TutorialGameFactory.Create();
        var location = game.World.Locations.First(locationState => locationState.Id == "abandoned-camp");
        location.Inspect(game.World.WorldDay);
        game.Knowledge.AddScoutReport(new ScoutReportState(
            "report-test",
            "mission-test",
            "Treasure sign",
            "A scout found a possible treasure location.",
            65,
            new[] { location.Coord },
            new[] { "Old camp" }));
        game.PlayerNotes.AddMarker(new PlayerMapMarkerState("marker-test", location.Coord, PlayerMapMarkerKind.Question, "Check later"));
        game.PlayerNotes.AddNote(new PlayerMapNoteState("note-test", location.Coord, "Found by first expedition."));
        game.Base.AddArchiveEntry("Field archive: treasure location found by first expedition.");
        game.Expedition.AddUnsecuredKnowledge(9);
        game.Events.Enqueue(new EventState(
            "event-test",
            EventKind.LocationDiscovery,
            "Found site",
            "Expedition",
            "A site was found.",
            new[] { new EventOptionState("ack", "Acknowledge", "Acknowledged.", EventOptionEffectKind.None) },
            location.Coord));
        game.Expedition.SetPosition(new HexCoord(8, 14));
        var hiddenBefore = game.World.Map.Tiles.Count(tile => tile.OwnerId == "hidden-ones");
        var wardensBefore = game.World.Map.Tiles.Count(tile => tile.OwnerId == "border-wardens");

        var fail = new FailExpeditionCommand().Execute(game, "All members died.", recoveryDays: 5);
        var earlyStart = new StartNewExpeditionCommand().Execute(game);
        var statusBeforeRestart = game.Expedition.Status;
        var baseTime = new AdvanceBaseTimeCommand().Execute(game, 5);
        var start = new StartNewExpeditionCommand().Execute(game);

        AssertTrue(fail.Success, "Failure succeeds");
        AssertEqual(ExpeditionStatus.Lost, game.Base.LastExpeditionOutcome, "Lost outcome stored");
        AssertEqual(ExpeditionStatus.Lost, statusBeforeRestart, "Expedition is lost before restart");
        AssertEqual(9, fail.LostUnsecuredKnowledge, "Failure reports lost unsecured knowledge");
        AssertEqual(0, game.Expedition.UnsecuredKnowledge, "Lost expedition clears unsecured knowledge");
        AssertEqual(0, game.Base.KnowledgePoints, "Lost expedition does not secure knowledge points");
        AssertEqual(1, game.Base.LostExpeditions.Count, "Lost expedition record is created");
        AssertEqual("expedition-1", game.Base.LostExpeditions[0].ExpeditionId, "Lost expedition id");
        AssertEqual(new HexCoord(8, 14), game.Base.LostExpeditions[0].LastKnownPosition, "Lost expedition last known position");
        AssertEqual(9, game.Base.LostExpeditions[0].EstimatedLostKnowledge, "Lost expedition estimated knowledge");
        AssertEqual(LostExpeditionStatus.Missing, game.Base.LostExpeditions[0].Status, "Lost expedition status");
        AssertTrue(game.Base.LostExpeditions[0].PossibleRecoveryClueIds.Any(clue => clue.Contains("last-known-position")), "Lost expedition has recovery clue");
        AssertEqual(0, game.Knowledge.ScoutReports.Count, "Lost reports are removed");
        AssertEqual(1, game.PlayerNotes.Markers.Count, "Lost expedition creates recovery marker");
        AssertEqual(new HexCoord(8, 14), game.PlayerNotes.Markers[0].Coord, "Recovery marker uses last known position");
        AssertTrue(game.PlayerNotes.Markers[0].Label.Contains("Last known position"), "Recovery marker label explains clue");
        AssertFalse(game.PlayerNotes.Markers.Any(marker => marker.Label.Contains("Check later")), "Old expedition markers are removed");
        AssertEqual(1, game.PlayerNotes.Notes.Count, "Lost expedition creates recovery note");
        AssertEqual(new HexCoord(8, 14), game.PlayerNotes.Notes[0].Coord, "Recovery note uses last known position");
        AssertTrue(game.PlayerNotes.Notes[0].Text.Contains("Recovery lead"), "Recovery note explains clue");
        AssertFalse(game.PlayerNotes.Notes.Any(note => note.Text.Contains("Found by first expedition")), "Old expedition notes are removed");
        AssertEqual(0, game.Events.PendingCount, "Lost expedition clears pending expedition events");
        AssertFalse(game.Base.ArchiveEntries.Any(entry => entry.Contains("treasure location")), "Lost field archive entries are discarded");
        AssertTrue(game.Base.ArchiveEntries.Any(entry => entry.Contains("No reports")), "Lost archive keeps only operational loss note");
        AssertFalse(location.IsDiscovered, "Lost special location is no longer known");
        AssertEqual(KnowledgeLevel.Confirmed, game.Knowledge.GetTileKnowledge(game.Base.Location), "Base remains known");
        AssertEqual(KnowledgeLevel.OldOrDoubtful, game.Knowledge.GetTileKnowledge(new HexCoord(8, 14)), "Last known position remains a doubtful recovery clue");
        AssertTrue(game.World.Map.Tiles.Count(tile => tile.OwnerId == "hidden-ones") >= hiddenBefore, "Hidden Ones territory remains or shifts slightly");
        AssertTrue(game.World.Map.Tiles.Count(tile => tile.OwnerId == "border-wardens") >= wardensBefore, "Border Warden territory remains or shifts slightly");
        AssertTrue(game.FindFaction("hidden-ones")!.Memories.Any(memory => memory.Contains("expedition-lost")), "Faction remembers world shift");
        AssertFalse(earlyStart.Success, "Lost expedition needs longer base time");
        AssertTrue(baseTime.Success, "Long base time advances");
        AssertTrue(start.Success, "Replacement expedition starts after recovery");
        AssertEqual(2, game.Expedition.ExpeditionNumber, "Replacement expedition number");
        AssertEqual(game.Base.Location, game.Expedition.Position, "Replacement expedition starts at base");
    }

    private static void LaterExpeditionCanRecoverPartOfLostKnowledge()
    {
        var game = TutorialGameFactory.Create();
        var lostPosition = new HexCoord(8, 14);
        game.Expedition.AddUnsecuredKnowledge(9);
        game.Expedition.SetPosition(lostPosition);

        var fail = new FailExpeditionCommand().Execute(game, "No one returned.", recoveryDays: 1);
        var baseTime = new AdvanceBaseTimeCommand().Execute(game, 1);
        var start = new StartNewExpeditionCommand().Execute(game);
        game.Expedition.SetPosition(lostPosition);

        var recover = new RecoverLostExpeditionCommand().Execute(game, lostPosition);

        AssertTrue(fail.Success, "Failure succeeds");
        AssertTrue(baseTime.Success, "Base time advances");
        AssertTrue(start.Success, "Replacement expedition starts");
        AssertTrue(recover.Success, "Recovery succeeds at last known position");
        AssertEqual(5, recover.RecoveredKnowledge, "Recovery restores part of the lost knowledge");
        AssertEqual(5, game.Expedition.UnsecuredKnowledge, "Recovered knowledge remains unsecured field knowledge");
        AssertEqual(0, game.Base.KnowledgePoints, "Recovered knowledge is not secured at base immediately");
        AssertEqual(5, game.Base.LostExpeditions[0].RecoveredKnowledge, "Lost record tracks recovered knowledge");
        AssertEqual(LostExpeditionStatus.PartiallyRecovered, game.Base.LostExpeditions[0].Status, "Lost record becomes partially recovered");
        AssertTrue(game.Base.ArchiveEntries.Any(entry => entry.Contains("Recovery lead")), "Recovery adds current expedition archive entry");
    }

    private static void EndDayWithoutFoodLosesExpedition()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(
            1,
            new HexCoord(1, 0),
            new[] { new ExpeditionMemberState("scout", "Scout", ExpeditionMemberRole.Scout) },
            supplies: 1);
        var game = new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));

        var result = new EndDayCommand(suppliesPerDay: 2).Execute(game);

        AssertTrue(result.Success, "End day with last food succeeds as failure result");
        AssertTrue(result.ExpeditionLost, "End day reports lost expedition");
        AssertEqual(ExpeditionStatus.Lost, game.Expedition.Status, "Expedition is lost after food reaches zero");
        AssertEqual(ExpeditionStatus.Lost, game.Base.LastExpeditionOutcome, "Lost outcome stored after supplies run out");
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

internal sealed class InspectLocationCommandTests
{
    public void RunAll()
    {
        InspectingKnownLocationAddsArchiveEntryOnce();
        UnknownLocationCannotBeInspected();
        RavineInspectionWarnsWithoutEngineer();
        MarkedGraveInspectionAddsLeverageItemOnce();
        AbandonedCampInspectionAddsDefinedLeverageItem();
        WatchtowerInspectionAddsHiddenLeverageItem();
    }

    private static void InspectingKnownLocationAddsArchiveEntryOnce()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var baseArchiveCount = game.Base.ArchiveEntries.Count;
        var baseCoord = game.Base.Location;

        var first = command.Execute(game, baseCoord);
        var second = command.Execute(game, baseCoord);

        AssertTrue(first.Success, "First base inspection succeeds");
        AssertTrue(second.Success, "Second base inspection succeeds");
        AssertEqual(baseArchiveCount + 1, game.Base.ArchiveEntries.Count, "Archive entry is added once");
        AssertEqual(0, game.Expedition.UnsecuredKnowledge, "Base inspection does not add unsecured knowledge");
        AssertTrue(first.Location != null && first.Location.IsInspected, "Location is marked inspected");
    }

    private static void UnknownLocationCannotBeInspected()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();

        var result = command.Execute(game, new HexCoord(39, 29));

        AssertFalse(result.Success, "Unknown territory inspection rejected");
    }

    private static void RavineInspectionWarnsWithoutEngineer()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var ravine = game.World.Locations.First(location => location.Kind == LocationKind.BrokenRavine);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, ravine.Coord);

        var result = command.Execute(game, ravine.Coord);

        AssertTrue(result.Success, "Ravine inspection succeeds");
        AssertTrue(result.Message.Contains("Without an engineer"), "Ravine warns about missing engineer");
        AssertEqual(8, game.Expedition.UnsecuredKnowledge, "Special location inspection adds unsecured knowledge");
    }

    private static void MarkedGraveInspectionAddsLeverageItemOnce()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var grave = game.World.Locations.First(location => location.Kind == LocationKind.MarkedGrave);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, grave.Coord);

        var first = command.Execute(game, grave.Coord);
        var second = command.Execute(game, grave.Coord);

        AssertTrue(first.Success, "First grave inspection succeeds");
        AssertTrue(second.Success, "Second grave inspection succeeds");
        AssertTrue(game.LeverageItems.Contains(FactionInteractionDefinitions.BorderWardenGraveTokenId), "Grave token leverage is recorded");
        AssertEqual(1, game.LeverageItems.ItemIds.Count, "Grave token leverage is added once");
    }

    private static void AbandonedCampInspectionAddsDefinedLeverageItem()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var camp = game.World.Locations.First(location => location.Kind == LocationKind.AbandonedCamp);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, camp.Coord);

        var result = command.Execute(game, camp.Coord);
        var definition = FactionInteractionDefinitions.LeverageDefinitions.First(item => item.ItemId == FactionInteractionDefinitions.CoastalRiverChartFragmentId);

        AssertTrue(result.Success, "Abandoned camp inspection succeeds");
        AssertEqual(camp.Id, definition.Source, "River chart source points to abandoned camp");
        AssertTrue(game.LeverageItems.Contains(FactionInteractionDefinitions.CoastalRiverChartFragmentId), "River chart leverage is recorded");
    }

    private static void WatchtowerInspectionAddsHiddenLeverageItem()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var watchtower = game.World.Locations.First(location => location.Kind == LocationKind.Watchtower);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, watchtower.Coord);

        var result = command.Execute(game, watchtower.Coord);
        var definition = FactionInteractionDefinitions.LeverageDefinitions.First(item => item.ItemId == FactionInteractionDefinitions.HiddenSealedSymbolId);

        AssertTrue(result.Success, "Watchtower inspection succeeds");
        AssertEqual(watchtower.Id, definition.Source, "Hidden symbol source points to watchtower");
        AssertTrue(game.LeverageItems.Contains(FactionInteractionDefinitions.HiddenSealedSymbolId), "Hidden symbol leverage is recorded");
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

internal sealed class LocationInteractionFrameworkTests
{
    public void RunAll()
    {
        TutorialBridgeIsImmediateEdgeLocation();
        TutorialBridgeTileIsMovableFromBase();
        TutorialBridgeBlocksForwardRouteUntilBypassOpensIt();
        BridgeActionsComeFromScenarioProfileAndModifiers();
        LocationActionCostsLockWhenMovementIsMissing();
        BridgeCrossingActionMovesExpeditionAcrossEdge();
        RopeCrossingChangesStateAndRiskWithoutVariantSwitch();
        RebuildBridgeIsGenericLockedProject();
        MarkedGraveUsesSameInteractionFramework();
    }

    private static void TutorialBridgeIsImmediateEdgeLocation()
    {
        var game = TutorialGameFactory.Create();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");

        AssertEqual(new HexCoord(2, 15), bridge.Coord, "Bridge sits on first field after base");
        AssertEqual(LocationAnchorKind.Edge, bridge.Anchor.Kind, "Bridge uses edge anchor");
        AssertEqual(new HexCoord(2, 15), bridge.Anchor.Coords[0], "Bridge edge starts at bridge field");
        AssertEqual(new HexCoord(3, 15), bridge.Anchor.Coords[1], "Bridge edge ends at field beyond bridge");
        AssertEqual("broken-ravine", game.World.Map.GetTile(new HexCoord(2, 15)).LocationId, "First field points to bridge location");
    }

    private static void TutorialBridgeTileIsMovableFromBase()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();

        var move = app.MoveExpedition(game, new HexCoord(2, 15));
        var interaction = app.GetLocationInteraction(game, "broken-ravine");

        AssertTrue(move.Success, "Expedition can move from base to bridge field");
        AssertEqual(new HexCoord(2, 15), game.Expedition.Position, "Expedition reaches bridge test field");
        AssertTrue(interaction.Success, "Bridge interaction is available after arrival");
        AssertTrue(interaction.Interaction != null && interaction.Interaction.Options.Count > 0, "Bridge interaction has actions after arrival");
    }

    private static void TutorialBridgeBlocksForwardRouteUntilBypassOpensIt()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();

        var arrival = app.MoveExpedition(game, new HexCoord(2, 15));
        AssertTrue(arrival.Success, "Expedition reaches bridge field");

        var blocked = app.MoveExpedition(game, new HexCoord(3, 15));
        AssertFalse(blocked.Success, "Blocked bridge edge rejects forward map movement");
        AssertEqual(new HexCoord(2, 15), game.Expedition.Position, "Blocked movement keeps position");

        var beforeBypassMovement = game.Expedition.MovementPoints;
        var bypass = app.ResolveLocationAction(game, "broken-ravine", LocationInteractionContent.ActionFindBypass, LocationOutcomeTier.Success);
        AssertTrue(bypass.Success, "Bypass action resolves");
        AssertEqual(beforeBypassMovement - 1, game.Expedition.MovementPoints, "Bypass spends declared movement cost");
        AssertTrue(game.World.Paths.Any(path => path.Id == "route-opened-broken-ravine"), "Bypass success opens a route over the blocked edge");

        var opened = app.MoveExpedition(game, new HexCoord(3, 15));
        AssertTrue(opened.Success, "Opened bypass allows forward movement");
    }

    private static void BridgeActionsComeFromScenarioProfileAndModifiers()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var result = app.GetLocationInteraction(game, bridge.Id);

        AssertTrue(result.Success, "Bridge interaction query succeeds");
        var interaction = result.Interaction;
        AssertTrue(interaction != null, "Bridge interaction model exists");
        if (interaction == null)
        {
            throw new InvalidOperationException("Bridge interaction model missing.");
        }

        AssertTrue(interaction.Options.Any(option => option.Action.Id == "action-assess-crossing"), "Scenario profile provides the basic route assessment action");
        AssertTrue(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionFindBypass), "Scenario profile provides its initial additional action");
        AssertTrue(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionRebuildBridge), "Repairable modifier adds rebuild action");
        AssertTrue(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionAttemptCrossing), "Scenario profile exposes the obvious but risky crossing attempt");
        AssertTrue(interaction.Options.All(option => option.Action.Id != LocationInteractionContent.ActionDisturb), "Investigation-site action is absent");
    }

    private static void LocationActionCostsLockWhenMovementIsMissing()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var arrival = app.MoveExpedition(game, new HexCoord(2, 15));
        AssertTrue(arrival.Success, "Expedition reaches bridge field");
        game.SetExpedition(new ExpeditionState(
            game.Expedition.ExpeditionNumber,
            game.Expedition.Position,
            game.Expedition.Members,
            game.Expedition.ExpeditionDay,
            movementPoints: 0,
            maxMovementPoints: game.Expedition.MaxMovementPoints,
            supplies: game.Expedition.Supplies,
            medicine: game.Expedition.Medicine,
            morale: game.Expedition.Morale,
            capacity: game.Expedition.Capacity,
            status: game.Expedition.Status,
            unsecuredKnowledge: game.Expedition.UnsecuredKnowledge));

        var interaction = app.GetLocationInteraction(game, "broken-ravine").Interaction;
        var bypass = interaction?.FindOption(LocationInteractionContent.ActionFindBypass);

        AssertTrue(bypass != null, "Bypass action exists");
        AssertFalse(bypass!.IsAvailable, "Bypass locks without movement points");
        AssertTrue(bypass.LockedReason != null && bypass.LockedReason.Contains("Bewegungspunkte"), "Bypass reports movement cost lock");
    }

    private static void BridgeCrossingActionMovesExpeditionAcrossEdge()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var arrival = app.MoveExpedition(game, new HexCoord(2, 15));
        AssertTrue(arrival.Success, "Expedition reaches bridge field");

        var beforeCrossingMovement = game.Expedition.MovementPoints;
        var crossing = app.ResolveLocationAction(game, "broken-ravine", LocationInteractionContent.ActionAttemptCrossing, LocationOutcomeTier.Success);

        AssertTrue(crossing.Success, "Crossing action resolves");
        AssertTrue(crossing.ExpeditionMoved, "Crossing result reports expedition movement");
        AssertEqual(new HexCoord(3, 15), game.Expedition.Position, "Crossing moves expedition to the far side");
        AssertEqual(beforeCrossingMovement - 1, game.Expedition.MovementPoints, "Crossing spends declared movement cost");
    }

    private static void RopeCrossingChangesStateAndRiskWithoutVariantSwitch()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var before = app.GetLocationInteraction(game, bridge.Id).Interaction;
        var crossingBefore = before?.FindOption(LocationInteractionContent.ActionAttemptCrossing);
        AssertTrue(crossingBefore != null, "Crossing action exists before rope crossing");
        AssertEqual(LocationRiskBand.High, crossingBefore!.RiskBand, "Blocked bridge crossing risk is high");

        AssertTrue(before?.FindOption(LocationInteractionContent.ActionConstructTemporaryPassage) == null,
            "Temporary-passage option stays hidden until its structural context is confirmed");
        game.Knowledge.LearnLocationContextTag(bridge.Id, "structural-failure");
        var prepared = app.GetLocationInteraction(game, bridge.Id).Interaction;
        AssertTrue(prepared?.FindOption(LocationInteractionContent.ActionConstructTemporaryPassage) != null,
            "Confirmed structural context reveals the temporary-passage option");

        var result = app.ResolveLocationAction(game, bridge.Id, LocationInteractionContent.ActionConstructTemporaryPassage, LocationOutcomeTier.SuccessWithCost);

        AssertTrue(result.Success, "Rope crossing action resolves");
        AssertEqual(LocationStateIds.Operational.RiskyPassage, bridge.OperationalStateId, "Rope crossing changes operational state");

        var after = app.GetLocationInteraction(game, bridge.Id).Interaction;
        var crossingAfter = after?.FindOption(LocationInteractionContent.ActionAttemptCrossing);
        AssertTrue(crossingAfter != null, "Crossing action exists after rope crossing");
        AssertEqual(LocationRiskBand.Moderate, crossingAfter!.RiskBand, "Risky passage crossing risk is recomputed from state");
    }

    private static void RebuildBridgeIsGenericLockedProject()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var withoutEngineer = app.GetLocationInteraction(game, bridge.Id).Interaction?.FindOption(LocationInteractionContent.ActionRebuildBridge);
        AssertTrue(withoutEngineer != null, "Rebuild action is visible without engineer");
        AssertFalse(withoutEngineer!.IsAvailable, "Rebuild action is locked without engineer");

        var members = game.Expedition.Members
            .Concat(new[] { new ExpeditionMemberState("engineer-test", "Ansel", ExpeditionMemberRole.Engineer) })
            .ToList();
        game.SetExpedition(new ExpeditionState(
            game.Expedition.ExpeditionNumber,
            game.Expedition.Position,
            members,
            game.Expedition.ExpeditionDay,
            game.Expedition.MovementPoints,
            game.Expedition.MaxMovementPoints,
            game.Expedition.Supplies,
            game.Expedition.Medicine,
            game.Expedition.Morale,
            game.Expedition.Capacity,
            game.Expedition.Status,
            game.Expedition.UnsecuredKnowledge));

        var start = app.ResolveLocationAction(game, bridge.Id, LocationInteractionContent.ActionRebuildBridge);
        AssertTrue(start.Success, "Engineer can start bridge project");
        AssertTrue(bridge.ActiveProject != null, "Bridge project state is stored on location");

        app.AdvanceLocationProject(game, bridge.Id);
        app.AdvanceLocationProject(game, bridge.Id);
        var complete = app.AdvanceLocationProject(game, bridge.Id);

        AssertTrue(complete.Success, "Project completion succeeds");
        AssertEqual(LocationStateIds.Operational.Repaired, bridge.OperationalStateId, "Bridge is repaired after project completion");
        AssertTrue(game.World.Paths.Any(path => path.Id == "route-opened-broken-ravine"), "OpenRoute effect creates persistent route");
        AssertTrue(game.World.WorldTriggers.Any(trigger =>
            trigger.TriggerId == "location-infrastructure-repaired" && trigger.ActionTags.Contains("repair")),
            "Project trigger retains the JSON-authored repair action tag");
    }

    private static void MarkedGraveUsesSameInteractionFramework()
    {
        var game = TutorialGameFactory.Create();
        var app = new GameApplication();
        var grave = game.World.Locations.First(location => location.Id == "marked-grave");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, grave.Coord);

        var result = app.GetLocationInteraction(game, grave.Id);

        AssertTrue(result.Success, "Marked grave interaction query succeeds");
        var interaction = result.Interaction;
        AssertTrue(interaction != null, "Marked grave interaction model exists");
        if (interaction == null)
        {
            throw new InvalidOperationException("Marked grave interaction model missing.");
        }

        AssertTrue(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionInspect), "Investigation archetype action is present");
        AssertTrue(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionLeaveOffering), "Marked grave variant adds offering action");
        AssertFalse(interaction.Options.Any(option => option.Action.Id == LocationInteractionContent.ActionRebuildBridge), "Route-obstacle action is absent");
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

internal sealed class LocationDataJsonTests
{
    public void RunAll()
    {
        AllCanonicalArchetypesLoadAndAreRepresentable();
        AllLocationActionsCarrySemanticTags();
        ActionCommitmentAndRequirementDisclosureLoad();
        ContentProfileSurfacesTitleAndFlavor();
        WeightedOutcomeStaysWithinAuthoredBandRow();
        ForcedTierAppliesAuthoredEffectBundle();
        ModifierCompatibilityIsEnforced();
        RepeatPolicyLocksAndReopensOnStateChange();
        RecoveryCheckSpareseOrAppliesInjury();
        SocialRiskRisesWithFactionAnger();
        ValidationRejectsUnknownDocumentType();
        ValidationRejectsWeightedTierWithoutEffectBundle();
    }

    private static LocationDataBundle LoadBundle()
    {
        var bundle = LocationDataLoader.LoadFromDirectory(LocationsDataRoot());
        AssertTrue(bundle != null, "Location JSON bundle loads from StreamingAssets");
        return bundle!;
    }

    private static readonly string[] AllArchetypeIds =
    {
        "investigation-site", "route-obstacle", "containment-site", "territorial-marker", "contact-site",
        "hazard-site", "natural-phenomenon"
    };

    private static void AllCanonicalArchetypesLoadAndAreRepresentable()
    {
        var defs = LoadBundle().Definitions;

        // No instances are authored as JSON anymore — placement is code/generator-driven (§18).
        foreach (var archetypeId in AllArchetypeIds)
        {
            AssertTrue(defs.Archetypes.ContainsKey(archetypeId), $"Archetype '{archetypeId}' loads from JSON");
            var archetype = defs.Archetypes[archetypeId];
            AssertTrue(archetype.DefaultActionIds.Count > 0, $"Archetype '{archetypeId}' has default actions");

            foreach (var actionId in archetype.DefaultActionIds)
            {
                AssertTrue(defs.Actions.ContainsKey(actionId), $"Archetype '{archetypeId}' action '{actionId}' is defined");
                var action = defs.Actions[actionId];
                if (!action.StartsProject)
                {
                    AssertTrue(defs.FindOutcomeTable(action.OutcomeTableId) != null, $"Action '{actionId}' has an outcome table");
                }
            }
        }
    }

    private static void AllLocationActionsCarrySemanticTags()
    {
        var missing = LoadBundle().Definitions.Actions.Values
            .Where(action => action.ActionTags.Count == 0)
            .Select(action => action.Id)
            .ToList();

        AssertEqual(0, missing.Count, "Every JSON-authored location action has at least one semantic action tag");
    }

    private static void ActionCommitmentAndRequirementDisclosureLoad()
    {
        var action = LoadBundle().Definitions.Actions["action-construct-temporary-passage"];
        AssertEqual(LocationActionCommitment.DayOperation, action.Commitment, "Temporary passage is a day operation");
        AssertEqual(LocationRequirementDisclosure.Known, action.HardRequirements[0].Disclosure, "Existing hard requirement is visibly disclosed");
    }

    private static void ContentProfileSurfacesTitleAndFlavor()
    {
        var defs = LoadBundle().Definitions;
        var profile = defs.FindContentProfile("content-old-trade-road-bridge");
        AssertTrue(profile != null, "Bridge content profile is present");
        AssertEqual("Zerstoerte Bruecke", profile!.Title, "Content profile carries the authored title");
        AssertEqual("Die eingestuerzte Handelsbruecke liegt in geborstenen Balken ueber der Schlucht; die alte Handelsroute ist damit unterbrochen.", profile.FlavorForState("blocked"), "Content profile flavor is keyed by state");
    }

    private static void WeightedOutcomeStaysWithinAuthoredBandRow()
    {
        var bundle = LoadBundle();
        var service = new LocationInteractionService(bundle.Definitions, new System.Random(20260710));
        var action = bundle.Definitions.Actions["action-attempt-crossing"];

        var allowed = new HashSet<LocationOutcomeTier>
        {
            LocationOutcomeTier.Success,
            LocationOutcomeTier.SuccessWithCost,
            LocationOutcomeTier.Failure,
            LocationOutcomeTier.SevereFailure
        };

        var seen = new HashSet<LocationOutcomeTier>();
        for (var i = 0; i < 300; i++)
        {
            var resolution = service.ResolveOutcome(action, LocationRiskBand.High);
            AssertTrue(resolution != null, "High-band roll produces a resolution");
            AssertTrue(allowed.Contains(resolution!.Tier), "Rolled tier is within the authored High band row");
            AssertTrue(resolution.Effects.Count > 0, "Rolled tier has an effect bundle");
            seen.Add(resolution.Tier);
        }

        AssertTrue(seen.Count >= 2, "Weighted roll actually varies across tiers");
    }

    private static void ForcedTierAppliesAuthoredEffectBundle()
    {
        var bundle = LoadBundle();
        var service = new LocationInteractionService(bundle.Definitions);
        var action = bundle.Definitions.Actions["action-attempt-crossing"];

        var resolution = service.ResolveOutcome(action, LocationRiskBand.High, LocationOutcomeTier.SevereFailure);
        AssertTrue(resolution != null, "Forced tier resolves");
        AssertEqual(LocationOutcomeTier.SevereFailure, resolution!.Tier, "Forced tier is honored");
        AssertTrue(resolution.Effects.Any(effect => effect.Kind == LocationEffectKind.InjureMember), "SevereFailure bundle injures a member");
    }

    private static void ModifierCompatibilityIsEnforced()
    {
        var defs = LoadBundle().Definitions;

        AssertTrue(defs.IsModifierCompatible("investigation-site", "modifier-sacred"), "Sacred is compatible with investigation-site");
        AssertFalse(defs.IsModifierCompatible("route-obstacle", "modifier-campable"), "Campable is not compatible with route-obstacle");

        var okErrors = defs.ValidateModifierSet("route-obstacle", new[] { "modifier-repairable", "modifier-unstable" });
        AssertEqual(0, okErrors.Count, "A valid modifier set has no compatibility errors");

        var incompatible = defs.ValidateModifierSet("hazard-site", new[] { "modifier-burning", "modifier-flooded" });
        AssertTrue(incompatible.Count > 0, "Declared-incompatible modifiers are rejected together");
    }

    private static void RepeatPolicyLocksAndReopensOnStateChange()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var once = app.ResolveLocationAction(game, bridge.Id, "action-assess-crossing", LocationOutcomeTier.Success);
        AssertTrue(once.Success, "OncePerLocation action resolves once");
        var locked = app.GetLocationInteraction(game, bridge.Id).Interaction!.FindOption("action-assess-crossing")!;
        AssertFalse(locked.IsAvailable, "OncePerLocation action locks after use");

        var bypass = app.ResolveLocationAction(game, bridge.Id, "action-find-bypass", LocationOutcomeTier.Success);
        AssertTrue(bypass.Success, "OncePerState action resolves in the current state");
        var lockedBypass = app.GetLocationInteraction(game, bridge.Id).Interaction!.FindOption("action-find-bypass")!;
        AssertFalse(lockedBypass.IsAvailable, "OncePerState action locks in the same state");

        game.Knowledge.LearnLocationContextTag(bridge.Id, "structural-failure");
        app.ResolveLocationAction(game, bridge.Id, "action-construct-temporary-passage", LocationOutcomeTier.SuccessWithCost);
        var reopened = app.GetLocationInteraction(game, bridge.Id).Interaction!.FindOption("action-find-bypass")!;
        AssertTrue(reopened.IsAvailable, "OncePerState action reopens after the operational state changes");
    }

    private static void RecoveryCheckSpareseOrAppliesInjury()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var bridge = game.World.Locations.First(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var before = game.Expedition.Members.Count(member => member.Status == ExpeditionMemberStatus.Injured);

        var preserved = app.ResolveLocationAction(game, bridge.Id, "action-attempt-crossing", LocationOutcomeTier.SevereFailure, LocationRecoveryOutcome.Preserved);
        AssertTrue(preserved.Success, "Severe failure resolves");
        AssertEqual(before, game.Expedition.Members.Count(member => member.Status == ExpeditionMemberStatus.Injured), "Preserved recovery spares the member");

        var lost = app.ResolveLocationAction(game, bridge.Id, "action-attempt-crossing", LocationOutcomeTier.SevereFailure, LocationRecoveryOutcome.Lost);
        AssertTrue(lost.Success, "Severe failure resolves again");
        AssertTrue(game.Expedition.Members.Count(member => member.Status == ExpeditionMemberStatus.Injured) > before, "Lost recovery applies the injury");
    }

    private static void SocialRiskRisesWithFactionAnger()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var sign = game.World.Locations.First(location => location.Id == "border-warning");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, sign.Coord);

        var before = app.GetLocationInteraction(game, sign.Id).Interaction!.FindOption("action-cross-boundary")!;
        var rawBefore = before.RawRisk;
        var bandBefore = before.RiskBand;

        game.FindFaction("border-wardens")!.Adjust(angerDelta: 80);

        var after = app.GetLocationInteraction(game, sign.Id).Interaction!.FindOption("action-cross-boundary")!;
        AssertTrue(after.RawRisk > rawBefore, "Higher faction anger raises the raw social risk");
        AssertTrue((int)after.RiskBand >= (int)bandBefore, "Displayed band never drops when true danger rises (Fairness Rule 3)");
    }

    private static void ValidationRejectsUnknownDocumentType()
    {
        var malformed = "{ \"documentType\": \"location-nonsense\", \"schemaVersion\": 1, \"items\": [] }";
        AssertThrows(() => LocationDataLoader.LoadFromJson(new[] { malformed }), "Unknown document type is rejected");
    }

    private static void ValidationRejectsWeightedTierWithoutEffectBundle()
    {
        var malformed =
            "{ \"documentType\": \"location-outcome-tables\", \"schemaVersion\": 1, \"items\": [" +
            "{ \"id\": \"bad-table\", \"tiers\": { \"High\": [ { \"tier\": \"Failure\", \"weight\": 10 } ] }, \"effectBundles\": {} } ] }";
        AssertThrows(() => LocationDataLoader.LoadFromJson(new[] { malformed }), "Weighted tier without an effect bundle is rejected");
    }

    private static string LocationsDataRoot()
    {
        const string relative = "UnityHexMapView/Assets/StreamingAssets/GameData/Locations";
        var cwd = Path.Combine(Directory.GetCurrentDirectory(), relative);
        if (Directory.Exists(cwd))
        {
            return cwd;
        }

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, relative);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Location JSON data folder not found for tests.");
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (LocationDataException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected a LocationDataException.");
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

internal sealed class EventQueueCommandTests
{
    public void RunAll()
    {
        InspectingSpecialLocationQueuesEvent();
        ResolvingArchiveOptionMutatesArchiveAndClosesEvent();
        EndDayQueuesScoutOverdueEvent();
    }

    private static void InspectingSpecialLocationQueuesEvent()
    {
        var game = TutorialGameFactory.Create();
        var command = new InspectLocationCommand();
        var grave = game.World.Locations.First(location => location.Kind == LocationKind.MarkedGrave);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, grave.Coord);

        var result = command.Execute(game, grave.Coord);

        AssertTrue(result.Success, "Grave inspection succeeds");
        AssertEqual(1, game.Events.PendingCount, "Inspection queues an event");
        AssertEqual("Marked Grave", game.Events.Current!.Title, "Queued event title");
    }

    private static void ResolvingArchiveOptionMutatesArchiveAndClosesEvent()
    {
        var game = TutorialGameFactory.Create();
        var archiveCount = game.Base.ArchiveEntries.Count;
        game.Events.Enqueue(new EventState(
            "event-test",
            EventKind.FoundObject,
            "Sealed Box",
            "Expedition",
            "A sealed box was found.",
            new[]
            {
                new EventOptionState("archive", "Archive object", "The sealed box was archived.", EventOptionEffectKind.Archive)
            },
            game.Base.Location));

        var result = new ResolveEventCommand().Execute(game, "event-test", "archive");

        AssertTrue(result.Success, "Event resolve succeeds");
        AssertEqual(0, game.Events.PendingCount, "Event closes after resolution");
        AssertEqual(archiveCount + 1, game.Base.ArchiveEntries.Count, "Archive effect mutates base archive");
    }

    private static void EndDayQueuesScoutOverdueEvent()
    {
        var game = TutorialGameFactory.Create();
        var send = new SendScoutMissionCommand().Execute(
            game,
            new[] { "scout-1" },
            ScoutDirection.East,
            1,
            ScoutMissionFocus.Survey,
            ScoutMissionBehavior.Balanced);
        AssertTrue(send.Success, "Scout mission sent");

        var result = new EndDayCommand().Execute(game);

        AssertTrue(result.Success, "End day succeeds");
        AssertEqual(1, game.Events.PendingCount, "Scout overdue queues an event");
        AssertEqual(EventKind.ScoutOverdue, game.Events.Current!.Kind, "Current event is scout overdue");
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

internal sealed class FactionPresenceTests
{
    public void RunAll()
    {
        TutorialGameIncludesMvpFactions();
        EnteringFactionTerritoryAddsKnowledgeOnce();
        EnteringBorderWardenWarningZoneQueuesEventAndMemoryOnce();
        EnteringHiddenTerritoryCreatesDangerReaction();
        FactionDefinitionsProvideMvpOfferSet();
        FactionReactionCanOpenRepresentativeInteraction();
        FactionOfferCanTradeKnowledgeForSupplies();
        GraveTokenUnlocksBorderWardenNegotiation();
        CoastalChartUnlocksGuidanceOffer();
        HiddenOpenContactCanRecordForbiddenWarning();
    }

    private static void TutorialGameIncludesMvpFactions()
    {
        var game = TutorialGameFactory.Create();

        var coastal = game.FindFaction("coastal-people");
        var wardens = game.FindFaction("border-wardens");
        var hidden = game.FindFaction("hidden-ones");

        AssertTrue(coastal != null, "Coastal People exist");
        AssertTrue(wardens != null, "Border Wardens exist");
        AssertTrue(hidden != null, "Hidden Ones exist");
        AssertEqual(FactionContactStatus.Contacted, coastal!.ContactStatus, "Coastal contact status");
        AssertEqual(FactionContactStatus.Rumored, hidden!.ContactStatus, "Hidden Ones contact status");
        AssertTrue(coastal.Memories.Any(memory => memory.Contains("warned")), "Coastal faction has partial warning memory");
        AssertTrue(wardens!.WarningZones.Count > 0, "Border Wardens have warning zones");
        AssertTrue(hidden.WarningZones.Count > 0, "Hidden Ones have warning zones");
        AssertTrue(game.World.Map.Tiles.Count(tile => tile.OwnerId == "coastal-people") >= 20, "Coastal People own a visible starting territory");
        AssertTrue(game.World.Map.Tiles.Count(tile => tile.OwnerId == "border-wardens") >= 60, "Border Wardens own a visible border territory");
        AssertTrue(game.World.Map.Tiles.Count(tile => tile.OwnerId == "hidden-ones") >= 20, "Hidden Ones own a visible hidden territory");
        AssertTrue(string.IsNullOrWhiteSpace(game.World.Map.GetTile(game.Base.Location).OwnerId), "Base is not inside faction territory");
        AssertFalse(game.Base.Location.Neighbors().Any(coord => game.World.Map.Contains(coord) && game.World.Map.GetTile(coord).OwnerId == "coastal-people"), "Coastal People do not surround the base");
        AssertFalse(OwnersTouch(game.World.Map, "border-wardens", "hidden-ones"), "Border Wardens and Hidden Ones territories do not touch");
        AssertTrue(MinOwnerDistanceTo(game.World.Map, game.Base.Location, "hidden-ones") >= 20, "Hidden Ones are far from the base");
    }

    private static void EnteringBorderWardenWarningZoneQueuesEventAndMemoryOnce()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var warningCoord = new HexCoord(2, 1);
        map.SetTile(map.GetTile(warningCoord).WithOwner("border-wardens"));
        var knowledge = new KnowledgeState();
        var origin = new HexCoord(1, 1);
        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);
        var expedition = new ExpeditionState(
            1,
            origin,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            movementPoints: 4,
            maxMovementPoints: 4,
            supplies: 10,
            medicine: 2,
            morale: 60,
            capacity: 10);
        var faction = new FactionState("border-wardens", "Border Wardens", warningZones: new[] { warningCoord });
        var game = new GameState(new WorldState(map), knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero), factions: new[] { faction });
        var command = new MoveExpeditionCommand(new MovementCostService());

        var first = command.Execute(game, warningCoord);
        var returnMove = command.Execute(game, origin);
        game.Expedition.AdvanceExpeditionDay();
        var second = command.Execute(game, warningCoord);

        AssertTrue(first.Success, "First warning zone move succeeds");
        AssertTrue(returnMove.Success, "Return move succeeds");
        AssertTrue(second.Success, "Second warning zone move succeeds");
        AssertEqual(1, game.Events.PendingCount, "Warning zone event queues once");
        AssertEqual(EventKind.WarningSign, game.Events.Current!.Kind, "Warning event kind");
        AssertEqual(10, game.Expedition.UnsecuredKnowledge, "Warning zone grants first faction knowledge once");
        AssertTrue(faction.Anger > 0, "Faction anger increases");
        AssertTrue(faction.HasMemory("warning-zone-entered:2:1"), "Faction remembers entered warning zone");
    }

    private static void EnteringFactionTerritoryAddsKnowledgeOnce()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var territoryCoord = new HexCoord(2, 1);
        var origin = new HexCoord(1, 1);
        map.SetTile(map.GetTile(territoryCoord).WithOwner("coastal-people"));
        var knowledge = new KnowledgeState();
        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);
        var expedition = new ExpeditionState(
            1,
            origin,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            movementPoints: 4,
            maxMovementPoints: 4,
            supplies: 10,
            medicine: 2,
            morale: 60,
            capacity: 10);
        var faction = new FactionState("coastal-people", "Coastal People", FactionContactStatus.Contacted);
        var game = new GameState(new WorldState(map), knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero), factions: new[] { faction });
        var command = new MoveExpeditionCommand(new MovementCostService());

        var first = command.Execute(game, territoryCoord);
        var returnMove = command.Execute(game, origin);
        game.Expedition.AdvanceExpeditionDay();
        var second = command.Execute(game, territoryCoord);

        AssertTrue(first.Success, "First faction territory move succeeds");
        AssertTrue(returnMove.Success, "Return from faction territory succeeds");
        AssertTrue(second.Success, "Second faction territory move succeeds");
        AssertEqual(10, game.Expedition.UnsecuredKnowledge, "Faction territory grants knowledge once");
        AssertEqual(1, game.Events.PendingCount, "Non-warning faction territory queues one reaction event per expedition");
        AssertEqual(EventKind.FactionReaction, game.Events.Current!.Kind, "Faction reaction event kind");
        AssertEqual(FactionContactStatus.Open, faction.ContactStatus, "Friendly faction opens after peaceful territory contact");
        AssertTrue(faction.Trust > 0, "Friendly faction trust increases");
        AssertTrue(faction.Memories.Any(memory => memory.Contains("entered-territory")), "Faction remembers territory entry");
        AssertTrue(faction.Memories.Any(memory => memory.Contains("territory-entry-expedition-1")), "Faction remembers expedition territory reaction");
    }

    private static void EnteringHiddenTerritoryCreatesDangerReaction()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Forest);
        var territoryCoord = new HexCoord(2, 1);
        var origin = new HexCoord(1, 1);
        map.SetTile(map.GetTile(territoryCoord).WithOwner("hidden-ones"));
        var knowledge = new KnowledgeState();
        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);
        var expedition = new ExpeditionState(
            1,
            origin,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            movementPoints: 4,
            maxMovementPoints: 4,
            supplies: 10,
            medicine: 2,
            morale: 60,
            capacity: 10);
        var faction = new FactionState("hidden-ones", "Hidden Ones", FactionContactStatus.Rumored);
        var game = new GameState(new WorldState(map), knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero), factions: new[] { faction });
        var command = new MoveExpeditionCommand(new MovementCostService());

        var result = command.Execute(game, territoryCoord);

        AssertTrue(result.Success, "Hidden territory move succeeds");
        AssertEqual(1, game.Events.PendingCount, "Hidden territory queues reaction event");
        AssertEqual(EventKind.FactionReaction, game.Events.Current!.Kind, "Hidden territory reaction event kind");
        AssertTrue(game.Events.Current.Body.Contains("seen them first"), "Hidden reaction communicates observation");
        AssertTrue(faction.Anger > 0, "Hidden faction anger increases");
        AssertTrue(faction.Fear > 0, "Hidden faction fear increases");
        AssertTrue(faction.Memories.Any(memory => memory.Contains("territory-entry-expedition-1")), "Hidden faction remembers territory reaction");
    }

    private static void FactionDefinitionsProvideMvpOfferSet()
    {
        var offers = FactionInteractionDefinitions.OfferDefinitions;

        AssertTrue(offers.Count >= 5 && offers.Count <= 10, "MVP offer set has curated scope");
        AssertTrue(offers.Any(offer => offer.FactionId == "coastal-people"), "Coastal offers exist");
        AssertTrue(offers.Any(offer => offer.FactionId == "border-wardens"), "Border Warden offers exist");
        AssertTrue(offers.Any(offer => offer.FactionId == "hidden-ones"), "Hidden Ones offers exist");
        AssertFalse(offers.Any(offer => offer.FactionId == "hidden-ones" && offer.EffectKind == FactionOfferEffectKind.SuppliesForKnowledge), "Hidden Ones do not provide normal trade");
        AssertTrue(offers.Any(offer => offer.RequiredLeverageItemId == FactionInteractionDefinitions.HiddenSealedSymbolId), "Hidden leverage offer exists");
    }

    private static void FactionReactionCanOpenRepresentativeInteraction()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var territoryCoord = new HexCoord(2, 1);
        var origin = new HexCoord(1, 1);
        map.SetTile(map.GetTile(territoryCoord).WithOwner("border-wardens"));
        var knowledge = new KnowledgeState();
        new KnowledgeService().RevealFromExpedition(map, knowledge, origin);
        var expedition = new ExpeditionState(
            1,
            origin,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            movementPoints: 4,
            maxMovementPoints: 4,
            supplies: 10,
            medicine: 2,
            morale: 60,
            capacity: 10);
        var faction = new FactionState("border-wardens", "Border Wardens", FactionContactStatus.Rumored);
        var game = new GameState(new WorldState(map), knowledge, new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero), factions: new[] { faction });

        var move = new MoveExpeditionCommand(new MovementCostService()).Execute(game, territoryCoord);
        var contactOption = game.Events.Current!.Options.First(option => option.EffectKind == EventOptionEffectKind.OpenFactionInteraction);
        var open = new ResolveEventCommand().Execute(game, game.Events.Current.Id, contactOption.Id);

        AssertTrue(move.Success, "Faction territory move succeeds");
        AssertTrue(open.Success, "Faction contact option resolves");
        AssertTrue(game.ActiveFactionInteraction != null, "Faction interaction is active");
        AssertEqual("border-wardens", game.ActiveFactionInteraction!.FactionId, "Active interaction faction");
        AssertEqual(FactionRepresentativeRole.Guard, game.ActiveFactionInteraction.Representative.Role, "Border contact starts with guard");
        AssertTrue(game.ActiveFactionInteraction.Offers.Any(offer => offer.Id == "knowledge-for-supplies"), "Supply offer is present");
        AssertTrue(game.ActiveFactionInteraction.Offers.Any(offer => offer.LockedReason == "Requires grave token"), "Locked leverage offer is visible");
    }

    private static void FactionOfferCanTradeKnowledgeForSupplies()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var coord = new HexCoord(2, 1);
        var expedition = new ExpeditionState(
            1,
            coord,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            supplies: 10);
        var faction = new FactionState("coastal-people", "Coastal People", FactionContactStatus.Open);
        var baseState = new BaseState(HexCoord.Zero);
        baseState.AddKnowledgePoints(10);
        var game = new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition, baseState, factions: new[] { faction });
        var open = new OpenFactionInteractionCommand().Execute(game, "coastal-people", coord);

        var result = new PurchaseFactionOfferCommand().Execute(game, "knowledge-for-supplies");

        AssertTrue(open.Success, "Open coastal interaction succeeds");
        AssertTrue(result.Success, "Supply trade succeeds");
        AssertEqual(20, game.Expedition.Supplies, "Supply offer adds supplies");
        AssertEqual(4, game.Base.KnowledgePoints, "Supply offer spends knowledge");
        AssertTrue(game.Base.ArchiveEntries.Any(entry => entry.Contains("10 Supplies")), "Offer is archived");
    }

    private static void GraveTokenUnlocksBorderWardenNegotiation()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var coord = new HexCoord(2, 1);
        var expedition = new ExpeditionState(
            1,
            coord,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            supplies: 10);
        var faction = new FactionState("border-wardens", "Border Wardens", FactionContactStatus.Rumored, anger: 10);
        var leverage = new LeverageInventoryState(new[] { FactionInteractionDefinitions.BorderWardenGraveTokenId });
        var leverageDefinition = FactionInteractionDefinitions.LeverageDefinitions.First(definition => definition.ItemId == FactionInteractionDefinitions.BorderWardenGraveTokenId);
        var offerDefinition = FactionInteractionDefinitions.OfferDefinitions.First(definition => definition.Id == leverageDefinition.UnlocksOfferId);
        var game = new GameState(
            new WorldState(map),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero),
            factions: new[] { faction },
            leverageItems: leverage);
        var open = new OpenFactionInteractionCommand().Execute(game, "border-wardens", coord);
        var offer = game.ActiveFactionInteraction!.FindOffer("grave-token-passage");

        var result = new PurchaseFactionOfferCommand().Execute(game, "grave-token-passage");

        AssertEqual("border-wardens", leverageDefinition.InterestedFactionId, "Leverage definition targets Border Wardens");
        AssertEqual(FactionInteractionDefinitions.BorderWardenGraveTokenId, offerDefinition.RequiredLeverageItemId, "Offer definition requires grave token");
        AssertTrue(open.Success, "Open border warden interaction succeeds");
        AssertTrue(offer != null && offer.IsAvailable, "Grave token offer is unlocked");
        AssertTrue(result.Success, "Grave token negotiation succeeds");
        AssertFalse(game.LeverageItems.Contains(FactionInteractionDefinitions.BorderWardenGraveTokenId), "Grave token is consumed");
        AssertTrue(faction.HasMemory(FactionInteractionDefinitions.BorderWardenGraveTokenReturnedMemory), "Border Wardens remember returned grave token");
        AssertTrue(faction.Trust > 0, "Border Warden trust improves");
        AssertTrue(game.PlayerNotes.Markers.Any(marker => marker.Kind == PlayerMapMarkerKind.FactionContact && marker.FactionId == "border-wardens"), "Passage marker is created");
    }

    private static void CoastalChartUnlocksGuidanceOffer()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Grassland);
        var coord = new HexCoord(1, 1);
        var expedition = new ExpeditionState(
            1,
            coord,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            supplies: 10);
        var faction = new FactionState("coastal-people", "Coastal People", FactionContactStatus.Open);
        var leverage = new LeverageInventoryState(new[] { FactionInteractionDefinitions.CoastalRiverChartFragmentId });
        var game = new GameState(
            new WorldState(map),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero),
            factions: new[] { faction },
            leverageItems: leverage);
        var open = new OpenFactionInteractionCommand().Execute(game, "coastal-people", coord);
        var offer = game.ActiveFactionInteraction!.FindOffer("coastal-chart-guidance");

        var result = new PurchaseFactionOfferCommand().Execute(game, "coastal-chart-guidance");

        AssertTrue(open.Success, "Open coastal interaction succeeds");
        AssertTrue(offer != null && offer.IsAvailable, "Coastal chart offer is unlocked");
        AssertTrue(result.Success, "Coastal chart guidance succeeds");
        AssertTrue(game.LeverageItems.Contains(FactionInteractionDefinitions.CoastalRiverChartFragmentId), "Coastal chart is kept as evidence");
        AssertTrue(faction.HasMemory(FactionInteractionDefinitions.CoastalRiverChartSharedMemory), "Coastal faction remembers shared chart");
        AssertTrue(game.PlayerNotes.Markers.Any(marker => marker.Kind == PlayerMapMarkerKind.FactionRumor && marker.FactionId == "coastal-people"), "Route marker is created");
    }

    private static void HiddenOpenContactCanRecordForbiddenWarning()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Forest);
        var coord = new HexCoord(2, 1);
        var expedition = new ExpeditionState(
            1,
            coord,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) },
            supplies: 10);
        var faction = new FactionState("hidden-ones", "Hidden Ones", FactionContactStatus.Open);
        var leverage = new LeverageInventoryState(new[] { FactionInteractionDefinitions.HiddenSealedSymbolId });
        var game = new GameState(
            new WorldState(map),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero),
            factions: new[] { faction },
            leverageItems: leverage);
        var open = new OpenFactionInteractionCommand().Execute(game, "hidden-ones", coord);
        var offer = game.ActiveFactionInteraction!.FindOffer("hidden-sealed-symbol-reading");

        var result = new PurchaseFactionOfferCommand().Execute(game, "hidden-sealed-symbol-reading");

        AssertTrue(open.Success, "Open hidden interaction succeeds when contact is open");
        AssertTrue(offer != null && offer.IsAvailable, "Hidden sealed symbol offer is unlocked");
        AssertTrue(result.Success, "Hidden warning offer succeeds");
        AssertTrue(game.LeverageItems.Contains(FactionInteractionDefinitions.HiddenSealedSymbolId), "Hidden symbol is kept as evidence");
        AssertTrue(faction.HasMemory(FactionInteractionDefinitions.HiddenSealedSymbolUnderstoodMemory), "Hidden faction remembers sealed symbol exchange");
        AssertTrue(game.PlayerNotes.Markers.Any(marker => marker.Kind == PlayerMapMarkerKind.Danger && marker.FactionId == "hidden-ones"), "Forbidden zone marker is created");
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

    private static bool OwnersTouch(HexMapState map, string firstOwnerId, string secondOwnerId)
    {
        foreach (var tile in map.Tiles)
        {
            if (tile.OwnerId != firstOwnerId)
            {
                continue;
            }

            foreach (var neighbor in tile.Coord.Neighbors())
            {
                if (map.Contains(neighbor) && map.GetTile(neighbor).OwnerId == secondOwnerId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int MinOwnerDistanceTo(HexMapState map, HexCoord origin, string ownerId)
    {
        var minDistance = int.MaxValue;
        foreach (var tile in map.Tiles)
        {
            if (tile.OwnerId == ownerId)
            {
                minDistance = Math.Min(minDistance, origin.DistanceTo(tile.Coord));
            }
        }

        return minDistance;
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

internal sealed class BaseGameplayCommandTests
{
    public void RunAll()
    {
        HealRestoresInjuredRosterMemberAndCostsKnowledgeAndTime();
        RecruitAddsMemberAndSpendsKnowledgeAndTime();
        RequestEngineerUnlocksEngineerOnceAndAddsItToRoster();
        ComposedExpeditionUsesSelectedRosterMembersIncludingEngineer();
        BaseTimeProducesDeterministicWorldReaction();
        RosterCarriesRichProfileData();
        ComposingWithUnavailableMemberIsRejected();
    }

    private static GameState ReturnedGameAtBase(int knowledgePoints = 50)
    {
        var game = TutorialGameFactory.Create();
        game.Base.AddKnowledgePoints(knowledgePoints);
        var complete = new CompleteExpeditionCommand().Execute(game);
        AssertTrue(complete.Success, "Setup: expedition returns to base");
        return game;
    }

    private static void HealRestoresInjuredRosterMemberAndCostsKnowledgeAndTime()
    {
        var game = TutorialGameFactory.Create();
        game.Base.AddKnowledgePoints(50);
        game.Expedition.FindMember("scout-1")!.SetStatus(ExpeditionMemberStatus.Injured);
        new CompleteExpeditionCommand().Execute(game);

        var member = game.Roster.FindMember("scout-1")!;
        AssertEqual(ExpeditionMemberStatus.Injured, member.Status, "Injured status carries into the roster on return");

        var knowledgeBefore = game.Base.KnowledgePoints;
        var dayBefore = game.World.WorldDay;
        var heal = new StartBaseActionCommand().Execute(game, BaseActionKind.HealMember, "scout-1");

        AssertTrue(heal.Success, "Heal succeeds");
        AssertEqual(ExpeditionMemberStatus.Available, member.Status, "Healed member becomes available");
        AssertEqual(knowledgeBefore - StartBaseActionCommand.HealCost, game.Base.KnowledgePoints, "Heal spends knowledge");
        AssertEqual(dayBefore + StartBaseActionCommand.HealDays, game.World.WorldDay, "Heal advances base time");
    }

    private static void RecruitAddsMemberAndSpendsKnowledgeAndTime()
    {
        var game = ReturnedGameAtBase();
        var countBefore = game.Roster.Members.Count;
        var knowledgeBefore = game.Base.KnowledgePoints;
        var dayBefore = game.World.WorldDay;

        var recruit = new StartBaseActionCommand().Execute(game, BaseActionKind.RecruitMember);

        AssertTrue(recruit.Success, "Recruit succeeds");
        AssertEqual(countBefore + 1, game.Roster.Members.Count, "Recruit adds a roster member");
        AssertEqual(knowledgeBefore - StartBaseActionCommand.RecruitCost, game.Base.KnowledgePoints, "Recruit spends knowledge");
        AssertEqual(dayBefore + StartBaseActionCommand.RecruitDays, game.World.WorldDay, "Recruit advances base time");
    }

    private static void RequestEngineerUnlocksEngineerOnceAndAddsItToRoster()
    {
        var game = ReturnedGameAtBase();
        AssertFalse(game.Base.HasRequestedEngineer, "No engineer requested initially");

        var request = new StartBaseActionCommand().Execute(game, BaseActionKind.RequestEngineer);
        AssertTrue(request.Success, "Engineer request succeeds");
        AssertTrue(game.Base.HasRequestedEngineer, "Engineer request flag is set");
        AssertTrue(game.Roster.Available().Any(member => member.Role == ExpeditionMemberRole.Engineer), "Engineer joins the roster");

        var second = new StartBaseActionCommand().Execute(game, BaseActionKind.RequestEngineer);
        AssertFalse(second.Success, "Engineer cannot be requested twice");
    }

    private static void ComposedExpeditionUsesSelectedRosterMembersIncludingEngineer()
    {
        var game = ReturnedGameAtBase();
        new StartBaseActionCommand().Execute(game, BaseActionKind.RequestEngineer);
        var engineer = game.Roster.Available().First(member => member.Role == ExpeditionMemberRole.Engineer);
        var selected = new[] { "scout-1", "guard-1", engineer.Id };

        var start = new StartNewExpeditionCommand().Execute(game, selected);

        AssertTrue(start.Success, "Composed expedition starts");
        AssertEqual(3, game.Expedition.Members.Count, "Expedition uses only the selected members");
        AssertTrue(game.Expedition.Members.Any(member => member.Id == "scout-1"), "Selected member joins the expedition");
        AssertTrue(game.Expedition.Members.Any(member => member.Role == ExpeditionMemberRole.Engineer), "Engineer joins the second expedition");
        AssertEqual(2, game.Expedition.ExpeditionNumber, "Second expedition number");
    }

    private static void BaseTimeProducesDeterministicWorldReaction()
    {
        var game = ReturnedGameAtBase(0);
        var entriesBefore = game.Base.ArchiveEntries.Count;

        var advance = new AdvanceBaseTimeCommand().Execute(game, 1);

        AssertTrue(advance.Success, "Base time advances");
        AssertTrue(advance.WorldReactionEntry != null, "Base time yields a world reaction");
        AssertTrue(game.Base.ArchiveEntries.Count > entriesBefore, "World reaction is archived");
    }

    private static void RosterCarriesRichProfileData()
    {
        var game = TutorialGameFactory.Create();
        var sela = game.Roster.FindMember("medic-1")!;

        AssertEqual(5, sela.Level, "Roster member carries a level");
        AssertTrue(sela.Skills.Count > 0, "Roster member carries skills");
        AssertTrue(sela.Gear.Count > 0, "Roster member carries gear");
        AssertTrue(!string.IsNullOrWhiteSpace(sela.Bio), "Roster member carries a bio");
    }

    private static void ComposingWithUnavailableMemberIsRejected()
    {
        var game = ReturnedGameAtBase();
        new AdvanceBaseTimeCommand().Execute(game, CompleteExpeditionCommand.NormalPreparationDays);
        AssertEqual(ExpeditionMemberStatus.Injured, game.Roster.FindMember("guard-2")!.Status, "Injured member stays injured in the pool");

        var rejected = new StartNewExpeditionCommand().Execute(game, new[] { "scout-1", "guard-2" });
        AssertFalse(rejected.Success, "Composing with an unavailable member is rejected");

        var ok = new StartNewExpeditionCommand().Execute(game, new[] { "scout-1", "guard-1" });
        AssertTrue(ok.Success, "Composing with available members succeeds");
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

internal sealed class BaseUpgradeCommandTests
{
    public void RunAll()
    {
        BuildingAnAvailableUpgradeSpendsKnowledgeAndPersists();
        PrerequisiteGatingBlocksLockedUpgrades();
        AlreadyBuiltAndInsufficientKnowledgeAreRejected();
        HerbalismUpgradeMakesHealingCheaper();
    }

    private static GameState ReturnedGameAtBase(int knowledgePoints = 50)
    {
        var game = TutorialGameFactory.Create();
        game.Base.AddKnowledgePoints(knowledgePoints);
        var complete = new CompleteExpeditionCommand().Execute(game);
        AssertTrue(complete.Success, "Setup: expedition returns to base");
        return game;
    }

    private static void BuildingAnAvailableUpgradeSpendsKnowledgeAndPersists()
    {
        var game = ReturnedGameAtBase();
        var knowledgeBefore = game.Base.KnowledgePoints;
        AssertFalse(game.Base.Upgrades.IsBuilt("gerberei"), "Gerberei starts unbuilt");

        var result = new StartUpgradeCommand().Execute(game, "gerberei");

        AssertTrue(result.Success, "Building an available upgrade succeeds");
        AssertTrue(game.Base.Upgrades.IsBuilt("gerberei"), "Upgrade is marked built");
        AssertEqual(knowledgeBefore - 3, game.Base.KnowledgePoints, "Upgrade spends its knowledge cost");
    }

    private static void PrerequisiteGatingBlocksLockedUpgrades()
    {
        var game = ReturnedGameAtBase();

        var locked = new StartUpgradeCommand().Execute(game, "ausbildungsplatz");
        AssertFalse(locked.Success, "Upgrade with unmet prerequisites is rejected");

        var prerequisite = new StartUpgradeCommand().Execute(game, "baracken");
        AssertTrue(prerequisite.Success, "Prerequisite upgrade can be built");

        var unlocked = new StartUpgradeCommand().Execute(game, "ausbildungsplatz");
        AssertTrue(unlocked.Success, "Upgrade builds once prerequisites are met");
    }

    private static void AlreadyBuiltAndInsufficientKnowledgeAreRejected()
    {
        var game = ReturnedGameAtBase(0);

        var alreadyBuilt = new StartUpgradeCommand().Execute(game, "schmiede");
        AssertFalse(alreadyBuilt.Success, "Already-built upgrade is rejected");

        var tooExpensive = new StartUpgradeCommand().Execute(game, "signalturm");
        AssertFalse(tooExpensive.Success, "Upgrade without enough knowledge is rejected");
    }

    private static void HerbalismUpgradeMakesHealingCheaper()
    {
        var game = ReturnedGameAtBase();

        var full = new StartBaseActionCommand().Execute(game, BaseActionKind.HealMember, "guard-2");
        AssertTrue(full.Success, "Heal without herbalism succeeds");
        AssertEqual(StartBaseActionCommand.HealCost, full.KnowledgeSpent, "Heal costs the full amount without herbalism");

        var herbalism = new StartUpgradeCommand().Execute(game, "kraeuterkunde");
        AssertTrue(herbalism.Success, "Kraeuterkunde builds");

        var cheaper = new StartBaseActionCommand().Execute(game, BaseActionKind.HealMember, "carrier-2");
        AssertTrue(cheaper.Success, "Heal with herbalism succeeds");
        AssertEqual(StartBaseActionCommand.HealCostWithHerbalism, cheaper.KnowledgeSpent, "Heal costs less with herbalism");
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

internal sealed class BaseLoadoutCommandTests
{
    public void RunAll()
    {
        LoadoutStartUsesSelectedUnitsAndResources();
        OverloadedLoadoutIsRejected();
        RationsAreClampedToBudget();
        ComputeReadinessReflectsPortersAndExhaustion();
        BaracksUpgradeGrowsSoldierStock();
    }

    private static GameState ReturnedGameAtBase(int knowledgePoints = 50)
    {
        var game = TutorialGameFactory.Create();
        game.Base.AddKnowledgePoints(knowledgePoints);
        var complete = new CompleteExpeditionCommand().Execute(game);
        AssertTrue(complete.Success, "Setup: expedition returns to base");
        new AdvanceBaseTimeCommand().Execute(game, CompleteExpeditionCommand.NormalPreparationDays);
        return game;
    }

    private static void LoadoutStartUsesSelectedUnitsAndResources()
    {
        var game = ReturnedGameAtBase();
        var porter = game.Base.UnitStock.Porters.First(p => !p.IsExhausted);
        var soldier = game.Base.UnitStock.Soldiers.First(s => !s.IsExhausted);
        var members = new[] { "scout-1", "guard-1" };
        var units = new[] { porter.Id, soldier.Id };
        var expectedCapacity = ExpeditionReadiness.Compute(members.Length, new[] { porter, soldier }, 8, 2).CarryCapacity;

        var start = new StartNewExpeditionCommand().Execute(game, members, units, 8, 2);

        AssertTrue(start.Success, "Valid loadout departs");
        AssertEqual(2, game.Expedition.Members.Count, "Loadout uses the selected members");
        AssertEqual(8, game.Expedition.Supplies, "Rations become expedition supplies");
        AssertEqual(2, game.Expedition.Medicine, "Medicine is carried");
        AssertEqual(expectedCapacity, game.Expedition.Capacity, "Capacity comes from readiness");
    }

    private static void OverloadedLoadoutIsRejected()
    {
        var game = ReturnedGameAtBase();
        var members = new[] { "scout-1", "guard-1" };

        var overloaded = new StartNewExpeditionCommand().Execute(game, members, System.Array.Empty<string>(), 40, 4);
        AssertFalse(overloaded.Success, "A loadout that exceeds carry capacity is rejected");
    }

    private static void RationsAreClampedToBudget()
    {
        var game = ReturnedGameAtBase();
        var members = new[] { "scout-1", "guard-1" };
        var allUnits = game.Base.UnitStock.Units.Select(u => u.Id).ToArray();

        var start = new StartNewExpeditionCommand().Execute(game, members, allUnits, 100, 0);

        AssertTrue(start.Success, "Loadout with full porter support departs");
        AssertEqual(StartNewExpeditionCommand.RationBudget, game.Expedition.Supplies, "Rations are clamped to the base budget");
    }

    private static void ComputeReadinessReflectsPortersAndExhaustion()
    {
        var fresh = new BaseUnitState("p-fresh", BaseUnitKind.Porter);
        var tired = new BaseUnitState("p-tired", BaseUnitKind.Porter, isExhausted: true);

        var freshReadiness = ExpeditionReadiness.Compute(1, new[] { fresh }, 0, 0);
        AssertEqual(8, freshReadiness.CarryCapacity, "A fresh porter adds full carry capacity");
        AssertFalse(freshReadiness.SlowMarch, "A fresh loadout marches at full speed");

        var tiredReadiness = ExpeditionReadiness.Compute(1, new[] { tired }, 0, 0);
        AssertEqual(6, tiredReadiness.CarryCapacity, "An exhausted porter adds less carry capacity");
        AssertTrue(tiredReadiness.SlowMarch, "An exhausted unit slows the march");
    }

    private static void BaracksUpgradeGrowsSoldierStock()
    {
        var game = ReturnedGameAtBase();
        var soldiersBefore = game.Base.UnitStock.Soldiers.Count;

        var built = new StartUpgradeCommand().Execute(game, "baracken");

        AssertTrue(built.Success, "Baracken builds");
        AssertEqual(soldiersBefore + 2, game.Base.UnitStock.Soldiers.Count, "Baracken grows the soldier stock");
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

internal sealed class ArchiveCommandTests
{
    public void RunAll()
    {
        PlainStringWriteBecomesNoteAndProjectsBack();
        EvaluatedInsightIsTypedErkenntnis();
        ReturnedExpeditionIsTypedBericht();
        FilterByKindAndSearchWork();
    }

    private static void PlainStringWriteBecomesNoteAndProjectsBack()
    {
        var game = TutorialGameFactory.Create();
        var before = game.Base.Archive.Count;
        game.Base.AddArchiveEntry("Ein Testeintrag");

        var last = game.Base.Archive[game.Base.Archive.Count - 1];
        AssertEqual(ArchiveEntryKind.Notiz, last.Kind, "Plain-string archive writes become notes");
        AssertEqual("Ein Testeintrag", last.Text, "Text projects back to the original string");
        AssertTrue(game.Base.ArchiveEntries.Contains("Ein Testeintrag"), "String projection still exposes the entry");
        AssertEqual(before + 1, game.Base.Archive.Count, "Archive grew by exactly one");
    }

    private static void EvaluatedInsightIsTypedErkenntnis()
    {
        var game = TutorialGameFactory.Create();
        new CompleteExpeditionCommand().Execute(game);
        EvaluationQueueCommandTests.SeedEvaluationItems(game);
        var result = new EvaluateKnowledgeItemCommand().Execute(game, "eval-pfaehle");
        AssertTrue(result.Success, "Evaluation succeeds");

        var insight = game.Base.Archive.First(e => e.Kind == ArchiveEntryKind.Erkenntnis);
        AssertEqual("Auswertung", insight.Source, "Insight source is the evaluation");
        AssertEqual(game.World.WorldDay, insight.WorldDay, "Insight carries the world day");
        AssertEqual(ArchiveReliability.Bestaetigt, insight.Reliability, "Insight is confirmed");
    }

    private static void ReturnedExpeditionIsTypedBericht()
    {
        var game = TutorialGameFactory.Create();
        new CompleteExpeditionCommand().Execute(game);
        AssertTrue(game.Base.Archive.Any(e => e.Kind == ArchiveEntryKind.Bericht), "The return summary is a typed report");
    }

    private static void FilterByKindAndSearchWork()
    {
        var entries = new List<ArchiveEntryState>
        {
            new ArchiveEntryState("Rauch hinter dem Kamm", ArchiveEntryKind.Bericht, "Späher", 5, ArchiveReliability.Mittel),
            new ArchiveEntryState("Handelsvertrag — Aschegilde", ArchiveEntryKind.Vertrag, "Nima", 6, ArchiveReliability.Bestaetigt),
            new ArchiveEntryState("Vergessene Handelsroute", ArchiveEntryKind.Erkenntnis, "Auswertung", 6, ArchiveReliability.Bestaetigt)
        };

        var berichte = ArchiveFilter.Filter(entries, ArchiveEntryKind.Bericht, null);
        AssertEqual(1, berichte.Count, "Kind filter returns only that kind");

        var handel = ArchiveFilter.Filter(entries, null, "handel");
        AssertEqual(2, handel.Count, "Text search matches titles case-insensitively");

        var bySource = ArchiveFilter.Filter(entries, null, "nima");
        AssertEqual(1, bySource.Count, "Text search also matches the source");

        AssertEqual(1, ArchiveFilter.CountOfKind(entries, ArchiveEntryKind.Vertrag), "CountOfKind counts a single kind");
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

internal sealed class EvaluationQueueCommandTests
{
    public void RunAll()
    {
        EvaluatingAReadyItemAwardsKnowledgeAndArchivesInsight();
        NotReadyItemIsRejected();
        BaseTimeMaturesItemsRespectingEvaluatorCapacity();
    }

    private static GameState ReturnedGameAtBase()
    {
        var game = TutorialGameFactory.Create();
        var complete = new CompleteExpeditionCommand().Execute(game);
        AssertTrue(complete.Success, "Setup: expedition returns to base");
        SeedEvaluationItems(game);
        return game;
    }

    public static void SeedEvaluationItems(GameState game)
    {
        game.Base.EvaluationQueue.Add(new EvaluationItemState(
            "eval-pfaehle",
            "Geschnitzte Pfähle",
            "Feld 14 / 08 · Expedition 1",
            1,
            3,
            "Die Pfähle markieren das Revier der Grenzwächter. Ihr Betreten gilt als Provokation; Umgehung oder Tribut empfohlen.",
            progressDays: 1));
        game.Base.EvaluationQueue.Add(new EvaluationItemState(
            "eval-saat",
            "Fremde Saatkörner",
            "Verlassenes Lager · Jonas",
            3,
            4,
            "Die Saatkörner gedeihen selbst in kargem Boden. Eine verlässliche Nahrungsquelle für längere Züge."));
        game.Base.EvaluationQueue.Add(new EvaluationItemState(
            "eval-karte",
            "Bruchstück einer Karte",
            "Kammland-Nord · Mara",
            4,
            5,
            "Das Kartenfragment zeigt einen alten Pfad durch den Kamm nach Osten."));
        game.Base.EvaluationQueue.Add(new EvaluationItemState(
            "eval-metall",
            "Unbekanntes Metall",
            "Aschegilde · Handel",
            5,
            4,
            "Ein ungewöhnlich leichtes, hartes Metall. Eine Fraktion könnte Interesse an der Quelle haben."));
    }

    private static void EvaluatingAReadyItemAwardsKnowledgeAndArchivesInsight()
    {
        var game = ReturnedGameAtBase();
        var item = game.Base.EvaluationQueue.FindItem("eval-pfaehle")!;
        AssertTrue(item.IsReady, "Seeded item starts ready");

        var knowledgeBefore = game.Base.KnowledgePoints;
        var result = new EvaluateKnowledgeItemCommand().Execute(game, "eval-pfaehle");

        AssertTrue(result.Success, "Evaluating a ready item succeeds");
        AssertEqual(knowledgeBefore + item.KnowledgeReward, game.Base.KnowledgePoints, "Evaluation awards knowledge");
        AssertTrue(game.Base.EvaluationQueue.FindItem("eval-pfaehle")!.IsEvaluated, "Item is marked evaluated");
        AssertTrue(game.Base.ArchiveEntries.Any(entry => entry.Contains("Geschnitzte")), "Insight is archived");

        var again = new EvaluateKnowledgeItemCommand().Execute(game, "eval-pfaehle");
        AssertFalse(again.Success, "The same item cannot be evaluated twice");
    }

    private static void NotReadyItemIsRejected()
    {
        var game = ReturnedGameAtBase();
        var result = new EvaluateKnowledgeItemCommand().Execute(game, "eval-saat");
        AssertFalse(result.Success, "An item still being evaluated cannot be collected");
    }

    private static void BaseTimeMaturesItemsRespectingEvaluatorCapacity()
    {
        var game = ReturnedGameAtBase();

        new AdvanceBaseTimeCommand().Execute(game, 3);

        AssertEqual(3, game.Base.EvaluationQueue.FindItem("eval-saat")!.ProgressDays, "First queued item progressed");
        AssertEqual(3, game.Base.EvaluationQueue.FindItem("eval-karte")!.ProgressDays, "Second queued item progressed");
        AssertEqual(0, game.Base.EvaluationQueue.FindItem("eval-metall")!.ProgressDays, "Third item waited (capacity 2)");
        AssertTrue(game.Base.EvaluationQueue.FindItem("eval-saat")!.IsReady, "Item becomes ready after enough base time");
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
