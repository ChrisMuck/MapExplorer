using UnityEngine;

/// <summary>
/// Base color per terrain kind, baked into terrain vertex colors by <see cref="HexMapChunkRenderer"/>.
/// Single source of truth for the map's terrain palette (previously duplicated per-terrain materials).
/// </summary>
public static class HexTerrainPalette
{
    public static Color BaseColor(HexTerrainKind terrain)
    {
        return terrain switch
        {
            HexTerrainKind.Water => FromHex("3f7180"),
            HexTerrainKind.Coast => FromHex("b49463"),
            HexTerrainKind.Grass => FromHex("70864d"),
            HexTerrainKind.Forest => FromHex("355741"),
            HexTerrainKind.Hills => FromHex("827a52"),
            HexTerrainKind.Mountain => FromHex("777972"),
            HexTerrainKind.Snow => FromHex("c9cec6"),
            _ => Color.magenta
        };
    }

    // Roughly the inverse of the old per-terrain "_Smoothness" values baked into materials.
    public static float Smoothness(HexTerrainKind terrain)
    {
        return terrain switch
        {
            HexTerrainKind.Water => 0.82f,
            HexTerrainKind.Coast => 0.82f,
            HexTerrainKind.Grass => 0.66f,
            HexTerrainKind.Forest => 0.78f,
            HexTerrainKind.Hills => 0.76f,
            HexTerrainKind.Mountain => 0.86f,
            HexTerrainKind.Snow => 0.9f,
            _ => 0.7f
        };
    }

    private static Color FromHex(string hex)
    {
        return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.magenta;
    }
}
