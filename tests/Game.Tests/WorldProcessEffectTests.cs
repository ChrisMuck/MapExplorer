#nullable enable
using System;
using System.Collections.Generic;
using Game.App;
using Game.Core;

internal sealed class WorldProcessEffectTests
{
    public void RunAll()
    {
        TypedEffectsChangeOnlyLogicalRuntimeState();
        CapacityDefersATriggerWithoutDiscardingIt();
        TerritoryObservationStaysHiddenWithoutGeneratedLocationRelation();
        WarningResponseCanPreventAnAuthoredSeriousEffect();
    }

    private static void TypedEffectsChangeOnlyLogicalRuntimeState()
    {
        var situationId = "situation-test";
        var authoring = new CrossSystemAuthoringBundle(
            Array.Empty<LocationStateProfileDefinition>(), Array.Empty<LocationScenarioProfileDefinition>(),
            Array.Empty<FindingDefinition>(), Array.Empty<ContextDefinition>(),
            new[] { new SituationDefinition(situationId, "warning", new[] { "world" }, new[] { "soon" }, new[] { "investigate" }, new[] { "resolved" }) },
            Array.Empty<FactionOfferContentDefinition>(), Array.Empty<FactionMemoryDefinition>());
        var stage = new ConsequenceStageDefinition("change", 0, null, null, null, effects: new[]
        {
            new WorldStageEffectDefinition("state", WorldStageEffectKind.ChangeLocationState, stateChannel: LocationStateChannels.Operational, stateId: "blocked"),
            new WorldStageEffectDefinition("tile", WorldStageEffectKind.SetTilePassability, isBlocked: true),
            new WorldStageEffectDefinition("evidence", WorldStageEffectKind.AddEvidence, "evidence-test"),
            new WorldStageEffectDefinition("flag", WorldStageEffectKind.SetLocationFlag, "flooded"),
            new WorldStageEffectDefinition("situation", WorldStageEffectKind.CreateSituation, situationId),
            new WorldStageEffectDefinition("attention", WorldStageEffectKind.EscalateRelatedFactionAwareness),
            new WorldStageEffectDefinition("connection", WorldStageEffectKind.CreateConnection, "region-downstream", connectionKind: "hazard-trail")
        });
        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(new[] { new EvidenceDefinition("evidence-test", "Scout text.", "World text.") }),
            factionProfiles: new[] { new FactionProfileDefinition("watchers", "Watchers") },
            triggers: new[] { new WorldTriggerDefinition("trigger-effects", "consequence-effects") },
            consequences: new[] { new ConsequenceDefinition("consequence-effects", new[] { stage }) });
        var game = CreateGame(new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Watched));
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "trigger-effects", 1, sourceLocationId: "location-1"));

        new WorldPhaseService(content, authoring).Resolve(game);

        var location = game.World.Locations[0];
        AssertEqual("blocked", location.OperationalStateId, "Effect changes the source location state");
        AssertTrue(location.HasFlag("flooded"), "Effect leaves a persistent location flag");
        AssertTrue(game.World.Map.GetTile(HexCoord.Zero).IsBlocked, "Effect changes logical tile access only");
        AssertEqual(1, game.Knowledge.Evidence.Count, "Effect creates earned world evidence");
        AssertEqual(1, game.World.Situations.Count, "Effect creates a runtime situation");
        AssertEqual(1, game.World.Connections.Count, "Effect creates a logical connection");
        AssertEqual(FactionAwarenessLevel.Alert, game.World.FactionAwareness[0].Level, "Observation and explicit effect are distinct deterministic attention steps");
    }

    private static void CapacityDefersATriggerWithoutDiscardingIt()
    {
        var stage = new ConsequenceStageDefinition("late", 9, null, null, null);
        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            triggers: new[] { new WorldTriggerDefinition("trigger-capacity", "consequence-capacity") },
            consequences: new[] { new ConsequenceDefinition("consequence-capacity", new[] { stage }) });
        var existing = new List<ScheduledConsequenceState>();
        for (var index = 0; index < WorldPhaseService.MaximumConcurrentWorldProcesses; index++)
        {
            existing.Add(new ScheduledConsequenceState($"process-{index}", "existing", "default", "location-1", null, null,
                new[] { new ScheduledConsequenceStageState("later", 99, new[] { "hold" }) }));
        }
        var game = CreateGame(scheduledConsequences: existing);
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "trigger-capacity", 1, sourceLocationId: "location-1"));

        new WorldPhaseService(content).Resolve(game);

        AssertEqual(WorldPhaseService.MaximumConcurrentWorldProcesses, game.World.ScheduledConsequences.Count, "Capacity does not exceed the configured MVP limit");
        AssertTrue(!game.World.WorldTriggers[0].IsResolved, "Deferred trigger remains available for a later world phase");
    }

    private static void TerritoryObservationStaysHiddenWithoutGeneratedLocationRelation()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        map.SetTile(map.GetTile(HexCoord.Zero).WithOwner("faction-1"));
        var location = new SpecialLocationState("location-1", LocationKind.Ruin, HexCoord.Zero, "Neutral ruin");
        var game = new GameState(
            new WorldState(map, locations: new[] { location }), new KnowledgeState(), new PlayerNotesState(),
            new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) }), new BaseState(HexCoord.Zero),
            factions: new[] { new FactionState("faction-1", "Hidden observers", reactionProfileId: "territorial") });
        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            factionProfiles: new[] { new FactionProfileDefinition("territorial", "Territorial", observationRange: 1, observationChannels: new[] { "territory" }) });
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "neutral-change", 1, new[] { "repair" }, sourceLocationId: location.Id));

        new WorldPhaseService(content).Resolve(game);

        AssertEqual(1, game.World.FactionAwareness.Count, "Faction may observe activity inside its generated territory");
        AssertEqual(0, game.Events.PendingCount, "Observation does not expose an unknown faction or create a reaction without a generated location relation");
    }

    private static void WarningResponseCanPreventAnAuthoredSeriousEffect()
    {
        const string situationId = "situation-contain";
        var authoring = new CrossSystemAuthoringBundle(
            Array.Empty<LocationStateProfileDefinition>(), Array.Empty<LocationScenarioProfileDefinition>(), Array.Empty<FindingDefinition>(), Array.Empty<ContextDefinition>(),
            new[] { new SituationDefinition(situationId, "warning", new[] { "world" }, new[] { "soon" }, new[] { "contain" }, new[] { "contained" }) },
            Array.Empty<FactionOfferContentDefinition>(), Array.Empty<FactionMemoryDefinition>());
        var consequence = new ConsequenceDefinition("consequence-warning", new[]
        {
            new ConsequenceStageDefinition("warning", 0, null, null, null, effects: new[] { new WorldStageEffectDefinition("warn", WorldStageEffectKind.CreateSituation, situationId) }),
            new ConsequenceStageDefinition("serious", 1, null, null, null, effects: new[] { new WorldStageEffectDefinition("spread", WorldStageEffectKind.RaiseWorldTrigger, "hazard-spread", suppressIfSituationResponseTags: new[] { "contain" }) }, severity: WorldConsequenceSeverity.Serious)
        });
        var content = new CrossSystemDataBundle(new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            triggers: new[] { new WorldTriggerDefinition("start", consequence.Id), new WorldTriggerDefinition("hazard-spread") }, consequences: new[] { consequence });
        var game = CreateGame();
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "start", 1, sourceLocationId: "location-1"));
        var phase = new WorldPhaseService(content, authoring);
        phase.Resolve(game);

        AssertTrue(new ResolveWorldSituationCommand(authoring).Execute(game, game.World.Situations[0].Id, "contain"), "Allowed response resolves the warning through a command");
        game.World.AdvanceDays(1);
        phase.Resolve(game);

        AssertEqual(1, game.World.WorldTriggers.Count, "Resolved containment suppresses the later authored spread trigger");
    }

    private static GameState CreateGame(LocationFactionRelationState? relation = null, IEnumerable<ScheduledConsequenceState>? scheduledConsequences = null)
    {
        var location = new SpecialLocationState(
            "location-1", LocationKind.Ruin, HexCoord.Zero, "Old site", LocationAnchor.Point(HexCoord.Zero),
            archetypeId: null, variantId: null, factionRelations: relation == null ? null : new[] { relation });
        return new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland), locations: new[] { location }, scheduledConsequences: scheduledConsequences),
            new KnowledgeState(), new PlayerNotesState(),
            new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) }), new BaseState(HexCoord.Zero),
            factions: new[] { new FactionState("faction-1", "Watchers", reactionProfileId: "watchers") });
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
