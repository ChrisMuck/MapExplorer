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

    public bool IsDiscovered { get; private set; }

    public bool IsInspected { get; private set; }

    public int? DiscoveredWorldDay { get; private set; }

    public int? InspectedWorldDay { get; private set; }

    public void Discover(int worldDay)
    {
        if (worldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must be at least 1.");
        }

        if (IsDiscovered)
        {
            return;
        }

        IsDiscovered = true;
        DiscoveredWorldDay = worldDay;
    }

    public void Inspect(int worldDay)
    {
        Discover(worldDay);
        if (IsInspected)
        {
            return;
        }

        IsInspected = true;
        InspectedWorldDay = worldDay;
    }

    public void ForgetDiscovery()
    {
        if (Kind == LocationKind.BaseCamp)
        {
            return;
        }

        IsDiscovered = false;
        IsInspected = false;
        DiscoveredWorldDay = null;
        InspectedWorldDay = null;
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
