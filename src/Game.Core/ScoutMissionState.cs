#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class ScoutMissionState
{
    private readonly List<string> scoutMemberIds;

    public ScoutMissionState(
        string id,
        IReadOnlyList<string> scoutMemberIds,
        HexCoord origin,
        ScoutDirection direction,
        int durationDays,
        int expectedReturnWorldDay,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior,
        ScoutMissionStatus status = ScoutMissionStatus.Active,
        string? targetLocationId = null)
    {
        Id = RequireText(id, nameof(id));
        if (scoutMemberIds == null || scoutMemberIds.Count == 0)
        {
            throw new ArgumentException("Scout mission needs at least one scout.", nameof(scoutMemberIds));
        }

        if (durationDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(durationDays), durationDays, "Scout mission duration must be at least one day.");
        }

        if (expectedReturnWorldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedReturnWorldDay), expectedReturnWorldDay, "Expected return world day must be at least one.");
        }

        this.scoutMemberIds = scoutMemberIds.Select(idValue => RequireText(idValue, nameof(scoutMemberIds))).Distinct().ToList();
        Origin = origin;
        Direction = direction;
        DurationDays = durationDays;
        ExpectedReturnWorldDay = expectedReturnWorldDay;
        Focus = focus;
        Behavior = behavior;
        TargetLocationId = string.IsNullOrWhiteSpace(targetLocationId) ? null : targetLocationId.Trim();
        Status = status;
    }

    public string Id { get; }

    public IReadOnlyList<string> ScoutMemberIds
    {
        get { return scoutMemberIds; }
    }

    public HexCoord Origin { get; }

    public ScoutDirection Direction { get; }

    public int DurationDays { get; }

    public int ExpectedReturnWorldDay { get; }

    public ScoutMissionFocus Focus { get; }

    public ScoutMissionBehavior Behavior { get; }

    public string? TargetLocationId { get; }

    public ScoutMissionStatus Status { get; private set; }

    public void SetStatus(ScoutMissionStatus status)
    {
        Status = status;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}
}
