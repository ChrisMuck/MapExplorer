using UnityEngine;

/// <summary>Presentation-layer terrain classification for a hex, collapsed from Game.Core's richer <c>TerrainType</c>.</summary>
public enum HexTerrainKind
{
    Water,
    Coast,
    Grass,
    Forest,
    Hills,
    Mountain,
    Snow
}

/// <summary>Forest stand classification driven by neighbor-aware region analysis.</summary>
public enum HexForestType
{
    Coniferous,
    Deciduous,
    Mixed
}

/// <summary>Individual tree species used to pick prefab/procedural tree geometry.</summary>
public enum HexTreeKind
{
    Pine,
    Broadleaf
}

/// <summary>Resolved per-hex presentation data: terrain kind, visual elevation and world position.</summary>
public struct HexTileVisual
{
    public HexTerrainKind Terrain;
    public float Elevation;
    public Vector3 World;
}
