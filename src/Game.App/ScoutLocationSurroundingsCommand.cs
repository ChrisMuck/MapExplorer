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
    public SendScoutMissionResult Execute(GameState game, string locationId, IReadOnlyList<string> scoutMemberIds)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var location = game.World.Locations.FirstOrDefault(item => item.Id == locationId);
        if (location == null) return SendScoutMissionResult.Rejected("Location was not found.");
        if (!location.Anchor.Coords.Any(coord => coord == game.Expedition.Position || coord.DistanceTo(game.Expedition.Position) == 1))
        {
            return SendScoutMissionResult.Rejected("The expedition must be at or beside the location.");
        }
        if (scoutMemberIds == null || scoutMemberIds.Count == 0) return SendScoutMissionResult.Rejected("A free scout is required.");
        foreach (var id in scoutMemberIds)
        {
            var member = game.Expedition.FindMember(id);
            if (member == null || member.Role != ExpeditionMemberRole.Scout || member.Status != ExpeditionMemberStatus.Available)
            {
                return SendScoutMissionResult.Rejected("Selected scout is not available.");
            }
        }
        var mission = new ScoutMissionState($"scout-mission-{game.Expedition.ScoutMissions.Count + 1}", scoutMemberIds, game.Expedition.Position,
            ScoutDirection.North, 1, game.World.WorldDay + 1, ScoutMissionFocus.FactionSigns, ScoutMissionBehavior.Cautious, targetLocationId: locationId, missionTypeId: "location-surroundings");
        foreach (var id in scoutMemberIds) game.Expedition.FindMember(id)!.SetStatus(ExpeditionMemberStatus.Assigned);
        game.Expedition.AddScoutMission(mission);
        return SendScoutMissionResult.Sent(mission);
    }
}
}
