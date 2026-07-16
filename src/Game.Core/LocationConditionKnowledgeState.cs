#nullable enable
using System;

namespace Game.Core
{

public enum LocationConditionReliability
{
    Confirmed,
    Old,
    Doubtful
}

/// <summary>A last-known location condition. It is player knowledge, never live World Truth.</summary>
public sealed class LocationConditionKnowledgeState
{
    public LocationConditionKnowledgeState(string locationId, string interactionStateId, string operationalStateId,
        string presenceStateId, int observedWorldDay, bool isDoubtful = false)
    {
        LocationId = Require(locationId, nameof(locationId));
        InteractionStateId = Require(interactionStateId, nameof(interactionStateId));
        OperationalStateId = Require(operationalStateId, nameof(operationalStateId));
        PresenceStateId = Require(presenceStateId, nameof(presenceStateId));
        ObservedWorldDay = observedWorldDay >= 1 ? observedWorldDay : throw new ArgumentOutOfRangeException(nameof(observedWorldDay));
        IsDoubtful = isDoubtful;
    }

    public string LocationId { get; }
    public string InteractionStateId { get; }
    public string OperationalStateId { get; }
    public string PresenceStateId { get; }
    public int ObservedWorldDay { get; }
    public bool IsDoubtful { get; private set; }

    public LocationConditionReliability ReliabilityAt(int worldDay, int maximumConfirmedAgeDays)
    {
        if (worldDay < ObservedWorldDay) throw new ArgumentOutOfRangeException(nameof(worldDay));
        if (maximumConfirmedAgeDays < 0) throw new ArgumentOutOfRangeException(nameof(maximumConfirmedAgeDays));
        if (IsDoubtful) return LocationConditionReliability.Doubtful;
        return worldDay - ObservedWorldDay > maximumConfirmedAgeDays
            ? LocationConditionReliability.Old
            : LocationConditionReliability.Confirmed;
    }

    public void MarkDoubtful() => IsDoubtful = true;

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
}
}
