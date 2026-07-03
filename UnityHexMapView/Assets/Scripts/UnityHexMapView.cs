using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class UnityHexMapView : MonoBehaviour
{
    private enum TerrainKind
    {
        Water,
        Coast,
        Grass,
        Forest,
        Hills,
        Mountain,
        Snow
    }

    private struct TileData
    {
        public TerrainKind Terrain;
        public float Elevation;
        public Vector3 World;
    }

    [Range(3, 32)] public int mapRadius = 16;
    [Range(0.5f, 2f)] public float hexSize = 1f;
    public int mapSeed = 4711;
    [Range(0f, 0.12f)] public float cameraOrbitSpeed = 0.018f;
    [Range(3f, 30f)] public float zoomedInSize = 5f;
    [Range(8f, 60f)] public float zoomedOutSize = 24f;
    [Range(0.5f, 8f)] public float zoomSpeed = 3.5f;
    public bool showDebugHexGrid = false;
    public bool showSelectionPreview = true;
    public Vector2Int selectedPreviewHex = Vector2Int.zero;
    [Range(0, 8)] public int reachablePreviewRadius = 2;

    private const float Sqrt3 = 1.73205080757f;
    private const float VisualTileTopY = 0.08f;
    private const float VisualTileBottomY = -0.035f;
    private readonly Dictionary<Vector2Int, TileData> tiles = new();
    private readonly Dictionary<TerrainKind, Material> topMaterials = new();
    private readonly Dictionary<TerrainKind, Material> sideMaterials = new();
    private readonly Dictionary<string, Material> featureMaterials = new();
    private Transform cameraRig;
    private Camera strategyCamera;
#if UNITY_EDITOR
    private bool editorRebuildQueued;
#endif

    private static readonly Vector2Int[] Directions =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    };

    private void OnEnable()
    {
        RequestRebuild();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            RequestRebuild();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (cameraRig != null)
        {
            cameraRig.Rotate(Vector3.up, cameraOrbitSpeed * Mathf.Rad2Deg * Time.deltaTime, Space.World);
        }

        UpdateCameraZoom();
    }

    [ContextMenu("Rebuild Hex Map")]
    public void Rebuild()
    {
        ClearGeneratedChildren();
        tiles.Clear();
        topMaterials.Clear();
        sideMaterials.Clear();
        featureMaterials.Clear();
        strategyCamera = null;

        CreateMaterials();
        BuildMap();
        BuildTerrainObjectGroups();
        BuildFeatures();
        BuildHexOverlays();
        BuildWaterPlane();
        BuildLighting();
        BuildCamera();
    }

    private void RequestRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }
#endif
        Rebuild();
    }

#if UNITY_EDITOR
    private void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += RebuildFromEditorDelay;
    }

    private void RebuildFromEditorDelay()
    {
        editorRebuildQueued = false;
        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        Rebuild();
    }
#endif

    private void ClearGeneratedChildren()
    {
        var children = new List<GameObject>();
        for (var i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i).gameObject);
        }

        foreach (var child in children)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void CreateMaterials()
    {
        topMaterials[TerrainKind.Water] = Material("Water", "3f7180", "355f6c", "527f88", 11, 0.18f);
        topMaterials[TerrainKind.Coast] = Material("Coast", "b49463", "9a7c50", "c0a472", 12, 0.18f);
        topMaterials[TerrainKind.Grass] = Material("Grass", "6d8350", "576f42", "819466", 13, 0.2f);
        topMaterials[TerrainKind.Forest] = Material("Forest", "36583f", "263f31", "49654a", 14, 0.18f);
        topMaterials[TerrainKind.Hills] = Material("Hills", "7a7456", "67614a", "898365", 15, 0.18f);
        topMaterials[TerrainKind.Mountain] = Material("Mountain", "777972", "62655f", "888a83", 16, 0.14f);
        topMaterials[TerrainKind.Snow] = Material("Snow", "c9cec6", "b8beb6", "d9ddd4", 17, 0.1f);

        sideMaterials[TerrainKind.Water] = Material("Water Side", "2f535d", 0.7f);
        sideMaterials[TerrainKind.Coast] = Material("Coast Side", "7e6748", 0.85f);
        sideMaterials[TerrainKind.Grass] = Material("Grass Side", "465c37", 0.9f);
        sideMaterials[TerrainKind.Forest] = Material("Forest Side", "223829", 0.92f);
        sideMaterials[TerrainKind.Hills] = Material("Hills Side", "514d3b", 0.94f);
        sideMaterials[TerrainKind.Mountain] = Material("Mountain Side", "4f524d", 0.94f);
        sideMaterials[TerrainKind.Snow] = Material("Snow Side", "878e87", 0.86f);

        featureMaterials["DebugHex"] = TransparentMaterial("Debug Hex", "101916", 0.32f);
        featureMaterials["ReachableHex"] = TransparentMaterial("Reachable Hex", "a8c884", 0.24f);
        featureMaterials["SelectedHex"] = EmissiveMaterial("Selected Hex", "ded69a", "f2e5a7", 0.22f);
        featureMaterials["RiverBank"] = Material("River Bank", "3f5d5c", 0.82f);
        featureMaterials["River"] = EmissiveMaterial("River", "57919b", "8fc8cf", 0.04f);
        featureMaterials["RiverFoam"] = TransparentMaterial("River Foam", "d8ede8", 0.11f);
        featureMaterials["RoadShadow"] = TransparentMaterial("Road Bed", "4c3c2a", 0.2f);
        featureMaterials["Road"] = Material("Road", "8b6d48", "6f5438", "9c815b", 41, 0.12f);
        featureMaterials["RoadCenter"] = TransparentMaterial("Road Center", "c3aa78", 0.11f);
        featureMaterials["Border"] = EmissiveMaterial("Territory Border", "4b9aaa", "88c8d1", 0.08f);
        featureMaterials["SettlementWall"] = Material("Settlement Wall", "766f5e", 0.9f);
        featureMaterials["SettlementRoof"] = Material("Settlement Roof", "79425f", 0.82f);
        featureMaterials["SettlementRoofWarm"] = Material("Settlement Roof Warm", "9b6240", 0.82f);
        featureMaterials["Smoke"] = TransparentMaterial("Smoke", "c2b9a8", 0.18f);
        featureMaterials["TreeTrunk"] = Material("Tree Trunk", "4c3327", 0.82f);
        featureMaterials["TreeCrown"] = Material("Tree Crown", "2a613f", 0.86f);
        featureMaterials["TreeCrownDark"] = Material("Tree Crown Dark", "1b3f2d", 0.9f);
        featureMaterials["ForestMass"] = TransparentMaterial("Forest Mass", "17281d", 0.34f);
        featureMaterials["ForestFloor"] = TransparentMaterial("Forest Floor", "1e3425", 0.26f);
        featureMaterials["Rock"] = Material("Rock", "777b71", 0.9f);
        featureMaterials["DarkRock"] = Material("Dark Rock", "383c36", 0.92f);
        featureMaterials["RidgeBase"] = Material("Ridge Base", "56574e", 0.92f);
        featureMaterials["RidgeSnow"] = TransparentMaterial("Ridge Snow", "d8d8bf", 0.5f);
        featureMaterials["SnowCap"] = Material("Snow Cap", "d8dedb", 0.72f);
        featureMaterials["Flag"] = Material("Flag", "c4574d", 0.62f);
        featureMaterials["Gold"] = Material("Ore Gold", "c89d3b", 0.58f);
        featureMaterials["MineWood"] = Material("Mine Wood", "5d422d", 0.86f);
        featureMaterials["WallStone"] = Material("Ancient Wall Stone", "8d8772", 0.9f);
        featureMaterials["TowerRoof"] = Material("Tower Roof", "4e5267", 0.78f);
        featureMaterials["WaterPlane"] = Material("Distant Water", "3b5f63", 0.44f);
    }

    private Material Material(string name, string hex, float smoothness)
    {
        return Material(name, ColorFromHex(hex), smoothness);
    }

    private Material Material(string name, string hex, string baseHex, string accentHex, int seedOffset, float strength)
    {
        return Material(name, ColorFromHex(hex), 0.88f, NoiseTexture(baseHex, accentHex, seedOffset, strength));
    }

    private Material Material(string name, Color color, float smoothness, Texture2D texture = null)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = name,
            color = color,
            hideFlags = HideFlags.DontSave
        };

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - smoothness));
        }

        if (material.HasProperty("_Roughness"))
        {
            material.SetFloat("_Roughness", smoothness);
        }

        return material;
    }

    private Material TransparentMaterial(string name, string hex, float alpha)
    {
        var color = ColorFromHex(hex);
        color.a = alpha;
        var material = Material(name, color, 0.9f);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        return material;
    }

    private Material EmissiveMaterial(string name, string hex, string emissionHex, float energy)
    {
        var material = Material(name, ColorFromHex(hex), 0.62f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", ColorFromHex(emissionHex) * energy);
        }

        return material;
    }

    private Texture2D NoiseTexture(string baseHex, string accentHex, int seedOffset, float strength)
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            hideFlags = HideFlags.DontSave
        };

        var baseColor = ColorFromHex(baseHex);
        var accentColor = ColorFromHex(accentHex);
        var offset = (mapSeed + seedOffset * 997) * 0.017f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var large = Mathf.PerlinNoise(x * 0.065f + offset, y * 0.065f - offset);
                var fine = Mathf.Sin(x * 0.73f + y * 0.31f + seedOffset) * 0.045f;
                var amount = Mathf.Clamp01(large * strength + fine + 0.08f);
                texture.SetPixel(x, y, Color.Lerp(baseColor, accentColor, amount));
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    private void BuildMap()
    {
        for (var q = -mapRadius; q <= mapRadius; q++)
        {
            var rMin = Mathf.Max(-mapRadius, -q - mapRadius);
            var rMax = Mathf.Min(mapRadius, -q + mapRadius);
            for (var r = rMin; r <= rMax; r++)
            {
                var coord = new Vector2Int(q, r);
                var world = AxialToWorld(coord);
                var radialFade = Mathf.Clamp01(new Vector2(world.x, world.z).magnitude / (mapRadius * 1.7f));
                var heightNoise = Noise(q * 0.18f, r * 0.18f, 0) - radialFade * 0.08f;
                var terrain = PickTerrain(heightNoise, coord);
                var mountainRange = MountainRangeStrength(coord);
                if (mountainRange > 0.82f)
                {
                    terrain = TerrainKind.Snow;
                }
                else if (mountainRange > 0.48f)
                {
                    terrain = TerrainKind.Mountain;
                }
                else if (mountainRange > 0.26f && terrain != TerrainKind.Water)
                {
                    terrain = TerrainKind.Hills;
                }

                var elevation = TerrainElevation(terrain, heightNoise);
                tiles[coord] = new TileData { Terrain = terrain, Elevation = elevation, World = world };

                var tile = NewChild($"Hex_{q}_{r}_{terrain}");
                tile.transform.localPosition = new Vector3(world.x, 0f, world.z);
                AddMesh(tile, "Top", HexTopMesh(hexSize, VisualTileTopY), topMaterials[terrain]);
                AddTileDetails(tile.transform, terrain, VisualTileTopY, coord);
            }
        }
    }

    private TerrainKind PickTerrain(float value, Vector2Int coord)
    {
        var h = value + Mathf.Sin(coord.x * 0.53f) * 0.04f + Mathf.Cos(coord.y * 0.71f) * 0.03f;
        if (h < -0.38f) return TerrainKind.Water;
        if (h < -0.24f) return TerrainKind.Coast;
        if (h < 0.12f) return TerrainKind.Grass;
        if (h < 0.29f) return TerrainKind.Forest;
        if (h < 0.46f) return TerrainKind.Hills;
        if (h < 0.62f) return TerrainKind.Mountain;
        return TerrainKind.Snow;
    }

    private float MountainRangeStrength(Vector2Int coord)
    {
        var q = coord.x;
        if (q < -14 || q > 13)
        {
            return 0f;
        }

        var ridgeR = -0.45f * q + 4.1f + Mathf.Sin((q + mapSeed * 0.01f) * 0.62f) * 1.25f;
        var distance = Mathf.Abs(coord.y - ridgeR);
        var ruggedness = Hash01(coord.x, coord.y, 8123) * 0.24f - 0.08f;
        var pass = Mathf.Abs(q + 4) < 1.2f || Mathf.Abs(q - 6) < 1.1f ? 0.22f : 0f;
        return Mathf.Clamp01(1f - distance / 3.1f + ruggedness - pass);
    }

    private static float TerrainElevation(TerrainKind terrain, float value)
    {
        if (terrain == TerrainKind.Water) return 0.06f;
        if (terrain == TerrainKind.Coast) return 0.18f;

        var raw = terrain switch
        {
            TerrainKind.Grass => 0.34f + Mathf.Max(value, 0f) * 0.28f,
            TerrainKind.Forest => 0.48f + Mathf.Max(value, 0f) * 0.34f,
            TerrainKind.Hills => 0.76f + value * 0.36f,
            TerrainKind.Mountain => 1.08f + value * 0.48f,
            TerrainKind.Snow => 1.36f + value * 0.48f,
            _ => 0.3f
        };
        return Mathf.Round(raw / 0.18f) * 0.18f;
    }

    private void AddTileDetails(Transform parent, TerrainKind terrain, float elevation, Vector2Int coord)
    {
        if (terrain == TerrainKind.Hills)
        {
            AddRock(parent, elevation, -0.28f, 0.08f, 0);
            AddRock(parent, elevation, 0.22f, -0.18f, 1);
        }
        else if (terrain == TerrainKind.Coast && Mathf.Abs(coord.x * 3 + coord.y * 5) % 7 == 0)
        {
            AddFlag(parent, elevation);
        }
    }

    private void BuildTerrainObjectGroups()
    {
        BuildForestRegions();
        BuildMountainRanges();
    }

    private void BuildForestRegions()
    {
        var regions = FindTerrainRegions(IsForestTerrain);
        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            var root = NewChild($"ForestRegion_{regionIndex:D2}");
            AddForestRegionGround(root, region, regionIndex);

            var treeTotal = Mathf.Clamp(Mathf.RoundToInt(region.Count * 4.8f), 8, 150);
            for (var treeIndex = 0; treeIndex < treeTotal; treeIndex++)
            {
                var coord = PickRegionCoord(region, regionIndex, treeIndex, 5);
                if (!tiles.TryGetValue(coord, out var tile))
                {
                    continue;
                }

                var interior = CountMatchingNeighbors(coord, IsForestTerrain) >= 4;
                var angle = Hash01(coord.x, coord.y, 21000 + treeIndex) * Mathf.PI * 2f;
                var radius = Mathf.Lerp(0.08f, interior ? 0.92f : 0.7f, Hash01(coord.y, coord.x, 21100 + treeIndex)) * hexSize;
                var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var leanToNeighbor = ForestNeighborOffset(coord, treeIndex) * Mathf.Lerp(0.08f, interior ? 0.42f : 0.58f, Hash01(coord.x, coord.y, 21200 + treeIndex));
                var position = tile.World + offset + leanToNeighbor + Vector3.up * (VisualTileTopY + 0.04f);
                AddTreeAt(root.transform, position, coord, treeIndex);
            }
        }
    }

    private void AddForestRegionGround(GameObject root, IReadOnlyList<Vector2Int> region, int regionIndex)
    {
        if (region.Count == 0)
        {
            return;
        }

        var blobCount = Mathf.Clamp(Mathf.CeilToInt(region.Count / 5f), 1, 12);
        for (var i = 0; i < blobCount; i++)
        {
            var centerCoord = PickRegionCoord(region, regionIndex, i, 17);
            if (!tiles.TryGetValue(centerCoord, out var centerTile))
            {
                continue;
            }

            var neighborCount = CountMatchingNeighbors(centerCoord, IsForestTerrain);
            var radius = Mathf.Lerp(1.05f, 2.25f, Mathf.Clamp01((neighborCount + region.Count * 0.08f) / 7f)) * hexSize;
            var y = VisualTileTopY + 0.024f + i * 0.001f;
            AddMesh(root, $"ForestFloor_{i}", IrregularDiskMesh(centerTile.World, radius, y, 14, regionIndex * 101 + i), featureMaterials["ForestFloor"]);

            if (region.Count > 2)
            {
                AddMesh(root, $"ForestMass_{i}", IrregularDiskMesh(centerTile.World + Vector3.up * 0.03f, radius * 0.8f, y + 0.08f, 12, regionIndex * 151 + i), featureMaterials["ForestMass"]);
            }
        }
    }

    private Vector3 ForestNeighborOffset(Vector2Int coord, int index)
    {
        for (var i = 0; i < Directions.Length; i++)
        {
            var direction = Directions[(i + index) % Directions.Length];
            var neighbor = coord + direction;
            if (tiles.TryGetValue(neighbor, out var tile) && IsForestTerrain(tile.Terrain))
            {
                var neighborWorld = AxialToWorld(neighbor);
                var centerWorld = AxialToWorld(coord);
                return (neighborWorld - centerWorld).normalized * hexSize;
            }
        }

        return Vector3.zero;
    }

    private void BuildMountainRanges()
    {
        var regions = FindTerrainRegions(IsMountainTerrain);
        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            var root = NewChild($"MountainRange_{regionIndex:D2}");
            AddMountainRidgeBase(root, region, regionIndex);

            var maxPeaks = Mathf.Clamp(Mathf.CeilToInt(region.Count * 0.42f), 2, 24);
            var peakIndex = 0;

            region.Sort((a, b) => MountainRangeStrength(b).CompareTo(MountainRangeStrength(a)));
            for (var i = 0; i < region.Count && peakIndex < maxPeaks; i += 2)
            {
                var coord = region[i];
                if (!tiles.TryGetValue(coord, out var tile))
                {
                    continue;
                }

                var ridgeStrength = Mathf.Max(MountainRangeStrength(coord), CountMatchingNeighbors(coord, IsMountainTerrain) / 6f);
                var skipChance = Mathf.Lerp(0.68f, 0.22f, ridgeStrength);
                if (peakIndex > 2 && Hash01(coord.x, coord.y, 22000) < skipChance)
                {
                    continue;
                }

                var angle = Hash01(coord.x, coord.y, 22100) * Mathf.PI * 2f;
                var radius = Mathf.Lerp(0.04f, 0.32f, Hash01(coord.y, coord.x, 22200)) * hexSize;
                var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var position = tile.World + offset;
                AddMountainAt(root.transform, position, coord, tiles[coord].Terrain == TerrainKind.Snow, ridgeStrength);
                peakIndex++;
            }
        }
    }

    private void AddMountainRidgeBase(GameObject root, List<Vector2Int> region, int regionIndex)
    {
        if (region.Count < 2)
        {
            return;
        }

        var path = new List<Vector3>();
        var sorted = new List<Vector2Int>(region);
        sorted.Sort((a, b) =>
        {
            var pa = a.x * 1.2f + a.y * 0.45f;
            var pb = b.x * 1.2f + b.y * 0.45f;
            return pa.CompareTo(pb);
        });

        var step = Mathf.Max(1, sorted.Count / 18);
        for (var i = 0; i < sorted.Count; i += step)
        {
            if (tiles.TryGetValue(sorted[i], out var tile))
            {
                path.Add(tile.World + Vector3.up * (VisualTileTopY + 0.035f));
            }
        }

        if (path.Count < 2 && tiles.TryGetValue(sorted[^1], out var lastTile))
        {
            path.Add(lastTile.World + Vector3.up * (VisualTileTopY + 0.035f));
        }

        if (path.Count < 2)
        {
            return;
        }

        var smoothPath = SmoothPath(path, 3);
        AddMesh(root, $"RidgeBase_{regionIndex}", RibbonMesh(smoothPath, Mathf.Lerp(1.4f, 2.2f, Mathf.Clamp01(region.Count / 24f)) * hexSize), featureMaterials["RidgeBase"]);

        if (RegionHasSnow(region))
        {
            AddMesh(root, $"RidgeSnow_{regionIndex}", RibbonMesh(RaisePoints(smoothPath, 0.025f), Mathf.Lerp(0.72f, 1.15f, Mathf.Clamp01(region.Count / 24f)) * hexSize), featureMaterials["RidgeSnow"]);
        }
    }

    private bool RegionHasSnow(IReadOnlyList<Vector2Int> region)
    {
        foreach (var coord in region)
        {
            if (tiles.TryGetValue(coord, out var tile) && tile.Terrain == TerrainKind.Snow)
            {
                return true;
            }
        }

        return false;
    }

    private List<List<Vector2Int>> FindTerrainRegions(TerrainMatcher matcher)
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

    private int CountMatchingNeighbors(Vector2Int coord, TerrainMatcher matcher)
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

    private Vector2Int PickRegionCoord(IReadOnlyList<Vector2Int> region, int regionIndex, int index, int salt)
    {
        if (region.Count == 0)
        {
            return Vector2Int.zero;
        }

        unchecked
        {
            var h = mapSeed;
            h = h * 73856093 ^ regionIndex * 19349663;
            h = h * 83492791 ^ index * 297121507;
            h = h * 1103515245 ^ salt * 12345;
            return region[(h & 0x7fffffff) % region.Count];
        }
    }

    private Mesh IrregularDiskMesh(Vector3 center, float radius, float y, int segments, int seedOffset)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        vertices.Add(new Vector3(center.x, y, center.z));
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.72f, 1.18f, Hash01(seedOffset, i, 23000));
            var pointRadius = radius * wobble;
            var point = new Vector3(center.x + Mathf.Cos(angle) * pointRadius, y, center.z + Mathf.Sin(angle) * pointRadius);
            vertices.Add(point);
            uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
        }

        for (var i = 0; i < segments; i++)
        {
            triangles.Add(0);
            triangles.Add(1 + (i + 1) % segments);
            triangles.Add(1 + i);
        }

        return MeshFrom(vertices, uvs, triangles, "IrregularDisk");
    }

    private delegate bool TerrainMatcher(TerrainKind terrain);

    private static bool IsForestTerrain(TerrainKind terrain)
    {
        return terrain == TerrainKind.Forest;
    }

    private static bool IsMountainTerrain(TerrainKind terrain)
    {
        return terrain == TerrainKind.Mountain || terrain == TerrainKind.Snow;
    }

    private void AddTree(Transform parent, float elevation, Vector2Int coord, int index)
    {
        var angle = index * 2.1f + (Hash01(coord.x, coord.y, index) - 0.5f) * 0.7f;
        var distance = Mathf.Lerp(0.18f, 0.52f, Hash01(coord.y, coord.x, index + 31)) * hexSize;
        AddTreeAt(parent, new Vector3(Mathf.Cos(angle) * distance, elevation + 0.04f, Mathf.Sin(angle) * distance), coord, index);
    }

    private void AddTreeAt(Transform parent, Vector3 localPosition, Vector2Int coord, int index)
    {
        var root = NewChild("Tree", parent);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, Hash01(coord.x, coord.y, index + 91) * 360f, 0f);
        var scale = Mathf.Lerp(0.78f, 1.28f, Hash01(coord.x, coord.y, index + 151));
        root.transform.localScale = Vector3.one * scale;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.13f, 0f);
        trunk.transform.localScale = new Vector3(0.07f, 0.13f, 0.07f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["TreeTrunk"];

        var style = Mathf.Abs(coord.x * 11 + coord.y * 7 + index) % 4;
        var crownMaterial = style == 0 || style == 3 ? featureMaterials["TreeCrownDark"] : featureMaterials["TreeCrown"];
        if (style == 0)
        {
            var lower = CreateCone("LowerCrown", 0.32f, 0.04f, 0.34f, crownMaterial);
            lower.transform.SetParent(root.transform, false);
            lower.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            lower.transform.localRotation = Quaternion.Euler(-3f, 30f + index * 17f, 3f);

            var upper = CreateCone("UpperCrown", 0.22f, 0.03f, 0.28f, crownMaterial);
            upper.transform.SetParent(root.transform, false);
            upper.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            upper.transform.localRotation = Quaternion.Euler(2f, 10f + index * 29f, -2f);
        }
        else if (style == 1)
        {
            var crown = CreateCone("RoundCrown", 0.27f, 0.2f, 0.32f, crownMaterial);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.43f, 0f);
            crown.transform.localRotation = Quaternion.Euler(0f, 30f + index * 17f, 0f);
        }
        else if (style == 2)
        {
            var crown = CreateCone("TallCrown", 0.24f, 0.02f, 0.62f, crownMaterial);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            crown.transform.localRotation = Quaternion.Euler(-2f, 30f + index * 17f, 2f);
        }
        else
        {
            for (var lobe = 0; lobe < 3; lobe++)
            {
                var lobeAngle = lobe * Mathf.PI * 2f / 3f + index * 0.3f;
                var crown = CreateCone("ClusterCrown", 0.17f, 0.12f, 0.24f, crownMaterial);
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition = new Vector3(Mathf.Cos(lobeAngle) * 0.1f, 0.42f + lobe * 0.035f, Mathf.Sin(lobeAngle) * 0.1f);
                crown.transform.localRotation = Quaternion.Euler(0f, lobeAngle * Mathf.Rad2Deg, 0f);
            }
        }
    }

    private void AddRock(Transform parent, float elevation, float x, float z, int index)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "Rock";
        rock.transform.SetParent(parent, false);
        rock.transform.localPosition = new Vector3(x, elevation + 0.09f + index * 0.03f, z);
        rock.transform.localScale = new Vector3(0.22f, 0.14f + index * 0.06f, 0.28f);
        rock.transform.localRotation = Quaternion.Euler(7f, 25f + index * 40f, 4f);
        rock.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Rock"];
    }

    private void AddMountain(Transform parent, float elevation, bool snowy, Vector2Int coord)
    {
        AddMountainAt(parent, Vector3.zero, coord, snowy, Mathf.Max(0.38f, MountainRangeStrength(coord)), elevation);
    }

    private void AddMountainAt(Transform parent, Vector3 localPosition, Vector2Int coord, bool snowy, float ridgeStrength)
    {
        AddMountainAt(parent, localPosition, coord, snowy, ridgeStrength, VisualTileTopY);
    }

    private void AddMountainAt(Transform parent, Vector3 localPosition, Vector2Int coord, bool snowy, float ridgeStrength, float elevation)
    {
        ridgeStrength = Mathf.Clamp01(Mathf.Max(0.32f, ridgeStrength));
        var rotation = Hash01(coord.x, coord.y, 5021) * 360f;
        var height = Mathf.Lerp(1.08f, 2.05f, ridgeStrength) * hexSize;
        var baseRadius = Mathf.Lerp(0.56f, 0.9f, ridgeStrength) * hexSize;
        var capHeight = height * 0.28f;

        var root = NewChild("Mountain", parent);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);

        var baseCone = CreateCone("MountainBase", baseRadius, 0.06f * hexSize, height, featureMaterials["Rock"]);
        baseCone.transform.SetParent(root.transform, false);
        baseCone.transform.localPosition = new Vector3(0f, elevation + height * 0.5f, 0f);
        baseCone.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);

        var cap = CreateCone("Peak", baseRadius * 0.44f, 0.01f * hexSize, capHeight, snowy ? featureMaterials["SnowCap"] : featureMaterials["Rock"]);
        cap.transform.SetParent(root.transform, false);
        cap.transform.localPosition = new Vector3(0f, elevation + height - capHeight * 0.5f, 0f);
        cap.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);

        if (ridgeStrength < 0.55f)
        {
            return;
        }

        for (var i = 0; i < 2; i++)
        {
            var angle = i * Mathf.PI + Hash01(coord.x, coord.y, 6200 + i) * 0.9f;
            var spurHeight = height * Mathf.Lerp(0.45f, 0.68f, Hash01(coord.y, coord.x, 6400 + i));
            var spurRadius = baseRadius * Mathf.Lerp(0.38f, 0.5f, Hash01(coord.x, coord.y, 6600 + i));
            var spurMaterial = i == 0 ? featureMaterials["Rock"] : featureMaterials["DarkRock"];
            var spur = CreateCone("RidgePeak", spurRadius, 0.04f * hexSize, spurHeight, spurMaterial);
            spur.transform.SetParent(root.transform, false);
            spur.transform.localPosition = new Vector3(Mathf.Cos(angle) * baseRadius * 0.52f, elevation + spurHeight * 0.5f, Mathf.Sin(angle) * baseRadius * 0.52f);
            spur.transform.localRotation = Quaternion.Euler(0f, 20f + i * 50f, 0f);
        }

        for (var i = 0; i < 3; i++)
        {
            var angle = i * Mathf.PI * 2f / 3f + Hash01(coord.x, coord.y, 6800 + i) * 0.35f;
            var boulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boulder.name = "MountainBoulder";
            boulder.transform.SetParent(root.transform, false);
            boulder.transform.localPosition = new Vector3(Mathf.Cos(angle) * baseRadius * 0.72f, elevation + 0.07f, Mathf.Sin(angle) * baseRadius * 0.72f);
            boulder.transform.localScale = new Vector3(baseRadius * 0.28f, baseRadius * 0.18f, baseRadius * 0.34f);
            boulder.transform.localRotation = Quaternion.Euler(8f, angle * Mathf.Rad2Deg + 23f, -5f);
            boulder.GetComponent<MeshRenderer>().sharedMaterial = i == 1 ? featureMaterials["DarkRock"] : featureMaterials["Rock"];
        }
    }

    private void AddFlag(Transform parent, float elevation)
    {
        var root = NewChild("CoastMarker", parent);
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0.12f, elevation + 0.36f, -0.18f);
        pole.transform.localScale = new Vector3(0.035f, 0.36f, 0.035f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["TreeTrunk"];

        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        flag.transform.SetParent(root.transform, false);
        flag.transform.localPosition = new Vector3(0.28f, elevation + 0.58f, -0.18f);
        flag.transform.localScale = new Vector3(0.34f, 0.2f, 0.035f);
        flag.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Flag"];
    }

    private void BuildFeatures()
    {
        BuildRiver(new[]
        {
            new Vector2Int(-8, 1), new Vector2Int(-7, 1), new Vector2Int(-6, 0), new Vector2Int(-5, 0),
            new Vector2Int(-4, 1), new Vector2Int(-3, 1), new Vector2Int(-2, 2), new Vector2Int(-1, 2),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(4, -1), new Vector2Int(5, -1), new Vector2Int(6, -2), new Vector2Int(7, -2)
        });
        BuildRiver(new[]
        {
            new Vector2Int(-2, -6), new Vector2Int(-1, -6), new Vector2Int(0, -6), new Vector2Int(0, -5),
            new Vector2Int(1, -5), new Vector2Int(1, -4), new Vector2Int(2, -4), new Vector2Int(2, -3),
            new Vector2Int(3, -3), new Vector2Int(4, -4)
        });

        var settlements = new[]
        {
            new Vector2Int(-5, 2), new Vector2Int(-1, 0), new Vector2Int(3, -2), new Vector2Int(4, 3), new Vector2Int(-3, 5)
        };
        foreach (var settlement in settlements)
        {
            BuildSettlement(settlement);
        }

        BuildRoad(new[]
        {
            new Vector2Int(-5, 2), new Vector2Int(-4, 2), new Vector2Int(-3, 2), new Vector2Int(-2, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -2)
        });
        BuildRoad(new[]
        {
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(1, 2),
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 3)
        });
        BuildRoad(new[] { new Vector2Int(-5, 2), new Vector2Int(-5, 3), new Vector2Int(-4, 4), new Vector2Int(-3, 5) });
        BuildTerritoryBorder(new[]
        {
            new Vector2Int(-6, 3), new Vector2Int(-5, 2), new Vector2Int(-4, 2), new Vector2Int(-3, 1),
            new Vector2Int(-2, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, -1),
            new Vector2Int(2, -1), new Vector2Int(3, -2), new Vector2Int(4, -2)
        });

        BuildTower(new Vector2Int(9, 2));
        BuildMine(new Vector2Int(-10, -2));
        BuildGreatWall(new[]
        {
            new Vector2Int(-12, 10), new Vector2Int(-11, 9), new Vector2Int(-10, 9), new Vector2Int(-9, 8),
            new Vector2Int(-8, 8), new Vector2Int(-7, 7), new Vector2Int(-6, 7), new Vector2Int(-5, 6)
        });
    }

    private void BuildHexOverlays()
    {
        if (showDebugHexGrid)
        {
            foreach (var coord in tiles.Keys)
            {
                AddHexOverlay(coord, "DebugHex", 0.988f, 0.94f, 0.055f, featureMaterials["DebugHex"]);
            }
        }

        if (!showSelectionPreview)
        {
            return;
        }

        foreach (var coord in tiles.Keys)
        {
            if (coord == selectedPreviewHex)
            {
                continue;
            }

            if (HexDistance(coord, selectedPreviewHex) <= reachablePreviewRadius)
            {
                AddHexOverlay(coord, "ReachableHex", 0.972f, 0.91f, 0.07f, featureMaterials["ReachableHex"]);
            }
        }

        AddHexOverlay(selectedPreviewHex, "SelectedHex", 1.005f, 0.88f, 0.09f, featureMaterials["SelectedHex"]);
    }

    private void AddHexOverlay(Vector2Int coord, string name, float outerScale, float innerScale, float yOffset, Material material)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var overlay = NewChild($"{name}_{coord.x}_{coord.y}");
        overlay.transform.localPosition = tile.World;
        AddMesh(overlay, name, HexRingMesh(hexSize * outerScale, hexSize * innerScale, VisualTileTopY + yOffset), material);
    }

    private static int HexDistance(Vector2Int a, Vector2Int b)
    {
        var dq = a.x - b.x;
        var dr = a.y - b.y;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
    }

    private void BuildRiver(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.06f, 0.14f, 2101);
        if (points.Count < 2) return;

        AddMesh(gameObject, "RiverBank", RibbonMesh(points, 0.32f * hexSize), featureMaterials["RiverBank"]);
        AddMesh(gameObject, "River", RibbonMesh(RaisePoints(points, 0.018f), 0.2f * hexSize), featureMaterials["River"]);
        AddMesh(gameObject, "RiverFoam", RibbonMesh(RaisePoints(points, 0.034f), 0.035f * hexSize), featureMaterials["RiverFoam"]);
    }

    private void BuildRoad(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.08f, 0.16f, 3307);
        if (points.Count < 2) return;

        AddMesh(gameObject, "RoadBed", RibbonMesh(points, 0.24f * hexSize), featureMaterials["RoadShadow"]);
        AddMesh(gameObject, "Road", RibbonMesh(RaisePoints(points, 0.012f), 0.14f * hexSize), featureMaterials["Road"]);
        AddMesh(gameObject, "RoadCenter", RibbonMesh(RaisePoints(points, 0.024f), 0.035f * hexSize), featureMaterials["RoadCenter"]);
    }

    private void BuildTerritoryBorder(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.12f, 0.13f, 5297);
        if (points.Count < 2) return;

        AddMesh(gameObject, "TerritoryBorder", DashedPathMesh(points, 0.07f * hexSize, 0.74f), featureMaterials["Border"]);
    }

    private void BuildSettlement(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var settlement = NewChild($"Settlement_{coord.x}_{coord.y}");
        settlement.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        var rotation = Hash01(coord.x, coord.y, 7301) * 360f;
        settlement.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
        AddMesh(settlement, "Plaza", CylinderMesh(0.48f * hexSize, 0.42f * hexSize, 0.05f, 6), featureMaterials["SettlementWall"]);

        var houseCount = 4 + Mathf.Abs(coord.x * 3 + coord.y * 5) % 3;
        for (var i = 0; i < houseCount; i++)
        {
            var angle = i * Mathf.PI * 2f / houseCount + Mathf.Lerp(-0.18f, 0.18f, Hash01(coord.x, coord.y, 7400 + i));
            var radius = Mathf.Lerp(0.2f, 0.38f, Hash01(coord.y, coord.x, 7500 + i)) * hexSize;
            var height = Mathf.Lerp(0.14f, 0.28f, Hash01(coord.x, coord.y, 7600 + i)) * hexSize;
            var width = Mathf.Lerp(0.16f, 0.26f, Hash01(coord.y, coord.x, 7700 + i)) * hexSize;
            var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "House";
            house.transform.SetParent(settlement.transform, false);
            house.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f + 0.03f, Mathf.Sin(angle) * radius);
            house.transform.localScale = new Vector3(width, height, width * Mathf.Lerp(0.82f, 1.25f, Hash01(coord.x, coord.y, 7800 + i)));
            house.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 28f, 0f);
            house.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["SettlementWall"];

            var roofMaterial = i % 3 == 0 ? featureMaterials["SettlementRoofWarm"] : featureMaterials["SettlementRoof"];
            var roof = CreateCone("Roof", width * 0.82f, 0.01f, height * 0.82f, roofMaterial);
            roof.transform.SetParent(settlement.transform, false);
            roof.transform.localPosition = house.transform.localPosition + Vector3.up * (height * 0.5f + height * 0.36f);
            roof.transform.localRotation = Quaternion.Euler(0f, house.transform.localEulerAngles.y + 45f, 0f);
        }

        var chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chimney.name = "ChimneySmoke";
        chimney.transform.SetParent(settlement.transform, false);
        chimney.transform.localPosition = new Vector3(0.1f, 0.45f, -0.08f);
        chimney.transform.localScale = new Vector3(0.06f, 0.2f, 0.06f);
        chimney.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Smoke"];

        for (var i = 0; i < 3; i++)
        {
            var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Fence";
            fence.transform.SetParent(settlement.transform, false);
            fence.transform.localPosition = new Vector3(-0.34f + i * 0.2f, 0.065f, -0.42f);
            fence.transform.localScale = new Vector3(0.16f, 0.08f, 0.035f);
            fence.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            fence.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];
        }
    }

    private void BuildTower(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var tower = NewChild($"Watchtower_{coord.x}_{coord.y}");
        tower.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        tower.transform.localRotation = Quaternion.Euler(0f, Hash01(coord.x, coord.y, 9001) * 360f, 0f);

        AddMesh(tower, "TowerBase", CylinderMesh(0.34f * hexSize, 0.3f * hexSize, 0.12f, 6), featureMaterials["WallStone"]);

        var body = CreateCone("TowerBody", 0.22f * hexSize, 0.18f * hexSize, 1.05f * hexSize, featureMaterials["WallStone"]);
        body.transform.SetParent(tower.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.58f * hexSize, 0f);

        var roof = CreateCone("TowerRoof", 0.28f * hexSize, 0.02f * hexSize, 0.34f * hexSize, featureMaterials["TowerRoof"]);
        roof.transform.SetParent(tower.transform, false);
        roof.transform.localPosition = new Vector3(0f, 1.28f * hexSize, 0f);

        var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "TowerBanner";
        banner.transform.SetParent(tower.transform, false);
        banner.transform.localPosition = new Vector3(0.18f * hexSize, 1.05f * hexSize, -0.04f * hexSize);
        banner.transform.localScale = new Vector3(0.28f * hexSize, 0.18f * hexSize, 0.035f * hexSize);
        banner.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Flag"];
    }

    private void BuildMine(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var mine = NewChild($"Mine_{coord.x}_{coord.y}");
        mine.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        mine.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);

        var hill = CreateCone("MineHill", 0.5f * hexSize, 0.12f * hexSize, 0.46f * hexSize, featureMaterials["DarkRock"]);
        hill.transform.SetParent(mine.transform, false);
        hill.transform.localPosition = new Vector3(0f, 0.23f * hexSize, 0.03f * hexSize);

        var entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
        entrance.name = "MineEntrance";
        entrance.transform.SetParent(mine.transform, false);
        entrance.transform.localPosition = new Vector3(0f, 0.18f * hexSize, -0.28f * hexSize);
        entrance.transform.localScale = new Vector3(0.34f * hexSize, 0.28f * hexSize, 0.08f * hexSize);
        entrance.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];

        for (var i = 0; i < 2; i++)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "MineRail";
            rail.transform.SetParent(mine.transform, false);
            rail.transform.localPosition = new Vector3((i == 0 ? -0.07f : 0.07f) * hexSize, 0.035f * hexSize, -0.48f * hexSize);
            rail.transform.localScale = new Vector3(0.035f * hexSize, 0.03f * hexSize, 0.48f * hexSize);
            rail.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];
        }

        for (var i = 0; i < 3; i++)
        {
            AddRock(mine.transform, 0f, -0.34f + i * 0.18f, 0.26f - i * 0.08f, i);
        }

        var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ore.name = "OreHint";
        ore.transform.SetParent(mine.transform, false);
        ore.transform.localPosition = new Vector3(0.32f * hexSize, 0.12f * hexSize, -0.18f * hexSize);
        ore.transform.localScale = new Vector3(0.12f * hexSize, 0.1f * hexSize, 0.12f * hexSize);
        ore.transform.localRotation = Quaternion.Euler(15f, 30f, 8f);
        ore.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Gold"];
    }

    private void BuildGreatWall(IReadOnlyList<Vector2Int> coords)
    {
        var points = new List<Vector3>();
        foreach (var coord in coords)
        {
            if (tiles.TryGetValue(coord, out var tile))
            {
                points.Add(tile.World + Vector3.up * (VisualTileTopY + 0.08f));
            }
        }

        if (points.Count < 2)
        {
            return;
        }

        var wall = NewChild("AncientWall");
        for (var i = 0; i < points.Count - 1; i++)
        {
            AddWallSegment(wall.transform, points[i], points[i + 1], i);
        }

        for (var i = 0; i < points.Count; i += 2)
        {
            var tower = CreateCone("WallTower", 0.22f * hexSize, 0.18f * hexSize, 0.46f * hexSize, featureMaterials["WallStone"]);
            tower.transform.SetParent(wall.transform, false);
            tower.transform.localPosition = points[i] + Vector3.up * (0.18f * hexSize);
        }
    }

    private void AddWallSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        var delta = b - a;
        var length = new Vector2(delta.x, delta.z).magnitude;
        if (length <= 0.001f)
        {
            return;
        }

        var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = $"WallSegment_{index}";
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = Vector3.Lerp(a, b, 0.5f) + Vector3.up * (0.12f * hexSize);
        segment.transform.localRotation = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.z), Vector3.up);
        segment.transform.localScale = new Vector3(0.22f * hexSize, 0.28f * hexSize, length);
        segment.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["WallStone"];

        if (index % 2 == 0)
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "WallCap";
            cap.transform.SetParent(parent, false);
            cap.transform.localPosition = segment.transform.localPosition + Vector3.up * (0.17f * hexSize);
            cap.transform.localRotation = segment.transform.localRotation;
            cap.transform.localScale = new Vector3(0.3f * hexSize, 0.08f * hexSize, length * 0.74f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["DarkRock"];
        }
    }

    private List<Vector3> CoordsToPathPoints(IReadOnlyList<Vector2Int> coords, float yOffset, float jitter, int seedOffset)
    {
        var points = new List<Vector3>();
        for (var i = 0; i < coords.Count; i++)
        {
            var coord = coords[i];
            if (!tiles.TryGetValue(coord, out var tile))
            {
                continue;
            }

            var jx = Mathf.Lerp(-jitter, jitter, Hash01(coord.x, coord.y, seedOffset + i * 37)) * hexSize;
            var jz = Mathf.Lerp(-jitter, jitter, Hash01(coord.y, coord.x, seedOffset + i * 73)) * hexSize;
            points.Add(tile.World + new Vector3(jx, VisualTileTopY + yOffset, jz));
        }

        return SmoothPath(points, 5);
    }

    private static List<Vector3> SmoothPath(IReadOnlyList<Vector3> points, int subdivisions)
    {
        if (points.Count < 3)
        {
            return new List<Vector3>(points);
        }

        var smooth = new List<Vector3>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Mathf.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            for (var step = 0; step < subdivisions; step++)
            {
                var t = step / (float)subdivisions;
                smooth.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        smooth.Add(points[^1]);
        return smooth;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static List<Vector3> RaisePoints(IReadOnlyList<Vector3> points, float amount)
    {
        var raised = new List<Vector3>(points.Count);
        foreach (var point in points)
        {
            raised.Add(point + Vector3.up * amount);
        }

        return raised;
    }

    private Mesh RibbonMesh(IReadOnlyList<Vector3> points, float width)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        if (points.Count < 2)
        {
            return MeshFrom(vertices, uvs, triangles, "EmptyRibbon");
        }

        var left = new List<Vector3>(points.Count);
        var right = new List<Vector3>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            var previous = points[Mathf.Max(i - 1, 0)];
            var next = points[Mathf.Min(i + 1, points.Count - 1)];
            var tangent = new Vector3(next.x - previous.x, 0f, next.z - previous.z).normalized;
            if (tangent.sqrMagnitude <= 0.0001f) tangent = Vector3.forward;
            var side = new Vector3(-tangent.z, 0f, tangent.x) * width * 0.5f;
            left.Add(points[i] + side);
            right.Add(points[i] - side);
        }

        for (var i = 0; i < points.Count - 1; i++)
        {
            AddRibbonQuad(vertices, uvs, triangles, left[i], left[i + 1], right[i], right[i + 1], i / (float)(points.Count - 1), (i + 1) / (float)(points.Count - 1));
        }

        return MeshFrom(vertices, uvs, triangles, "Ribbon");
    }

    private Mesh DashedPathMesh(IReadOnlyList<Vector3> points, float width, float dashFraction)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (var i = 0; i < points.Count - 1; i++)
        {
            if (i % 2 == 1) continue;

            var start = points[i];
            var end = Vector3.Lerp(points[i], points[i + 1], dashFraction);
            var tangent = new Vector3(end.x - start.x, 0f, end.z - start.z).normalized;
            if (tangent.sqrMagnitude <= 0.0001f) continue;
            var side = new Vector3(-tangent.z, 0f, tangent.x) * width * 0.5f;
            AddRibbonQuad(vertices, uvs, triangles, start + side, end + side, start - side, end - side, 0f, 1f);
        }

        return MeshFrom(vertices, uvs, triangles, "DashedPath");
    }

    private static void AddRibbonQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Vector3 leftA, Vector3 leftB, Vector3 rightA, Vector3 rightB, float u0, float u1)
    {
        var start = vertices.Count;
        vertices.Add(leftA); uvs.Add(new Vector2(u0, 0f));
        vertices.Add(leftB); uvs.Add(new Vector2(u1, 0f));
        vertices.Add(rightA); uvs.Add(new Vector2(u0, 1f));
        vertices.Add(rightB); uvs.Add(new Vector2(u1, 1f));
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start + 2);
        triangles.Add(start + 1);
        triangles.Add(start + 3);
    }

    private Mesh HexTopMesh(float radius, float y)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var a = HexCorner(radius, i);
            var b = HexCorner(radius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f)); uvs.Add(new Vector2(0.5f, 0.5f));
            vertices.Add(new Vector3(b.x, y, b.y)); uvs.Add(TopUv(b, radius));
            vertices.Add(new Vector3(a.x, y, a.y)); uvs.Add(TopUv(a, radius));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        return MeshFrom(vertices, uvs, triangles, "HexTop");
    }

    private Mesh HexSideMesh(float radius, float y)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var a = HexCorner(radius, i);
            var b = HexCorner(radius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(a.x, y, a.y)); uvs.Add(new Vector2(i / 6f, 0f));
            vertices.Add(new Vector3(b.x, y, b.y)); uvs.Add(new Vector2((i + 1) / 6f, 0f));
            vertices.Add(new Vector3(a.x, VisualTileBottomY, a.y)); uvs.Add(new Vector2(i / 6f, 1f));
            vertices.Add(new Vector3(b.x, VisualTileBottomY, b.y)); uvs.Add(new Vector2((i + 1) / 6f, 1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }

        return MeshFrom(vertices, uvs, triangles, "HexSide");
    }

    private Mesh HexRingMesh(float outerRadius, float innerRadius, float y)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var outerA = HexCorner(outerRadius, i);
            var outerB = HexCorner(outerRadius, (i + 1) % 6);
            var innerA = HexCorner(innerRadius, i);
            var innerB = HexCorner(innerRadius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(outerA.x, y, outerA.y)); uvs.Add(TopUv(outerA, outerRadius));
            vertices.Add(new Vector3(outerB.x, y, outerB.y)); uvs.Add(TopUv(outerB, outerRadius));
            vertices.Add(new Vector3(innerA.x, y, innerA.y)); uvs.Add(TopUv(innerA, outerRadius));
            vertices.Add(new Vector3(innerB.x, y, innerB.y)); uvs.Add(TopUv(innerB, outerRadius));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }

        return MeshFrom(vertices, uvs, triangles, "HexRing");
    }

    private Mesh CylinderMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        var half = height * 0.5f;

        for (var i = 0; i < segments; i++)
        {
            var a0 = Mathf.PI * 2f * i / segments;
            var a1 = Mathf.PI * 2f * (i + 1) / segments;
            var b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, -half, Mathf.Sin(a0) * bottomRadius);
            var b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, -half, Mathf.Sin(a1) * bottomRadius);
            var t0 = new Vector3(Mathf.Cos(a0) * topRadius, half, Mathf.Sin(a0) * topRadius);
            var t1 = new Vector3(Mathf.Cos(a1) * topRadius, half, Mathf.Sin(a1) * topRadius);
            var start = vertices.Count;
            vertices.Add(t0); uvs.Add(new Vector2(i / (float)segments, 0f));
            vertices.Add(t1); uvs.Add(new Vector2((i + 1) / (float)segments, 0f));
            vertices.Add(b0); uvs.Add(new Vector2(i / (float)segments, 1f));
            vertices.Add(b1); uvs.Add(new Vector2((i + 1) / (float)segments, 1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);

            start = vertices.Count;
            vertices.Add(Vector3.up * half); uvs.Add(new Vector2(0.5f, 0.5f));
            vertices.Add(t1); uvs.Add(new Vector2(1f, 0f));
            vertices.Add(t0); uvs.Add(new Vector2(0f, 0f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        return MeshFrom(vertices, uvs, triangles, "Cylinder");
    }

    private GameObject CreateCone(string name, float bottomRadius, float topRadius, float height, Material material)
    {
        var cone = new GameObject(name);
        var filter = cone.AddComponent<MeshFilter>();
        var renderer = cone.AddComponent<MeshRenderer>();
        filter.sharedMesh = CylinderMesh(bottomRadius, topRadius, height, 6);
        renderer.sharedMaterial = material;
        return cone;
    }

    private void BuildWaterPlane()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "DistantWaterPlane";
        plane.transform.SetParent(transform, false);
        plane.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        plane.transform.localScale = Vector3.one * mapRadius * hexSize * 0.44f;
        plane.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["WaterPlane"];
    }

    private void BuildLighting()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = ColorFromHex("738083");
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ColorFromHex("8d9386");
        RenderSettings.ambientEquatorColor = ColorFromHex("5f665a");
        RenderSettings.ambientGroundColor = ColorFromHex("30382f");
        RenderSettings.ambientIntensity = 0.68f;

        var sun = NewChild("LateAfternoonSun");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = ColorFromHex("ffe2a3");
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
        sun.transform.localRotation = Quaternion.Euler(48f, -38f, 0f);

        var fill = NewChild("SoftBlueFill");
        var fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = ColorFromHex("8ba6a8");
        fillLight.intensity = 0.2f;
        fillLight.range = 28f;
        fill.transform.localPosition = new Vector3(-8f, 9f, 6f);
    }

    private void BuildCamera()
    {
        var rig = NewChild("SlowOrbitRig");
        cameraRig = rig.transform;
        var cameraObject = NewChild("StrategyCamera", rig.transform);
        var camera = cameraObject.AddComponent<Camera>();
        strategyCamera = camera;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Clamp(mapRadius * 1.05f, zoomedInSize, zoomedOutSize);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 180f;
        camera.backgroundColor = ColorFromHex("30383a");
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.localPosition = new Vector3(10.6f, 13.8f, 13.2f);
        cameraObject.transform.LookAt(Vector3.up * 0.35f, Vector3.up);
    }

    private void UpdateCameraZoom()
    {
        if (strategyCamera == null)
        {
            return;
        }

        var input = Input.mouseScrollDelta.y;
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
        {
            input += 1f;
        }

        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            input -= 1f;
        }

        if (Mathf.Abs(input) <= 0.001f)
        {
            return;
        }

        var targetSize = strategyCamera.orthographicSize - input * zoomSpeed * Time.deltaTime * 12f;
        strategyCamera.orthographicSize = Mathf.Clamp(targetSize, zoomedInSize, zoomedOutSize);
    }

    private GameObject AddMesh(GameObject parent, string name, Mesh mesh, Material material)
    {
        var go = NewChild(name, parent.transform);
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        return go;
    }

    private GameObject NewChild(string childName)
    {
        return NewChild(childName, transform);
    }

    private static GameObject NewChild(string childName, Transform parent)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private Mesh MeshFrom(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, string meshName)
    {
        var mesh = new Mesh { name = meshName, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Vector3 AxialToWorld(Vector2Int coord)
    {
        var x = hexSize * Sqrt3 * (coord.x + coord.y * 0.5f);
        var z = hexSize * 1.5f * coord.y;
        return new Vector3(x, 0f, z);
    }

    private static Vector2 HexCorner(float radius, int index)
    {
        var angle = Mathf.Deg2Rad * (60f * index + 30f);
        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    private static Vector2 TopUv(Vector2 point, float radius)
    {
        return new Vector2(point.x / (radius * 2f) + 0.5f, point.y / (radius * 2f) + 0.5f);
    }

    private float Noise(float x, float y, int offset)
    {
        var seed = (mapSeed + offset * 131) * 0.013f;
        var a = Mathf.PerlinNoise(x + seed, y - seed) * 2f - 1f;
        var b = Mathf.PerlinNoise(x * 2.1f - seed, y * 2.1f + seed) * 2f - 1f;
        return a * 0.72f + b * 0.28f;
    }

    private float Hash01(int a, int b, int c)
    {
        unchecked
        {
            var h = mapSeed;
            h = h * 73856093 ^ a * 19349663;
            h = h * 83492791 ^ b * 297121507;
            h = h * 1103515245 ^ c * 12345;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private static Color ColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            return color;
        }

        return Color.white;
    }
}
