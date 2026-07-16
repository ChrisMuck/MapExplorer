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
    private readonly List<WorldConnectionState> connections;
    private readonly List<SimulationTraceState> traces;
    private readonly HashSet<string> acquiredFindingKeys;
    private readonly List<FindingTableRollState> findingTableRolls;

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
        DeterministicRandomState? random = null,
        IEnumerable<string>? acquiredFindingKeys = null,
        IEnumerable<WorldConnectionState>? connections = null,
        IEnumerable<FindingTableRollState>? findingTableRolls = null)
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
        this.connections = new List<WorldConnectionState>(connections ?? Enumerable.Empty<WorldConnectionState>());
        this.traces = new List<SimulationTraceState>(traces ?? Enumerable.Empty<SimulationTraceState>());
        this.acquiredFindingKeys = new HashSet<string>((acquiredFindingKeys ?? Enumerable.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim()), StringComparer.Ordinal);
        this.findingTableRolls = new List<FindingTableRollState>(findingTableRolls ?? Enumerable.Empty<FindingTableRollState>());
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

    /// <summary>Logical world connections created by processes; they are not a visual map graph.</summary>
    public IReadOnlyList<WorldConnectionState> Connections => connections;

    /// <summary>Objective developer provenance; normal player UI must not expose it directly.</summary>
    public IReadOnlyList<SimulationTraceState> Traces => traces;

    /// <summary>Stable repeat-policy keys for findings already recovered in this world.</summary>
    public IReadOnlyCollection<string> AcquiredFindingKeys => acquiredFindingKeys;

    public IReadOnlyList<FindingTableRollState> FindingTableRolls => findingTableRolls;

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

    public bool TryAddSituation(WorldSituationState situation)
    {
        if (situation == null) throw new ArgumentNullException(nameof(situation));
        if (situations.Any(existing => existing.Id == situation.Id)) return false;
        if (situation.FactionId != null && situations.Any(existing =>
                existing.FactionId == situation.FactionId &&
                (existing.Status == WorldSituationStatus.Dormant || existing.Status == WorldSituationStatus.Active)))
        {
            return false;
        }
        situations.Add(situation);
        return true;
    }

    public void AddSituation(WorldSituationState situation)
    {
        TryAddSituation(situation);
    }

    public bool AddConnection(WorldConnectionState connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (connections.Any(existing => existing.Id == connection.Id ||
                                        (existing.SourceId == connection.SourceId && existing.TargetId == connection.TargetId && existing.Kind == connection.Kind)))
        {
            return false;
        }

        connections.Add(connection);
        return true;
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

    public bool TryRegisterFinding(string findingDefinitionId, string repeatPolicy, string sourceLocationId)
    {
        if (string.IsNullOrWhiteSpace(findingDefinitionId)) throw new ArgumentException("Finding definition ID must not be empty.", nameof(findingDefinitionId));
        if (string.IsNullOrWhiteSpace(sourceLocationId)) throw new ArgumentException("Finding source location ID must not be empty.", nameof(sourceLocationId));

        var key = FindingRepeatKey(findingDefinitionId, repeatPolicy, sourceLocationId);
        return acquiredFindingKeys.Add(key);
    }

    public bool CanRegisterFinding(string findingDefinitionId, string repeatPolicy, string sourceLocationId) =>
        !acquiredFindingKeys.Contains(FindingRepeatKey(findingDefinitionId, repeatPolicy, sourceLocationId));

    public FindingTableRollState? FindFindingTableRoll(string key) =>
        findingTableRolls.FirstOrDefault(roll => roll.Key == key);

    public void RecordFindingTableRoll(FindingTableRollState roll)
    {
        if (roll == null) throw new ArgumentNullException(nameof(roll));
        if (FindFindingTableRoll(roll.Key) != null) throw new InvalidOperationException($"Finding table roll '{roll.Key}' is already resolved.");
        findingTableRolls.Add(roll);
    }

    private static string FindingRepeatKey(string findingDefinitionId, string repeatPolicy, string sourceLocationId)
    {
        if (string.IsNullOrWhiteSpace(findingDefinitionId)) throw new ArgumentException("Finding definition ID must not be empty.", nameof(findingDefinitionId));
        if (string.IsNullOrWhiteSpace(sourceLocationId)) throw new ArgumentException("Finding source location ID must not be empty.", nameof(sourceLocationId));
        var policy = string.IsNullOrWhiteSpace(repeatPolicy) ? "once-per-world" : repeatPolicy.Trim();
        return policy == "once-per-location"
            ? $"location:{sourceLocationId.Trim()}:{findingDefinitionId.Trim()}"
            : $"world:{findingDefinitionId.Trim()}";
    }
}
}
