using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public readonly struct HexMapBounds
{
    public HexMapBounds(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Map width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Map height must be positive.");
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public bool Contains(HexCoord coord)
    {
        return coord.Q >= 0 && coord.Q < Width && coord.R >= 0 && coord.R < Height;
    }

    public IEnumerable<HexCoord> AllCoords()
    {
        for (var r = 0; r < Height; r++)
        {
            for (var q = 0; q < Width; q++)
            {
                yield return new HexCoord(q, r);
            }
        }
    }
}
}
