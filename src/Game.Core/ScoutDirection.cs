#nullable enable
using System;

namespace Game.Core
{

public enum ScoutDirection
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}

public static class ScoutDirectionExtensions
{
    private static readonly HexCoord[] Offsets =
    {
        new(0, -1),
        new(1, -1),
        new(1, 0),
        new(1, 1),
        new(0, 1),
        new(-1, 1),
        new(-1, 0),
        new(-1, -1)
    };

    public static HexCoord ToScoutOffset(this ScoutDirection direction)
    {
        var index = (int)direction;
        if (index < 0 || index >= Offsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown scout direction.");
        }

        return Offsets[index];
    }
}
}
