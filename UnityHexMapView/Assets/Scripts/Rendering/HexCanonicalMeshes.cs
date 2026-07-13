using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small procedural meshes for content that was never prefab-authored (ground scatter blades/stems/
/// blooms/pebbles) or used as a minimal safety-net shape when a decoration category has no assigned
/// prefab. Each shape is built once and shared across every GPU-instanced draw call.
/// </summary>
internal static class HexCanonicalMeshes
{
    public static Mesh Cone(float bottomRadius, float topRadius, float height, int sides = 6)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        var bottomCenter = vertices.Count;
        vertices.Add(Vector3.zero);
        var bottomStart = vertices.Count;
        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(Mathf.Cos(angle) * bottomRadius, 0f, Mathf.Sin(angle) * bottomRadius));
        }

        var topStart = vertices.Count;
        var hasFlatTop = topRadius > 0.0001f;
        if (hasFlatTop)
        {
            for (var i = 0; i < sides; i++)
            {
                var angle = Mathf.PI * 2f * i / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * topRadius, height, Mathf.Sin(angle) * topRadius));
            }
        }
        else
        {
            vertices.Add(new Vector3(0f, height, 0f));
        }

        for (var i = 0; i < sides; i++)
        {
            triangles.Add(bottomCenter);
            triangles.Add(bottomStart + i);
            triangles.Add(bottomStart + (i + 1) % sides);
        }

        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            if (hasFlatTop)
            {
                triangles.Add(bottomStart + i);
                triangles.Add(topStart + i);
                triangles.Add(topStart + next);
                triangles.Add(bottomStart + i);
                triangles.Add(topStart + next);
                triangles.Add(bottomStart + next);
            }
            else
            {
                triangles.Add(bottomStart + i);
                triangles.Add(topStart);
                triangles.Add(bottomStart + next);
            }
        }

        return Build("CanonicalCone", vertices, triangles);
    }

    public static Mesh Box(Vector3 size)
    {
        var h = size * 0.5f;
        var vertices = new List<Vector3>
        {
            new(-h.x, -h.y, -h.z), new(h.x, -h.y, -h.z), new(h.x, h.y, -h.z), new(-h.x, h.y, -h.z),
            new(-h.x, -h.y, h.z), new(h.x, -h.y, h.z), new(h.x, h.y, h.z), new(-h.x, h.y, h.z)
        };
        var triangles = new List<int>
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            3, 7, 6, 3, 6, 2,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5
        };
        return Build("CanonicalBox", vertices, triangles);
    }

    // Flat hex disc, used as the forest-floor cover instanced under tree clusters.
    public static Mesh HexDisc(float radius)
    {
        var vertices = new List<Vector3> { Vector3.zero };
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.Deg2Rad * (60f * i + 30f);
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        for (var i = 0; i < 6; i++)
        {
            triangles.Add(0);
            triangles.Add(1 + (i + 1) % 6);
            triangles.Add(1 + i);
        }

        return Build("CanonicalHexDisc", vertices, triangles);
    }

    private static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
