#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>How the expedition acquired an evidence item. Evidence is player knowledge, not World Truth.</summary>
public enum EvidenceSourceKind
{
    LocationInspection,
    ScoutReport,
    FactionContact,
    WorldEvent,
    ArchiveAnalysis
}

/// <summary>Player-facing reliability state for an individual evidence item.</summary>
public enum EvidenceKnowledgeState
{
    Reported,
    Confirmed,
    Old,
    Doubtful,
    Contradicted
}

public sealed class EvidenceState
{
    public EvidenceState(
        string id,
        string definitionId,
        EvidenceSourceKind source,
        EvidenceKnowledgeState knowledgeState,
        string playerText,
        string? subjectLocationId = null,
        string? symbolId = null,
        int confidence = 50)
    {
        if (confidence < 0 || confidence > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Evidence confidence must be between 0 and 100.");
        }

        Id = RequireText(id, nameof(id));
        DefinitionId = RequireText(definitionId, nameof(definitionId));
        Source = source;
        KnowledgeState = knowledgeState;
        PlayerText = RequireText(playerText, nameof(playerText));
        SubjectLocationId = Normalize(subjectLocationId);
        SymbolId = Normalize(symbolId);
        Confidence = confidence;
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public EvidenceSourceKind Source { get; }
    public EvidenceKnowledgeState KnowledgeState { get; private set; }
    public string PlayerText { get; }
    public string? SubjectLocationId { get; }
    public string? SymbolId { get; }
    public int Confidence { get; private set; }

    public void UpdateKnowledge(EvidenceKnowledgeState knowledgeState, int confidence)
    {
        if (confidence < 0 || confidence > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Evidence confidence must be between 0 and 100.");
        }

        KnowledgeState = knowledgeState;
        Confidence = confidence;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

/// <summary>A generated relationship between one concrete location and one faction.</summary>
public enum LocationFactionRelationKind
{
    Claimed,
    Watched,
    Sacred,
    Guarded
}

public sealed class LocationFactionRelationState
{
    private readonly List<string> contextTags;

    public LocationFactionRelationState(
        string factionId,
        LocationFactionRelationKind kind,
        IEnumerable<string>? contextTags = null)
    {
        FactionId = RequireText(factionId, nameof(factionId));
        Kind = kind;
        this.contextTags = (contextTags ?? Enumerable.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public string FactionId { get; }
    public LocationFactionRelationKind Kind { get; }
    public IReadOnlyList<string> ContextTags => contextTags;

    public bool HasContextTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && contextTags.Contains(tag, StringComparer.Ordinal);
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value.Trim();
    }
}

public enum FactionAwarenessLevel
{
    Unaware,
    Suspicious,
    Alert,
    HostileResponse
}

/// <summary>Hidden regional attention caused by observed expedition or scout activity.</summary>
public sealed class RegionFactionAwarenessState
{
    public RegionFactionAwarenessState(string factionId, string regionId, FactionAwarenessLevel level = FactionAwarenessLevel.Unaware)
    {
        FactionId = RequireText(factionId, nameof(factionId));
        RegionId = RequireText(regionId, nameof(regionId));
        Level = level;
    }

    public string FactionId { get; }
    public string RegionId { get; }
    public FactionAwarenessLevel Level { get; private set; }

    public void Escalate()
    {
        if (Level < FactionAwarenessLevel.HostileResponse)
        {
            Level++;
        }
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}

/// <summary>A neutral, hidden runtime message raised by a world-changing action.</summary>
public sealed class WorldTriggerState
{
    private readonly List<string> actionTags;

    public WorldTriggerState(
        string id,
        string triggerId,
        int raisedWorldDay,
        IEnumerable<string>? actionTags = null,
        string? sourceLocationId = null,
        HexCoord? sourceCoord = null,
        string? causedByTraceId = null)
    {
        if (raisedWorldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(raisedWorldDay), raisedWorldDay, "World day must be at least 1.");
        }

        Id = RequireText(id, nameof(id));
        TriggerId = RequireText(triggerId, nameof(triggerId));
        RaisedWorldDay = raisedWorldDay;
        SourceLocationId = Normalize(sourceLocationId);
        SourceCoord = sourceCoord;
        CausedByTraceId = Normalize(causedByTraceId);
        this.actionTags = (actionTags ?? Enumerable.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public string Id { get; }
    public string TriggerId { get; }
    public int RaisedWorldDay { get; }
    public string? SourceLocationId { get; }
    public HexCoord? SourceCoord { get; }
    public string? CausedByTraceId { get; }
    public IReadOnlyList<string> ActionTags => actionTags;
    public bool IsResolved { get; private set; }

    public void MarkResolved()
    {
        IsResolved = true;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

/// <summary>
/// A consequence whose concrete branch was resolved at trigger time and whose stages apply later.
/// The effect IDs are data references; their interpretation belongs to the application-layer scheduler.
/// </summary>
public sealed class ScheduledConsequenceStageState
{
    private readonly List<string> resolvedEffectIds;

    public ScheduledConsequenceStageState(string stageId, int dueWorldDay, IEnumerable<string> resolvedEffectIds)
    {
        if (dueWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(dueWorldDay), "Due world day must be at least 1.");
        StageId = RequireText(stageId, nameof(stageId));
        DueWorldDay = dueWorldDay;
        this.resolvedEffectIds = NormalizeEffects(resolvedEffectIds);
    }

    public string StageId { get; }
    public int DueWorldDay { get; }
    public IReadOnlyList<string> ResolvedEffectIds => resolvedEffectIds;
    public bool IsApplied { get; private set; }
    public void MarkApplied() => IsApplied = true;

    private static List<string> NormalizeEffects(IEnumerable<string> effectIds)
    {
        var result = (effectIds ?? throw new ArgumentNullException(nameof(effectIds)))
            .Where(effectId => !string.IsNullOrWhiteSpace(effectId))
            .Select(effectId => effectId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (result.Count == 0) throw new ArgumentException("A consequence stage needs at least one resolved effect id.", nameof(effectIds));
        return result;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}

/// <summary>
/// One fixed, persistent consequence branch. All future stages belong to this one instance so
/// time advancement never reselects history. The legacy constructor creates one default stage.
/// </summary>
public sealed class ScheduledConsequenceState
{
    private readonly List<string> affectedContextIds;
    private readonly List<ScheduledConsequenceStageState> stages;

    public ScheduledConsequenceState(
        string id,
        string definitionId,
        string sourceLocationId,
        int dueWorldDay,
        IEnumerable<string> resolvedEffectIds)
        : this(
            id,
            definitionId,
            resolvedBranchId: "legacy-default",
            sourceLocationId,
            sourceTriggerId: null,
            affectedContextIds: null,
            stages: new[] { new ScheduledConsequenceStageState("legacy-stage", dueWorldDay, resolvedEffectIds) })
    {
    }

    public ScheduledConsequenceState(
        string id,
        string definitionId,
        string resolvedBranchId,
        string sourceLocationId,
        string? sourceTriggerId,
        IEnumerable<string>? affectedContextIds,
        IEnumerable<ScheduledConsequenceStageState> stages)
    {
        Id = RequireText(id, nameof(id));
        DefinitionId = RequireText(definitionId, nameof(definitionId));
        ResolvedBranchId = RequireText(resolvedBranchId, nameof(resolvedBranchId));
        SourceLocationId = RequireText(sourceLocationId, nameof(sourceLocationId));
        SourceTriggerId = Normalize(sourceTriggerId);
        this.affectedContextIds = (affectedContextIds ?? Enumerable.Empty<string>())
            .Where(contextId => !string.IsNullOrWhiteSpace(contextId))
            .Select(contextId => contextId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        this.stages = (stages ?? throw new ArgumentNullException(nameof(stages))).ToList();
        if (this.stages.Count == 0) throw new ArgumentException("A scheduled consequence needs at least one stage.", nameof(stages));
        if (this.stages.Select(stage => stage.StageId).Distinct(StringComparer.Ordinal).Count() != this.stages.Count)
            throw new ArgumentException("Scheduled consequence stage IDs must be unique.", nameof(stages));
        if (this.stages.Zip(this.stages.Skip(1), (first, second) => first.DueWorldDay <= second.DueWorldDay).Any(isOrdered => !isOrdered))
            throw new ArgumentException("Scheduled consequence stages must be in due-day order.", nameof(stages));
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public string ResolvedBranchId { get; }
    public string SourceLocationId { get; }
    public string? SourceTriggerId { get; }
    public IReadOnlyList<string> AffectedContextIds => affectedContextIds;
    public IReadOnlyList<ScheduledConsequenceStageState> Stages => stages;
    public int CurrentStageIndex { get; private set; }
    public int DueWorldDay => CurrentStage?.DueWorldDay ?? stages[stages.Count - 1].DueWorldDay;
    public IReadOnlyList<string> ResolvedEffectIds => CurrentStage?.ResolvedEffectIds ?? Array.Empty<string>();
    public ScheduledConsequenceStageState? CurrentStage => CurrentStageIndex < stages.Count ? stages[CurrentStageIndex] : null;
    public bool IsApplied => IsCompleted;
    public bool IsCompleted => CurrentStageIndex >= stages.Count;

    public bool IsDue(int worldDay) => CurrentStage != null && !CurrentStage.IsApplied && worldDay >= CurrentStage.DueWorldDay;

    public ScheduledConsequenceStageState? MarkCurrentStageApplied()
    {
        var stage = CurrentStage;
        if (stage == null || stage.IsApplied) return null;
        stage.MarkApplied();
        CurrentStageIndex++;
        return stage;
    }

    /// <summary>World-phase-safe application helper; it cannot advance a stage before its due day.</summary>
    public ScheduledConsequenceStageState? TryApplyDueStage(int worldDay)
    {
        return IsDue(worldDay) ? MarkCurrentStageApplied() : null;
    }

    /// <summary>Compatibility operation for legacy callers: complete every remaining stage.</summary>
    public void MarkApplied()
    {
        while (MarkCurrentStageApplied() != null) { }
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
}
