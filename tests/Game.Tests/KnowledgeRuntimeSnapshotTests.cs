#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class KnowledgeRuntimeSnapshotTests
{
    public void RunAll()
    {
        RoundTripPreservesFallibleKnowledgeWithoutReadingWorldTruth();
        DeliveredMissionOutcomeRoundTripPreservesHistoricalCondition();
        OlderSnapshotWithoutDeliveredOutcomesMigratesToEmptyHistory();
    }

    private static void RoundTripPreservesFallibleKnowledgeWithoutReadingWorldTruth()
    {
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(new HexCoord(2, 3), KnowledgeLevel.OldOrDoubtful);
        knowledge.ClaimKnowledgeSource("confirmed-hex:2:3");
        knowledge.LearnLocationContextTag("location-1", "structural-failure");
        knowledge.AddEvidence(new EvidenceState("evidence-1", "structural-cuts", EvidenceSourceKind.LocationInspection,
            EvidenceKnowledgeState.Doubtful, "The old supports may have been removed deliberately.", "location-1", "cut-mark", 45));
        knowledge.AddScoutReport(new ScoutReportState("report-1", "mission-1", "Old crossing", "The crossing looked usable.", 65,
            Array.Empty<HexCoord>(), new[] { "east of camp" }, new[]
            {
                new ScoutLeadState(ScoutLeadKind.RouteHint, ScoutLeadScope.Directional, ScoutDirection.East, 65, "A crossing may lie east.", "bridge-mark")
            }));

        var location = new SpecialLocationState("location-1", LocationKind.Ruin, HexCoord.Zero, "Crossing",
            LocationAnchor.Point(HexCoord.Zero), "route-obstacle", "test", interactionStateId: "inspected",
            operationalStateId: "open", presenceStateId: "unknown");
        knowledge.ObserveLocationCondition(location, 4);
        knowledge.MarkLocationConditionDoubtful(location.Id);

        // Objective truth changes after the last observation. It is deliberately not supplied to deserialize.
        location.SetState(LocationStateChannels.Operational, "destroyed");
        var restored = KnowledgeRuntimeSnapshotSerializer.Deserialize(KnowledgeRuntimeSnapshotSerializer.Serialize(knowledge));
        var knownCondition = restored.FindLocationCondition(location.Id)!;

        AssertEqual("open", knownCondition.OperationalStateId, "Save/load retains last-known condition instead of current World Truth");
        AssertTrue(knownCondition.IsDoubtful, "Explicit doubt survives save/load");
        AssertEqual(4, knownCondition.ObservedWorldDay, "Observation day survives save/load");
        AssertEqual(KnowledgeLevel.OldOrDoubtful, restored.GetTileKnowledge(new HexCoord(2, 3)), "Tile reliability survives save/load");
        AssertTrue(restored.HasClaimedKnowledgeSource("confirmed-hex:2:3"), "Claimed source survives save/load");
        AssertTrue(restored.KnowsLocationContextTag(location.Id, "structural-failure"), "Known context survives save/load");
        AssertEqual(EvidenceKnowledgeState.Doubtful, restored.FindEvidence("evidence-1")!.KnowledgeState, "Evidence reliability survives save/load");
        AssertEqual(ScoutDirection.East, restored.ScoutReports.Single().Leads.Single().Direction, "Approximate scout lead survives save/load");
        AssertTrue(!restored.ScoutReports.Single().HasExactCoordinates, "Save/load does not introduce exact scout coordinates");
    }

    private static void DeliveredMissionOutcomeRoundTripPreservesHistoricalCondition()
    {
        var knowledge = new KnowledgeState();
        knowledge.RecordDeliveredMissionOutcome(new DeliveredMissionOutcomeState(
            "mission-7:delivery:12:ReturnedInjured", "mission-7", ScoutMissionStatus.ReturnedInjured,
            expectedReturnWorldDay: 10, deliveredWorldDay: 12, actualReturnWorldDay: 12, wasOverdue: true,
            new[] { new DeliveredMissionParticipantOutcome("scout-7", returned: true, DeliveredScoutStatus.Injured) },
            new[] { "field-kit" }, new[] { "report-7" }));

        var restored = KnowledgeRuntimeSnapshotSerializer.Deserialize(KnowledgeRuntimeSnapshotSerializer.Serialize(knowledge));
        var outcome = restored.DeliveredMissionOutcomes.Single();

        AssertEqual(KnowledgeRuntimeSnapshot.CurrentSchemaVersion, KnowledgeRuntimeSnapshot.Capture(restored).SchemaVersion,
            "New knowledge saves use the incremented schema version");
        AssertTrue(outcome.WasOverdue, "Overdue fact survives save/load");
        AssertEqual(12, outcome.ActualReturnWorldDay, "Actual return day survives save/load");
        AssertEqual(DeliveredScoutStatus.Injured, outcome.ParticipantOutcomes.Single().DeliveredStatus,
            "Historical delivered condition survives save/load");
        AssertEqual("field-kit", outcome.LostEquipmentIds.Single(), "Lost equipment survives save/load");
    }

    private static void OlderSnapshotWithoutDeliveredOutcomesMigratesToEmptyHistory()
    {
        const string oldJson = "{\"Tiles\":[],\"ScoutReports\":[],\"Evidence\":[],\"ClaimedKnowledgeSources\":[],\"LocationContexts\":[],\"LocationConditions\":[]}";

        var restored = KnowledgeRuntimeSnapshotSerializer.Deserialize(oldJson);

        AssertEqual(0, restored.DeliveredMissionOutcomes.Count,
            "Missing outcome section in a pre-versioned save means not recorded and is not reconstructed");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
