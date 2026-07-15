#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

/// <summary>
/// End-to-end proof flows for the reusable location/scouting/world contracts. The two examples
/// use different archetypes, but neither flow receives variant-specific simulation code.
/// </summary>
internal sealed class CrossSystemIntegrationProofTests
{
    public void RunAll()
    {
        RouteObstacleFlowConnectsInspectionScoutingProjectAndWorldReaction();
        SealedContainmentFlowConnectsInspectionScoutingOpeningAndDelayedConsequences();
        SealedModifierCanReuseTheOpeningFlowOnAnotherCompatibleArchetype();
    }

    private static void RouteObstacleFlowConnectsInspectionScoutingProjectAndWorldReaction()
    {
        var bridgeCoord = new HexCoord(2, 1);
        var bridge = new SpecialLocationState(
            "location-route-proof",
            LocationKind.BrokenRavine,
            bridgeCoord,
            "Broken Crossing",
            LocationAnchor.Edge(new HexCoord(1, 1), bridgeCoord),
            archetypeId: "route-obstacle",
            variantId: "broken-bridge",
            modifierIds: new[] { "modifier-repairable", "modifier-watched" },
            operationalStateId: "blocked",
            factionRelations: new[] { new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Watched) },
            evidenceSeedIds: new[] { "evidence-claim-markers" });
        var (app, game) = CreateGame(bridge, bridgeCoord, new FactionState("faction-1", "Unknown Watchers", reactionProfileId: "hostile", signatureProfileId: "carved-boundaries"));

        AssertTrue(app.InspectLocation(game, bridgeCoord).Success, "The expedition can inspect the route obstacle");
        AssertTrue(app.ScoutLocationSurroundings(game, bridge.Id, new[] { "scout-1" }).Success, "A free scout can investigate the route obstacle surroundings");
        AssertEqual("evidence-claim-markers", game.Knowledge.Evidence.Single().DefinitionId, "Scout evidence comes from the generated location evidence seed");
        AssertEqual(FactionAwarenessLevel.Suspicious, game.World.FactionAwareness.Single().Level, "Scout evidence raises generated regional awareness without revealing ownership");

        AssertTrue(app.ResolveLocationAction(game, bridge.Id, "action-rebuild-bridge").Success, "The repair action starts as a generic location project");
        for (var day = 0; day < 3; day++)
        {
            AssertTrue(app.AdvanceLocationProject(game, bridge.Id).Success, "Generic project progress can complete the route repair");
        }

        AssertEqual("repaired", bridge.OperationalStateId, "The project completion changes only the location runtime state");
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-bridge-repaired"), "Project completion adds authored evidence");
        AssertTrue(app.EndDay(game).Success, "World phase resolves the neutral repair trigger");
        AssertEqual(7, game.FindFaction("faction-1")!.Anger, "The generated watched hostile context resolves the authored repair reaction");

        AssertTrue(app.EndDay(game).Success, "Delayed route consequence advances through later world time");
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-route-restored"), "The delayed consequence becomes player-facing evidence later");
    }

    private static void SealedContainmentFlowConnectsInspectionScoutingOpeningAndDelayedConsequences()
    {
        var gateCoord = new HexCoord(1, 1);
        var sealedLocation = new SpecialLocationState(
            "location-containment-proof",
            LocationKind.Ruin,
            gateCoord,
            "Sealed Entrance",
            LocationAnchor.Point(gateCoord),
            archetypeId: "containment-site",
            variantId: "sealed-gate",
            modifierIds: new[] { "modifier-sealed", "modifier-guarded" },
            operationalStateId: "sealed",
            factionRelations: new[] { new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Guarded) },
            evidenceSeedIds: new[] { "evidence-guard-routine" });
        var (app, game) = CreateGame(sealedLocation, gateCoord, new FactionState("faction-1", "Unknown Keepers", reactionProfileId: "neutral-cautious", signatureProfileId: "stone-watchmarks"));

        AssertTrue(app.InspectLocation(game, gateCoord).Success, "The expedition can inspect a sealed containment location");
        AssertTrue(app.ScoutLocationSurroundings(game, sealedLocation.Id, new[] { "scout-1" }).Success, "The location reconnaissance is the same scout mission type used by other archetypes");
        AssertEqual("evidence-guard-routine", game.Knowledge.Evidence.Single().DefinitionId, "The scout report returns neutral generated evidence for the sealed location");

        var opening = app.ResolveLocationAction(game, sealedLocation.Id, "action-open-seal", LocationOutcomeTier.Success);
        AssertTrue(opening.Success, "The sealed modifier exposes the reusable opening action");
        AssertEqual("opened", sealedLocation.OperationalStateId, "Opening changes the local containment state");
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-seal-opened"), "Opening adds immediate evidence without revealing the hidden cause");

        app.EndDay(game);
        AssertEqual(5, game.FindFaction("faction-1")!.Anger, "The guarded generated context evaluates the generic disturb tag through its profile policy");
        app.EndDay(game);
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-seal-disturbance"), "A delayed stage turns the opening into later player knowledge");
    }

    private static void SealedModifierCanReuseTheOpeningFlowOnAnotherCompatibleArchetype()
    {
        var coord = new HexCoord(1, 1);
        var sealedInvestigationSite = new SpecialLocationState(
            "location-sealed-investigation",
            LocationKind.Ruin,
            coord,
            "Sealed Reliquary",
            LocationAnchor.Point(coord),
            archetypeId: "investigation-site",
            variantId: null,
            modifierIds: new[] { "modifier-sealed" },
            operationalStateId: "sealed");
        var (app, game) = CreateGame(sealedInvestigationSite, coord, new FactionState("faction-1", "Unrelated Faction", reactionProfileId: "welcoming"));

        var opening = app.ResolveLocationAction(game, sealedInvestigationSite.Id, "action-open-seal", LocationOutcomeTier.Success);

        AssertTrue(opening.Success, "The sealed modifier reuses the opening action outside the containment archetype");
        AssertEqual("opened", sealedInvestigationSite.OperationalStateId, "The generic outcome remains valid on another compatible archetype");
    }

    private static (GameApplication App, GameState Game) CreateGame(SpecialLocationState location, HexCoord locationCoord, FactionState faction)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var locations = LocationDataLoader.LoadFromDirectory(Path.Combine(root, "Locations"))
            ?? throw new InvalidOperationException("Location content was not loaded.");
        var content = CrossSystemDataLoader.LoadFromDirectories(new[]
        {
            Path.Combine(root, "World"),
            Path.Combine(root, "Factions"),
            Path.Combine(root, "Scouting")
        }) ?? throw new InvalidOperationException("Cross-system content was not loaded.");

        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(locationCoord, KnowledgeLevel.Confirmed);
        var expedition = new ExpeditionState(
            1,
            locationCoord,
            new[]
            {
                new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout),
                new ExpeditionMemberState("engineer-1", "Iven", ExpeditionMemberRole.Engineer)
            },
            supplies: 12);
        var game = new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(6, 6), TerrainType.Grassland), locations: new[] { location }),
            knowledge,
            new PlayerNotesState(),
            expedition,
            new BaseState(new HexCoord(5, 5)),
            factions: new[] { faction });

        return (new GameApplication(locations, content), game);
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
