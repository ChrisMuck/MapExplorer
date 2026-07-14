#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class WorldState
{
    private readonly List<WorldPathState> paths;
    private readonly List<SpecialLocationState> locations;
    private readonly List<WorldTriggerState> worldTriggers;
    private readonly List<ScheduledConsequenceState> scheduledConsequences;
    private readonly List<RegionFactionAwarenessState> factionAwareness;
    private readonly List<GeneratedContextAssignmentState> generatedContexts;
    private readonly List<WorldSituationState> situations;
    private readonly List<SimulationTraceState> traces;

    public WorldState(
        HexMapState map,
        IEnumerable<WorldPathState>? paths = null,
        IEnumerable<SpecialLocationState>? locations = null,
        int worldDay = 1,
        IEnumerable<WorldTriggerState>? worldTriggers = null,
        IEnumerable<ScheduledConsequenceState>? scheduledConsequences = null,
        IEnumerable<RegionFactionAwarenessState>? factionAwareness = null,
        IEnumerable<GeneratedContextAssignmentState>? generatedContexts = null,
        IEnumerable<WorldSituationState>? situations = null,
        IEnumerable<SimulationTraceState>? traces = null,
        RuntimeIdAllocatorState? runtimeIds = null,
        DeterministicRandomState? random = null)
    {
        if (worldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must be at least 1.");
        }

        Map = map ?? throw new ArgumentNullException(nameof(map));
        this.paths = new List<WorldPathState>(paths ?? Enumerable.Empty<WorldPathState>());
        this.locations = new List<SpecialLocationState>(locations ?? Enumerable.Empty<SpecialLocationState>());
        this.worldTriggers = new List<WorldTriggerState>(worldTriggers ?? Enumerable.Empty<WorldTriggerState>());
        this.scheduledConsequences = new List<ScheduledConsequenceState>(scheduledConsequences ?? Enumerable.Empty<ScheduledConsequenceState>());
        this.factionAwareness = new List<RegionFactionAwarenessState>(factionAwareness ?? Enumerable.Empty<RegionFactionAwarenessState>());
        this.generatedContexts = new List<GeneratedContextAssignmentState>(generatedContexts ?? Enumerable.Empty<GeneratedContextAssignmentState>());
        this.situations = new List<WorldSituationState>(situations ?? Enumerable.Empty<WorldSituationState>());
        this.traces = new List<SimulationTraceState>(traces ?? Enumerable.Empty<SimulationTraceState>());
        RuntimeIds = runtimeIds ?? new RuntimeIdAllocatorState();
        Random = random ?? new DeterministicRandomState();
        WorldDay = worldDay;
    }

    public HexMapState Map { get; }

    public IReadOnlyList<WorldPathState> Paths
    {
        get { return paths; }
    }

    public IReadOnlyList<SpecialLocationState> Locations
    {
        get { return locations; }
    }

    public int WorldDay { get; private set; }

    public IReadOnlyList<WorldTriggerState> WorldTriggers
    {
        get { return worldTriggers; }
    }

    public IReadOnlyList<ScheduledConsequenceState> ScheduledConsequences
    {
        get { return scheduledConsequences; }
    }

    public IReadOnlyList<RegionFactionAwarenessState> FactionAwareness => factionAwareness;

    /// <summary>Generated neutral context attached to concrete runtime targets, never static content.</summary>
    public IReadOnlyList<GeneratedContextAssignmentState> GeneratedContexts => generatedContexts;

    /// <summary>Current world pressures, requests and warnings that can outlive an expedition.</summary>
    public IReadOnlyList<WorldSituationState> Situations => situations;

    /// <summary>Objective developer provenance; normal player UI must not expose it directly.</summary>
    public IReadOnlyList<SimulationTraceState> Traces => traces;

    public RuntimeIdAllocatorState RuntimeIds { get; }

    public DeterministicRandomState Random { get; }

    public void AddPath(WorldPathState path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (paths.Any(existing => existing.Id == path.Id))
        {
            return;
        }

        paths.Add(path);
    }

    public void AdvanceDays(int days)
    {
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Advance must be at least one day.");
        }

        WorldDay += days;
    }

    public void QueueWorldTrigger(WorldTriggerState trigger)
    {
        if (trigger == null)
        {
            throw new ArgumentNullException(nameof(trigger));
        }

        if (worldTriggers.Any(existing => existing.Id == trigger.Id))
        {
            return;
        }

        worldTriggers.Add(trigger);
    }

    public void ScheduleConsequence(ScheduledConsequenceState consequence)
    {
        if (consequence == null)
        {
            throw new ArgumentNullException(nameof(consequence));
        }

        if (scheduledConsequences.Any(existing => existing.Id == consequence.Id))
        {
            return;
        }

        scheduledConsequences.Add(consequence);
    }

    public void AddGeneratedContext(GeneratedContextAssignmentState context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (generatedContexts.Any(existing => existing.Id == context.Id)) return;
        generatedContexts.Add(context);
    }

    public void AddSituation(WorldSituationState situation)
    {
        if (situation == null) throw new ArgumentNullException(nameof(situation));
        if (situations.Any(existing => existing.Id == situation.Id)) return;
        situations.Add(situation);
    }

    public SimulationTraceState RecordTrace(
        SimulationTraceKind kind,
        string summary,
        IEnumerable<string>? causedByTraceIds = null,
        IEnumerable<string>? subjectIds = null)
    {
        var trace = new SimulationTraceState(
            RuntimeIds.Allocate("trace"),
            kind,
            WorldDay,
            summary,
            causedByTraceIds,
            subjectIds);
        traces.Add(trace);
        return trace;
    }

    public void EscalateFactionAwareness(string factionId, string regionId)
    {
        var state = factionAwareness.FirstOrDefault(item => item.FactionId == factionId && item.RegionId == regionId);
        if (state == null)
        {
            state = new RegionFactionAwarenessState(factionId, regionId);
            factionAwareness.Add(state);
        }

        state.Escalate();
    }
}
}
