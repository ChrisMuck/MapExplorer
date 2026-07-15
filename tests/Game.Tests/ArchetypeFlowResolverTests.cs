#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ArchetypeFlowResolverTests
{
    public void RunAll()
    {
        StateRuleAddsFollowUpOnlyForMatchingPersistentState();
        StateRuleCombinesInteractionOperationalAndPresenceConditions();
        RegistrySelectsAFlowByArchetypeRatherThanVariantOrLocationKind();
        AllSevenAuthoredArchetypesExposeTheirFollowUpChain();
        AllSevenAuthoredArchetypesResolveTheirFirstPlayableChain();
        ActionOutcomeFindingStaysUnsecuredUntilReturnAndAnalysis();
        InvalidStateRuleIsRejectedByCrossSystemValidation();
        ScenarioWithoutRegisteredArchetypeFlowIsRejected();
    }

    private static void StateRuleAddsFollowUpOnlyForMatchingPersistentState()
    {
        var resolver = new LocationScenarioActionResolver(LoadAuthoring());
        var sealedLocation = Containment("sealed", "untouched", "unknown");
        var openedLocation = Containment("opened", "untouched", "unknown");

        var sealedOptions = resolver.ResolveBaseActionIds(sealedLocation, new KnowledgeState())!;
        var openedOptions = resolver.ResolveBaseActionIds(openedLocation, new KnowledgeState())!;

        AssertFalse(sealedOptions.Contains("action-follow-up"), "Sealed containment does not expose the opened-state follow-up");
        AssertTrue(openedOptions.Contains("action-follow-up"), "Opened containment exposes a JSON-authored follow-up without a variant branch");
    }

    private static void StateRuleCombinesInteractionOperationalAndPresenceConditions()
    {
        var resolver = new LocationScenarioActionResolver(LoadAuthoring());
        var matching = Containment("opened", "inspected", "guarded");
        var uninspected = Containment("opened", "untouched", "guarded");

        var matchingOptions = resolver.ResolveBaseActionIds(matching, new KnowledgeState())!;
        var uninspectedOptions = resolver.ResolveBaseActionIds(uninspected, new KnowledgeState())!;

        AssertTrue(matchingOptions.Contains("action-secure"), "All matching state channels expose the compound follow-up");
        AssertFalse(uninspectedOptions.Contains("action-secure"), "A missing interaction state keeps the compound follow-up unavailable");
    }

    private static void RegistrySelectsAFlowByArchetypeRatherThanVariantOrLocationKind()
    {
        var registry = LocationArchetypeInteractionFlowRegistry.CreateInitialSlice();
        var routeFlow = registry.Get("route-obstacle");
        var containmentFlow = registry.Get("containment-site");

        AssertTrue(routeFlow is RouteObstacleInteractionFlow, "Route Obstacle selects its registered archetype flow");
        AssertTrue(containmentFlow is ContainmentSiteInteractionFlow, "Containment Site selects its registered archetype flow");
        AssertTrue(registry.Get("hazard-site") is HazardSiteInteractionFlow, "Hazard Site is registered before its first Slice profile is authored");
        AssertTrue(registry.Get("natural-phenomenon") is NaturalPhenomenonInteractionFlow, "Natural Phenomenon is registered before its first Slice profile is authored");
        AssertFalse(routeFlow.GetType() == containmentFlow.GetType(), "Two archetypes do not share a variant-derived flow type");
        AssertThrows(() => registry.Get("sealed-gate"), "A variant ID cannot select an archetype flow");
    }

    private static void InvalidStateRuleIsRejectedByCrossSystemValidation()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-invalid-rule", "archetypeId":"containment-site", "channels":{ "operational":{ "initial":"sealed", "values":["sealed","opened"] } } }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-invalid-rule", "archetypeId":"containment-site", "variantId":"sealed-gate", "stateProfileId":"state-invalid-rule", "contentProfileId":"content-sealed-gate", "worldgen":{ "anchorKinds":["Point"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-observe"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3, "stateActionRules":[{ "whenOperationalStatesAny":["breached"], "addActionIds":["action-observe"] }] } }] }"""
        });
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        AssertThrows(() => CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring),
            "A state action rule cannot name a state outside its State Profile");
    }

    private static void AllSevenAuthoredArchetypesExposeTheirFollowUpChain()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");
        var resolver = new LocationScenarioActionResolver(catalog.Authoring);

        AssertContains(resolver, Location("route-obstacle", "broken-bridge", "inspected", "blocked", "unknown"), "action-rebuild-bridge", "Route Obstacle continues from assessment to infrastructure options");
        AssertContains(resolver, Location("investigation-site", "marked-grave", "inspected", "sealed", "unknown"), "action-investigate", "Investigation Site continues from inspection to examination");
        AssertContains(resolver, Location("territorial-marker", "border-warning-sign", "interpreted", "standing", "unknown"), "action-respect-warning", "Territorial Marker continues from interpretation to a respectful choice");
        AssertContains(resolver, Location("containment-site", "sealed-gate", "inspected", "sealed", "unknown"), "action-open-seal", "Containment Site continues from inspection to opening");
        AssertContains(resolver, Location("contact-site", "first-contact", "approached", "available", "present"), "action-communicate", "Contact Site continues from approach to communication");
        AssertContains(resolver, Location("hazard-site", "restless-marsh", "assessed", "active", "unknown"), "action-contain-hazard", "Hazard Site continues from assessment to specialist containment");
        AssertContains(resolver, Location("natural-phenomenon", "wind-carved-peak", "approached", "stable", "unknown"), "action-map-surroundings", "Natural Phenomenon continues from approach to mapping");
    }

    private static void AllSevenAuthoredArchetypesResolveTheirFirstPlayableChain()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        var route = CreatePlayableGame(catalog, "route-obstacle", "broken-bridge", "untouched", "blocked", "unknown");
        Resolve(route, "action-assess-crossing");
        AssertEqual("inspected", route.Location.InteractionStateId, "Route Obstacle assessment persists its interaction state");

        var investigation = CreatePlayableGame(catalog, "investigation-site", "marked-grave", "untouched", "sealed", "unknown");
        Resolve(investigation, "action-inspect");
        Resolve(investigation, "action-investigate");
        AssertEqual("investigated", investigation.Location.InteractionStateId, "Investigation Site continues from inspection to investigation");

        var marker = CreatePlayableGame(catalog, "territorial-marker", "border-warning-sign", "untouched", "standing", "unknown");
        Resolve(marker, "action-observe-sign");
        Resolve(marker, "action-interpret");
        Resolve(marker, "action-respect-warning");
        AssertEqual("respected", marker.Location.OperationalStateId, "Territorial Marker records the respectful choice");

        var containment = CreatePlayableGame(catalog, "containment-site", "sealed-gate", "untouched", "sealed", "unknown");
        Resolve(containment, "action-inspect-seal");
        Resolve(containment, "action-open-seal");
        Resolve(containment, "action-secure-site");
        AssertEqual("sealed", containment.Location.OperationalStateId, "Containment Site can return to a secured local state");

        var contact = CreatePlayableGame(catalog, "contact-site", "first-contact", "unapproached", "available", "present");
        Resolve(contact, "action-approach-cautiously");
        Resolve(contact, "action-communicate");
        AssertEqual("contacted", contact.Location.InteractionStateId, "Contact Site records established contact");

        var hazard = CreatePlayableGame(catalog, "hazard-site", "restless-marsh", "untouched", "active", "unknown");
        Resolve(hazard, "action-assess-risk");
        Resolve(hazard, "action-contain-hazard");
        AssertEqual("contained", hazard.Location.OperationalStateId, "Hazard Site containment persists its result");

        var phenomenon = CreatePlayableGame(catalog, "natural-phenomenon", "wind-carved-peak", "untouched", "stable", "unknown");
        Resolve(phenomenon, "action-observe-from-distance");
        Resolve(phenomenon, "action-approach");
        Resolve(phenomenon, "action-survey");
        AssertEqual("surveyed", phenomenon.Location.InteractionStateId, "Natural Phenomenon survey persists its finding state");
    }

    private static void ActionOutcomeFindingStaysUnsecuredUntilReturnAndAnalysis()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");
        var hazard = CreatePlayableGame(catalog, "hazard-site", "restless-marsh", "untouched", "active", "unknown");

        Resolve(hazard, "action-assess-risk");
        Resolve(hazard, "action-contain-hazard");
        AssertEqual(1, hazard.Game.Expedition.FieldFindings.Count, "A location action records its authored finding in the field");
        AssertEqual("finding-marsh-water-sample", hazard.Game.Expedition.FieldFindings[0].DefinitionId, "Outcome uses its JSON finding reference");
        AssertEqual(0, hazard.Game.Base.EvaluationQueue.Items.Count, "A field finding is not analyzable before return");

        var returned = hazard.App.CompleteExpedition(hazard.Game);
        AssertTrue(returned.Success, "The active expedition returns the action-acquired finding at base");
        AssertEqual("contained", hazard.Location.OperationalStateId, "The physical containment result persists after expedition return");
        AssertEqual(0, hazard.Game.Expedition.FieldFindings.Count, "Returned finding leaves the expedition inventory");
        var queued = hazard.Game.Base.EvaluationQueue.Items.Single(item => item.Id.StartsWith("finding-", StringComparison.Ordinal));
        var knowledgeBeforeAnalysis = hazard.Game.Base.KnowledgePoints;

        hazard.App.AdvanceBaseTime(hazard.Game, queued.RequiredDays);
        var analysed = hazard.App.EvaluateKnowledgeItem(hazard.Game, queued.Id);
        AssertTrue(analysed.Success, "Returned action finding can be analysed after its authored base time");
        AssertEqual(knowledgeBeforeAnalysis + queued.KnowledgeReward, hazard.Game.Base.KnowledgePoints, "Only analysis awards the finding's Knowledge Points");
    }

    private static (GameApplication App, GameState Game, SpecialLocationState Location) CreatePlayableGame(
        GameDataCatalog catalog,
        string archetypeId,
        string variantId,
        string interactionStateId,
        string operationalStateId,
        string presenceStateId)
    {
        var coord = HexCoord.Zero;
        var location = Location(archetypeId, variantId, interactionStateId, operationalStateId, presenceStateId);
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(coord, KnowledgeLevel.Confirmed);
        var expedition = new ExpeditionState(1, coord, new[]
        {
            new ExpeditionMemberState("engineer", "Engineer", ExpeditionMemberRole.Engineer),
            new ExpeditionMemberState("medic", "Medic", ExpeditionMemberRole.Medic),
            new ExpeditionMemberState("scholar", "Scholar", ExpeditionMemberRole.Scholar)
        }, supplies: 12);
        var game = new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(4, 4), TerrainType.Grassland), locations: new[] { location }),
            knowledge,
            new PlayerNotesState(),
            expedition,
            new BaseState(coord));
        return (new GameApplication(catalog), game, location);
    }

    private static void Resolve((GameApplication App, GameState Game, SpecialLocationState Location) fixture, string actionId)
    {
        var result = fixture.App.ResolveLocationAction(fixture.Game, fixture.Location.Id, actionId, LocationOutcomeTier.Success);
        AssertTrue(result.Success, $"'{fixture.Location.ArchetypeId}' resolves '{actionId}' through the shared action pipeline");
    }

    private static SpecialLocationState Location(string archetypeId, string variantId, string interactionStateId, string operationalStateId, string presenceStateId)
    {
        return new SpecialLocationState(
            $"location-{archetypeId}",
            LocationKind.Ruin,
            HexCoord.Zero,
            "Archetype chain test",
            LocationAnchor.Point(HexCoord.Zero),
            archetypeId: archetypeId,
            variantId: variantId,
            interactionStateId: interactionStateId,
            operationalStateId: operationalStateId,
            presenceStateId: presenceStateId);
    }

    private static void AssertContains(LocationScenarioActionResolver resolver, SpecialLocationState location, string actionId, string message)
    {
        var actionIds = resolver.ResolveBaseActionIds(location, new KnowledgeState()) ?? Array.Empty<string>();
        AssertTrue(actionIds.Contains(actionId), message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void ScenarioWithoutRegisteredArchetypeFlowIsRejected()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-unregistered-test", "archetypeId":"unregistered-site", "channels":{ "operational":{ "initial":"active", "values":["active"] } } }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-unregistered-test", "archetypeId":"unregistered-site", "variantId":"broken-bridge", "stateProfileId":"state-unregistered-test", "contentProfileId":"content-old-trade-road-bridge", "worldgen":{ "anchorKinds":["Point"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-observe"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3 } }] }"""
        });
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        AssertThrows(() => CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring),
            "A Scenario Profile needs a registered code-owned archetype flow");
    }

    private static CrossSystemAuthoringBundle LoadAuthoring()
    {
        return CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-containment-test", "archetypeId":"containment-site", "channels":{ "interaction":{ "initial":"untouched", "values":["untouched","inspected"] }, "operational":{ "initial":"sealed", "values":["sealed","opened"] }, "presence":{ "initial":"unknown", "values":["unknown","guarded"] } } }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-containment-test", "archetypeId":"containment-site", "variantId":"test-containment-variant", "stateProfileId":"state-containment-test", "contentProfileId":"content-sealed-gate", "worldgen":{ "anchorKinds":["Point"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-observe"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3, "stateActionRules":[{ "whenOperationalStatesAny":["opened"], "addActionIds":["action-follow-up"] },{ "whenInteractionStatesAny":["inspected"], "whenOperationalStatesAny":["opened"], "whenPresenceStatesAny":["guarded"], "addActionIds":["action-secure"] }] } }] }"""
        });
    }

    private static SpecialLocationState Containment(string operationalStateId, string interactionStateId, string presenceStateId)
    {
        return new SpecialLocationState(
            "location-containment-test",
            LocationKind.Ruin,
            HexCoord.Zero,
            "Test containment",
            LocationAnchor.Point(HexCoord.Zero),
            archetypeId: "containment-site",
            variantId: "test-containment-variant",
            interactionStateId: interactionStateId,
            operationalStateId: operationalStateId,
            presenceStateId: presenceStateId);
    }

    private static string GameDataRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        if (!Directory.Exists(root)) throw new InvalidOperationException("Game-data root was not found for tests.");
        return root;
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (LocationDataException) { return; }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException($"{message}: expected a validation or flow-selection failure.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"{message}: expected true.");
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition) throw new InvalidOperationException($"{message}: expected false.");
    }
}
