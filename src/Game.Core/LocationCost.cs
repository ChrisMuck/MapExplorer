#nullable enable
using System;

namespace Game.Core
{

/// <summary>Reusable cost kinds an action may charge (concept Section 17.5). MVP subset.</summary>
public enum LocationCostKind
{
    MovementPoints,
    Supplies,
    Medicine,
    Morale
}

/// <summary>A declarative cost on an action. Presentation-only for the MVP screen (rendered as a summary).</summary>
public sealed class LocationCostDefinition
{
    public LocationCostDefinition(LocationCostKind kind, int amount, string timing = "onCommit")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cost amount must not be negative.");
        }

        Kind = kind;
        Amount = amount;
        Timing = string.IsNullOrWhiteSpace(timing) ? "onCommit" : timing;
    }

    public LocationCostKind Kind { get; }

    public int Amount { get; }

    public string Timing { get; }
}
}
