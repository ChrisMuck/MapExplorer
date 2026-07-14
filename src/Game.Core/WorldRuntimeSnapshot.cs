#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// Serialization-safe snapshot of the changing cross-system part of a world. Map topology,
/// paths and location instances are deliberately supplied by the wider campaign save, while this
/// type preserves generated context, resolved process history, deterministic state and traces.
/// </summary>
public sealed class WorldRuntimeSnapshot
{
    public int WorldDay { get; set; }
    public Dictionary<string, int> RuntimeIdNextNumbers { get; set; } = new(StringComparer.Ordinal);
    public uint RandomSeed { get; set; }
    public uint RandomCurrentState { get; set; }
    public List<WorldTriggerRuntimeSnapshot> WorldTriggers { get; set; } = new();
    public List<ScheduledConsequenceRuntimeSnapshot> ScheduledConsequences { get; set; } = new();
    public List<FactionAwarenessRuntimeSnapshot> FactionAwareness { get; set; } = new();
    public List<GeneratedContextRuntimeSnapshot> GeneratedContexts { get; set; } = new();
    public List<WorldSituationRuntimeSnapshot> Situations { get; set; } = new();
    public List<SimulationTraceRuntimeSnapshot> Traces { get; set; } = new();

    public static WorldRuntimeSnapshot Capture(WorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        return new WorldRuntimeSnapshot
        {
            WorldDay = world.WorldDay,
            RuntimeIdNextNumbers = world.RuntimeIds.NextNumbers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            RandomSeed = world.Random.Seed,
            RandomCurrentState = world.Random.CurrentState,
            WorldTriggers = world.WorldTriggers.Select(WorldTriggerRuntimeSnapshot.FromState).ToList(),
            ScheduledConsequences = world.ScheduledConsequences.Select(ScheduledConsequenceRuntimeSnapshot.FromState).ToList(),
            FactionAwareness = world.FactionAwareness.Select(FactionAwarenessRuntimeSnapshot.FromState).ToList(),
            GeneratedContexts = world.GeneratedContexts.Select(GeneratedContextRuntimeSnapshot.FromState).ToList(),
            Situations = world.Situations.Select(WorldSituationRuntimeSnapshot.FromState).ToList(),
            Traces = world.Traces.Select(SimulationTraceRuntimeSnapshot.FromState).ToList()
        };
    }

    /// <summary>
    /// Restores runtime history onto the separately loaded immutable/generated world topology.
    /// The caller is responsible for loading matching map, paths and locations from the campaign save.
    /// </summary>
    public WorldState Restore(
        HexMapState map,
        IEnumerable<WorldPathState>? paths = null,
        IEnumerable<SpecialLocationState>? locations = null)
    {
        if (WorldDay < 1) throw new InvalidOperationException("World runtime snapshot has an invalid world day.");
        if (map == null) throw new ArgumentNullException(nameof(map));

        var triggers = (WorldTriggers ?? new List<WorldTriggerRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();
        var consequences = (ScheduledConsequences ?? new List<ScheduledConsequenceRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();
        var awareness = (FactionAwareness ?? new List<FactionAwarenessRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();
        var contexts = (GeneratedContexts ?? new List<GeneratedContextRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();
        var situations = (Situations ?? new List<WorldSituationRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();
        var traces = (Traces ?? new List<SimulationTraceRuntimeSnapshot>()).Select(snapshot => snapshot.ToState()).ToList();

        return new WorldState(
            map,
            paths,
            locations,
            WorldDay,
            triggers,
            consequences,
            awareness,
            contexts,
            situations,
            traces,
            new RuntimeIdAllocatorState(RuntimeIdNextNumbers ?? new Dictionary<string, int>()),
            new DeterministicRandomState(RandomSeed, RandomCurrentState));
    }
}

public sealed class RuntimeHexCoordSnapshot
{
    public int Q { get; set; }
    public int R { get; set; }

    public static RuntimeHexCoordSnapshot FromState(HexCoord coord) => new() { Q = coord.Q, R = coord.R };
    public HexCoord ToState() => new(Q, R);
}

public sealed class WorldTriggerRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string TriggerId { get; set; } = string.Empty;
    public int RaisedWorldDay { get; set; }
    public List<string> ActionTags { get; set; } = new();
    public string? SourceLocationId { get; set; }
    public RuntimeHexCoordSnapshot? SourceCoord { get; set; }
    public string? CausedByTraceId { get; set; }
    public bool IsResolved { get; set; }

    public static WorldTriggerRuntimeSnapshot FromState(WorldTriggerState state) => new()
    {
        Id = state.Id,
        TriggerId = state.TriggerId,
        RaisedWorldDay = state.RaisedWorldDay,
        ActionTags = state.ActionTags.ToList(),
        SourceLocationId = state.SourceLocationId,
        SourceCoord = state.SourceCoord.HasValue ? RuntimeHexCoordSnapshot.FromState(state.SourceCoord.Value) : null,
        CausedByTraceId = state.CausedByTraceId,
        IsResolved = state.IsResolved
    };

    public WorldTriggerState ToState()
    {
        var state = new WorldTriggerState(Id, TriggerId, RaisedWorldDay, ActionTags, SourceLocationId, SourceCoord?.ToState(), CausedByTraceId);
        if (IsResolved) state.MarkResolved();
        return state;
    }
}

public sealed class ScheduledConsequenceStageRuntimeSnapshot
{
    public string StageId { get; set; } = string.Empty;
    public int DueWorldDay { get; set; }
    public List<string> ResolvedEffectIds { get; set; } = new();
    public bool IsApplied { get; set; }

    public static ScheduledConsequenceStageRuntimeSnapshot FromState(ScheduledConsequenceStageState state) => new()
    {
        StageId = state.StageId,
        DueWorldDay = state.DueWorldDay,
        ResolvedEffectIds = state.ResolvedEffectIds.ToList(),
        IsApplied = state.IsApplied
    };
}

public sealed class ScheduledConsequenceRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string ResolvedBranchId { get; set; } = string.Empty;
    public string SourceLocationId { get; set; } = string.Empty;
    public string? SourceTriggerId { get; set; }
    public List<string> AffectedContextIds { get; set; } = new();
    public List<ScheduledConsequenceStageRuntimeSnapshot> Stages { get; set; } = new();

    public static ScheduledConsequenceRuntimeSnapshot FromState(ScheduledConsequenceState state) => new()
    {
        Id = state.Id,
        DefinitionId = state.DefinitionId,
        ResolvedBranchId = state.ResolvedBranchId,
        SourceLocationId = state.SourceLocationId,
        SourceTriggerId = state.SourceTriggerId,
        AffectedContextIds = state.AffectedContextIds.ToList(),
        Stages = state.Stages.Select(ScheduledConsequenceStageRuntimeSnapshot.FromState).ToList()
    };

    public ScheduledConsequenceState ToState()
    {
        var stageSnapshots = Stages ?? new List<ScheduledConsequenceStageRuntimeSnapshot>();
        var appliedCount = 0;
        var encounteredUnappliedStage = false;
        foreach (var stage in stageSnapshots)
        {
            if (stage.IsApplied)
            {
                if (encounteredUnappliedStage)
                {
                    throw new InvalidOperationException("A saved consequence cannot contain an applied stage after an unapplied stage.");
                }

                appliedCount++;
            }
            else
            {
                encounteredUnappliedStage = true;
            }
        }

        var state = new ScheduledConsequenceState(
            Id,
            DefinitionId,
            ResolvedBranchId,
            SourceLocationId,
            SourceTriggerId,
            AffectedContextIds,
            stageSnapshots.Select(stage => new ScheduledConsequenceStageState(stage.StageId, stage.DueWorldDay, stage.ResolvedEffectIds)));
        for (var index = 0; index < appliedCount; index++) state.MarkCurrentStageApplied();
        return state;
    }
}

public sealed class FactionAwarenessRuntimeSnapshot
{
    public string FactionId { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public FactionAwarenessLevel Level { get; set; }

    public static FactionAwarenessRuntimeSnapshot FromState(RegionFactionAwarenessState state) => new()
    {
        FactionId = state.FactionId,
        RegionId = state.RegionId,
        Level = state.Level
    };

    public RegionFactionAwarenessState ToState() => new(FactionId, RegionId, Level);
}

public sealed class GeneratedContextRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string ContextDefinitionId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int AssignedWorldDay { get; set; }

    public static GeneratedContextRuntimeSnapshot FromState(GeneratedContextAssignmentState state) => new()
    {
        Id = state.Id,
        ContextDefinitionId = state.ContextDefinitionId,
        TargetId = state.TargetId,
        AssignedWorldDay = state.AssignedWorldDay
    };

    public GeneratedContextAssignmentState ToState() => new(Id, ContextDefinitionId, TargetId, AssignedWorldDay);
}

public sealed class WorldSituationRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public int CreatedWorldDay { get; set; }
    public string? SourceProcessId { get; set; }
    public string? SourceLocationId { get; set; }
    public int? DueWorldDay { get; set; }
    public WorldSituationStatus Status { get; set; }

    public static WorldSituationRuntimeSnapshot FromState(WorldSituationState state) => new()
    {
        Id = state.Id,
        DefinitionId = state.DefinitionId,
        CreatedWorldDay = state.CreatedWorldDay,
        SourceProcessId = state.SourceProcessId,
        SourceLocationId = state.SourceLocationId,
        DueWorldDay = state.DueWorldDay,
        Status = state.Status
    };

    public WorldSituationState ToState() => new(Id, DefinitionId, CreatedWorldDay, SourceProcessId, SourceLocationId, DueWorldDay, Status);
}

public sealed class SimulationTraceRuntimeSnapshot
{
    public string TraceId { get; set; } = string.Empty;
    public SimulationTraceKind Kind { get; set; }
    public int WorldDay { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> CausedByTraceIds { get; set; } = new();
    public List<string> SubjectIds { get; set; } = new();

    public static SimulationTraceRuntimeSnapshot FromState(SimulationTraceState state) => new()
    {
        TraceId = state.TraceId,
        Kind = state.Kind,
        WorldDay = state.WorldDay,
        Summary = state.Summary,
        CausedByTraceIds = state.CausedByTraceIds.ToList(),
        SubjectIds = state.SubjectIds.ToList()
    };

    public SimulationTraceState ToState() => new(TraceId, Kind, WorldDay, Summary, CausedByTraceIds, SubjectIds);
}
}
