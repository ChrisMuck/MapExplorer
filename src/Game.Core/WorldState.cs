#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class WorldState
{
    private readonly List<WorldPathState> paths;
    private readonly List<SpecialLocationState> locations;

    public WorldState(
        HexMapState map,
        IEnumerable<WorldPathState>? paths = null,
        IEnumerable<SpecialLocationState>? locations = null,
        int worldDay = 1)
    {
        if (worldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must be at least 1.");
        }

        Map = map ?? throw new ArgumentNullException(nameof(map));
        this.paths = new List<WorldPathState>(paths ?? Enumerable.Empty<WorldPathState>());
        this.locations = new List<SpecialLocationState>(locations ?? Enumerable.Empty<SpecialLocationState>());
        WorldDay = worldDay;
    }

    public HexMapState Map { get; }

    public IReadOnlyList<WorldPathState> Paths
    {
        get { return paths; }
    }

    public IReadOnlyList<SpecialLocationState> Locations
    {
        get { return locations; }
    }

    public int WorldDay { get; private set; }

    public void AddPath(WorldPathState path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (paths.Any(existing => existing.Id == path.Id))
        {
            return;
        }

        paths.Add(path);
    }

    public void AdvanceDays(int days)
    {
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Advance must be at least one day.");
        }

        WorldDay += days;
    }
}
}
