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
    Expired
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
        WorldSituationStatus status = WorldSituationStatus.Dormant)
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
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public int CreatedWorldDay { get; }
    public string? SourceProcessId { get; }
    public string? SourceLocationId { get; }
    public int? DueWorldDay { get; }
    public WorldSituationStatus Status { get; private set; }

    public void Activate() { if (Status == WorldSituationStatus.Dormant) Status = WorldSituationStatus.Active; }
    public void Resolve() { if (Status == WorldSituationStatus.Active || Status == WorldSituationStatus.Dormant) Status = WorldSituationStatus.Resolved; }
    public void Expire(int worldDay)
    {
        if (DueWorldDay.HasValue && worldDay >= DueWorldDay.Value && Status == WorldSituationStatus.Active) Status = WorldSituationStatus.Expired;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
}
