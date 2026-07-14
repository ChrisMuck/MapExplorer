#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Determines what a concrete faction can plausibly notice. It reads only generated territory,
/// generated location relations and physical map distance; it produces hidden awareness and a
/// developer trace, never a player-facing territorial reveal.
/// </summary>
public sealed class FactionObservationResolver
{
    private readonly CrossSystemDataBundle? content;

    public FactionObservationResolver(CrossSystemDataBundle? content)
    {
        this.content = content;
    }

    public int Resolve(GameState game, WorldTriggerState trigger)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (content == null) return 0;

        var location = trigger.SourceLocationId == null ? null : game.World.Locations.FirstOrDefault(item => item.Id == trigger.SourceLocationId);
        var coord = trigger.SourceCoord ?? location?.Coord;
        if (!coord.HasValue) return 0;

        var relatedFactionIds = new HashSet<string>(location?.FactionRelations.Select(relation => relation.FactionId) ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        var observed = 0;
        foreach (var faction in game.Factions)
        {
            var profile = content.FactionProfiles.Find(faction.ReactionProfileId);
            if (profile == null || !CanObserve(game, faction, profile, location, coord.Value, relatedFactionIds.Contains(faction.Id))) continue;

            var regionId = location == null ? $"coord-region:{coord.Value.Q}:{coord.Value.R}" : $"location-region:{location.Id}";
            game.World.EscalateFactionAwareness(faction.Id, regionId);
            game.World.RecordTrace(
                SimulationTraceKind.FactionObservation,
                $"Faction '{faction.Id}' observed trigger '{trigger.TriggerId}' through generated spatial context.",
                trigger.CausedByTraceId == null ? null : new[] { trigger.CausedByTraceId },
                new[] { faction.Id, trigger.Id, regionId });
            observed++;
        }

        return observed;
    }

    private static bool CanObserve(GameState game, FactionState faction, FactionProfileDefinition profile, SpecialLocationState? location, HexCoord coord, bool hasGeneratedRelation)
    {
        // Watched, guarded and sacred sites have a generated observation channel. A plain claim
        // deliberately does not make the faction omniscient.
        if (hasGeneratedRelation && location != null && profile.ObservationChannels.Contains("watch", StringComparer.Ordinal) &&
            location.FactionRelations.Any(relation => relation.FactionId == faction.Id &&
                (relation.Kind == LocationFactionRelationKind.Watched || relation.Kind == LocationFactionRelationKind.Guarded || relation.Kind == LocationFactionRelationKind.Sacred)))
        {
            return true;
        }

        var isInObservationRange = game.World.Map.Tiles.Any(tile => tile.OwnerId == faction.Id && tile.Coord.DistanceTo(coord) <= profile.ObservationRange);
        if (profile.ObservationChannels.Contains("territory", StringComparer.Ordinal) && isInObservationRange) return true;

        // Roads and crossings provide a second, still bounded channel: a faction must both have
        // observers in range and be able to watch the generated route itself.
        return profile.ObservationChannels.Contains("route", StringComparer.Ordinal) && isInObservationRange &&
               game.World.Map.TryGetTile(coord, out var sourceTile) && sourceTile != null && sourceTile.HasRoad;
    }
}
}
