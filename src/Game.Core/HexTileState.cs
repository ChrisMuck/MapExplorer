using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class HexTileState
{
    public HexTileState(
        HexCoord coord,
        TerrainType terrain,
        int elevation = 0,
        bool isBlocked = false,
        string? roadId = null,
        string? riverId = null,
        string? locationId = null,
        string? ownerId = null)
    {
        Coord = coord;
        Terrain = terrain;
        Elevation = elevation;
        IsBlocked = isBlocked;
        RoadId = roadId;
        RiverId = riverId;
        LocationId = locationId;
        OwnerId = ownerId;
    }

    public HexCoord Coord { get; }

    public TerrainType Terrain { get; }

    public int Elevation { get; }

    public bool IsBlocked { get; }

    public string? RoadId { get; }

    public string? RiverId { get; }

    public string? LocationId { get; }

    public string? OwnerId { get; }

    public bool HasRoad
    {
        get { return !string.IsNullOrWhiteSpace(RoadId); }
    }

    public bool HasRiver
    {
        get { return !string.IsNullOrWhiteSpace(RiverId); }
    }

    public bool HasLocation
    {
        get { return !string.IsNullOrWhiteSpace(LocationId); }
    }

    public HexTileState WithTerrain(TerrainType terrain, int? elevation = null, bool? isBlocked = null)
    {
        return new HexTileState(
            Coord,
            terrain,
            elevation ?? Elevation,
            isBlocked ?? IsBlocked,
            RoadId,
            RiverId,
            LocationId,
            OwnerId);
    }

    public HexTileState WithRoad(string? roadId)
    {
        return new HexTileState(Coord, Terrain, Elevation, IsBlocked, roadId, RiverId, LocationId, OwnerId);
    }

    public HexTileState WithRiver(string? riverId)
    {
        return new HexTileState(Coord, Terrain, Elevation, IsBlocked, RoadId, riverId, LocationId, OwnerId);
    }

    public HexTileState WithLocation(string? locationId)
    {
        return new HexTileState(Coord, Terrain, Elevation, IsBlocked, RoadId, RiverId, locationId, OwnerId);
    }

    public HexTileState WithOwner(string? ownerId)
    {
        return new HexTileState(Coord, Terrain, Elevation, IsBlocked, RoadId, RiverId, LocationId, ownerId);
    }
}
}
