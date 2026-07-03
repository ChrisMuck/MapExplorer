using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class MovementCostService
{
    public MovementCostResult GetEntryCost(HexTileState tile)
    {
        if (tile.IsBlocked)
        {
            return MovementCostResult.Blocked("Tile is blocked.");
        }

        if (tile.Terrain == TerrainType.Water)
        {
            return MovementCostResult.Blocked("Water cannot be entered by land expedition.");
        }

        var baseCost = GetBaseTerrainCost(tile.Terrain);

        if (tile.HasRoad)
        {
            baseCost = Math.Max(1, baseCost - 1);
        }

        return MovementCostResult.Allowed(baseCost);
    }

    public bool CanEnter(HexTileState tile)
    {
        return GetEntryCost(tile).CanEnter;
    }

    public int GetRequiredEntryCost(HexTileState tile)
    {
        var result = GetEntryCost(tile);
        if (!result.CanEnter)
        {
            throw new InvalidOperationException(result.Reason ?? "Tile cannot be entered.");
        }

        return result.Cost;
    }

    private static int GetBaseTerrainCost(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Coast:
            case TerrainType.Grassland:
            case TerrainType.DryPlains:
            case TerrainType.Desert:
                return 1;

            case TerrainType.Forest:
            case TerrainType.Hills:
                return 2;

            case TerrainType.Mountain:
            case TerrainType.Snow:
            case TerrainType.Swamp:
                return 3;

            case TerrainType.Water:
                return 1;

            default:
                throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unknown terrain type.");
        }
    }
}
}
