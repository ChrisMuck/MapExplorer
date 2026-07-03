using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class WorldPathState
{
    private readonly List<HexCoord> coords;

    public WorldPathState(string id, WorldPathKind kind, IEnumerable<HexCoord> coords)
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        this.coords = new List<HexCoord>(coords ?? throw new ArgumentNullException(nameof(coords)));

        if (this.coords.Count < 2)
        {
            throw new ArgumentException("A world path needs at least two coordinates.", nameof(coords));
        }
    }

    public string Id { get; }

    public WorldPathKind Kind { get; }

    public IReadOnlyList<HexCoord> Coords
    {
        get { return coords; }
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
