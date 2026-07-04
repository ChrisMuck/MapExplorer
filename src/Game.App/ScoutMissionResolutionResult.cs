#nullable enable
using Game.Core;

namespace Game.App
{

public sealed class ScoutMissionResolutionResult
{
    public ScoutMissionResolutionResult(string missionId, ScoutMissionStatus status, ScoutReportState? report)
    {
        MissionId = missionId;
        Status = status;
        Report = report;
    }

    public string MissionId { get; }

    public ScoutMissionStatus Status { get; }

    public ScoutReportState? Report { get; }
}
}
