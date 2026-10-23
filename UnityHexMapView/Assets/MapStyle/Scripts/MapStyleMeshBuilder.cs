using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Builds flat-shaded low-poly meshes from a few primitives. Every triangle owns its vertices,
    /// so RecalculateNormals produces hard facets and the vertex color carries the whole look.
    /// Presentation only; contains no game rules.
    /// </summary>
    public sealed class MapStyleMeshBuilder
    {
        private static readonly int[][] IcoFaces =
        {
            new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 },
            new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
            new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 },
            new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 }
        };

        private static Vector3[] icoVertices;

        private readonly List<Vector3> vertices = new();
        private readonly List<Color32> colors = new();
        private readonly List<int> triangles = new();

        public int TriangleCount => triangles.Count / 3;

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            var index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            Color32 color32 = color;
            colors.Add(color32);
            colors.Add(color32);
            colors.Add(color32);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        /// <summary>Adds a triangle whose front side faces away from <paramref name="inside"/>.</summary>
        public void AddTriangleOutward(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Color color)
        {
            var normal = Vector3.Cross(b - a, c - a);
            var centroid = (a + b + c) / 3f;
            if (Vector3.Dot(normal, centroid - inside) < 0f)
            {
                AddTriangle(a, c, b, color);
            }
            else
            {
                AddTriangle(a, b, c, color);
            }
        }

        public void AddTriangleUp(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.y < 0f)
            {
                AddTriangle(a, c, b, color);
            }
            else
            {
                AddTriangle(a, b, c, color);
            }
        }

        public void AddBox(Vector3 center, Vector3 size, Vector3 euler, Color color)
        {
            var m = Matrix4x4.TRS(center, Quaternion.Euler(euler), size);
            var inside = m.MultiplyPoint3x4(Vector3.zero);
            var p = new Vector3[8];
            for (var i = 0; i < 8; i++)
            {
                var local = new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f);
                p[i] = m.MultiplyPoint3x4(local);
            }

            AddQuad(p[0], p[1], p[3], p[2], inside, color);
            AddQuad(p[4], p[5], p[7], p[6], inside, color);
            AddQuad(p[0], p[1], p[5], p[4], inside, color);
            AddQuad(p[2], p[3], p[7], p[6], inside, color);
            AddQuad(p[0], p[2], p[6], p[4], inside, color);
            AddQuad(p[1], p[3], p[7], p[5], inside, color);
        }

        /// <summary>Prism or cone. Unit height 1, base radius 0.5; topScale 0 gives a cone.</summary>
        public void AddPrism(Vector3 center, Vector3 size, Vector3 euler, int sides, float topScale, Color color)
        {
            sides = Mathf.Max(3, sides);
            var m = Matrix4x4.TRS(center, Quaternion.Euler(euler), size);
            var inside = m.MultiplyPoint3x4(Vector3.zero);
            var bottom = new Vector3[sides];
            var top = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = Mathf.PI * 2f * i / sides;
                var cos = Mathf.Cos(angle) * 0.5f;
                var sin = Mathf.Sin(angle) * 0.5f;
                bottom[i] = m.MultiplyPoint3x4(new Vector3(cos, -0.5f, sin));
                top[i] = m.MultiplyPoint3x4(new Vector3(cos * topScale, 0.5f, sin * topScale));
            }

            var bottomCenter = m.MultiplyPoint3x4(new Vector3(0f, -0.5f, 0f));
            var topCenter = m.MultiplyPoint3x4(new Vector3(0f, 0.5f, 0f));
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                if (topScale < 0.001f)
                {
                    AddTriangleOutward(bottom[i], bottom[next], topCenter, inside, color);
                }
                else
                {
                    AddTriangleOutward(bottom[i], bottom[next], top[next], inside, color);
                    AddTriangleOutward(bottom[i], top[next], top[i], inside, color);
                    AddTriangleOutward(topCenter, top[i], top[next], inside, color);
                }

                AddTriangleOutward(bottomCenter, bottom[i], bottom[next], inside, color);
            }
        }

        /// <summary>Icosahedron with diameter 1 in unit space.</summary>
        public void AddIco(Vector3 center, Vector3 size, Vector3 euler, Color color)
        {
            EnsureIco();
            var m = Matrix4x4.TRS(center, Quaternion.Euler(euler), size);
            var inside = m.MultiplyPoint3x4(Vector3.zero);
            foreach (var face in IcoFaces)
            {
                AddTriangleOutward(
                    m.MultiplyPoint3x4(icoVertices[face[0]]),
                    m.MultiplyPoint3x4(icoVertices[face[1]]),
                    m.MultiplyPoint3x4(icoVertices[face[2]]),
                    inside,
                    color);
            }
        }

        /// <summary>Upper half of a sphere with diameter 1 in unit space.</summary>
        public void AddDome(Vector3 center, Vector3 size, Vector3 euler, int segments, int rings, Color color)
        {
            segments = Mathf.Max(3, segments);
            rings = Mathf.Max(2, rings);
            var m = Matrix4x4.TRS(center, Quaternion.Euler(euler), size);
            var inside = m.MultiplyPoint3x4(Vector3.zero);
            var grid = new Vector3[rings + 1, segments];
            for (var r = 0; r <= rings; r++)
            {
                var phi = Mathf.PI * 0.5f * r / rings;
                for (var s = 0; s < segments; s++)
                {
                    var theta = Mathf.PI * 2f * s / segments;
                    grid[r, s] = m.MultiplyPoint3x4(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * 0.5f);
                }
            }

            for (var r = 0; r < rings; r++)
            {
                for (var s = 0; s < segments; s++)
                {
                    var next = (s + 1) % segments;
                    AddTriangleOutward(grid[r, s], grid[r + 1, s], grid[r + 1, next], inside, color);
                    if (r > 0)
                    {
                        AddTriangleOutward(grid[r, s], grid[r + 1, next], grid[r, next], inside, color);
                    }
                }
            }
        }

        /// <summary>Flat, upward-facing ring. <paramref name="gaps"/> removes evenly spaced segments to form a dashed marker.</summary>
        public void AddRing(Vector3 center, float inner, float outer, int segments, int gaps, Color color)
        {
            segments = Mathf.Max(6, segments);
            var gapEvery = gaps > 0 ? Mathf.Max(2, segments / gaps) : 0;
            for (var i = 0; i < segments; i++)
            {
                if (gapEvery > 0 && i % gapEvery == 0)
                {
                    continue;
                }

                var a0 = Mathf.PI * 2f * i / segments;
                var a1 = Mathf.PI * 2f * (i + 1) / segments;
                var i0 = center + new Vector3(Mathf.Cos(a0) * inner, 0f, Mathf.Sin(a0) * inner);
                var i1 = center + new Vector3(Mathf.Cos(a1) * inner, 0f, Mathf.Sin(a1) * inner);
                var o0 = center + new Vector3(Mathf.Cos(a0) * outer, 0f, Mathf.Sin(a0) * outer);
                var o1 = center + new Vector3(Mathf.Cos(a1) * outer, 0f, Mathf.Sin(a1) * outer);
                AddTriangleUp(i0, o0, o1, color);
                AddTriangleUp(i0, o1, i1, color);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside, Color color)
        {
            AddTriangleOutward(a, b, c, inside, color);
            AddTriangleOutward(a, c, d, inside, color);
        }

        private static void EnsureIco()
        {
            if (icoVertices != null)
            {
                return;
            }

            var t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var raw = new[]
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };
            icoVertices = new Vector3[raw.Length];
            for (var i = 0; i < raw.Length; i++)
            {
                icoVertices[i] = raw[i].normalized * 0.5f;
            }
        }
    }
}
