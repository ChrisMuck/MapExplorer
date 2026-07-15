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
        InvestigationSiteDisturbanceOffersAResponseWithoutPunishingRespect();
        ContactSiteDeliversFactionRequestOnlyThroughEstablishedLocalContact();
        SealedContainmentFlowConnectsInspectionScoutingOpeningAndDelayedConsequences();
        SealedModifierCanReuseTheOpeningFlowOnAnotherCompatibleArchetype();
    }

    private static void ContactSiteDeliversFactionRequestOnlyThroughEstablishedLocalContact()
    {
        var coord = new HexCoord(1, 2);
        var contactSite = new SpecialLocationState(
            "location-contact-proof", LocationKind.Settlement, coord, "Prepared Meeting Place",
            LocationAnchor.Point(coord), "contact-site", "first-contact", interactionStateId: "unapproached",
            operationalStateId: "available", presenceStateId: "present",
            factionRelations: new[] { new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Guarded) });
        var (app, game) = CreateGame(contactSite, coord,
            new FactionState("faction-1", "Unknown Locals", contactStatus: FactionContactStatus.Unknown, reactionProfileId: "welcoming"));

        AssertTrue(app.ResolveLocationAction(game, contactSite.Id, "action-approach-cautiously", LocationOutcomeTier.Success).Success,
            "Contact archetype approaches through its authored action");
        AssertTrue(app.ResolveLocationAction(game, contactSite.Id, "action-communicate", LocationOutcomeTier.Success).Success,
            "Successful communication uses the shared contact outcome");
        AssertEqual(FactionContactStatus.Contacted, game.FindFaction("faction-1")!.ContactStatus,
            "Generic effect establishes contact with the one generated related faction");
        app.EndDay(game);

        var request = game.World.Situations.Single(item => item.DefinitionId == "situation-request-help-with-crossing");
        AssertEqual("faction", request.SourceKind, "Delivered request records its plausible runtime source kind");
        AssertEqual("direct-contact", request.DeliveryChannel, "Delivered request records the earned local contact channel");
        AssertEqual("faction-1", request.FactionId, "Request keeps the concrete generated faction only in runtime state");
        var authoring = app.DataCatalog?.Authoring ?? throw new InvalidOperationException("Authored game data was not loaded.");
        AssertTrue(new ResolveWorldSituationCommand(authoring).Execute(game, request.Id, "assist"),
            "Delivered request accepts an authored response through the generic command");

        var silentSite = new SpecialLocationState(
            "location-contact-silent-proof", LocationKind.Settlement, coord, "Distant Signs",
            LocationAnchor.Point(coord), "contact-site", "first-contact", interactionStateId: "unapproached",
            operationalStateId: "available", presenceStateId: "unknown",
            factionRelations: new[] { new LocationFactionRelationState("faction-2", LocationFactionRelationKind.Watched) });
        var (silentApp, silentGame) = CreateGame(silentSite, coord,
            new FactionState("faction-2", "Unknown Watchers", contactStatus: FactionContactStatus.Unknown, reactionProfileId: "neutral-cautious"));
        silentGame.World.QueueWorldTrigger(new WorldTriggerState("trigger-undeliverable", "contact-help-requested", 1, sourceLocationId: silentSite.Id, sourceCoord: coord));
        silentApp.EndDay(silentGame);
        AssertTrue(!silentGame.World.Situations.Any(item => item.DefinitionId == "situation-request-help-with-crossing"),
            "Generated relation alone cannot deliver a faction request without established contact");
    }

    private static void InvestigationSiteDisturbanceOffersAResponseWithoutPunishingRespect()
    {
        var coord = new HexCoord(2, 2);
        var disturbedSite = new SpecialLocationState(
            "location-investigation-proof", LocationKind.MarkedGrave, coord, "Marked Grave",
            LocationAnchor.Point(coord), "investigation-site", "marked-grave",
            modifierIds: new[] { "modifier-sacred", "modifier-watched" }, operationalStateId: "sealed",
            factionRelations: new[] { new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Sacred) },
            contextTags: new[] { "protected-remains" });
        var (app, game) = CreateGame(disturbedSite, coord,
            new FactionState("faction-1", "Unknown Mourners", reactionProfileId: "neutral-cautious", signatureProfileId: "carved-boundaries"));
        game.Knowledge.LearnLocationContextTag(disturbedSite.Id, "protected-remains");

        AssertTrue(app.ResolveLocationAction(game, disturbedSite.Id, "action-inspect", LocationOutcomeTier.Success).Success,
            "The investigation archetype begins with its generic inspection action");
        AssertTrue(app.ResolveLocationAction(game, disturbedSite.Id, "action-disturb", LocationOutcomeTier.SuccessWithCost).Success,
            "Known protected remains expose the authored risky disturbance choice");
        AssertEqual("disturbed", disturbedSite.OperationalStateId, "The disturbance persists as objective location state");

        app.EndDay(game);
        app.EndDay(game);
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-grave-disturbance-rumour"),
            "The delayed process first reaches the player as an earned, uncertain rumour");
        var awarenessBeforeResponse = game.World.FactionAwareness.FirstOrDefault()?.Level;
        var warning = game.World.Situations.Single(item => item.DefinitionId == "situation-investigate-disturbance");
        var authoring = app.DataCatalog?.Authoring ?? throw new InvalidOperationException("Authored game data was not loaded.");
        AssertTrue(new ResolveWorldSituationCommand(authoring).Execute(game, warning.Id, "prepare-response"),
            "The warning accepts its authored preparation response");

        app.EndDay(game);
        app.EndDay(game);
        app.EndDay(game);
        AssertEqual(awarenessBeforeResponse, game.World.FactionAwareness.FirstOrDefault()?.Level,
            "Preparation prevents the later related-faction awareness escalation without erasing the disturbance");
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-grave-disturbance-change"),
            "The later observable change remains player knowledge despite mitigation");

        var respectfulSite = new SpecialLocationState(
            "location-investigation-respect-proof", LocationKind.MarkedGrave, coord, "Marked Grave",
            LocationAnchor.Point(coord), "investigation-site", "marked-grave", operationalStateId: "sealed");
        var (respectfulApp, respectfulGame) = CreateGame(respectfulSite, coord,
            new FactionState("faction-2", "Unknown Mourners", reactionProfileId: "neutral-cautious"));
        AssertTrue(respectfulApp.ResolveLocationAction(respectfulGame, respectfulSite.Id, "action-leave-offering", LocationOutcomeTier.Success).Success,
            "The safe respectful choice remains available without the risky context branch");
        respectfulApp.EndDay(respectfulGame);
        AssertTrue(!respectfulGame.World.ScheduledConsequences.Any(process => process.DefinitionId == "consequence-grave-disturbed"),
            "Respect does not start the disturbance process");
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
        var awarenessBeforeResponse = game.World.FactionAwareness.Single().Level;
        var routeSituation = game.World.Situations.Single(item => item.DefinitionId == "situation-route-use-changed");
        var authoring = app.DataCatalog?.Authoring ?? throw new InvalidOperationException("Authored game data was not loaded.");
        AssertTrue(new ResolveWorldSituationCommand(authoring).Execute(game, routeSituation.Id, "organize-help"),
            "The observed route use offers a generic authored coordination response");

        app.EndDay(game);
        var awarenessAfterFollowUpTrigger = game.World.FactionAwareness.Single().Level;
        app.EndDay(game);
        app.EndDay(game);
        AssertEqual(WorldSituationStatus.Resolved, routeSituation.Status, "The route response persists through later World Phases");
        AssertTrue((int)awarenessAfterFollowUpTrigger >= (int)awarenessBeforeResponse,
            "The independent observed-traffic trigger may still produce its authored immediate reaction");
        AssertEqual(awarenessAfterFollowUpTrigger, game.World.FactionAwareness.Single().Level,
            "Coordinating the route response prevents the later unmanaged attention escalation");
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
        var warning = game.World.Situations.Single(item => item.DefinitionId == "situation-investigate-opened-seal");
        AssertEqual(WorldSituationStatus.Active, warning.Status, "The earned disturbance observation creates an active response situation");
        var authoring = app.DataCatalog?.Authoring ?? throw new InvalidOperationException("Authored game data was not loaded.");
        AssertTrue(new ResolveWorldSituationCommand(authoring).Execute(game, warning.Id, "contain"),
            "The generic situation command accepts an authored containment response");

        app.EndDay(game);
        app.EndDay(game);
        app.EndDay(game);
        AssertEqual(WorldSituationStatus.Resolved, warning.Status, "The response remains persistent after later World Phases");
        AssertTrue(!game.World.WorldTriggers.Any(trigger => trigger.TriggerId == "seal-disturbance-observed"),
            "The authored containment response suppresses the later escalation trigger");
        AssertTrue(game.Knowledge.Evidence.Any(item => item.DefinitionId == "evidence-seal-consequence"),
            "Mitigation suppresses escalation without erasing the observed history");
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
        var catalog = GameDataCatalog.LoadFromDirectory(root)
            ?? throw new InvalidOperationException("Game-data catalog was not loaded.");

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

        return (new GameApplication(catalog), game);
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
