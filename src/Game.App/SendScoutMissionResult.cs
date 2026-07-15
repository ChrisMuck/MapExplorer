#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class SendScoutMissionResult
{
    private SendScoutMissionResult(bool success, ScoutMissionState? mission, ScoutReportState? report, int movementPointCost, string? error)
    {
        Success = success;
        Mission = mission;
        Report = report;
        MovementPointCost = movementPointCost;
        Error = error;
    }

    public bool Success { get; }

    public ScoutMissionState? Mission { get; }

    /// <summary>Present only when a local reconnaissance action was resolved immediately.</summary>
    public ScoutReportState? Report { get; }

    /// <summary>Movement spent by an immediate local reconnaissance action.</summary>
    public int MovementPointCost { get; }

    public string? Error { get; }

    public static SendScoutMissionResult Sent(ScoutMissionState mission)
    {
        return new SendScoutMissionResult(true, mission ?? throw new ArgumentNullException(nameof(mission)), null, 0, null);
    }

    public static SendScoutMissionResult ResolvedLocal(ScoutReportState report, int movementPointCost)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (movementPointCost < 0) throw new ArgumentOutOfRangeException(nameof(movementPointCost));
        return new SendScoutMissionResult(true, null, report, movementPointCost, null);
    }

    public static SendScoutMissionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected scout mission needs an error message.", nameof(error));
        }

        return new SendScoutMissionResult(false, null, null, 0, error);
    }
}
}
