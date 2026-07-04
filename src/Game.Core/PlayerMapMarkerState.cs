#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class PlayerMapMarkerState
{
    public PlayerMapMarkerState(string id, HexCoord coord, PlayerMapMarkerKind kind, string label, string? factionId = null)
    {
        Id = RequireText(id, nameof(id));
        Coord = coord;
        Kind = kind;
        Label = RequireText(label, nameof(label));
        FactionId = string.IsNullOrWhiteSpace(factionId) ? null : factionId;
    }

    public string Id { get; }

    public HexCoord Coord { get; }

    public PlayerMapMarkerKind Kind { get; }

    public string Label { get; }

    public string? FactionId { get; }

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
