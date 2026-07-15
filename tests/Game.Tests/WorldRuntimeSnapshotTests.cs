#nullable enable
using System;
using System.Collections.Generic;
using Game.App;
using Game.Core;

internal sealed class WorldRuntimeSnapshotTests
{
    public void RunAll()
    {
        RuntimeSnapshotRoundTripPreservesCrossSystemHistory();
        SnapshotRejectsNonContiguousAppliedStages();
    }

    private static void RuntimeSnapshotRoundTripPreservesCrossSystemHistory()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 4), TerrainType.Grassland);
        var location = new SpecialLocationState("location-1", LocationKind.Ruin, new HexCoord(1, 1), "Old Site");
        var path = new WorldPathState("road-1", WorldPathKind.Road, new[] { HexCoord.Zero, new HexCoord(1, 0) });
        var world = new WorldState(
            map,
            new[] { path },
            new[] { location },
            random: new DeterministicRandomState(9001));
        world.AdvanceDays(3);
        world.Random.NextUInt32();
        world.AddGeneratedContext(new GeneratedContextAssignmentState("context-1", "context-deliberate-closure", location.Id, 1));
        var situation = new WorldSituationState("situation-1", "situation-request-help-with-crossing", 2, "process-1", location.Id, 8);
        situation.Activate();
        world.AddSituation(situation);
        world.AddSituation(new WorldSituationState(
            "situation-promise", "situation-request-help-with-crossing", 3, sourceLocationId: location.Id,
            dueWorldDay: 9, status: WorldSituationStatus.Promised, factionId: "faction-1", resolutionActionTag: ResolveWorldSituationCommand.PromiseReturnActionTag));
        world.EscalateFactionAwareness("faction-1", "location-region:location-1");
        world.EscalateFactionAwareness("faction-1", "location-region:location-1");
        AssertTrue(world.TryRegisterFinding("finding-seal-fragment", "once-per-world", location.Id), "World registers acquired finding before snapshot");
        world.RecordFindingTableRoll(new FindingTableRollState(
            "table-sealed:location:location-1", "table-sealed", location.Id,
            "opened|inactive|unknown", null, world.WorldDay));

        var commandTrace = world.RecordTrace(SimulationTraceKind.Command, "Opened an old site.", subjectIds: new[] { location.Id });
        var trigger = new WorldTriggerState("trigger-1", "location-seal-broken", 2, new[] { "open" }, location.Id, location.Coord, commandTrace.TraceId);
        trigger.MarkResolved();
        world.QueueWorldTrigger(trigger);
        var process = new ScheduledConsequenceState(
            "process-1",
            "consequence-seal-broken",
            "spreading",
            location.Id,
            trigger.Id,
            new[] { "context-deliberate-closure" },
            new[]
            {
                new ScheduledConsequenceStageState("early", 3, new[] { "early-effect" }),
                new ScheduledConsequenceStageState("late", 7, new[] { "late-effect" })
            });
        process.TryApplyDueStage(world.WorldDay);
        world.ScheduleConsequence(process);
        world.RecordTrace(SimulationTraceKind.WorldProcessScheduled, "Scheduled the fixed spreading branch.", new[] { commandTrace.TraceId }, new[] { process.Id });

        var json = WorldRuntimeSnapshotSerializer.Serialize(world);
        var restored = WorldRuntimeSnapshotSerializer.Deserialize(json, map, new[] { path }, new[] { location });

        AssertEqual(world.WorldDay, restored.WorldDay, "World day survives runtime snapshot roundtrip");
        AssertEqual(world.Random.Seed, restored.Random.Seed, "Random seed survives runtime snapshot roundtrip");
        AssertEqual(world.Random.CurrentState, restored.Random.CurrentState, "Current deterministic random state survives runtime snapshot roundtrip");
        AssertEqual("trace-3", restored.RuntimeIds.Allocate("trace"), "Runtime ID sequence resumes after restored trace history");
        AssertEqual(1, restored.GeneratedContexts.Count, "Generated context survives runtime snapshot roundtrip");
        AssertEqual(WorldSituationStatus.Active, restored.Situations[0].Status, "Active situation survives runtime snapshot roundtrip");
        var restoredPromise = restored.Situations.Single(item => item.Id == "situation-promise");
        AssertEqual(WorldSituationStatus.Promised, restoredPromise.Status, "Open promise survives runtime snapshot roundtrip");
        AssertEqual(ResolveWorldSituationCommand.PromiseReturnActionTag, restoredPromise.ResolutionActionTag, "Promise response tag survives runtime snapshot roundtrip");
        AssertEqual(FactionAwarenessLevel.Alert, restored.FactionAwareness[0].Level, "Faction awareness survives runtime snapshot roundtrip");
        AssertTrue(restored.WorldTriggers[0].IsResolved, "Trigger resolution state survives runtime snapshot roundtrip");
        AssertEqual(commandTrace.TraceId, restored.WorldTriggers[0].CausedByTraceId, "Trigger trace parent survives runtime snapshot roundtrip");
        AssertEqual(location.Coord, restored.WorldTriggers[0].SourceCoord!.Value, "Trigger source coordinate survives runtime snapshot roundtrip");
        AssertEqual("spreading", restored.ScheduledConsequences[0].ResolvedBranchId, "Resolved branch survives runtime snapshot roundtrip");
        AssertEqual(1, restored.ScheduledConsequences[0].CurrentStageIndex, "Applied process stage survives runtime snapshot roundtrip");
        AssertEqual(2, restored.Traces.Count, "Causal trace entries survive runtime snapshot roundtrip");
        AssertEqual(commandTrace.TraceId, restored.Traces[1].CausedByTraceIds[0], "Trace parent link survives runtime snapshot roundtrip");
        AssertTrue(!restored.TryRegisterFinding("finding-seal-fragment", "once-per-world", location.Id), "Finding repeat state survives runtime snapshot roundtrip");
        AssertEqual(1, restored.FindingTableRolls.Count, "Finding-table history survives runtime snapshot roundtrip");
        AssertTrue(restored.FindingTableRolls[0].IsEmpty, "An empty finding-table result survives without becoming rerollable");

        var content = new CrossSystemDataBundle(
            new EvidenceDefinitionSet(Array.Empty<EvidenceDefinition>()),
            consequences: new[]
            {
                new ConsequenceDefinition("consequence-seal-broken", new[]
                {
                    new ConsequenceBranchDefinition("spreading", 1, new[]
                    {
                        new ConsequenceStageDefinition("early", 0, null, null, null),
                        new ConsequenceStageDefinition("late", 0, null, null, null)
                    })
                })
            });
        restored.AdvanceDays(3);
        var resumedGame = new GameState(
            restored,
            new KnowledgeState(),
            new PlayerNotesState(),
            new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }, supplies: 5),
            new BaseState(HexCoord.Zero));
        new WorldPhaseService(content).Resolve(resumedGame);
        AssertEqual("spreading", restored.ScheduledConsequences[0].ResolvedBranchId, "Restored process continues its saved branch without rerolling");
        AssertTrue(restored.ScheduledConsequences[0].IsCompleted, "Restored process can continue with its remaining saved stage");
    }

    private static void SnapshotRejectsNonContiguousAppliedStages()
    {
        var snapshot = new ScheduledConsequenceRuntimeSnapshot
        {
            Id = "process-1",
            DefinitionId = "consequence-test",
            ResolvedBranchId = "default",
            SourceLocationId = "location-1",
            Stages = new List<ScheduledConsequenceStageRuntimeSnapshot>
            {
                new() { StageId = "first", DueWorldDay = 2, ResolvedEffectIds = new List<string> { "effect-first" }, IsApplied = false },
                new() { StageId = "second", DueWorldDay = 3, ResolvedEffectIds = new List<string> { "effect-second" }, IsApplied = true }
            }
        };

        AssertThrows(() => snapshot.ToState(), "Snapshot rejects a later applied stage without its earlier stage");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
