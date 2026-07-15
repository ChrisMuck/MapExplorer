#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class SimulationRuntimeStateTests
{
    public void RunAll()
    {
        RuntimeIdsAndRandomStreamAreDeterministic();
        WorldStoresContextSituationAndCausalTraceWithoutChangingKnowledge();
        ConsequenceStagesAdvanceInOrderWithoutRerollingBranch();
        WeightedBranchSelectionIsDeterministic();
        SameSeedAndTriggerSequenceFixSameWorldProcessBranch();
        DueStageCanCreateAnAuthoredWorldSituation();
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

    private static void WeightedBranchSelectionIsDeterministic()
    {
        var consequence = new ConsequenceDefinition(
            "consequence-branching",
            new[]
            {
                new ConsequenceBranchDefinition("contained", 1, new[] { new ConsequenceStageDefinition("contained-sign", 1, null, null, null) }),
                new ConsequenceBranchDefinition("spreading", 3, new[] { new ConsequenceStageDefinition("spreading-sign", 1, null, null, null) })
            });

        AssertEqual("contained", consequence.SelectBranch(new FixedRandomSource(0)).Id, "First weighted roll selects first sorted branch");
        AssertEqual("spreading", consequence.SelectBranch(new FixedRandomSource(1)).Id, "Later weighted roll selects second branch");
        AssertEqual("spreading", consequence.SelectBranch(new FixedRandomSource(3)).Id, "Weighted selection includes the final slot");

        var oneBranch = new ConsequenceDefinition("consequence-fixed", new[] { new ConsequenceStageDefinition("only-stage", 1, null, null, null) });
        var world = new WorldState(CreateMap(), random: new DeterministicRandomState(77));
        var before = world.Random.CurrentState;
        AssertEqual("default", oneBranch.SelectBranch(new WorldDeterministicRandomSource(world)).Id, "Single branch remains the default branch");
        AssertEqual(before, world.Random.CurrentState, "A fixed branch does not consume the deterministic random stream");
    }

    private static void DueStageCanCreateAnAuthoredWorldSituation()
    {
        var situationId = "situation-test-warning";
        var authoring = new CrossSystemAuthoringBundle(
            Array.Empty<LocationStateProfileDefinition>(),
            Array.Empty<LocationScenarioProfileDefinition>(),
            Array.Empty<FindingDefinition>(),
            Array.Empty<ContextDefinition>(),
            new[] { new SituationDefinition(situationId, "warning", new[] { "world" }, new[] { "soon" }, new[] { "investigate" }, new[] { "resolved" }) },
            Array.Empty<FactionOfferContentDefinition>(),
            Array.Empty<FactionMemoryDefinition>());
        var consequence = new ConsequenceDefinition(
            "consequence-situation",
            new[]
            {
                new ConsequenceBranchDefinition("default", 1, new[]
                {
                    new ConsequenceStageDefinition("warning-stage", 0, null, "Warning", "Something changed.", new[] { situationId })
                })
            });
        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            triggers: new[] { new WorldTriggerDefinition("trigger-situation", consequence.Id) },
            consequences: new[] { consequence });
        var game = CreateGame();
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "trigger-situation", game.World.WorldDay));

        new WorldPhaseService(content, authoring).Resolve(game);

        AssertEqual(1, game.World.Situations.Count, "Due stage creates a generated situation instance");
        AssertEqual(situationId, game.World.Situations[0].DefinitionId, "Situation retains authored definition ID");
        AssertEqual(WorldSituationStatus.Active, game.World.Situations[0].Status, "Created situation is active");
        AssertEqual(0, game.Knowledge.Evidence.Count, "World truth situation does not automatically reveal player knowledge");
    }

    private static void SameSeedAndTriggerSequenceFixSameWorldProcessBranch()
    {
        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            triggers: new[] { new WorldTriggerDefinition("trigger-branching", "consequence-branching") },
            consequences: new[]
            {
                new ConsequenceDefinition("consequence-branching", new[]
                {
                    new ConsequenceBranchDefinition("contained", 1, new[] { new ConsequenceStageDefinition("contained-stage", 3, null, null, null) }),
                    new ConsequenceBranchDefinition("spreading", 3, new[] { new ConsequenceStageDefinition("spreading-stage", 3, null, null, null) })
                })
            });
        var first = CreateGame(seed: 12345);
        var second = CreateGame(seed: 12345);
        first.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "trigger-branching", 1));
        second.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "trigger-branching", 1));

        new WorldPhaseService(content).Resolve(first);
        new WorldPhaseService(content).Resolve(second);

        AssertEqual(first.World.ScheduledConsequences[0].ResolvedBranchId, second.World.ScheduledConsequences[0].ResolvedBranchId, "Same seed and trigger sequence fixes the same branch");
        AssertEqual(first.World.Traces.Select(trace => trace.Kind).ToArray().Length, second.World.Traces.Select(trace => trace.Kind).ToArray().Length, "Same sequence emits the same trace count");
    }

    private static GameState CreateGame(uint seed = 1)
    {
        var expedition = new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }, supplies: 5);
        return new GameState(new WorldState(CreateMap(), random: new DeterministicRandomState(seed)), new KnowledgeState(), new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero));
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

    private sealed class FixedRandomSource : IDeterministicRandomSource
    {
        private readonly int value;
        public FixedRandomSource(int value) => this.value = value;
        public int NextInt(int exclusiveMaximum) => value;
    }
}
