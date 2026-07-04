#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.App
{

public sealed class EndDayResult
{
    private readonly List<ScoutMissionResolutionResult> scoutResolutions;

    private EndDayResult(
        bool success,
        int worldDay,
        int expeditionDay,
        int suppliesConsumed,
        IEnumerable<ScoutMissionResolutionResult>? scoutResolutions,
        string? error)
    {
        Success = success;
        WorldDay = worldDay;
        ExpeditionDay = expeditionDay;
        SuppliesConsumed = suppliesConsumed;
        this.scoutResolutions = new List<ScoutMissionResolutionResult>(scoutResolutions ?? Enumerable.Empty<ScoutMissionResolutionResult>());
        Error = error;
    }

    public bool Success { get; }

    public int WorldDay { get; }

    public int ExpeditionDay { get; }

    public int SuppliesConsumed { get; }

    public IReadOnlyList<ScoutMissionResolutionResult> ScoutResolutions
    {
        get { return scoutResolutions; }
    }

    public string? Error { get; }

    public static EndDayResult Advanced(
        int worldDay,
        int expeditionDay,
        int suppliesConsumed,
        IEnumerable<ScoutMissionResolutionResult>? scoutResolutions = null)
    {
        return new EndDayResult(true, worldDay, expeditionDay, suppliesConsumed, scoutResolutions, null);
    }

    public static EndDayResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected end-day needs an error message.", nameof(error));
        }

        return new EndDayResult(false, 0, 0, 0, null, error);
    }
}
}
