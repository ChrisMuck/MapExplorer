#nullable enable
using System;
using System.Collections.Generic;
using Game.App;
using Game.Core;

internal sealed class SimulationRuntimeStateTests
{
    public void RunAll()
    {
        RuntimeIdsAndRandomStreamAreDeterministic();
        WorldStoresContextSituationAndCausalTraceWithoutChangingKnowledge();
        ConsequenceStagesAdvanceInOrderWithoutRerollingBranch();
    }

    private static void RuntimeIdsAndRandomStreamAreDeterministic()
    {
        var first = new WorldState(CreateMap(), random: new DeterministicRandomState(991));
        var second = new WorldState(CreateMap(), random: new DeterministicRandomState(991));

        AssertEqual("trace-1", first.RuntimeIds.Allocate("trace"), "First allocated ID is deterministic");
        AssertEqual("trace-1", second.RuntimeIds.Allocate("trace"), "Same allocation sequence has same ID");
        var firstRandom = new WorldDeterministicRandomSource(first);
        var secondRandom = new WorldDeterministicRandomSource(second);
        AssertEqual(firstRandom.NextInt(1000), secondRandom.NextInt(1000), "Same seed produces same random result");
        AssertEqual(firstRandom.NextInt(1000), secondRandom.NextInt(1000), "Persisted random state advances reproducibly");
    }

    private static void WorldStoresContextSituationAndCausalTraceWithoutChangingKnowledge()
    {
        var game = CreateGame();
        game.World.AddGeneratedContext(new GeneratedContextAssignmentState("context-1", "context-deliberate-closure", "location-1", 1));
        var situation = new WorldSituationState("situation-1", "situation-request-help-with-crossing", 1, sourceLocationId: "location-1");
        game.World.AddSituation(situation);
        situation.Activate();
        var trace = game.World.RecordTrace(SimulationTraceKind.GeneratorDecision, "Generated a deliberate closure context.", subjectIds: new[] { "location-1" });

        AssertEqual(1, game.World.GeneratedContexts.Count, "Generated context remains objective runtime state");
        AssertEqual(WorldSituationStatus.Active, game.World.Situations[0].Status, "Situation persists with its current status");
        AssertEqual("trace-1", trace.TraceId, "Trace receives runtime ID");
        AssertEqual(0, game.Knowledge.Evidence.Count, "Inspecting world-runtime data never grants player knowledge");
    }

    private static void ConsequenceStagesAdvanceInOrderWithoutRerollingBranch()
    {
        var process = new ScheduledConsequenceState(
            "process-1",
            "consequence-test",
            "branch-contained",
            "location-1",
            "trigger-1",
            new[] { "context-deliberate-closure" },
            new[]
            {
                new ScheduledConsequenceStageState("early-sign", 2, new[] { "effect-early" }),
                new ScheduledConsequenceStageState("aftermath", 5, new[] { "effect-late" })
            });

        AssertTrue(!process.IsDue(1), "First stage waits until its due day");
        AssertTrue(process.IsDue(2), "First stage becomes due");
        AssertEqual("early-sign", process.TryApplyDueStage(2)!.StageId, "First stage resolves first");
        AssertEqual("branch-contained", process.ResolvedBranchId, "Resolved branch cannot change after time advances");
        AssertTrue(!process.IsDue(4), "Second stage waits for its own due day");
        AssertTrue(process.TryApplyDueStage(4) == null, "A stage cannot be applied before it is due");
        AssertEqual("aftermath", process.TryApplyDueStage(5)!.StageId, "Second stage resolves on its due day");
        AssertTrue(process.IsCompleted, "The fixed process records completion after its final stage");
    }

    private static GameState CreateGame()
    {
        var expedition = new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }, supplies: 5);
        return new GameState(new WorldState(CreateMap()), new KnowledgeState(), new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
    }

    private static HexMapState CreateMap() => HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
