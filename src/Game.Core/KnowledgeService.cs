using System;
using System.Collections.Generic;

namespace Game.Core
{

public sealed class KnowledgeService
{
    public void RevealFromExpedition(HexMapState map, KnowledgeState knowledge, HexCoord origin)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (knowledge == null)
        {
            throw new ArgumentNullException(nameof(knowledge));
        }

        if (!map.TryGetTile(origin, out var originTile) || originTile == null)
        {
            return;
        }

        var reportRadius = GetReportRadius(originTile);
        foreach (var coord in CoordsInRange(origin, reportRadius))
        {
            if (!map.Contains(coord) || !HasLineOfSight(map, origin, coord))
            {
                continue;
            }

            var distance = origin.DistanceTo(coord);
            var level = distance <= 1 ? KnowledgeLevel.Confirmed : KnowledgeLevel.Reported;
            knowledge.PromoteTileKnowledge(coord, level);
        }
    }

    public int GetReportRadius(HexTileState originTile)
    {
        if (originTile == null)
        {
            throw new ArgumentNullException(nameof(originTile));
        }

        switch (originTile.Terrain)
        {
            case TerrainType.Hills:
            case TerrainType.Mountain:
            case TerrainType.Snow:
                return 3;

            case TerrainType.Forest:
            case TerrainType.Swamp:
                return 1;

            default:
                return 2;
        }
    }

    private static bool HasLineOfSight(HexMapState map, HexCoord origin, HexCoord target)
    {
        var distance = origin.DistanceTo(target);
        if (distance <= 1)
        {
            return true;
        }

        for (var step = 1; step < distance; step++)
        {
            var coord = HexLerpRound(origin, target, step / (double)distance);
            if (coord == origin || coord == target)
            {
                continue;
            }

            if (map.TryGetTile(coord, out var tile) && tile != null && BlocksSight(tile.Terrain))
            {
                return false;
            }
        }

        return true;
    }

    private static bool BlocksSight(TerrainType terrain)
    {
        return terrain == TerrainType.Mountain || terrain == TerrainType.Snow;
    }

    private static IEnumerable<HexCoord> CoordsInRange(HexCoord origin, int radius)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = Math.Max(-radius, -dq - radius);
            var maxDr = Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                yield return new HexCoord(origin.Q + dq, origin.R + dr);
            }
        }
    }

    private static HexCoord HexLerpRound(HexCoord from, HexCoord to, double t)
    {
        var q = Lerp(from.Q, to.Q, t);
        var r = Lerp(from.R, to.R, t);
        var s = Lerp(from.S, to.S, t);
        return RoundCube(q, r, s);
    }

    private static double Lerp(double from, double to, double t)
    {
        return from + (to - from) * t;
    }

    private static HexCoord RoundCube(double q, double r, double s)
    {
        var rq = Math.Round(q);
        var rr = Math.Round(r);
        var rs = Math.Round(s);

        var qDiff = Math.Abs(rq - q);
        var rDiff = Math.Abs(rr - r);
        var sDiff = Math.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            rq = -rr - rs;
        }
        else if (rDiff > sDiff)
        {
            rr = -rq - rs;
        }

        return new HexCoord((int)rq, (int)rr);
    }
}
}
