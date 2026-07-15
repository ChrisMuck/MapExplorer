#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>Deterministic random contract used by Core resolution services.</summary>
public interface IDeterministicRandomSource
{
    int NextInt(int exclusiveMaximum);
}

/// <summary>
/// Persisted deterministic runtime support. The allocator is intentionally part of simulation
/// state so generated IDs and causal references remain stable after a suspend/load boundary.
/// </summary>
public sealed class RuntimeIdAllocatorState
{
    private readonly Dictionary<string, int> nextNumbers;

    public RuntimeIdAllocatorState(IEnumerable<KeyValuePair<string, int>>? nextNumbers = null)
    {
        this.nextNumbers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in nextNumbers ?? Enumerable.Empty<KeyValuePair<string, int>>())
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 1)
            {
                throw new ArgumentException("Runtime ID counters require a non-empty scope and a positive next number.", nameof(nextNumbers));
            }

            this.nextNumbers[pair.Key.Trim()] = pair.Value;
        }
    }

    public IReadOnlyDictionary<string, int> NextNumbers => nextNumbers;

    public string Allocate(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Runtime ID scope must not be empty.", nameof(scope));
        }

        scope = scope.Trim();
        var next = nextNumbers.TryGetValue(scope, out var stored) ? stored : 1;
        nextNumbers[scope] = checked(next + 1);
        return $"{scope}-{next}";
    }
}

/// <summary>Stateful deterministic random stream for simulation decisions. Unity must not supply it.</summary>
public sealed class DeterministicRandomState
{
    public DeterministicRandomState(uint seed = 1, uint? currentState = null)
    {
        Seed = seed == 0 ? 1u : seed;
        CurrentState = currentState.GetValueOrDefault(Seed);
        if (CurrentState == 0) CurrentState = Seed;
    }

    public uint Seed { get; }
    public uint CurrentState { get; private set; }

    public uint NextUInt32()
    {
        // xorshift32: tiny, deterministic and sufficient for repeatable authored branch selection.
        var value = CurrentState;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        CurrentState = value == 0 ? Seed : value;
        return CurrentState;
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum < 1) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        return (int)(NextUInt32() % (uint)exclusiveMaximum);
    }
}

public enum SimulationTraceKind
{
    Command,
    GeneratorDecision,
    WorldTrigger,
    WorldProcessScheduled,
    ConsequenceStageApplied,
    FactionObservation,
    FactionReaction,
    SituationChanged,
    KnowledgeObserved
}

/// <summary>
/// Developer-only provenance for an authoritative state change. It is objective runtime history,
/// never a player-facing explanation unless a separate knowledge source makes it known.
/// </summary>
public sealed class SimulationTraceState
{
    private readonly List<string> causedByTraceIds;
    private readonly List<string> subjectIds;

    public SimulationTraceState(
        string traceId,
        SimulationTraceKind kind,
        int worldDay,
        string summary,
        IEnumerable<string>? causedByTraceIds = null,
        IEnumerable<string>? subjectIds = null)
    {
        if (worldDay < 1) throw new ArgumentOutOfRangeException(nameof(worldDay));
        TraceId = RequireText(traceId, nameof(traceId));
        Kind = kind;
        WorldDay = worldDay;
        Summary = RequireText(summary, nameof(summary));
        this.causedByTraceIds = Normalize(causedByTraceIds);
        this.subjectIds = Normalize(subjectIds);
    }

    public string TraceId { get; }
    public SimulationTraceKind Kind { get; }
    public int WorldDay { get; }
    public string Summary { get; }
    public IReadOnlyList<string> CausedByTraceIds => causedByTraceIds;
    public IReadOnlyList<string> SubjectIds => subjectIds;

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }

    private static List<string> Normalize(IEnumerable<string>? values) => (values ?? Enumerable.Empty<string>())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public sealed class GeneratedContextAssignmentState
{
    public GeneratedContextAssignmentState(string id, string contextDefinitionId, string targetId, int assignedWorldDay)
    {
        if (assignedWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(assignedWorldDay));
        Id = RequireText(id, nameof(id));
        ContextDefinitionId = RequireText(contextDefinitionId, nameof(contextDefinitionId));
        TargetId = RequireText(targetId, nameof(targetId));
        AssignedWorldDay = assignedWorldDay;
    }

    public string Id { get; }
    public string ContextDefinitionId { get; }
    public string TargetId { get; }
    public int AssignedWorldDay { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}

public enum WorldSituationStatus
{
    Dormant,
    Active,
    Resolved,
    Expired,
    Promised
}

/// <summary>A logical relationship discovered or created by a world process; presentation chooses how to show it.</summary>
public sealed class WorldConnectionState
{
    private readonly List<string> tags;

    public WorldConnectionState(string id, string sourceId, string targetId, string kind, int createdWorldDay, IEnumerable<string>? tags = null)
    {
        if (createdWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(createdWorldDay));
        Id = RequireText(id, nameof(id));
        SourceId = RequireText(sourceId, nameof(sourceId));
        TargetId = RequireText(targetId, nameof(targetId));
        Kind = RequireText(kind, nameof(kind));
        CreatedWorldDay = createdWorldDay;
        this.tags = (tags ?? Enumerable.Empty<string>()).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.Ordinal).ToList();
    }

    public string Id { get; }
    public string SourceId { get; }
    public string TargetId { get; }
    public string Kind { get; }
    public int CreatedWorldDay { get; }
    public IReadOnlyList<string> Tags => tags;

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}

/// <summary>A concrete generated pressure, request or warning. It is not a static quest definition.</summary>
public sealed class WorldSituationState
{
    public WorldSituationState(
        string id,
        string definitionId,
        int createdWorldDay,
        string? sourceProcessId = null,
        string? sourceLocationId = null,
        int? dueWorldDay = null,
        WorldSituationStatus status = WorldSituationStatus.Dormant,
        string? factionId = null,
        string? resolutionActionTag = null,
        string? sourceKind = null,
        string? deliveryChannel = null)
    {
        if (createdWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(createdWorldDay));
        if (dueWorldDay.HasValue && dueWorldDay.Value < createdWorldDay) throw new ArgumentOutOfRangeException(nameof(dueWorldDay));
        Id = RequireText(id, nameof(id));
        DefinitionId = RequireText(definitionId, nameof(definitionId));
        CreatedWorldDay = createdWorldDay;
        SourceProcessId = Normalize(sourceProcessId);
        SourceLocationId = Normalize(sourceLocationId);
        DueWorldDay = dueWorldDay;
        Status = status;
        FactionId = Normalize(factionId);
        ResolutionActionTag = Normalize(resolutionActionTag);
        SourceKind = Normalize(sourceKind);
        DeliveryChannel = Normalize(deliveryChannel);
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public int CreatedWorldDay { get; }
    public string? SourceProcessId { get; }
    public string? SourceLocationId { get; }
    public int? DueWorldDay { get; }
    public WorldSituationStatus Status { get; private set; }
    /// <summary>Optional concrete runtime faction affected by this direct request or warning.</summary>
    public string? FactionId { get; }
    /// <summary>Recorded player response tag; it can suppress later authored process effects.</summary>
    public string? ResolutionActionTag { get; private set; }
    public string? SourceKind { get; }
    public string? DeliveryChannel { get; }

    public void Activate() { if (Status == WorldSituationStatus.Dormant) Status = WorldSituationStatus.Active; }
    public void Resolve() { if (Status == WorldSituationStatus.Active || Status == WorldSituationStatus.Dormant || Status == WorldSituationStatus.Promised) Status = WorldSituationStatus.Resolved; }
    public void Resolve(string responseActionTag)
    {
        if (string.IsNullOrWhiteSpace(responseActionTag)) throw new ArgumentException("Response action tag must not be empty.", nameof(responseActionTag));
        Resolve();
        if (Status == WorldSituationStatus.Resolved) ResolutionActionTag = responseActionTag.Trim();
    }
    public void Promise(string responseActionTag)
    {
        if (string.IsNullOrWhiteSpace(responseActionTag)) throw new ArgumentException("Response action tag must not be empty.", nameof(responseActionTag));
        if (Status != WorldSituationStatus.Active) return;
        Status = WorldSituationStatus.Promised;
        ResolutionActionTag = responseActionTag.Trim();
    }
    public void Expire(int worldDay)
    {
        if (DueWorldDay.HasValue && worldDay >= DueWorldDay.Value &&
            (Status == WorldSituationStatus.Active || Status == WorldSituationStatus.Promised)) Status = WorldSituationStatus.Expired;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
}
