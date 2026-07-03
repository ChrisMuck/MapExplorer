using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public static class HexDirectionExtensions
{
    private static readonly HexCoord[] Offsets =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    };

    public static HexCoord ToOffset(this HexDirection direction)
    {
        var index = (int)direction;
        if (index < 0 || index >= Offsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
        }

        return Offsets[index];
    }

    public static HexDirection Opposite(this HexDirection direction)
    {
        return (HexDirection)(((int)direction + 3) % 6);
    }
}
}
