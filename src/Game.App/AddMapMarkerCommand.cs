#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class AddMapMarkerCommand
{
    public MapAnnotationResult Execute(GameState game, HexCoord coord, PlayerMapMarkerKind kind, string label, string? factionId = null)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (!game.World.Map.Contains(coord))
        {
            return MapAnnotationResult.Rejected("Marker coordinate is outside the map.");
        }

        if (game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Unknown)
        {
            return MapAnnotationResult.Rejected("Cannot mark completely unknown territory yet.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return MapAnnotationResult.Rejected("Marker label must not be empty.");
        }

        var id = $"marker-{game.PlayerNotes.Markers.Count + 1}";
        game.PlayerNotes.AddMarker(new PlayerMapMarkerState(id, coord, kind, label.Trim(), factionId));
        return MapAnnotationResult.Added(id);
    }
}
}
