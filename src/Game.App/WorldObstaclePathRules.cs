using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Keeps map lines and movement blocking consistent. A route obstacle blocks movement through its
/// edge location and its operational state; a River or Chasm path is only the visible line. These
/// rules make sure every blocking edge has such a line, so a map can never show a blocked crossing
/// without the ravine or river that explains it.
/// </summary>
public static class WorldObstaclePathRules
{
    public const string RouteObstacleArchetypeId = "route-obstacle";

    /// <summary>
    /// An edge is supported when one of its two hexes lies on a River or Chasm path, or when a path
    /// segment runs across the edge between the two hexes that neighbor both.
    /// </summary>
    public static bool PathSupportsEdge(IEnumerable<WorldPathState> paths, HexCoord a, HexCoord b)
    {
        if (paths == null) throw new ArgumentNullException(nameof(paths));
        var shared = CommonNeighbors(a, b);
        foreach (var path in paths)
        {
            if (path.Kind != WorldPathKind.River && path.Kind != WorldPathKind.Chasm)
            {
                continue;
            }

            if (path.Coords.Contains(a) || path.Coords.Contains(b))
            {
                return true;
            }

            if (shared.Count == 2)
            {
                for (var i = 0; i < path.Coords.Count - 1; i++)
                {
                    var from = path.Coords[i];
                    var to = path.Coords[i + 1];
                    if ((from == shared[0] && to == shared[1]) || (from == shared[1] && to == shared[0]))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// The line a ravine takes across an edge: the two hexes that neighbor both sides, so the line
    /// passes through the middle of the edge. Falls back to the edge itself at the map border.
    /// </summary>
    public static IReadOnlyList<HexCoord> CrossingLine(HexCoord a, HexCoord b, HexMapBounds bounds)
    {
        var shared = CommonNeighbors(a, b).Where(bounds.Contains).ToList();
        return shared.Count == 2 ? shared : new[] { a, b };
    }

    /// <summary>Returns one message per blocking edge location that has no River or Chasm line.</summary>
    public static IReadOnlyList<string> Validate(WorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        var issues = new List<string>();
        foreach (var location in world.Locations)
        {
            if (!IsRouteObstacleEdge(location))
            {
                continue;
            }

            var a = location.Anchor.Coords[0];
            var b = location.Anchor.Coords[1];
            if (!PathSupportsEdge(world.Paths, a, b))
            {
                issues.Add($"Route obstacle '{location.Id}' at edge ({a.Q},{a.R})-({b.Q},{b.R}) has no River or Chasm path.");
            }
        }

        return issues;
    }

    /// <summary>Chasm paths that no route obstacle edge uses. Informational only.</summary>
    public static IReadOnlyList<string> FindUnusedChasms(WorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        var edges = world.Locations
            .Where(IsRouteObstacleEdge)
            .Select(location => (A: location.Anchor.Coords[0], B: location.Anchor.Coords[1]))
            .ToList();
        return world.Paths
            .Where(path => path.Kind == WorldPathKind.Chasm)
            .Where(path => !edges.Any(edge => PathSupportsEdge(new[] { path }, edge.A, edge.B)))
            .Select(path => path.Id)
            .ToList();
    }

    public static bool IsRouteObstacleEdge(SpecialLocationState location)
    {
        return location.ArchetypeId == RouteObstacleArchetypeId
            && location.Anchor.Kind == LocationAnchorKind.Edge
            && location.Anchor.Coords.Count == 2;
    }

    private static List<HexCoord> CommonNeighbors(HexCoord a, HexCoord b)
    {
        var neighborsOfB = b.Neighbors();
        return a.Neighbors().Where(neighborsOfB.Contains).ToList();
    }
}

}
