#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>Serialization-safe player knowledge. It contains no objective WorldState references.</summary>
public sealed class KnowledgeRuntimeSnapshot
{
    public const int CurrentSchemaVersion = 2;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<TileKnowledgeRuntimeSnapshot> Tiles { get; set; } = new();
    public List<ScoutReportRuntimeSnapshot> ScoutReports { get; set; } = new();
    public List<DeliveredMissionOutcomeRuntimeSnapshot> DeliveredMissionOutcomes { get; set; } = new();
    public List<EvidenceRuntimeSnapshot> Evidence { get; set; } = new();
    public List<string> ClaimedKnowledgeSources { get; set; } = new();
    public List<LocationContextKnowledgeRuntimeSnapshot> LocationContexts { get; set; } = new();
    public List<LocationConditionKnowledgeRuntimeSnapshot> LocationConditions { get; set; } = new();

    public static KnowledgeRuntimeSnapshot Capture(KnowledgeState knowledge)
    {
        if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
        return new KnowledgeRuntimeSnapshot
        {
            Tiles = knowledge.KnownTiles.OrderBy(pair => pair.Key.Q).ThenBy(pair => pair.Key.R)
                .Select(pair => new TileKnowledgeRuntimeSnapshot { Q = pair.Key.Q, R = pair.Key.R, Level = pair.Value }).ToList(),
            ScoutReports = knowledge.ScoutReports.Select(ScoutReportRuntimeSnapshot.FromState).ToList(),
            DeliveredMissionOutcomes = knowledge.DeliveredMissionOutcomes.Select(DeliveredMissionOutcomeRuntimeSnapshot.FromState).ToList(),
            Evidence = knowledge.Evidence.Select(EvidenceRuntimeSnapshot.FromState).ToList(),
            ClaimedKnowledgeSources = knowledge.ClaimedKnowledgeSources.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            LocationContexts = knowledge.KnownLocationContexts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new LocationContextKnowledgeRuntimeSnapshot { LocationId = pair.Key, ContextTags = pair.Value.OrderBy(tag => tag, StringComparer.Ordinal).ToList() }).ToList(),
            LocationConditions = knowledge.KnownLocationConditions.OrderBy(item => item.LocationId, StringComparer.Ordinal)
                .Select(LocationConditionKnowledgeRuntimeSnapshot.FromState).ToList()
        };
    }

    public KnowledgeState Restore()
    {
        if (SchemaVersion < 0 || SchemaVersion > CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported knowledge snapshot schema version '{SchemaVersion}'.");
        var knowledge = new KnowledgeState();
        var tileCoords = new HashSet<HexCoord>();
        foreach (var tile in Tiles ?? new())
        {
            var coord = new HexCoord(tile.Q, tile.R);
            if (tile.Level == KnowledgeLevel.Unknown) throw new InvalidOperationException($"Saved tile '{coord}' must not use Unknown knowledge.");
            if (!tileCoords.Add(coord)) throw new InvalidOperationException($"Tile '{coord}' occurs more than once in saved knowledge.");
            knowledge.SetTileKnowledge(coord, tile.Level);
        }
        var reportIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var report in ScoutReports ?? new())
        {
            if (!reportIds.Add(report.Id)) throw new InvalidOperationException($"Scout report '{report.Id}' occurs more than once in saved knowledge.");
            knowledge.AddScoutReport(report.ToState());
        }
        foreach (var outcome in DeliveredMissionOutcomes ?? new()) knowledge.RecordDeliveredMissionOutcome(outcome.ToState());
        foreach (var item in Evidence ?? new())
            if (!knowledge.AddEvidence(item.ToState())) throw new InvalidOperationException($"Evidence '{item.Id}' occurs more than once in saved knowledge.");
        foreach (var source in ClaimedKnowledgeSources ?? new())
            if (!knowledge.ClaimKnowledgeSource(source)) throw new InvalidOperationException($"Knowledge source '{source}' occurs more than once in saved knowledge.");
        var contextLocations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var context in LocationContexts ?? new())
        {
            if (!contextLocations.Add(context.LocationId)) throw new InvalidOperationException($"Location context '{context.LocationId}' occurs more than once in saved knowledge.");
            foreach (var tag in context.ContextTags ?? new())
                if (!knowledge.LearnLocationContextTag(context.LocationId, tag)) throw new InvalidOperationException($"Context tag '{tag}' is duplicated for '{context.LocationId}'.");
        }
        foreach (var condition in LocationConditions ?? new()) knowledge.RestoreLocationCondition(condition.ToState());
        return knowledge;
    }
}

public sealed class DeliveredMissionParticipantRuntimeSnapshot
{
    public string MemberId { get; set; } = string.Empty;
    public bool Returned { get; set; }
    public DeliveredScoutStatus DeliveredStatus { get; set; }
    public static DeliveredMissionParticipantRuntimeSnapshot FromState(DeliveredMissionParticipantOutcome state) => new()
    {
        MemberId = state.MemberId, Returned = state.Returned, DeliveredStatus = state.DeliveredStatus
    };
    public DeliveredMissionParticipantOutcome ToState() => new(MemberId, Returned, DeliveredStatus);
}

public sealed class DeliveredMissionOutcomeRuntimeSnapshot
{
    public string DeliveryId { get; set; } = string.Empty;
    public string MissionId { get; set; } = string.Empty;
    public ScoutMissionStatus MissionStatus { get; set; }
    public int ExpectedReturnWorldDay { get; set; }
    public int DeliveredWorldDay { get; set; }
    public int? ActualReturnWorldDay { get; set; }
    public bool WasOverdue { get; set; }
    public List<DeliveredMissionParticipantRuntimeSnapshot> ParticipantOutcomes { get; set; } = new();
    public List<string> LostEquipmentIds { get; set; } = new();
    public List<string> DeliveredReportIds { get; set; } = new();
    public static DeliveredMissionOutcomeRuntimeSnapshot FromState(DeliveredMissionOutcomeState state) => new()
    {
        DeliveryId = state.DeliveryId, MissionId = state.MissionId, MissionStatus = state.MissionStatus,
        ExpectedReturnWorldDay = state.ExpectedReturnWorldDay, DeliveredWorldDay = state.DeliveredWorldDay,
        ActualReturnWorldDay = state.ActualReturnWorldDay, WasOverdue = state.WasOverdue,
        ParticipantOutcomes = state.ParticipantOutcomes.Select(DeliveredMissionParticipantRuntimeSnapshot.FromState).ToList(),
        LostEquipmentIds = state.LostEquipmentIds.ToList(), DeliveredReportIds = state.DeliveredReportIds.ToList()
    };
    public DeliveredMissionOutcomeState ToState() => new(DeliveryId, MissionId, MissionStatus,
        ExpectedReturnWorldDay, DeliveredWorldDay, ActualReturnWorldDay, WasOverdue,
        (ParticipantOutcomes ?? new()).Select(item => item.ToState()), LostEquipmentIds, DeliveredReportIds);
}

public sealed class TileKnowledgeRuntimeSnapshot { public int Q { get; set; } public int R { get; set; } public KnowledgeLevel Level { get; set; } }

public sealed class LocationContextKnowledgeRuntimeSnapshot
{
    public string LocationId { get; set; } = string.Empty;
    public List<string> ContextTags { get; set; } = new();
}

public sealed class LocationConditionKnowledgeRuntimeSnapshot
{
    public string LocationId { get; set; } = string.Empty;
    public string InteractionStateId { get; set; } = string.Empty;
    public string OperationalStateId { get; set; } = string.Empty;
    public string PresenceStateId { get; set; } = string.Empty;
    public int ObservedWorldDay { get; set; }
    public bool IsDoubtful { get; set; }
    public static LocationConditionKnowledgeRuntimeSnapshot FromState(LocationConditionKnowledgeState state) => new()
    {
        LocationId = state.LocationId, InteractionStateId = state.InteractionStateId, OperationalStateId = state.OperationalStateId,
        PresenceStateId = state.PresenceStateId, ObservedWorldDay = state.ObservedWorldDay, IsDoubtful = state.IsDoubtful
    };
    public LocationConditionKnowledgeState ToState() => new(LocationId, InteractionStateId, OperationalStateId, PresenceStateId, ObservedWorldDay, IsDoubtful);
}

public sealed class EvidenceRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public EvidenceSourceKind Source { get; set; }
    public EvidenceKnowledgeState KnowledgeState { get; set; }
    public string PlayerText { get; set; } = string.Empty;
    public string? SubjectLocationId { get; set; }
    public string? SymbolId { get; set; }
    public int Confidence { get; set; }
    public static EvidenceRuntimeSnapshot FromState(EvidenceState state) => new()
    {
        Id = state.Id, DefinitionId = state.DefinitionId, Source = state.Source, KnowledgeState = state.KnowledgeState,
        PlayerText = state.PlayerText, SubjectLocationId = state.SubjectLocationId, SymbolId = state.SymbolId, Confidence = state.Confidence
    };
    public EvidenceState ToState() => new(Id, DefinitionId, Source, KnowledgeState, PlayerText, SubjectLocationId, SymbolId, Confidence);
}

public sealed class ScoutReportRuntimeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string MissionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Reliability { get; set; }
    public List<string> Hints { get; set; } = new();
    public List<ScoutLeadRuntimeSnapshot> Leads { get; set; } = new();
    public static ScoutReportRuntimeSnapshot FromState(ScoutReportState state) => new()
    {
        Id = state.Id, MissionId = state.MissionId, Title = state.Title, Body = state.Body, Reliability = state.Reliability,
        Hints = state.Hints.ToList(), Leads = state.Leads.Select(ScoutLeadRuntimeSnapshot.FromState).ToList()
    };
    public ScoutReportState ToState() => new(Id, MissionId, Title, Body, Reliability, Array.Empty<HexCoord>(), Hints, Leads.Select(lead => lead.ToState()));
}

public sealed class ScoutLeadRuntimeSnapshot
{
    public ScoutLeadKind Kind { get; set; }
    public ScoutLeadScope Scope { get; set; }
    public ScoutDirection Direction { get; set; }
    public int Confidence { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SymbolId { get; set; }
    public string? SourceLocationId { get; set; }
    public static ScoutLeadRuntimeSnapshot FromState(ScoutLeadState state) => new()
    {
        Kind = state.Kind, Scope = state.Scope, Direction = state.Direction, Confidence = state.Confidence,
        Summary = state.Summary, SymbolId = state.SymbolId, SourceLocationId = state.SourceLocationId
    };
    public ScoutLeadState ToState() => new(Kind, Scope, Direction, Confidence, Summary, SymbolId, SourceLocationId);
}
}
