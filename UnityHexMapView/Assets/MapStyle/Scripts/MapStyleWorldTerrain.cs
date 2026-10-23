using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Maps Game.Core tile coordinates to the axial view grid the map is drawn on. The tutorial world stores
    /// offset (column, row) coordinates and generated worlds store axial ones, exactly as the existing map view
    /// treats them, so both draw as an upright rectangle instead of a sheared one.
    /// </summary>
    public sealed class MapStyleHexLayout
    {
        private readonly bool offsetRows;
        private readonly int width;
        private readonly int height;
        private readonly int qOffset;

        private MapStyleHexLayout(bool offsetRows, int width, int height, int qOffset)
        {
            this.offsetRows = offsetRows;
            this.width = width;
            this.height = height;
            this.qOffset = qOffset;
        }

        public static MapStyleHexLayout OffsetCentered(int width, int height)
        {
            return new MapStyleHexLayout(true, width, height, 0);
        }

        public static MapStyleHexLayout Axial(int qOffset)
        {
            return new MapStyleHexLayout(false, 0, 0, qOffset);
        }

        public HexCoord ToView(HexCoord core)
        {
            if (!offsetRows)
            {
                return new HexCoord(core.Q - qOffset, core.R);
            }

            var row = core.R - height / 2;
            var column = core.Q - width / 2;
            return new HexCoord(column - (row - (row & 1)) / 2, row);
        }

        public HexCoord ToCore(HexCoord view)
        {
            if (!offsetRows)
            {
                return new HexCoord(view.Q + qOffset, view.R);
            }

            var column = view.Q + (view.R - (view.R & 1)) / 2;
            return new HexCoord(column + width / 2, view.R + height / 2);
        }
    }

    /// <summary>
    /// Builds relief terrain for a real <see cref="WorldState"/>: heights come from the terrain type of
    /// each hex (blended so hills and mountains rise smoothly), ravines and rivers are carved along the
    /// world paths, and roads tint the ground. The relief is presentation only; the hex grid stays the
    /// logic. With knowledge enabled, unknown and reported areas are flattened and paled so the relief
    /// never shows more than the player knows.
    /// </summary>
    public sealed class MapStyleWorldTerrain
    {
        public const float HexSize = 2.5f;
        public const float WaterLevel = -1.3f;
        public const float VertexStep = 0.9f;

        private const float Sqrt3 = 1.7320508f;
        private static readonly Color Paper = new Color32(0xd8, 0xd4, 0xc2, 255);

        private readonly struct Tile
        {
            public readonly HexCoord Core;
            public readonly TerrainType Terrain;
            public readonly float Height;
            public readonly float Amplitude;
            public readonly float Knowledge;

            public Tile(HexCoord core, TerrainType terrain, float knowledge)
            {
                Core = core;
                Terrain = terrain;
                Height = BaseHeight(terrain);
                Amplitude = BaseAmplitude(terrain);
                Knowledge = knowledge;
            }

            public Tile WithKnowledge(float knowledge)
            {
                return new Tile(Core, Terrain, knowledge);
            }
        }

        public struct Sample
        {
            public bool Valid;
            public float Height;
            public float Knowledge;
            public TerrainType Terrain;
        }

        private readonly Dictionary<HexCoord, Tile> tiles = new();
        private readonly MapStyleHexLayout layout;
        private readonly Vector2 min;
        private readonly Vector2 max;

        public MapStyleWorldTerrain(WorldState world, MapStyleHexLayout layout, Func<HexCoord, float> knowledgeOf)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            var lo = new Vector2(float.MaxValue, float.MaxValue);
            var hi = new Vector2(float.MinValue, float.MinValue);
            foreach (var tile in world.Map.Tiles)
            {
                var view = layout.ToView(tile.Coord);
                tiles[view] = new Tile(tile.Coord, tile.Terrain, knowledgeOf(tile.Coord));
                var p = ViewToWorld(view);
                lo = Vector2.Min(lo, p);
                hi = Vector2.Max(hi, p);
            }

            min = lo - Vector2.one * HexSize;
            max = hi + Vector2.one * HexSize;

            Chasms = new MapStylePolylineIndex();
            Rivers = new MapStylePolylineIndex();
            Roads = new MapStylePolylineIndex();
            foreach (var path in world.Paths)
            {
                var points = new List<Vector2>(path.Coords.Count);
                foreach (var coord in path.Coords)
                {
                    points.Add(CoreToWorld(coord));
                }

                switch (path.Kind)
                {
                    case WorldPathKind.Chasm:
                        Chasms.AddPolyline(path.Id, points);
                        break;
                    case WorldPathKind.River:
                        Rivers.AddPolyline(path.Id, points);
                        break;
                    case WorldPathKind.Road:
                        Roads.AddPolyline(path.Id, points);
                        break;
                }
            }
        }

        public MapStylePolylineIndex Chasms { get; }

        public MapStylePolylineIndex Rivers { get; }

        public MapStylePolylineIndex Roads { get; }

        public Vector2 Min => min;

        public Vector2 Max => max;

        // ---- Hex layout (pointy-top axial) ------------------------------------------------

        /// <summary>World position of a Game.Core tile coordinate.</summary>
        public Vector2 CoreToWorld(HexCoord core)
        {
            return ViewToWorld(layout.ToView(core));
        }

        /// <summary>World position of an axial view coordinate.</summary>
        public static Vector2 ViewToWorld(HexCoord view)
        {
            return new Vector2(HexSize * Sqrt3 * (view.Q + view.R * 0.5f), HexSize * 1.5f * view.R);
        }

        public static HexCoord WorldToView(Vector2 point)
        {
            var q = (Sqrt3 / 3f * point.x - point.y / 3f) / HexSize;
            var r = (2f / 3f * point.y) / HexSize;
            var s = -q - r;
            var rq = Mathf.Round(q);
            var rr = Mathf.Round(r);
            var rs = Mathf.Round(s);
            var dq = Mathf.Abs(rq - q);
            var dr = Mathf.Abs(rr - r);
            var ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds)
            {
                rq = -rr - rs;
            }
            else if (dr > ds)
            {
                rr = -rq - rs;
            }

            return new HexCoord((int)rq, (int)rr);
        }

        // ---- Knowledge mapping --------------------------------------------------------------

        /// <summary>Pale amount for a knowledge level: 0 confirmed, 0.45 old, 0.55 reported, 0.95 unknown.</summary>
        public static float PaleOf(KnowledgeLevel level)
        {
            switch (level)
            {
                case KnowledgeLevel.Confirmed: return 0f;
                case KnowledgeLevel.OldOrDoubtful: return 0.45f;
                case KnowledgeLevel.Reported: return 0.55f;
                default: return 0.95f;
            }
        }

        public static float FlattenFactor(float knowledge)
        {
            if (knowledge <= 0.55f)
            {
                return 1f - 0.3f * (knowledge / 0.55f);
            }

            return 0.7f - 0.55f * Mathf.Clamp01((knowledge - 0.55f) / 0.4f);
        }

        // ---- Heights --------------------------------------------------------------------------

        public static float BaseHeight(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water: return -2.4f;
                case TerrainType.Coast: return -0.4f;
                case TerrainType.Grassland: return 0.1f;
                case TerrainType.DryPlains: return 0.15f;
                case TerrainType.Forest: return 0.2f;
                case TerrainType.Hills: return 1.0f;
                case TerrainType.Mountain: return 3.0f;
                case TerrainType.Snow: return 3.8f;
                case TerrainType.Swamp: return -0.1f;
                case TerrainType.Desert: return 0.1f;
                default: return 0.1f;
            }
        }

        private static float BaseAmplitude(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Mountain: return 0.9f;
                case TerrainType.Snow: return 0.8f;
                case TerrainType.Hills: return 0.45f;
                case TerrainType.Water: return 0.05f;
                default: return 0.18f;
            }
        }

        private static float Noise(float x, float z)
        {
            var value = Mathf.Sin(x * 0.45f) * Mathf.Cos(z * 0.38f) * 0.28f
                + Mathf.Sin(x * 1.1f + z * 0.9f) * 0.1f
                + Mathf.Cos(z * 0.8f - x * 0.5f) * 0.08f;
            return value / 0.46f;
        }

        public static float SmoothStep(float a, float b, float x)
        {
            x = Mathf.Clamp01((x - a) / (b - a));
            return x * x * (3f - 2f * x);
        }

        public static float Hash(float a, float b)
        {
            var s = Mathf.Sin(a * 127.1f + b * 311.7f) * 43758.5453f;
            var h = s - Mathf.Floor(s);
            return h >= 1f ? 0f : h;
        }

        public float TrenchFactor(Vector2 point)
        {
            var distance = Chasms.Distance(point);
            return distance >= 2.3f ? 0f : 1f - SmoothStep(0.7f, 2.3f, distance);
        }

        public float RiverFactor(Vector2 point)
        {
            var distance = Rivers.Distance(point);
            return distance >= 1.4f ? 0f : 1f - SmoothStep(0.45f, 1.4f, distance);
        }

        public Sample SampleAt(Vector2 point, bool useKnowledge)
        {
            var center = WorldToView(point);
            float weightSum = 0f, heightSum = 0f, amplitudeSum = 0f, knowledgeSum = 0f;
            var nearest = TerrainType.Grassland;
            var nearestDistance = float.MaxValue;
            AddTile(center, point, ref weightSum, ref heightSum, ref amplitudeSum, ref knowledgeSum, ref nearest, ref nearestDistance);
            foreach (var neighbor in center.Neighbors())
            {
                AddTile(neighbor, point, ref weightSum, ref heightSum, ref amplitudeSum, ref knowledgeSum, ref nearest, ref nearestDistance);
            }

            if (weightSum < 0.0001f || !tiles.ContainsKey(center))
            {
                return new Sample { Valid = false };
            }

            var knowledge = knowledgeSum / weightSum;
            var height = heightSum / weightSum + Noise(point.x, point.y) * (amplitudeSum / weightSum);
            height -= 2.1f * TrenchFactor(point);
            var river = RiverFactor(point);
            if (river > 0f)
            {
                height = Mathf.Lerp(height, Mathf.Min(height, -1.9f), river);
            }

            if (useKnowledge)
            {
                height *= FlattenFactor(knowledge);
            }

            return new Sample { Valid = true, Height = height, Knowledge = knowledge, Terrain = nearest };
        }

        private void AddTile(
            HexCoord coord,
            Vector2 point,
            ref float weightSum,
            ref float heightSum,
            ref float amplitudeSum,
            ref float knowledgeSum,
            ref TerrainType nearest,
            ref float nearestDistance)
        {
            if (!tiles.TryGetValue(coord, out var tile))
            {
                return;
            }

            var distance = Vector2.Distance(point, ViewToWorld(coord));
            var weight = Mathf.Max(0f, 1f - distance / (HexSize * 1.35f));
            weight *= weight;
            weightSum += weight;
            heightSum += tile.Height * weight;
            amplitudeSum += tile.Amplitude * weight;
            knowledgeSum += tile.Knowledge * weight;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = tile.Terrain;
            }
        }

        /// <summary>Forest tiles as (axial view coordinate, Game.Core coordinate).</summary>
        public IEnumerable<KeyValuePair<HexCoord, HexCoord>> ForestTiles()
        {
            foreach (var pair in tiles)
            {
                if (pair.Value.Terrain == TerrainType.Forest)
                {
                    yield return new KeyValuePair<HexCoord, HexCoord>(pair.Key, pair.Value.Core);
                }
            }
        }

        // ---- Chunked mesh -------------------------------------------------------------------------

        /// <summary>Quads per chunk side. A chunk is rebuilt on its own when the knowledge around it changes.</summary>
        public const int ChunkCells = 24;

        private int VertexCountX => Mathf.CeilToInt((max.x - min.x) / VertexStep) + 1;

        private int VertexCountZ => Mathf.CeilToInt((max.y - min.y) / VertexStep) + 1;

        public int ChunkCountX => Mathf.CeilToInt((VertexCountX - 1) / (float)ChunkCells);

        public int ChunkCountZ => Mathf.CeilToInt((VertexCountZ - 1) / (float)ChunkCells);

        public static float ChunkWorldSize => ChunkCells * VertexStep;

        /// <summary>The chunk a world position falls into, clamped to the map.</summary>
        public Vector2Int ChunkOf(Vector2 point)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((point.x - min.x) / ChunkWorldSize), 0, ChunkCountX - 1),
                Mathf.Clamp(Mathf.FloorToInt((point.y - min.y) / ChunkWorldSize), 0, ChunkCountZ - 1));
        }

        /// <summary>
        /// Replaces the knowledge values of all tiles and returns the chunks whose ground changes, so a caller
        /// can rebuild only those. A tile influences the ground up to about one and a half hexes away.
        /// </summary>
        public HashSet<Vector2Int> UpdateKnowledge(Func<HexCoord, float> knowledgeOf)
        {
            var dirty = new HashSet<Vector2Int>();
            var changed = new List<KeyValuePair<HexCoord, Tile>>();
            foreach (var pair in tiles)
            {
                var knowledge = knowledgeOf(pair.Value.Core);
                if (Mathf.Abs(knowledge - pair.Value.Knowledge) > 0.0001f)
                {
                    changed.Add(new KeyValuePair<HexCoord, Tile>(pair.Key, pair.Value.WithKnowledge(knowledge)));
                }
            }

            var reach = HexSize * 1.35f + VertexStep * 2f;
            foreach (var pair in changed)
            {
                tiles[pair.Key] = pair.Value;
                var center = ViewToWorld(pair.Key);
                var low = ChunkOf(center - Vector2.one * reach);
                var high = ChunkOf(center + Vector2.one * reach);
                for (var x = low.x; x <= high.x; x++)
                {
                    for (var z = low.y; z <= high.y; z++)
                    {
                        dirty.Add(new Vector2Int(x, z));
                    }
                }
            }

            return dirty;
        }

        public Mesh BuildChunkMesh(Vector2Int chunk, bool useKnowledge)
        {
            var nx = VertexCountX;
            var nz = VertexCountZ;
            var ix0 = chunk.x * ChunkCells;
            var iz0 = chunk.y * ChunkCells;
            var ix1 = Mathf.Min(ix0 + ChunkCells, nx - 1);
            var iz1 = Mathf.Min(iz0 + ChunkCells, nz - 1);
            var width = ix1 - ix0 + 1;
            var depth = iz1 - iz0 + 1;
            var samples = new Sample[width, depth];
            var positions = new Vector3[width, depth];
            for (var iz = iz0; iz <= iz1; iz++)
            {
                for (var ix = ix0; ix <= ix1; ix++)
                {
                    var jitter = new Vector2(
                        (Hash(ix, iz) - 0.5f) * VertexStep * 0.45f,
                        (Hash(iz, ix + 17f) - 0.5f) * VertexStep * 0.45f);
                    var p = new Vector2(min.x + ix * VertexStep, min.y + iz * VertexStep) + jitter;
                    var sample = SampleAt(p, useKnowledge);
                    samples[ix - ix0, iz - iz0] = sample;
                    positions[ix - ix0, iz - iz0] = new Vector3(p.x, sample.Height, p.y);
                }
            }

            var builder = new MapStyleMeshBuilder();
            for (var iz = iz0; iz < iz1; iz++)
            {
                for (var ix = ix0; ix < ix1; ix++)
                {
                    var lx = ix - ix0;
                    var lz = iz - iz0;
                    if (!samples[lx, lz].Valid || !samples[lx + 1, lz].Valid || !samples[lx, lz + 1].Valid || !samples[lx + 1, lz + 1].Valid)
                    {
                        continue;
                    }

                    var face = (iz * (nx - 1) + ix) * 2;
                    AddFace(builder, positions[lx, lz], positions[lx, lz + 1], positions[lx + 1, lz + 1], samples[lx, lz], useKnowledge, face);
                    AddFace(builder, positions[lx, lz], positions[lx + 1, lz + 1], positions[lx + 1, lz], samples[lx + 1, lz + 1], useKnowledge, face + 1);
                }
            }

            return builder.Build("MapStyleChunk-" + chunk.x + "-" + chunk.y);
        }

        /// <summary>Forest tiles whose center lies in the chunk, as (axial view coordinate, Game.Core coordinate).</summary>
        public IEnumerable<KeyValuePair<HexCoord, HexCoord>> ForestTilesIn(Vector2Int chunk)
        {
            foreach (var pair in tiles)
            {
                if (pair.Value.Terrain == TerrainType.Forest && ChunkOf(ViewToWorld(pair.Key)) == chunk)
                {
                    yield return new KeyValuePair<HexCoord, HexCoord>(pair.Key, pair.Value.Core);
                }
            }
        }

        public Mesh BuildWaterMesh()
        {
            var builder = new MapStyleMeshBuilder();
            var margin = HexSize * 3f;
            var a = new Vector3(min.x - margin, WaterLevel, min.y - margin);
            var b = new Vector3(min.x - margin, WaterLevel, max.y + margin);
            var c = new Vector3(max.x + margin, WaterLevel, max.y + margin);
            var d = new Vector3(max.x + margin, WaterLevel, min.y - margin);
            var color = new Color32(0x2f, 0x5d, 0x7a, 255);
            builder.AddTriangleUp(a, b, c, color);
            builder.AddTriangleUp(a, c, d, color);
            return builder.Build("MapStyleWorldWater");
        }

        private void AddFace(MapStyleMeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Sample sample, bool useKnowledge, int face)
        {
            var center = (a + b + c) / 3f;
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var point = new Vector2(center.x, center.z);
            var color = FaceColor(point, center.y, Mathf.Abs(normal.y), sample.Terrain, face);
            if (useKnowledge)
            {
                color = Color.Lerp(color, Paper, sample.Knowledge * 0.88f);
            }

            builder.AddTriangleUp(a, b, c, color);
        }

        private Color FaceColor(Vector2 point, float height, float flatness, TerrainType terrain, int face)
        {
            var pick = Hash(face, 5f);
            var color = TerrainColor(terrain, pick);
            if ((terrain == TerrainType.Mountain || terrain == TerrainType.Snow) && height > 3.4f)
            {
                color = Color.Lerp(new Color32(0xe8, 0xea, 0xee, 255), new Color32(0xcf, 0xd3, 0xda, 255), Hash(face, 8f));
            }
            else if ((terrain == TerrainType.Mountain || terrain == TerrainType.Hills) && height > 1.8f)
            {
                color = Color.Lerp(new Color32(0x8d, 0x8f, 0x84, 255), new Color32(0xa5, 0xa7, 0x9c, 255), Hash(face, 9f));
            }

            var trench = TrenchFactor(point);
            var river = RiverFactor(point);
            if (trench > 0.1f && flatness < 0.93f)
            {
                color = Hash(face, 3f) > 0.5f ? (Color)new Color32(0x8a, 0x6b, 0x4a, 255) : (Color)new Color32(0x7a, 0x74, 0x68, 255);
            }

            if (height < -0.9f && terrain != TerrainType.Water)
            {
                color = new Color32(0x4a, 0x42, 0x38, 255);
            }

            if (trench < 0.05f && river < 0.05f && terrain != TerrainType.Water && Roads.Distance(point) < 0.75f)
            {
                color = Color.Lerp(new Color32(0xc2, 0xa8, 0x78, 255), new Color32(0xb5, 0x9a, 0x68, 255), Hash(face, 4f) * 0.5f);
            }

            return color;
        }

        private static Color TerrainColor(TerrainType terrain, float pick)
        {
            switch (terrain)
            {
                case TerrainType.Water: return new Color32(0x3f, 0x5a, 0x62, 255);
                case TerrainType.Coast: return Mix(0xcdbd8e, 0xc4b484, pick);
                case TerrainType.DryPlains: return Mix(0xa3b36a, 0x98a862, pick);
                case TerrainType.Forest: return Mix(0x5f8d4a, 0x588444, pick);
                case TerrainType.Hills: return Mix(0x8fa062, 0x859858, pick);
                case TerrainType.Mountain: return Mix(0x8d8f84, 0x7f8177, pick);
                case TerrainType.Snow: return Mix(0xe8eaee, 0xcfd3da, pick);
                case TerrainType.Swamp: return Mix(0x5f7a58, 0x566f50, pick);
                case TerrainType.Desert: return Mix(0xd6bd80, 0xcdb376, pick);
                default:
                    return pick < 0.25f ? Mix(0x7fa85a, 0x7fa85a, 0f)
                        : pick < 0.5f ? Mix(0x74a054, 0x74a054, 0f)
                        : pick < 0.75f ? Mix(0x88b062, 0x88b062, 0f)
                        : Mix(0x6f9a50, 0x6f9a50, 0f);
            }
        }

        private static Color Mix(int a, int b, float t)
        {
            return Color.Lerp(MapStyleLocation.C(a), MapStyleLocation.C(b), t);
        }
    }
}
