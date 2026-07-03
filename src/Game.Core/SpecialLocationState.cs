using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class SpecialLocationState
{
    public SpecialLocationState(string id, LocationKind kind, HexCoord coord, string name)
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        Coord = coord;
        Name = RequireText(name, nameof(name));
    }

    public string Id { get; }

    public LocationKind Kind { get; }

    public HexCoord Coord { get; }

    public string Name { get; }

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
