#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class SendScoutMissionCommand
{
    private const int MinDurationDays = 1;
    private const int MaxDurationDays = 5;

    public SendScoutMissionResult Execute(
        GameState game,
        IReadOnlyList<string> scoutMemberIds,
        ScoutDirection direction,
        int durationDays,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return SendScoutMissionResult.Rejected("Expedition is not active.");
        }

        if (scoutMemberIds == null || scoutMemberIds.Count == 0)
        {
            return SendScoutMissionResult.Rejected("Select at least one scout.");
        }

        var distinctIds = scoutMemberIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (distinctIds.Count != scoutMemberIds.Count)
        {
            return SendScoutMissionResult.Rejected("Scout selection contains duplicate or empty member ids.");
        }

        if (distinctIds.Count > 2)
        {
            return SendScoutMissionResult.Rejected("A scout mission can use at most two scouts.");
        }

        if (durationDays < MinDurationDays || durationDays > MaxDurationDays)
        {
            return SendScoutMissionResult.Rejected($"Scout mission duration must be between {MinDurationDays} and {MaxDurationDays} days.");
        }

        foreach (var memberId in distinctIds)
        {
            var member = game.Expedition.FindMember(memberId);
            if (member == null)
            {
                return SendScoutMissionResult.Rejected($"Unknown expedition member '{memberId}'.");
            }

            if (member.Role != ExpeditionMemberRole.Scout)
            {
                return SendScoutMissionResult.Rejected($"{member.Name} is not a scout.");
            }

            if (member.Status != ExpeditionMemberStatus.Available)
            {
                return SendScoutMissionResult.Rejected($"{member.Name} is not available.");
            }
        }

        var mission = new ScoutMissionState(
            $"scout-mission-{game.Expedition.ScoutMissions.Count + 1}",
            distinctIds,
            game.Expedition.Position,
            direction,
            durationDays,
            game.World.WorldDay + durationDays,
            focus,
            behavior);

        foreach (var memberId in distinctIds)
        {
            game.Expedition.FindMember(memberId)!.SetStatus(ExpeditionMemberStatus.Assigned);
        }

        game.Expedition.AddScoutMission(mission);
        return SendScoutMissionResult.Sent(mission);
    }
}
}
