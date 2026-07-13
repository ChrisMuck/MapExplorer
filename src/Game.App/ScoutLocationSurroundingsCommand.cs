#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Dispatches a free scout to investigate the social and spatial context around one location.</summary>
public sealed class ScoutLocationSurroundingsCommand
{
    private readonly ScoutContentDefinitionSet? scoutContent;

    public ScoutLocationSurroundingsCommand(ScoutContentDefinitionSet? scoutContent = null)
    {
        this.scoutContent = scoutContent;
    }

    public SendScoutMissionResult Execute(GameState game, string locationId, IReadOnlyList<string> scoutMemberIds)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (game.Expedition.Status != ExpeditionStatus.Active) return SendScoutMissionResult.Rejected("Expedition is not active.");
        var location = game.World.Locations.FirstOrDefault(item => item.Id == locationId);
        if (location == null) return SendScoutMissionResult.Rejected("Location was not found.");
        if (!location.Anchor.Coords.Any(coord => coord == game.Expedition.Position || coord.DistanceTo(game.Expedition.Position) == 1))
        {
            return SendScoutMissionResult.Rejected("The expedition must be at or beside the location.");
        }
        if (scoutMemberIds == null || scoutMemberIds.Count == 0) return SendScoutMissionResult.Rejected("A free scout is required.");
        var distinctIds = scoutMemberIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (distinctIds.Count != scoutMemberIds.Count) return SendScoutMissionResult.Rejected("Scout selection contains duplicate or empty member ids.");

        var missionDefinition = scoutContent?.FindMissionType("location-surroundings");
        if (missionDefinition == null && scoutContent?.HasMissionTypeDefinitions == true) return SendScoutMissionResult.Rejected("Location surroundings reconnaissance is not configured.");
        if (missionDefinition != null && distinctIds.Count > missionDefinition.MaxScouts) return SendScoutMissionResult.Rejected($"This scout mission can use at most {missionDefinition.MaxScouts} scouts.");
        if (missionDefinition != null && !missionDefinition.Allows(ScoutMissionFocus.FactionSigns)) return SendScoutMissionResult.Rejected("Faction-sign focus is not available for location surroundings reconnaissance.");
        var focusDefinition = scoutContent?.FindFocus(ScoutMissionFocus.FactionSigns);
        if (focusDefinition != null && !focusDefinition.Allows("location-surroundings")) return SendScoutMissionResult.Rejected("Faction-sign focus is not compatible with location surroundings reconnaissance.");

        foreach (var id in distinctIds)
        {
            var member = game.Expedition.FindMember(id);
            if (member == null || member.Role != ExpeditionMemberRole.Scout || member.Status != ExpeditionMemberStatus.Available)
            {
                return SendScoutMissionResult.Rejected("Selected scout is not available.");
            }
        }
        var durationDays = missionDefinition?.MinDurationDays ?? 1;
        var mission = new ScoutMissionState($"scout-mission-{game.Expedition.ScoutMissions.Count + 1}", distinctIds, game.Expedition.Position,
            ScoutDirection.North, durationDays, game.World.WorldDay + durationDays, ScoutMissionFocus.FactionSigns, ScoutMissionBehavior.Cautious, targetLocationId: locationId, missionTypeId: "location-surroundings");
        foreach (var id in distinctIds) game.Expedition.FindMember(id)!.SetStatus(ExpeditionMemberStatus.Assigned);
        game.Expedition.AddScoutMission(mission);
        return SendScoutMissionResult.Sent(mission);
    }
}
}
