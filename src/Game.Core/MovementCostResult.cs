#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public readonly struct MovementCostResult
{
    private MovementCostResult(bool canEnter, int cost, string? reason)
    {
        CanEnter = canEnter;
        Cost = cost;
        Reason = reason;
    }

    public bool CanEnter { get; }

    public int Cost { get; }

    public string? Reason { get; }

    public static MovementCostResult Allowed(int cost)
    {
        if (cost < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "Movement cost must be at least 1.");
        }

        return new MovementCostResult(true, cost, null);
    }

    public static MovementCostResult Blocked(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Blocked movement needs a reason.", nameof(reason));
        }

        return new MovementCostResult(false, 0, reason);
    }
}
}
