#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class SendScoutMissionResult
{
    private SendScoutMissionResult(bool success, ScoutMissionState? mission, string? error)
    {
        Success = success;
        Mission = mission;
        Error = error;
    }

    public bool Success { get; }

    public ScoutMissionState? Mission { get; }

    public string? Error { get; }

    public static SendScoutMissionResult Sent(ScoutMissionState mission)
    {
        return new SendScoutMissionResult(true, mission ?? throw new ArgumentNullException(nameof(mission)), null);
    }

    public static SendScoutMissionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected scout mission needs an error message.", nameof(error));
        }

        return new SendScoutMissionResult(false, null, error);
    }
}
}
