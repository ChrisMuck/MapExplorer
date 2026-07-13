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
    private readonly ScoutContentDefinitionSet? scoutContent;

    public SendScoutMissionCommand(ScoutContentDefinitionSet? scoutContent = null)
    {
        this.scoutContent = scoutContent;
    }

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

        var missionDefinition = FindMissionDefinition("directional-recon");
        if (missionDefinition == null && scoutContent?.HasMissionTypeDefinitions == true)
        {
            return SendScoutMissionResult.Rejected("Directional reconnaissance is not configured.");
        }

        var maxScouts = missionDefinition?.MaxScouts ?? 2;
        if (distinctIds.Count > maxScouts)
        {
            return SendScoutMissionResult.Rejected($"This scout mission can use at most {maxScouts} scouts.");
        }

        var minDuration = missionDefinition?.MinDurationDays ?? MinDurationDays;
        var maxDuration = missionDefinition?.MaxDurationDays ?? MaxDurationDays;
        if (durationDays < minDuration || durationDays > maxDuration)
        {
            return SendScoutMissionResult.Rejected($"Scout mission duration must be between {minDuration} and {maxDuration} days.");
        }

        if (missionDefinition != null && !missionDefinition.Allows(focus))
        {
            return SendScoutMissionResult.Rejected("This focus is not available for directional reconnaissance.");
        }

        var focusDefinition = scoutContent?.FindFocus(focus);
        if (focusDefinition != null && !focusDefinition.Allows("directional-recon"))
        {
            return SendScoutMissionResult.Rejected("This focus is not compatible with directional reconnaissance.");
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
            behavior,
            missionTypeId: "directional-recon");

        foreach (var memberId in distinctIds)
        {
            game.Expedition.FindMember(memberId)!.SetStatus(ExpeditionMemberStatus.Assigned);
        }

        game.Expedition.AddScoutMission(mission);
        return SendScoutMissionResult.Sent(mission);
    }

    private ScoutMissionTypeDefinition? FindMissionDefinition(string missionTypeId)
    {
        return scoutContent?.FindMissionType(missionTypeId);
    }
}
}
