#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

internal sealed class LocationConditionKnowledgeTests
{
    public void RunAll()
    {
        ObservationIsLastKnownKnowledgeRatherThanLiveWorldTruth();
        DoubtOverridesAgeWithoutDeletingHistory();
    }

    private static void ObservationIsLastKnownKnowledgeRatherThanLiveWorldTruth()
    {
        var knowledge = new KnowledgeState();
        var location = Location();
        knowledge.ObserveLocationCondition(location, 3);
        location.SetState(LocationStateChannels.Operational, "damaged-later");

        var known = knowledge.FindLocationCondition(location.Id)!;
        AssertEqual("open", known.OperationalStateId, "Later WorldState changes do not update player knowledge omnisciently");
        AssertEqual(LocationConditionReliability.Confirmed, known.ReliabilityAt(5, 2), "Observation remains confirmed inside the supplied freshness window");
        AssertEqual(LocationConditionReliability.Old, known.ReliabilityAt(6, 2), "Observation becomes old outside the supplied freshness window");
    }

    private static void DoubtOverridesAgeWithoutDeletingHistory()
    {
        var knowledge = new KnowledgeState();
        var location = Location();
        knowledge.ObserveLocationCondition(location, 3);
        AssertTrue(knowledge.MarkLocationConditionDoubtful(location.Id), "Conflicting evidence can mark a known condition doubtful");
        var known = knowledge.FindLocationCondition(location.Id)!;
        AssertEqual(LocationConditionReliability.Doubtful, known.ReliabilityAt(3, 99), "Doubt is explicit rather than inferred from objective truth");
        AssertEqual("open", known.OperationalStateId, "Doubt preserves the historical observation");
    }

    private static SpecialLocationState Location() => new(
        "location-1", LocationKind.Ruin, HexCoord.Zero, "Known place", LocationAnchor.Point(HexCoord.Zero),
        "route-obstacle", "test", interactionStateId: "inspected", operationalStateId: "open", presenceStateId: "unknown");

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
