#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class HexMapState
{
    private readonly Dictionary<HexCoord, HexTileState> tiles;

    public HexMapState(HexMapBounds bounds, IEnumerable<HexTileState> tiles)
    {
        Bounds = bounds;
        this.tiles = new Dictionary<HexCoord, HexTileState>();

        foreach (var tile in tiles)
        {
            SetTile(tile);
        }
    }

    public HexMapBounds Bounds { get; }

    public int Count
    {
        get { return tiles.Count; }
    }

    public IEnumerable<HexTileState> Tiles
    {
        get { return tiles.Values; }
    }

    public static HexMapState CreateFilled(HexMapBounds bounds, TerrainType terrain)
    {
        var tiles = bounds.AllCoords().Select(coord => new HexTileState(coord, terrain));
        return new HexMapState(bounds, tiles);
    }

    public bool Contains(HexCoord coord)
    {
        return tiles.ContainsKey(coord);
    }

    public HexTileState GetTile(HexCoord coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            throw new KeyNotFoundException($"No tile exists at hex coordinate {coord}.");
        }

        return tile;
    }

    public bool TryGetTile(HexCoord coord, out HexTileState? tile)
    {
        return tiles.TryGetValue(coord, out tile);
    }

    public void SetTile(HexTileState tile)
    {
        if (!Bounds.Contains(tile.Coord))
        {
            throw new ArgumentOutOfRangeException(nameof(tile), tile.Coord, "Tile coordinate is outside map bounds.");
        }

        tiles[tile.Coord] = tile;
    }

    public IReadOnlyList<HexTileState> GetNeighborTiles(HexCoord coord)
    {
        var result = new List<HexTileState>(6);

        foreach (var neighbor in coord.Neighbors())
        {
            if (tiles.TryGetValue(neighbor, out var tile))
            {
                result.Add(tile);
            }
        }

        return result;
    }
}
}
