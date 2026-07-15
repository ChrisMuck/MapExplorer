#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class FailExpeditionCommand
{
    public const int LostRecoveryDays = 21;

    private readonly KnowledgeService knowledgeService = new KnowledgeService();

    public FailExpeditionResult Execute(GameState game, string reason, int recoveryDays = LostRecoveryDays)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason must not be empty.", nameof(reason));
        }

        if (recoveryDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recoveryDays), recoveryDays, "Recovery days must not be negative.");
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return FailExpeditionResult.Rejected("Expedition is not active.");
        }

        foreach (var member in game.Expedition.Members)
        {
            if (member.Status != ExpeditionMemberStatus.Dead)
            {
                member.SetStatus(ExpeditionMemberStatus.Missing);
            }
        }

        var lostUnsecuredKnowledge = game.Expedition.ClearUnsecuredKnowledge();
        game.Expedition.ClearFieldFindings();

        game.Expedition.SetStatus(ExpeditionStatus.Lost);
        game.Base.ScheduleNextExpedition(ExpeditionStatus.Lost, game.World.WorldDay, recoveryDays);
        var lostRecord = CreateLostExpeditionRecord(game, lostUnsecuredKnowledge);
        game.Base.AddLostExpeditionRecord(lostRecord);
        game.Base.DiscardCurrentExpeditionArchiveEntries();
        ClearExpeditionKnowledge(game);
        AddRecoveryLead(game, lostRecord);
        ApplyLimitedWorldReaction(game);

        var archiveEntry = $"Expedition {game.Expedition.ExpeditionNumber} was lost on world day {game.World.WorldDay}. No reports, notes or location details returned. Unsecured knowledge lost: {lostUnsecuredKnowledge}. Reason: {reason.Trim()}";
        game.Base.AddArchiveEntry(archiveEntry);
        game.Base.SecureCurrentExpeditionArchiveEntries();

        return FailExpeditionResult.Failed(
            game.Expedition.ExpeditionNumber,
            game.Base.NextExpeditionAvailableWorldDay,
            lostUnsecuredKnowledge,
            archiveEntry);
    }

    private static LostExpeditionRecord CreateLostExpeditionRecord(GameState game, int lostUnsecuredKnowledge)
    {
        var position = game.Expedition.Position;
        return new LostExpeditionRecord(
            $"expedition-{game.Expedition.ExpeditionNumber}",
            game.Expedition.ExpeditionNumber,
            game.World.WorldDay,
            position,
            lostUnsecuredKnowledge,
            LostExpeditionStatus.Missing,
            new[] { $"last-known-position:{position.Q}:{position.R}" });
    }

    private void ClearExpeditionKnowledge(GameState game)
    {
        game.Knowledge.Clear();
        game.PlayerNotes.Clear();
        game.Events.Clear();

        foreach (var location in game.World.Locations)
        {
            location.ForgetDiscovery();
        }

        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, game.Base.Location);
    }

    private static void AddRecoveryLead(GameState game, LostExpeditionRecord lostRecord)
    {
        if (lostRecord.LastKnownPosition != game.Base.Location)
        {
            game.Knowledge.SetTileKnowledge(lostRecord.LastKnownPosition, KnowledgeLevel.OldOrDoubtful);
        }

        game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
            $"lost-expedition-marker-{lostRecord.ExpeditionNumber}",
            lostRecord.LastKnownPosition,
            PlayerMapMarkerKind.Question,
            $"Last known position of Expedition {lostRecord.ExpeditionNumber}"));

        game.PlayerNotes.AddNote(new PlayerMapNoteState(
            $"lost-expedition-note-{lostRecord.ExpeditionNumber}",
            lostRecord.LastKnownPosition,
            $"Recovery lead: Expedition {lostRecord.ExpeditionNumber} disappeared here with about {lostRecord.EstimatedLostKnowledge} unsecured knowledge."));
    }

    private static void ApplyLimitedWorldReaction(GameState game)
    {
        foreach (var faction in game.Factions)
        {
            if (faction.Id == "coastal-people")
            {
                faction.Adjust(fearDelta: 3);
                faction.AddMemory($"expedition-lost-near-base:{game.World.WorldDay}");
                continue;
            }

            faction.Adjust(angerDelta: 2, fearDelta: 4);
            faction.AddMemory($"expedition-lost-world-shift:{game.World.WorldDay}");
            ExpandTerritory(game.World.Map, faction.Id, game.Base.Location, maxClaims: 3);
        }
    }

    private static void ExpandTerritory(HexMapState map, string ownerId, HexCoord baseLocation, int maxClaims)
    {
        var claims = new List<HexCoord>();
        foreach (var tile in map.Tiles.OrderBy(tile => tile.Coord.Q).ThenBy(tile => tile.Coord.R))
        {
            if (tile.OwnerId != ownerId)
            {
                continue;
            }

            foreach (var neighbor in tile.Coord.Neighbors())
            {
                if (claims.Count >= maxClaims)
                {
                    return;
                }

                if (!map.TryGetTile(neighbor, out var neighborTile) || neighborTile == null)
                {
                    continue;
                }

                if (neighbor == baseLocation || neighborTile.Terrain == TerrainType.Water || !string.IsNullOrWhiteSpace(neighborTile.OwnerId))
                {
                    continue;
                }

                if (Hash(neighbor.Q, neighbor.R, ownerId.Length) % 3 != 0)
                {
                    continue;
                }

                claims.Add(neighbor);
                map.SetTile(neighborTile.WithOwner(ownerId));
            }
        }
    }

    private static int Hash(int q, int r, int seed)
    {
        unchecked
        {
            var value = q * 73856093 ^ r * 19349663 ^ seed * 83492791;
            return Math.Abs(value);
        }
    }
}
}
