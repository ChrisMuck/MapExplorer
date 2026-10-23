using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Procedural relief for the style prototype: gentle noise, hills and peaks, and a ditch that
    /// runs across the whole map so a bridge cannot be bypassed. The relief is presentation only.
    /// "Known" meshes flatten and pale the parts the player has no knowledge of, so the relief
    /// itself never reveals what the player does not know.
    /// </summary>
    public static class MapStyleTerrain
    {
        public const float Size = 40f;
        public const int Cells = 80;
        public const float WaterLevel = -1.3f;

        private static readonly Color[] Grass =
        {
            new Color32(0x7f, 0xa8, 0x5a, 255), new Color32(0x74, 0xa0, 0x54, 255),
            new Color32(0x88, 0xb0, 0x62, 255), new Color32(0x6f, 0x9a, 0x50, 255)
        };

        private static readonly Color Paper = new Color32(0xd8, 0xd4, 0xc2, 255);

        public static float Hash(float a, float b)
        {
            var s = Mathf.Sin(a * 127.1f + b * 311.7f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        public static float SmoothStep(float a, float b, float x)
        {
            x = Mathf.Clamp01((x - a) / (b - a));
            return x * x * (3f - 2f * x);
        }

        public static float DitchCenterZ(float x)
        {
            return 2f + 1.5f * Mathf.Sin(x * 0.25f);
        }

        public static float Trench(float x, float z)
        {
            var distance = Mathf.Abs(z - DitchCenterZ(x));
            return 1f - SmoothStep(0.7f, 2.3f, distance);
        }

        public static float Height(float x, float z)
        {
            var noise = Mathf.Sin(x * 0.45f) * Mathf.Cos(z * 0.38f) * 0.28f
                + Mathf.Sin(x * 1.1f + z * 0.9f) * 0.1f
                + Mathf.Cos(z * 0.8f - x * 0.5f) * 0.08f;
            var hills = Mathf.Max(0f, -12f - z) * 0.28f * (1f + 0.5f * Mathf.Sin(x * 0.6f));
            var peaks = Peak(x, z, 14f, -9f, 5f, 3.4f) + Peak(x, z, -15f, -13f, 5.5f, 3.0f) + Peak(x, z, 1f, -15f, 6f, 3.8f);
            return noise + hills + peaks - 2.1f * Trench(x, z);
        }

        private static float Peak(float x, float z, float cx, float cz, float radius, float height)
        {
            var d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
            return height * (1f - SmoothStep(0f, radius, d));
        }

        /// <summary>0 = confirmed, about 0.55 = reported, close to 1 = unknown. Zones run along x.</summary>
        public static float Knowledge(float x)
        {
            if (x < -2f) return 0f;
            if (x < 0f) return SmoothStep(-2f, 0f, x) * 0.55f;
            if (x < 6f) return 0.55f;
            return 0.55f + 0.4f * SmoothStep(6f, 9f, x);
        }

        /// <summary>Zone used to give whole objects a discrete knowledge level: 0, 1 or 2.</summary>
        public static int KnowledgeLevel(float x)
        {
            return x < -2f ? 0 : x < 6f ? 1 : 2;
        }

        public static float DistanceToRoads(IReadOnlyList<Vector2[]> roads, float x, float z)
        {
            var best = float.MaxValue;
            foreach (var road in roads)
            {
                for (var i = 0; i < road.Length - 1; i++)
                {
                    var a = road[i];
                    var b = road[i + 1];
                    var dx = b.x - a.x;
                    var dz = b.y - a.y;
                    var t = Mathf.Clamp01(((x - a.x) * dx + (z - a.y) * dz) / (dx * dx + dz * dz));
                    best = Mathf.Min(best, Mathf.Sqrt((x - a.x - t * dx) * (x - a.x - t * dx) + (z - a.y - t * dz) * (z - a.y - t * dz)));
                }
            }

            return best;
        }

        public static Mesh Build(bool known, IReadOnlyList<Vector2[]> roads)
        {
            var step = Size / Cells;
            var count = Cells + 1;
            var grid = new Vector3[count, count];
            for (var iz = 0; iz < count; iz++)
            {
                for (var ix = 0; ix < count; ix++)
                {
                    var index = iz * count + ix;
                    var x = -Size * 0.5f + ix * step + (Hash(index, 1f) - 0.5f) * step * 0.45f;
                    var z = -Size * 0.5f + iz * step + (Hash(index, 2f) - 0.5f) * step * 0.45f;
                    var y = Height(x, z);
                    if (known)
                    {
                        y *= 1f - 0.3f * SmoothStep(-1f, 1f, x) - 0.55f * SmoothStep(5.5f, 9f, x);
                    }

                    grid[ix, iz] = new Vector3(x, y, z);
                }
            }

            var builder = new MapStyleMeshBuilder();
            var face = 0;
            for (var iz = 0; iz < Cells; iz++)
            {
                for (var ix = 0; ix < Cells; ix++)
                {
                    var p00 = grid[ix, iz];
                    var p10 = grid[ix + 1, iz];
                    var p01 = grid[ix, iz + 1];
                    var p11 = grid[ix + 1, iz + 1];
                    AddFace(builder, p00, p01, p11, known, roads, face++);
                    AddFace(builder, p00, p11, p10, known, roads, face++);
                }
            }

            return builder.Build(known ? "MapStyleTerrainKnown" : "MapStyleTerrainTruth");
        }

        private static void AddFace(MapStyleMeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, bool known, IReadOnlyList<Vector2[]> roads, int face)
        {
            var center = (a + b + c) / 3f;
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var color = FaceColor(center, Mathf.Abs(normal.y), roads, face);
            if (known)
            {
                color = Color.Lerp(color, Paper, Knowledge(center.x) * 0.88f);
            }

            builder.AddTriangleUp(a, b, c, color);
        }

        private static Color FaceColor(Vector3 center, float flatness, IReadOnlyList<Vector2[]> roads, int face)
        {
            var color = Grass[Mathf.FloorToInt(Hash(face, 5f) * 4f) % 4];
            var trench = Trench(center.x, center.z);
            if (center.y > 2.6f)
            {
                color = Color.Lerp(new Color32(0xe8, 0xea, 0xee, 255), new Color32(0xcf, 0xd3, 0xda, 255), Hash(face, 8f));
            }
            else if (center.y > 1.1f)
            {
                color = Color.Lerp(new Color32(0x8d, 0x8f, 0x84, 255), new Color32(0xa5, 0xa7, 0x9c, 255), Hash(face, 9f));
            }
            else if (center.y > 0.6f)
            {
                color = Color.Lerp(color, new Color32(0x9a, 0xa8, 0x76, 255), 0.5f);
            }

            if (trench > 0.1f && flatness < 0.93f)
            {
                color = Hash(face, 3f) > 0.5f ? (Color)new Color32(0x8a, 0x6b, 0x4a, 255) : (Color)new Color32(0x7a, 0x74, 0x68, 255);
            }

            if (center.y < -0.9f)
            {
                color = new Color32(0x4a, 0x42, 0x38, 255);
            }

            if (roads != null && trench < 0.05f && DistanceToRoads(roads, center.x, center.z) < 0.8f)
            {
                color = Color.Lerp(new Color32(0xc2, 0xa8, 0x78, 255), new Color32(0xb5, 0x9a, 0x68, 255), Hash(face, 4f) * 0.5f);
            }

            return color;
        }

        public static Mesh BuildWater()
        {
            var builder = new MapStyleMeshBuilder();
            var h = Size * 0.5f;
            var color = new Color32(0x2f, 0x5d, 0x7a, 255);
            builder.AddTriangleUp(new Vector3(-h, WaterLevel, -h), new Vector3(-h, WaterLevel, h), new Vector3(h, WaterLevel, h), color);
            builder.AddTriangleUp(new Vector3(-h, WaterLevel, -h), new Vector3(h, WaterLevel, h), new Vector3(h, WaterLevel, -h), color);
            return builder.Build("MapStyleWater");
        }
    }
}
