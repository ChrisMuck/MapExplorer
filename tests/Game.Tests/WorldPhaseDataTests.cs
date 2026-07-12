#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;
using Game.Core;

internal sealed class WorldPhaseDataTests
{
    public void RunAll()
    {
        ExpeditionTimeResolvesAStagedConsequenceExactlyOnce();
        BaseTimeAlsoResolvesAStagedConsequence();
        UndefinedEvidenceReferencesAreRejectedByTheLoader();
    }

    private static void ExpeditionTimeResolvesAStagedConsequenceExactlyOnce()
    {
        var content = LoadContent();
        var game = CreateGame();
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "location-grave-disturbed", game.World.WorldDay, sourceLocationId: "location-1"));
        var command = new EndDayCommand(suppliesPerDay: 0, worldPhaseService: new WorldPhaseService(content));

        command.Execute(game);
        AssertEqual(2, game.World.ScheduledConsequences.Count, "Trigger schedules every fixed consequence stage");
        AssertEqual(0, game.Knowledge.Evidence.Count, "First stage is not visible before its delay");

        command.Execute(game);
        AssertEqual(1, game.Knowledge.Evidence.Count, "Due stage creates world evidence");
        AssertEqual("evidence-grave-disturbance-rumour", game.Knowledge.Evidence[0].DefinitionId, "First defined stage is applied");
        AssertEqual(1, game.Events.PendingCount, "Due stage creates a player-visible event");

        new WorldPhaseService(content).Resolve(game);
        AssertEqual(1, game.Knowledge.Evidence.Count, "Applied stage cannot create evidence twice");
        AssertEqual(1, game.Events.PendingCount, "Applied stage cannot queue a second event");
    }

    private static void BaseTimeAlsoResolvesAStagedConsequence()
    {
        var content = LoadContent();
        var game = CreateGame();
        game.Expedition.SetStatus(ExpeditionStatus.Returned);
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "location-infrastructure-repaired", game.World.WorldDay, sourceLocationId: "location-1"));

        new AdvanceBaseTimeCommand(new WorldPhaseService(content)).Execute(game, 2);

        AssertEqual(1, game.Knowledge.Evidence.Count, "Base time reaches and resolves a due consequence");
        AssertEqual("evidence-route-restored", game.Knowledge.Evidence[0].DefinitionId, "Base time applies the configured stage");
    }

    private static void UndefinedEvidenceReferencesAreRejectedByTheLoader()
    {
        const string evidence = "{ \"documentType\": \"evidence-definitions\", \"schemaVersion\": 1, \"items\": [ { \"id\": \"evidence-known\", \"scoutReportText\": \"Known.\" } ] }";
        const string consequences = "{ \"documentType\": \"consequence-definitions\", \"schemaVersion\": 1, \"items\": [ { \"id\": \"consequence-test\", \"stages\": [ { \"id\": \"stage-1\", \"delayDays\": 1, \"evidenceId\": \"evidence-missing\" } ] } ] }";

        AssertThrows(() => CrossSystemDataLoader.LoadFromJson(new[] { evidence, consequences }), "Unknown evidence references are rejected");
    }

    private static CrossSystemDataBundle LoadContent()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData", "World");
        return CrossSystemDataLoader.LoadFromDirectory(root) ?? throw new InvalidOperationException("World content was not loaded.");
    }

    private static GameState CreateGame()
    {
        var location = new SpecialLocationState("location-1", LocationKind.Ruin, HexCoord.Zero, "Old Site");
        var expedition = new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }, supplies: 5);
        return new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland), locations: new[] { location }),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero));
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
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

        throw new InvalidOperationException($"{message}: expected LocationDataException.");
    }
}
