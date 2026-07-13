using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How a hex's fog-of-war tint should read; decoupled from Game.Core's KnowledgeLevel.</summary>
internal enum HexFogState
{
    None,
    Unknown,
    Reported
}

/// <summary>
/// Unified presentation renderer for terrain + decoration, replacing both the classic per-hex
/// GameObject path and the old prefab-less ChunkedCoreMapRenderer. Partitions the map into 16x16-hex
/// chunks: terrain and fog become one combined mesh each per chunk (real draw-call reduction), while
/// forest/mountain/foothill/ground-scatter decoration is GPU-instanced using render data extracted
/// once from the existing authored HexMapPrefabLibrary prefabs, so the actual hand-authored art
/// renders everywhere instead of generic primitives. Game.Core is never referenced here; the caller
/// hands in already-resolved view-space tiles plus small visibility delegates.
/// </summary>
internal sealed class HexMapChunkRenderer
{
    private const int ChunkSize = 16;
    private const int MaxInstancesPerDraw = 1023;

    private static readonly Vector2Int[] Directions =
    {
        new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
    };

    private readonly Transform parent;
    private readonly HexMapPrefabLibrary prefabs;
    private readonly bool usePrefabOverrides;
    private readonly HexDecorationLibrary decorationLibrary;
    private readonly Material terrainMaterial;
    private readonly Material unknownFogMaterial;
    private readonly Material reportedFogMaterial;
    private readonly Material natureShaderTemplate;
    private readonly float hexSize;
    private readonly float terrainY;
    private readonly float fogY;
    private readonly float groundScatterZoomCutoff;

    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private readonly Dictionary<string, Material> natureMaterials = new();
    private readonly Dictionary<string, Material> fallbackMaterials = new();
    private Transform root;
    private Mesh unitConeMesh;
    private Mesh unitBoxMesh;
    private Mesh unitHexDiscMesh;

    public HexMapChunkRenderer(
        Transform parent,
        HexMapPrefabLibrary prefabs,
        bool usePrefabOverrides,
        HexDecorationLibrary decorationLibrary,
        Material terrainMaterial,
        Material unknownFogMaterial,
        Material reportedFogMaterial,
        Material natureShaderTemplate,
        float hexSize,
        float terrainY,
        float fogY,
        float groundScatterZoomCutoff)
    {
        this.parent = parent;
        this.prefabs = prefabs;
        this.usePrefabOverrides = usePrefabOverrides;
        this.decorationLibrary = decorationLibrary;
        this.terrainMaterial = terrainMaterial;
        this.unknownFogMaterial = unknownFogMaterial;
        this.reportedFogMaterial = reportedFogMaterial;
        this.natureShaderTemplate = natureShaderTemplate;
        this.hexSize = hexSize;
        this.terrainY = terrainY;
        this.fogY = fogY;
        this.groundScatterZoomCutoff = groundScatterZoomCutoff;
    }

    // ------------------------------------------------------------------ public API

    public void Build(
        IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles,
        int visualSeed,
        Func<Vector2Int, bool> isDecorationVisible,
        Func<Vector2Int, HexFogState> fogStateForCoord)
    {
        Dispose();

        root = new GameObject("Hex Map Chunks").transform;
        root.SetParent(parent, false);
        EnsureCanonicalMeshes();

        var forestTypeByCoord = ClassifyForestRegions(tiles, visualSeed);

        foreach (var pair in tiles)
        {
            var key = new Vector2Int(FloorDiv(pair.Key.x, ChunkSize), FloorDiv(pair.Key.y, ChunkSize));
            if (!chunks.TryGetValue(key, out var chunk))
            {
                chunk = new Chunk(key, new GameObject($"Chunk_{key.x}_{key.y}").transform);
                chunk.Root.SetParent(root, false);
                chunks.Add(key, chunk);
            }

            chunk.Coords.Add(pair.Key);
        }

        foreach (var chunk in chunks.Values)
        {
            BuildTerrainMesh(chunk, tiles, visualSeed);
            BuildDecorationForChunk(chunk, tiles, visualSeed, forestTypeByCoord);
            ComputeBounds(chunk, tiles);
        }

        RefreshKnowledge(isDecorationVisible, fogStateForCoord);
    }

    public void RefreshKnowledge(Func<Vector2Int, bool> isDecorationVisible, Func<Vector2Int, HexFogState> fogStateForCoord)
    {
        foreach (var chunk in chunks.Values)
        {
            BuildFogMesh(chunk, fogStateForCoord);
            RefreshVisibleInstances(chunk, isDecorationVisible);
        }
    }

    public void Present(Camera camera)
    {
        if (camera == null || chunks.Count == 0)
        {
            return;
        }

        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        var submitScatter = camera.orthographic
            ? camera.orthographicSize <= groundScatterZoomCutoff
            : true;

        foreach (var chunk in chunks.Values)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, chunk.WorldBounds))
            {
                continue;
            }

            foreach (var batch in chunk.Batches.Values)
            {
                if (batch.IsGroundScatter && !submitScatter)
                {
                    continue;
                }

                SubmitBatch(batch);
            }
        }
    }

    public void Dispose()
    {
        foreach (var chunk in chunks.Values)
        {
            DestroyMesh(chunk.TerrainMesh);
            DestroyMesh(chunk.FogUnknownMesh);
            DestroyMesh(chunk.FogReportedMesh);
        }

        chunks.Clear();
        tileWorldLookup.Clear();

        foreach (var material in natureMaterials.Values)
        {
            DestroyObject(material);
        }

        natureMaterials.Clear();

        foreach (var material in fallbackMaterials.Values)
        {
            DestroyObject(material);
        }

        fallbackMaterials.Clear();

        DestroyMesh(unitConeMesh);
        DestroyMesh(unitBoxMesh);
        DestroyMesh(unitHexDiscMesh);
        unitConeMesh = null;
        unitBoxMesh = null;
        unitHexDiscMesh = null;

        decorationLibrary?.Dispose();

        if (root != null)
        {
            DestroyObject(root.gameObject);
            root = null;
        }
    }

    // ------------------------------------------------------------------ terrain + fog

    private void BuildTerrainMesh(Chunk chunk, IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, int visualSeed)
    {
        var builder = new HexMapChunkGeometryBuilder();
        foreach (var coord in chunk.Coords)
        {
            var tile = tiles[coord];
            var baseColor = HexTerrainPalette.BaseColor(tile.Terrain);
            var tinted = TintColor(baseColor, HexVisualHash.Value01(visualSeed, coord.x, coord.y, 40011));
            builder.AddHex(tile.World, hexSize, terrainY, tinted);

            if (tile.Terrain == HexTerrainKind.Forest)
            {
                // Forest floor cover: a slightly darker, slightly raised second hex layer so the
                // ground under a tree cluster reads as lit forest floor rather than bare terrain.
                var cover = HexTerrainPalette.BaseColor(HexTerrainKind.Forest) * 0.86f;
                cover.a = 1f;
                builder.AddHex(tile.World, hexSize * 0.995f, terrainY + 0.006f, cover);
            }
        }

        if (chunk.TerrainObject == null)
        {
            chunk.TerrainObject = NewChild("Terrain", chunk.Root);
            chunk.TerrainFilter = chunk.TerrainObject.AddComponent<MeshFilter>();
            chunk.TerrainObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
        }

        DestroyMesh(chunk.TerrainMesh);
        chunk.TerrainMesh = builder.ToMesh($"Terrain_{chunk.Key.x}_{chunk.Key.y}");
        chunk.TerrainFilter.sharedMesh = chunk.TerrainMesh;
    }

    // Continuous HSV jitter replacing the old 6-bucket baked-material-variant scheme.
    private static Color TintColor(Color baseColor, float roll)
    {
        Color.RGBToHSV(baseColor, out var h, out var s, out var v);
        var t = roll - 0.5f;
        h = Mathf.Repeat(h + t * 0.02f, 1f);
        s = Mathf.Clamp01(s + t * 0.06f);
        v = Mathf.Clamp01(v + t * 0.13f);
        var tinted = Color.HSVToRGB(h, s, v);
        tinted.a = baseColor.a;
        return tinted;
    }

    private void BuildFogMesh(Chunk chunk, Func<Vector2Int, HexFogState> fogStateForCoord)
    {
        var unknown = new HexMapChunkGeometryBuilder();
        var reported = new HexMapChunkGeometryBuilder();
        foreach (var coord in chunk.Coords)
        {
            switch (fogStateForCoord(coord))
            {
                case HexFogState.Unknown:
                    unknown.AddHex(WorldOf(coord), hexSize * 0.996f, fogY, Color.white);
                    break;
                case HexFogState.Reported:
                    reported.AddHex(WorldOf(coord), hexSize * 0.996f, fogY, Color.white);
                    break;
            }
        }

        SetFogSubmesh(chunk, ref chunk.FogUnknownObject, ref chunk.FogUnknownFilter, ref chunk.FogUnknownMesh, unknown, unknownFogMaterial, "FogUnknown");
        SetFogSubmesh(chunk, ref chunk.FogReportedObject, ref chunk.FogReportedFilter, ref chunk.FogReportedMesh, reported, reportedFogMaterial, "FogReported");
    }

    private void SetFogSubmesh(Chunk chunk, ref GameObject obj, ref MeshFilter filter, ref Mesh mesh, HexMapChunkGeometryBuilder builder, Material material, string name)
    {
        DestroyMesh(mesh);
        mesh = null;

        if (builder.Count == 0)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }

            return;
        }

        if (obj == null)
        {
            obj = NewChild(name, chunk.Root);
            filter = obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        obj.SetActive(true);
        mesh = builder.ToMesh($"{name}_{chunk.Key.x}_{chunk.Key.y}");
        filter.sharedMesh = mesh;
    }

    private Vector3 WorldOf(Vector2Int coord)
    {
        // Tiles are only known per-chunk at build time; fog needs the same coord -> world mapping
        // terrain used, so chunks retain it via their tile snapshot below.
        return tileWorldLookup.TryGetValue(coord, out var world) ? world : Vector3.zero;
    }

    private readonly Dictionary<Vector2Int, Vector3> tileWorldLookup = new();

    private void ComputeBounds(Chunk chunk, IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles)
    {
        var min = new Vector3(float.PositiveInfinity, 0f, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, 0f, float.NegativeInfinity);
        foreach (var coord in chunk.Coords)
        {
            var world = tiles[coord].World;
            tileWorldLookup[coord] = world;
            min = Vector3.Min(min, world);
            max = Vector3.Max(max, world);
        }

        var pad = hexSize * 1.5f;
        min -= new Vector3(pad, 1f, pad);
        max += new Vector3(pad, 1f, pad);
        chunk.WorldBounds = new Bounds((min + max) * 0.5f, max - min);
    }

    // ------------------------------------------------------------------ forest classification (global, chunk-agnostic)

    private delegate bool TerrainMatcher(HexTerrainKind terrain);

    private static bool IsForestTerrain(HexTerrainKind terrain) => terrain == HexTerrainKind.Forest;
    private static bool IsMountainTerrain(HexTerrainKind terrain) => terrain == HexTerrainKind.Mountain || terrain == HexTerrainKind.Snow;

    private Dictionary<Vector2Int, HexForestType> ClassifyForestRegions(IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, int visualSeed)
    {
        var result = new Dictionary<Vector2Int, HexForestType>();
        var regions = FindTerrainRegions(tiles, IsForestTerrain);
        foreach (var region in regions)
        {
            var type = ClassifyForest(tiles, region, visualSeed);
            foreach (var coord in region)
            {
                result[coord] = type;
            }
        }

        return result;
    }

    private List<List<Vector2Int>> FindTerrainRegions(IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, TerrainMatcher matcher)
    {
        var regions = new List<List<Vector2Int>>();
        var visited = new HashSet<Vector2Int>();

        foreach (var pair in tiles)
        {
            if (visited.Contains(pair.Key) || !matcher(pair.Value.Terrain))
            {
                continue;
            }

            var region = new List<Vector2Int>();
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(pair.Key);
            visited.Add(pair.Key);

            while (frontier.Count > 0)
            {
                var coord = frontier.Dequeue();
                region.Add(coord);

                foreach (var direction in Directions)
                {
                    var neighbor = coord + direction;
                    if (visited.Contains(neighbor) || !tiles.TryGetValue(neighbor, out var neighborTile) || !matcher(neighborTile.Terrain))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            regions.Add(region);
        }

        return regions;
    }

    // Region-representative-hex driven classification, biased coniferous near mountains. Order
    // independent: the representative is the lexicographically smallest coord in the region, and
    // region membership (not traversal order) is the only BFS-derived input.
    private HexForestType ClassifyForest(IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, IReadOnlyList<Vector2Int> region, int visualSeed)
    {
        var rep = region[0];
        var mountainTouch = 0;
        foreach (var coord in region)
        {
            if (coord.x < rep.x || (coord.x == rep.x && coord.y < rep.y))
            {
                rep = coord;
            }

            if (CountMatchingNeighbors(tiles, coord, IsMountainTerrain) > 0)
            {
                mountainTouch++;
            }
        }

        var roll = HexVisualHash.Value01(visualSeed, rep.x, rep.y, 52000);
        var mountainBias = region.Count > 0 ? mountainTouch / (float)region.Count : 0f;
        roll -= mountainBias * 0.35f;

        if (roll < 0.42f) return HexForestType.Coniferous;
        if (roll < 0.74f) return HexForestType.Deciduous;
        return HexForestType.Mixed;
    }

    private static int CountMatchingNeighbors(IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, Vector2Int coord, TerrainMatcher matcher)
    {
        var count = 0;
        foreach (var direction in Directions)
        {
            if (tiles.TryGetValue(coord + direction, out var tile) && matcher(tile.Terrain))
            {
                count++;
            }
        }

        return count;
    }

    // ------------------------------------------------------------------ decoration (per chunk)

    private void BuildDecorationForChunk(
        Chunk chunk,
        IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles,
        int visualSeed,
        IReadOnlyDictionary<Vector2Int, HexForestType> forestTypeByCoord)
    {
        foreach (var coord in chunk.Coords)
        {
            var tile = tiles[coord];
            switch (tile.Terrain)
            {
                case HexTerrainKind.Forest:
                    AddForestDecoration(chunk, tiles, coord, tile, forestTypeByCoord.TryGetValue(coord, out var type) ? type : HexForestType.Mixed, visualSeed);
                    break;
                case HexTerrainKind.Mountain:
                    AddMountainDecoration(chunk, tiles, coord, tile, false, visualSeed);
                    break;
                case HexTerrainKind.Snow:
                    AddMountainDecoration(chunk, tiles, coord, tile, true, visualSeed);
                    break;
                case HexTerrainKind.Hills:
                    AddFoothillDecoration(chunk, tiles, coord, tile, true, visualSeed);
                    AddGroundScatterDecoration(chunk, coord, tile, visualSeed);
                    continue;
                case HexTerrainKind.Grass:
                    AddGroundScatterDecoration(chunk, coord, tile, visualSeed);
                    continue;
            }

            if (tile.Terrain != HexTerrainKind.Mountain && tile.Terrain != HexTerrainKind.Snow &&
                tile.Terrain != HexTerrainKind.Water && tile.Terrain != HexTerrainKind.Coast)
            {
                var mountainNeighbors = CountMatchingNeighbors(tiles, coord, IsMountainTerrain);
                if (mountainNeighbors > 0)
                {
                    AddFoothillDecoration(chunk, tiles, coord, tile, false, visualSeed);
                }
            }
        }
    }

    private void AddForestDecoration(Chunk chunk, IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, Vector2Int coord, HexTileVisual tile, HexForestType forestType, int visualSeed)
    {
        var salt = HexSalt(coord);
        var interior = CountMatchingNeighbors(tiles, coord, IsForestTerrain) >= 4;

        // Forest floor cover (see BuildTerrainMesh) is baked into the terrain mesh already; here we
        // only add the tree geometry itself.
        var clusterPrefabs = ForestClusterPrefabsFor(forestType);
        var rotation = Quaternion.Euler(0f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt) * 360f, 0f);
        var scale = interior ? 1.12f : 1f;
        var clusterMatrix = Matrix4x4.TRS(tile.World + Vector3.up * (terrainY + 0.02f), rotation, Vector3.one * hexSize * scale);

        if (TryGetPrefabParts(clusterPrefabs, visualSeed, coord.x, coord.y, salt, out var parts))
        {
            AddPrefabInstance(chunk, coord, parts, clusterMatrix);
            return;
        }

        // Safety-net fallback when no cluster prefab is assigned: a handful of simple canonical trees.
        var treeCount = interior ? 8 : 6;
        for (var i = 0; i < treeCount; i++)
        {
            var angle = i * Mathf.PI * 2f / treeCount + Mathf.Lerp(-0.2f, 0.2f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + i));
            var ring = i == 0 ? 0.08f : Mathf.Lerp(0.34f, 0.78f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, salt + 80 + i));
            var offset = new Vector3(Mathf.Cos(angle) * ring * hexSize, 0f, Mathf.Sin(angle) * ring * hexSize);
            var position = tile.World + offset + Vector3.up * (terrainY + 0.07f);
            var treeScale = Mathf.Lerp(0.5f, 0.85f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 150 + i)) * hexSize;
            AddFallbackTree(chunk, coord, position, treeScale);
        }
    }

    private GameObject[] ForestClusterPrefabsFor(HexForestType forestType)
    {
        if (prefabs == null)
        {
            return null;
        }

        var typed = forestType switch
        {
            HexForestType.Coniferous => prefabs.coniferousForestClusterPrefabs,
            HexForestType.Deciduous => prefabs.deciduousForestClusterPrefabs,
            _ => prefabs.mixedForestClusterPrefabs
        };

        return HasPrefab(typed) ? typed : prefabs.forestClusterPrefabs;
    }

    private void AddMountainDecoration(Chunk chunk, IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, Vector2Int coord, HexTileVisual tile, bool snowTerrain, int visualSeed)
    {
        var salt = HexSalt(coord);
        var mountainNeighbors = CountMatchingNeighbors(tiles, coord, IsMountainTerrain);
        var core = mountainNeighbors >= 5;
        var snowy = core || snowTerrain;
        var strength = Mathf.Clamp01(mountainNeighbors / 6f + (HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 7777) - 0.5f) * 0.12f);
        var rotation = Quaternion.Euler(0f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt) * 360f, 0f);
        var position = tile.World + Vector3.up * (terrainY + 0.015f);

        var isPeak = mountainNeighbors >= 2;
        var candidates = isPeak
            ? (snowy && HasPrefab(prefabs?.snowyMountainPeakPrefabs) ? prefabs.snowyMountainPeakPrefabs : prefabs?.mountainPeakPrefabs)
            : (snowy && HasPrefab(prefabs?.snowyRockyRidgePrefabs) ? prefabs.snowyRockyRidgePrefabs : prefabs?.rockyRidgePrefabs);
        var scale = isPeak ? Mathf.Lerp(0.62f, 1.32f, strength) : Mathf.Lerp(0.88f, 1.08f, strength);
        var matrix = Matrix4x4.TRS(position, rotation, Vector3.one * hexSize * scale);

        if (TryGetPrefabParts(candidates, visualSeed, coord.x, coord.y, salt, out var parts))
        {
            AddPrefabInstance(chunk, coord, parts, matrix);
            return;
        }

        AddFallbackMountain(chunk, coord, position, rotation, hexSize * Mathf.Lerp(0.7f, 1.3f, strength), snowy);
    }

    private void AddFoothillDecoration(Chunk chunk, IReadOnlyDictionary<Vector2Int, HexTileVisual> tiles, Vector2Int coord, HexTileVisual tile, bool hillHex, int visualSeed)
    {
        var salt = HexSalt(coord) + 25000;
        var mountainNeighbors = CountMatchingNeighbors(tiles, coord, IsMountainTerrain);
        var rotation = Quaternion.Euler(0f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt) * 360f, 0f);
        var position = tile.World + Vector3.up * (terrainY + 0.025f);
        var scale = hillHex ? 1f : 0.82f;
        var matrix = Matrix4x4.TRS(position, rotation, Vector3.one * hexSize * scale);

        if (TryGetPrefabParts(prefabs?.foothillsPrefabs, visualSeed, coord.x, coord.y, salt, out var parts))
        {
            AddPrefabInstance(chunk, coord, parts, matrix);
            return;
        }

        AddFallbackMountain(chunk, coord, position, rotation, hexSize * (hillHex ? 0.5f : 0.35f), false);
    }

    private void AddGroundScatterDecoration(Chunk chunk, Vector2Int coord, HexTileVisual tile, int visualSeed)
    {
        var salt = HexSalt(coord);
        var density = HexVisualHash.Value01(visualSeed, coord.x, coord.y, 30011);
        if (density > 0.62f)
        {
            return;
        }

        var baseY = terrainY + 0.01f;
        var tuftCount = tile.Terrain == HexTerrainKind.Hills ? 2 : 3;
        for (var i = 0; i < tuftCount; i++)
        {
            var angle = HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 30100 + i) * Mathf.PI * 2f;
            var radius = Mathf.Lerp(0.12f, 0.62f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, salt + 30200 + i)) * hexSize;
            var pos = tile.World + new Vector3(Mathf.Cos(angle) * radius, baseY, Mathf.Sin(angle) * radius);
            AddGrassTuft(chunk, coord, pos, visualSeed, salt + 30300 + i * 100);
        }

        if (tile.Terrain == HexTerrainKind.Grass && HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 31000) < 0.34f)
        {
            var angle = HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 31100) * Mathf.PI * 2f;
            var radius = Mathf.Lerp(0.1f, 0.5f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, salt + 31200)) * hexSize;
            var pos = tile.World + new Vector3(Mathf.Cos(angle) * radius, baseY, Mathf.Sin(angle) * radius);
            AddFlowerPatch(chunk, coord, pos, visualSeed, salt + 31300);
        }

        if (HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 32000) < 0.28f)
        {
            var angle = HexVisualHash.Value01(visualSeed, coord.x, coord.y, salt + 32100) * Mathf.PI * 2f;
            var radius = Mathf.Lerp(0.14f, 0.58f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, salt + 32200)) * hexSize;
            var pos = tile.World + new Vector3(Mathf.Cos(angle) * radius, baseY, Mathf.Sin(angle) * radius);
            AddPebble(chunk, coord, pos, visualSeed, salt + 32300);
        }
    }

    private void AddGrassTuft(Chunk chunk, Vector2Int coord, Vector3 tuftCenter, int visualSeed, int seedOffset)
    {
        var yaw = HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset) * 360f;
        var scale = Mathf.Lerp(0.8f, 1.25f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 3)) * hexSize;
        var amplitude = Mathf.Lerp(4f, 8f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 5));
        var speed = Mathf.Lerp(1.4f, 2.4f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 7));
        var phase = HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 9) * Mathf.PI * 2f;
        var sway = new Vector4(amplitude, speed, phase, 0f);

        const int blades = 3;
        for (var i = 0; i < blades; i++)
        {
            var bladeAngle = i * Mathf.PI * 2f / blades + HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 10 + i);
            var lean = Mathf.Lerp(6f, 20f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 20 + i));
            var height = Mathf.Lerp(0.08f, 0.15f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 30 + i)) * scale;
            var bladePos = tuftCenter + new Vector3(Mathf.Cos(bladeAngle) * 0.02f * scale, 0f, Mathf.Sin(bladeAngle) * 0.02f * scale);
            var rotation = Quaternion.Euler(yaw, 0f, 0f) * Quaternion.Euler(lean * Mathf.Cos(bladeAngle), bladeAngle * Mathf.Rad2Deg, lean * Mathf.Sin(bladeAngle));
            var matrix = Matrix4x4.TRS(bladePos, rotation, new Vector3(0.02f * scale, height, 0.02f * scale));
            var material = i == 1 ? NatureMaterial("GrassDark", "5f7c42") : NatureMaterial("GrassLight", "8ba95a");
            AddBatchInstance(chunk, unitConeMesh, material, matrix, sway, coord, isGroundScatter: true);
        }
    }

    private void AddFlowerPatch(Chunk chunk, Vector2Int coord, Vector3 patchCenter, int visualSeed, int seedOffset)
    {
        var scale = Mathf.Lerp(0.85f, 1.2f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 1)) * hexSize;
        var colorRoll = HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 2);
        var headMaterialKey = colorRoll < 0.4f ? ("FlowerWhite", "e9e7d6") : colorRoll < 0.75f ? ("FlowerYellow", "e6c64f") : ("FlowerRed", "c25b4c");
        var headMaterial = NatureMaterial(headMaterialKey.Item1, headMaterialKey.Item2);
        var stemMaterial = NatureMaterial("GrassDark", "5f7c42");

        var flowers = 2 + Mathf.FloorToInt(HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 3) * 2f);
        for (var i = 0; i < flowers; i++)
        {
            var angle = HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 10 + i) * Mathf.PI * 2f;
            var dist = HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 20 + i) * 0.08f * scale;
            var stemHeight = Mathf.Lerp(0.08f, 0.13f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset + 30 + i)) * scale;
            var offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            var stemMatrix = Matrix4x4.TRS(patchCenter + offset, Quaternion.identity, new Vector3(0.016f * scale, stemHeight, 0.016f * scale));
            AddBatchInstance(chunk, unitConeMesh, stemMaterial, stemMatrix, Vector4.zero, coord, isGroundScatter: true);

            var headPos = patchCenter + offset + Vector3.up * (stemHeight + 0.015f * scale);
            var headRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 45f);
            var headMatrix = Matrix4x4.TRS(headPos, headRotation, Vector3.one * 0.045f * scale);
            AddBatchInstance(chunk, unitBoxMesh, headMaterial, headMatrix, Vector4.zero, coord, isGroundScatter: true);
        }
    }

    private void AddPebble(Chunk chunk, Vector2Int coord, Vector3 position, int visualSeed, int seedOffset)
    {
        var size = Mathf.Lerp(0.05f, 0.1f, HexVisualHash.Value01(visualSeed, coord.x, coord.y, seedOffset)) * hexSize;
        var rotation = Quaternion.Euler(6f, HexVisualHash.Value01(visualSeed, coord.y, coord.x, seedOffset + 1) * 360f, -4f);
        var matrix = Matrix4x4.TRS(position + Vector3.up * (size * 0.3f), rotation, new Vector3(size, size * 0.55f, size * 1.2f));
        AddBatchInstance(chunk, unitBoxMesh, NatureMaterial("Pebble", "8b8a7c"), matrix, Vector4.zero, coord, isGroundScatter: true);
    }

    private void AddFallbackTree(Chunk chunk, Vector2Int coord, Vector3 position, float scale)
    {
        var trunk = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(scale * 0.12f, scale * 0.5f, scale * 0.12f));
        AddBatchInstance(chunk, unitConeMesh, FallbackMaterial("TreeTrunk", "4c3327"), trunk, Vector4.zero, coord, isGroundScatter: false);

        var crown = Matrix4x4.TRS(position + Vector3.up * (scale * 0.55f), Quaternion.identity, Vector3.one * scale * 0.8f);
        AddBatchInstance(chunk, unitConeMesh, FallbackMaterial("TreeCrown", "355741"), crown, Vector4.zero, coord, isGroundScatter: false);
    }

    private void AddFallbackMountain(Chunk chunk, Vector2Int coord, Vector3 position, Quaternion rotation, float scale, bool snowy)
    {
        var baseMatrix = Matrix4x4.TRS(position, rotation, new Vector3(scale, scale * 1.4f, scale));
        AddBatchInstance(chunk, unitConeMesh, FallbackMaterial("Rock", "777972"), baseMatrix, Vector4.zero, coord, isGroundScatter: false);

        if (snowy)
        {
            var capMatrix = Matrix4x4.TRS(position + Vector3.up * (scale * 1.05f), rotation, Vector3.one * scale * 0.42f);
            AddBatchInstance(chunk, unitConeMesh, FallbackMaterial("SnowCap", "d8dedb"), capMatrix, Vector4.zero, coord, isGroundScatter: false);
        }
    }

    private bool TryGetPrefabParts(GameObject[] candidates, int visualSeed, int seedA, int seedB, int seedOffset, out IReadOnlyList<HexDecorationPart> parts)
    {
        parts = null;
        if (!usePrefabOverrides || !HasPrefab(candidates))
        {
            return false;
        }

        var prefab = PickPrefab(candidates, visualSeed, seedA, seedB, seedOffset);
        if (prefab == null)
        {
            return false;
        }

        parts = decorationLibrary.Extract(prefab);
        return parts.Count > 0;
    }

    private void AddPrefabInstance(Chunk chunk, Vector2Int coord, IReadOnlyList<HexDecorationPart> parts, Matrix4x4 placement)
    {
        foreach (var part in parts)
        {
            AddBatchInstance(chunk, part.Mesh, part.Material, placement * part.LocalMatrix, Vector4.zero, coord, isGroundScatter: false);
        }
    }

    private static bool HasPrefab(GameObject[] candidates)
    {
        if (candidates == null)
        {
            return false;
        }

        for (var i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject PickPrefab(GameObject[] candidates, int visualSeed, int seedA, int seedB, int seedOffset)
    {
        var validCount = 0;
        for (var i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        var target = Mathf.FloorToInt(HexVisualHash.Value01(visualSeed, seedA, seedB, seedOffset) * validCount);
        var index = 0;
        for (var i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] == null)
            {
                continue;
            }

            if (index == target)
            {
                return candidates[i];
            }

            index++;
        }

        return candidates[0];
    }

    private static int HexSalt(Vector2Int coord)
    {
        unchecked
        {
            return coord.x * 7919 + coord.y * 104729;
        }
    }

    // ------------------------------------------------------------------ instance batching + submission

    private void AddBatchInstance(Chunk chunk, Mesh mesh, Material material, Matrix4x4 matrix, Vector4 sway, Vector2Int coord, bool isGroundScatter)
    {
        if (mesh == null || material == null)
        {
            return;
        }

        var key = (mesh, material);
        if (!chunk.Batches.TryGetValue(key, out var batch))
        {
            batch = new DecorationBatch(mesh, material, isGroundScatter);
            chunk.Batches[key] = batch;
        }

        batch.Coords.Add(coord);
        batch.Matrices.Add(matrix);
        batch.Sway.Add(sway);
    }

    private void RefreshVisibleInstances(Chunk chunk, Func<Vector2Int, bool> isDecorationVisible)
    {
        foreach (var batch in chunk.Batches.Values)
        {
            batch.VisibleMatrices.Clear();
            batch.VisibleSway.Clear();
            for (var i = 0; i < batch.Coords.Count; i++)
            {
                if (isDecorationVisible(batch.Coords[i]))
                {
                    batch.VisibleMatrices.Add(batch.Matrices[i]);
                    batch.VisibleSway.Add(batch.Sway[i]);
                }
            }
        }
    }

    private readonly List<Matrix4x4> submissionScratch = new(MaxInstancesPerDraw);
    private readonly List<Vector4> swayScratch = new(MaxInstancesPerDraw);
    private MaterialPropertyBlock propertyBlock;

    private void SubmitBatch(DecorationBatch batch)
    {
        if (batch.VisibleMatrices.Count == 0)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();

        var total = batch.VisibleMatrices.Count;
        for (var offset = 0; offset < total; offset += MaxInstancesPerDraw)
        {
            var count = Mathf.Min(MaxInstancesPerDraw, total - offset);
            submissionScratch.Clear();
            swayScratch.Clear();
            for (var i = 0; i < count; i++)
            {
                submissionScratch.Add(batch.VisibleMatrices[offset + i]);
                swayScratch.Add(batch.VisibleSway[offset + i]);
            }

            propertyBlock.Clear();
            if (batch.IsGroundScatter)
            {
                propertyBlock.SetVectorArray(SwayParamsId, swayScratch);
            }

            Graphics.DrawMeshInstanced(batch.Mesh, 0, batch.Material, submissionScratch, propertyBlock);
        }
    }

    private static readonly int SwayParamsId = Shader.PropertyToID("_SwayParams");

    // ------------------------------------------------------------------ canonical meshes + materials

    private void EnsureCanonicalMeshes()
    {
        unitConeMesh ??= HexCanonicalMeshes.Cone(1f, 0.22f, 1f);
        unitBoxMesh ??= HexCanonicalMeshes.Box(Vector3.one);
        unitHexDiscMesh ??= HexCanonicalMeshes.HexDisc(1f);
    }

    private Material NatureMaterial(string key, string hex)
    {
        if (natureMaterials.TryGetValue(key, out var material))
        {
            return material;
        }

        material = new Material(natureShaderTemplate)
        {
            name = "Nature_" + key,
            hideFlags = HideFlags.DontSave
        };
        material.SetColor("_BaseColor", ColorFromHex(hex));
        natureMaterials[key] = material;
        return material;
    }

    private Material FallbackMaterial(string key, string hex)
    {
        if (fallbackMaterials.TryGetValue(key, out var material))
        {
            return material;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader)
        {
            name = "Fallback_" + key,
            color = ColorFromHex(hex),
            hideFlags = HideFlags.DontSave,
            enableInstancing = true
        };
        fallbackMaterials[key] = material;
        return material;
    }

    private static Color ColorFromHex(string hex)
    {
        return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.magenta;
    }

    // ------------------------------------------------------------------ small utilities

    private static GameObject NewChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static int FloorDiv(int value, int divisor)
    {
        return value >= 0 ? value / divisor : (value - divisor + 1) / divisor;
    }

    private static void DestroyMesh(Mesh mesh)
    {
        if (mesh == null)
        {
            return;
        }

        DestroyObject(mesh);
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    // ------------------------------------------------------------------ nested data

    private sealed class Chunk
    {
        public Chunk(Vector2Int key, Transform root)
        {
            Key = key;
            Root = root;
        }

        public Vector2Int Key { get; }
        public Transform Root { get; }
        public List<Vector2Int> Coords { get; } = new();
        public Bounds WorldBounds;

        public GameObject TerrainObject;
        public MeshFilter TerrainFilter;
        public Mesh TerrainMesh;

        public GameObject FogUnknownObject;
        public MeshFilter FogUnknownFilter;
        public Mesh FogUnknownMesh;

        public GameObject FogReportedObject;
        public MeshFilter FogReportedFilter;
        public Mesh FogReportedMesh;

        public Dictionary<(Mesh, Material), DecorationBatch> Batches { get; } = new();
    }

    private sealed class DecorationBatch
    {
        public DecorationBatch(Mesh mesh, Material material, bool isGroundScatter)
        {
            Mesh = mesh;
            Material = material;
            IsGroundScatter = isGroundScatter;
        }

        public Mesh Mesh { get; }
        public Material Material { get; }
        public bool IsGroundScatter { get; }
        public List<Vector2Int> Coords { get; } = new();
        public List<Matrix4x4> Matrices { get; } = new();
        public List<Vector4> Sway { get; } = new();
        public List<Matrix4x4> VisibleMatrices { get; } = new();
        public List<Vector4> VisibleSway { get; } = new();
    }
}
