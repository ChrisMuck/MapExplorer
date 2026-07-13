using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accumulates flat hex geometry (one hex = one 7-vertex fan, not shared with neighbors, matching
/// the map's existing "distinct tile" look) into a single combined mesh. Used per-chunk for both the
/// terrain layer (with baked vertex color for per-tile variety) and the fog-of-war overlay (flat
/// tint, no vertex color needed).
/// </summary>
internal sealed class HexMapChunkGeometryBuilder
{
    private readonly List<Vector3> vertices = new();
    private readonly List<Color> colors = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<int> triangles = new();

    public int Count => vertices.Count;

    public void AddHex(Vector3 center, float radius, float y, Color color)
    {
        var start = vertices.Count;
        vertices.Add(new Vector3(center.x, y, center.z));
        colors.Add(color);
        uvs.Add(new Vector2(center.x, center.z));

        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.Deg2Rad * (60f * i + 30f);
            var x = center.x + Mathf.Cos(angle) * radius;
            var z = center.z + Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, y, z));
            colors.Add(color);
            uvs.Add(new Vector2(x, z));
        }

        for (var i = 0; i < 6; i++)
        {
            triangles.Add(start);
            triangles.Add(start + 1 + (i + 1) % 6);
            triangles.Add(start + 1 + i);
        }
    }

    public Mesh ToMesh(string name)
    {
        var mesh = new Mesh
        {
            name = name,
            hideFlags = HideFlags.DontSave
        };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
