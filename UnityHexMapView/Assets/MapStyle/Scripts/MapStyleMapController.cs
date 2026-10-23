using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.App;
using Game.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Draws a real <see cref="GameState"/> as the low-poly map and keeps the picture in line with it. It has no
    /// UI and no camera: a host (the development world view or the game view) creates it, calls
    /// <see cref="Refresh"/> after commands and world turns, and asks it for ground heights and picked hexes.
    /// Updates are incremental: only terrain chunks whose knowledge changed are rebuilt, and a location view only
    /// animates the elements that changed. It reads the game state and never changes it.
    /// </summary>
    public sealed class MapStyleMapController
    {
        private sealed class ChunkRuntime
        {
            public GameObject Terrain;
            public readonly List<MapStyleLocation> Scenery = new();
        }

        private const float DeckLength = 4.6f;
        private const float RaycastStep = 0.6f;
        private const float RaycastMaxDistance = 320f;

        private readonly GameState game;
        private readonly GameDataCatalog catalog;
        private readonly Shader shader;
        private readonly MapStyleVisualKit kit;
        private readonly LocationVisualResolver resolver;
        private readonly Material terrainMaterial;
        private readonly Material plainMaterial;
        private readonly Dictionary<Vector2Int, ChunkRuntime> chunks = new();
        private readonly Dictionary<string, MapStyleLocationView> dataViews = new();
        private readonly Dictionary<string, MapStyleLocation> legacyMarkers = new();
        private readonly List<Vector2> drawnPoints = new();
        private readonly List<(Vector2 A, Vector2 B)> wallSegments = new();
        private readonly List<Vector2> wallPoints = new();
        private MapStyleLocation expeditionView;

        /// <param name="unitScale">Scale of the style world relative to the host: the host's hex size divided by <see cref="MapStyleWorldTerrain.HexSize"/>.</param>
        public MapStyleMapController(GameState game, MapStyleHexLayout layout, GameDataCatalog catalog, Shader shader, Transform parent, float unitScale)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.shader = shader != null ? shader : throw new ArgumentNullException(nameof(shader));
            UnitScale = unitScale;
            kit = new MapStyleVisualKit(catalog.VisualDefinitions);
            resolver = new LocationVisualResolver(catalog.VisualDefinitions);

            var rootObject = new GameObject("MapStyleWorld");
            rootObject.transform.SetParent(parent, false);
            rootObject.transform.localScale = Vector3.one * unitScale;
            Root = rootObject.transform;

            terrainMaterial = new Material(shader) { name = "MapStyleWorld-terrain" };
            plainMaterial = new Material(shader) { name = "MapStyleWorld-plain" };
            Terrain = new MapStyleWorldTerrain(game.World, layout, KnowledgeValue);
            AddMesh("Water", Terrain.BuildWaterMesh(), plainMaterial, false);
            CollectWalls();
        }

        public Transform Root { get; }
        public MapStyleWorldTerrain Terrain { get; }
        public float UnitScale { get; }

        /// <summary>Developer view: ignore what the player knows and show the world as it is.</summary>
        public bool ShowTruth { get; set; }

        /// <summary>Draw the built-in expedition group. The game view keeps its own expedition marker.</summary>
        public bool DrawExpedition { get; set; } = true;

        public int LastChunks { get; private set; }
        public long LastMilliseconds { get; private set; }
        public int ChunkTotal => Terrain.ChunkCountX * Terrain.ChunkCountZ;
        public IReadOnlyDictionary<string, MapStyleLocationView> DataViews => dataViews;
        public IReadOnlyDictionary<string, MapStyleLocation> LegacyMarkers => legacyMarkers;

        // ---- Refresh -------------------------------------------------------------------------

        private float KnowledgeValue(HexCoord coord)
        {
            return ShowTruth ? 0f : MapStyleWorldTerrain.PaleOf(game.Knowledge.GetTileKnowledge(coord));
        }

        /// <summary>
        /// Brings the picture in line with the game state. Only chunks whose knowledge changed are rebuilt;
        /// with <paramref name="all"/> every chunk is rebuilt (first build, or the developer truth toggle).
        /// </summary>
        public void Refresh(bool all)
        {
            var stopwatch = Stopwatch.StartNew();
            var useKnowledge = !ShowTruth;
            var dirty = Terrain.UpdateKnowledge(KnowledgeValue);
            if (all)
            {
                for (var x = 0; x < Terrain.ChunkCountX; x++)
                {
                    for (var z = 0; z < Terrain.ChunkCountZ; z++)
                    {
                        dirty.Add(new Vector2Int(x, z));
                    }
                }
            }

            RefreshLocations(useKnowledge);
            foreach (var chunk in dirty)
            {
                RebuildChunk(chunk, useKnowledge);
            }

            RefreshExpedition(useKnowledge);
            if (dirty.Count > 0)
            {
                LastChunks = dirty.Count;
                LastMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        private void RebuildChunk(Vector2Int index, bool useKnowledge)
        {
            if (chunks.TryGetValue(index, out var old))
            {
                DestroyChunk(old);
            }

            var chunk = new ChunkRuntime { Terrain = AddMesh("Chunk " + index.x + "," + index.y, Terrain.BuildChunkMesh(index, useKnowledge), terrainMaterial, true) };
            BuildForests(index, useKnowledge, chunk);
            BuildWalls(index, useKnowledge, chunk);
            chunks[index] = chunk;
        }

        private static void DestroyChunk(ChunkRuntime chunk)
        {
            DestroyObject(chunk.Terrain);
            foreach (var scenery in chunk.Scenery)
            {
                if (scenery != null)
                {
                    DestroyObject(scenery.gameObject);
                }
            }
        }

        private static void DestroyObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public void Dispose()
        {
            if (Root == null)
            {
                return;
            }

            foreach (var filter in Root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    DestroyMesh(filter.sharedMesh);
                }
            }

            DestroyObject(Root.gameObject);
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        // ---- Terrain scenery -------------------------------------------------------------------

        private void BuildForests(Vector2Int index, bool useKnowledge, ChunkRuntime chunk)
        {
            var zones = new[] { new List<Vector3>(), new List<Vector3>() };
            var sizes = new[] { new List<float>(), new List<float>() };
            foreach (var pair in Terrain.ForestTilesIn(index))
            {
                var level = KnowledgeLevelOf(pair.Value, useKnowledge);
                if (level >= 2)
                {
                    continue;
                }

                var coord = pair.Key;
                var center = MapStyleWorldTerrain.ViewToWorld(coord);
                for (var i = 0; i < 7; i++)
                {
                    var angle = MapStyleWorldTerrain.Hash(coord.Q * 7 + i, coord.R + 3f) * Mathf.PI * 2f;
                    var radius = Mathf.Sqrt(MapStyleWorldTerrain.Hash(coord.R * 11 + i, coord.Q + 5f)) * MapStyleWorldTerrain.HexSize * 0.82f;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (Terrain.TrenchFactor(point) > 0.05f || Terrain.RiverFactor(point) > 0.1f || Terrain.Roads.Distance(point) < 0.9f)
                    {
                        continue;
                    }

                    var blocked = false;
                    foreach (var spot in drawnPoints)
                    {
                        if (Vector2.Distance(spot, point) < 2.6f)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    var ground = GroundAt(point, useKnowledge);
                    if (!IsFinite(ground))
                    {
                        Debug.LogWarning("Skipped a tree with a non-finite position: tile (" + coord.Q + "," + coord.R + "), index " + i + ", point " + point + ", ground " + ground);
                        continue;
                    }

                    if (blocked || ground.y > 1.6f || ground.y < -0.5f)
                    {
                        continue;
                    }

                    zones[level].Add(ground);
                    sizes[level].Add(0.8f + MapStyleWorldTerrain.Hash(coord.Q + i * 3, coord.R * 5f + i) * 0.6f);
                }
            }

            for (var level = 0; level < 2; level++)
            {
                if (zones[level].Count == 0)
                {
                    continue;
                }

                var forest = MapStyleLocations.Forest(Root, shader, "Wald " + index.x + "," + index.y + (level == 0 ? " bestätigt" : " berichtet"), zones[level], sizes[level]);
                forest.SetKnowledge(level);
                chunk.Scenery.Add(forest);
            }
        }

        private void CollectWalls()
        {
            foreach (var path in game.World.Paths)
            {
                if (path.Kind != WorldPathKind.Wall)
                {
                    continue;
                }

                for (var i = 0; i < path.Coords.Count; i++)
                {
                    var point = Terrain.CoreToWorld(path.Coords[i]);
                    wallPoints.Add(point);
                    if (i > 0)
                    {
                        wallSegments.Add((Terrain.CoreToWorld(path.Coords[i - 1]), point));
                    }
                }
            }
        }

        /// <summary>Walls are drawn per chunk from their segments; a segment follows the knowledge of the ground under its middle.</summary>
        private void BuildWalls(Vector2Int index, bool useKnowledge, ChunkRuntime chunk)
        {
            if (wallSegments.Count == 0)
            {
                return;
            }

            var levels = new[] { new List<(Vector2 A, Vector2 B)>(), new List<(Vector2 A, Vector2 B)>() };
            var towers = new[] { new List<Vector2>(), new List<Vector2>() };
            foreach (var segment in wallSegments)
            {
                var middle = (segment.A + segment.B) * 0.5f;
                if (Terrain.ChunkOf(middle) != index || !TryLevelAt(middle, useKnowledge, out var level))
                {
                    continue;
                }

                levels[level].Add(segment);
            }

            foreach (var point in wallPoints)
            {
                if (Terrain.ChunkOf(point) == index && TryLevelAt(point, useKnowledge, out var level))
                {
                    towers[level].Add(point);
                }
            }

            for (var level = 0; level < 2; level++)
            {
                if (levels[level].Count == 0 && towers[level].Count == 0)
                {
                    continue;
                }

                var segmentsOfLevel = levels[level];
                var towersOfLevel = towers[level];
                var wall = MapStyleLocations.Scenery(Root, shader, "Mauer " + index.x + "," + index.y + (level == 0 ? " bestätigt" : " berichtet"), builder =>
                {
                    foreach (var segment in segmentsOfLevel)
                    {
                        var direction = segment.B - segment.A;
                        var length = direction.magnitude;
                        if (length < 0.01f)
                        {
                            continue;
                        }

                        var middle = (segment.A + segment.B) * 0.5f;
                        var y = Mathf.Max(GroundAt(segment.A, useKnowledge).y, GroundAt(middle, useKnowledge).y, GroundAt(segment.B, useKnowledge).y);
                        var yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                        builder.AddBox(new Vector3(middle.x, y + 0.55f, middle.y), new Vector3(0.5f, 1.3f, length), new Vector3(0f, yaw, 0f), MapStyleLocation.C(0x8d8a80));
                    }

                    foreach (var point in towersOfLevel)
                    {
                        var y = GroundAt(point, useKnowledge).y;
                        builder.AddBox(new Vector3(point.x, y + 0.9f, point.y), new Vector3(0.95f, 1.9f, 0.95f), Vector3.zero, MapStyleLocation.C(0x7d7a70));
                        builder.AddPrism(new Vector3(point.x, y + 2.15f, point.y), new Vector3(1.15f, 0.7f, 1.15f), Vector3.zero, 4, 0f, MapStyleLocation.C(0xb5683c));
                    }
                });
                wall.SetKnowledge(level);
                chunk.Scenery.Add(wall);
            }
        }

        private bool TryLevelAt(Vector2 point, bool useKnowledge, out int level)
        {
            level = 2;
            if (!Terrain.TryGetCoreAt(point, out var core))
            {
                return false;
            }

            level = KnowledgeLevelOf(core, useKnowledge);
            return level < 2;
        }

        private GameObject AddMesh(string name, Mesh mesh, Material material, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
                || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        private Vector3 GroundAt(Vector2 point, bool useKnowledge)
        {
            var sample = Terrain.SampleAt(point, useKnowledge);
            return new Vector3(point.x, sample.Valid ? sample.Height : 0f, point.y);
        }

        private int KnowledgeLevelOf(HexCoord coord, bool useKnowledge)
        {
            if (!useKnowledge)
            {
                return 0;
            }

            switch (game.Knowledge.GetTileKnowledge(coord))
            {
                case KnowledgeLevel.Confirmed: return 0;
                case KnowledgeLevel.Unknown: return 2;
                default: return 1;
            }
        }

        // ---- Host queries (heights and picking) ---------------------------------------------------

        /// <summary>
        /// Ground height at the center of a hex, in the units of the parent the controller was created under.
        /// With <paramref name="highest"/> the highest of the center and the corners, so a flat overlay stays above a slope.
        /// </summary>
        public float GroundHeight(Vector2Int view, bool highest)
        {
            var useKnowledge = !ShowTruth;
            var center = MapStyleWorldTerrain.ViewToWorld(new HexCoord(view.x, view.y));
            var height = GroundAt(center, useKnowledge).y;
            if (highest)
            {
                for (var corner = 0; corner < 6; corner++)
                {
                    var angle = Mathf.Deg2Rad * (60f * corner + 30f);
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (MapStyleWorldTerrain.HexSize * 0.95f);
                    height = Mathf.Max(height, GroundAt(point, useKnowledge).y);
                }
            }

            return Mathf.Max(height, MapStyleWorldTerrain.WaterLevel) * UnitScale;
        }

        /// <summary>
        /// Finds the hex under a world-space ray by marching it against the displayed relief. Returns false when
        /// the ray misses the map or ends over a place the map has no tile for.
        /// </summary>
        public bool TryRaycast(Ray worldRay, out Vector2Int view)
        {
            view = default;
            var useKnowledge = !ShowTruth;
            var origin = Root.InverseTransformPoint(worldRay.origin);
            var direction = Root.InverseTransformDirection(worldRay.direction).normalized;
            var previous = 0f;
            var distance = 0f;
            var hit = false;
            for (; distance <= RaycastMaxDistance; distance += RaycastStep)
            {
                var point = origin + direction * distance;
                var sample = Terrain.SampleAt(new Vector2(point.x, point.z), useKnowledge);
                var ground = sample.Valid ? sample.Height : MapStyleWorldTerrain.WaterLevel;
                if (point.y <= ground)
                {
                    hit = true;
                    break;
                }

                previous = distance;
            }

            if (!hit)
            {
                return false;
            }

            var low = previous;
            var high = distance;
            for (var i = 0; i < 8; i++)
            {
                var mid = (low + high) * 0.5f;
                var point = origin + direction * mid;
                var sample = Terrain.SampleAt(new Vector2(point.x, point.z), useKnowledge);
                var ground = sample.Valid ? sample.Height : MapStyleWorldTerrain.WaterLevel;
                if (point.y <= ground)
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            var hitPoint = origin + direction * high;
            var coord = MapStyleWorldTerrain.WorldToView(new Vector2(hitPoint.x, hitPoint.z));
            if (!Terrain.HasTile(coord))
            {
                return false;
            }

            view = new Vector2Int(coord.Q, coord.R);
            return true;
        }

        // ---- Locations ----------------------------------------------------------------------

        private void RefreshLocations(bool useKnowledge)
        {
            drawnPoints.Clear();
            var seen = new HashSet<string>();
            foreach (var location in game.World.Locations)
            {
                var description = useKnowledge ? resolver.ResolveForPlayer(game, location) : resolver.ResolveWorldTruth(location);
                if (description == null)
                {
                    continue;
                }

                if (description.CompositionId != null && EnsureDataView(location, description, useKnowledge))
                {
                    seen.Add(location.Id);
                    continue;
                }

                EnsureLegacyMarker(location, description, useKnowledge);
                seen.Add(location.Id);
            }

            RemoveUnseen(dataViews, seen, view => view.gameObject);
            RemoveUnseen(legacyMarkers, seen, marker => marker.gameObject);
        }

        private static void RemoveUnseen<T>(Dictionary<string, T> views, HashSet<string> seen, Func<T, GameObject> objectOf)
        {
            var gone = new List<string>();
            foreach (var pair in views)
            {
                if (!seen.Contains(pair.Key))
                {
                    gone.Add(pair.Key);
                }
            }

            foreach (var id in gone)
            {
                DestroyObject(objectOf(views[id]));
                views.Remove(id);
            }
        }

        /// <summary>Data-driven location (bridge, camp). Returns false when it cannot be placed.</summary>
        private bool EnsureDataView(SpecialLocationState location, LocationVisualDescription description, bool useKnowledge)
        {
            var onHex = description.AnchorMode == "point";
            if (!dataViews.TryGetValue(location.Id, out var view))
            {
                Vector3 position;
                float yaw;
                var scale = Vector3.one * description.Scale;
                if (onHex)
                {
                    position = GroundAt(Terrain.CoreToWorld(location.Anchor.PrimaryCoord), useKnowledge);
                    yaw = 0f;
                }
                else
                {
                    if (!TryPlaceOnLine(location, description, out position, out yaw, out var span))
                    {
                        return false;
                    }

                    scale = new Vector3(1f, 1f, Mathf.Clamp((span + 0.8f) / DeckLength, 0.8f, 1.5f));
                }

                view = MapStyleLocationView.Create(location.Id, DisplayNameOf(location), Root, position, yaw, shader, kit);
                view.transform.localScale = scale;
                dataViews[location.Id] = view;
                view.Apply(description);
                view.Snap();
            }
            else
            {
                view.Apply(description);
                if (onHex)
                {
                    // The ground under a place changes with knowledge, so it follows it.
                    view.transform.localPosition = GroundAt(Terrain.CoreToWorld(location.Anchor.PrimaryCoord), useKnowledge);
                }
            }

            drawnPoints.Add(new Vector2(view.transform.localPosition.x, view.transform.localPosition.z));
            return true;
        }

        /// <summary>
        /// Puts an edge location where the ravine or river that explains it is narrowest and straightest near
        /// the blocked edge. Positions along the line and directions up to 40 degrees off the right angle are
        /// tried; a spot scores better the shorter the crossing, the closer to the edge and the more dry land
        /// lies beside both bridge ends. The bridge is centered between the banks and stretched to span them.
        /// </summary>
        private bool TryPlaceOnLine(SpecialLocationState location, LocationVisualDescription description, out Vector3 position, out float yaw, out float span)
        {
            position = Vector3.zero;
            yaw = 0f;
            span = DeckLength;
            if (description.AnchorMode != "edge" || !WorldObstaclePathRules.IsRouteObstacleEdge(location))
            {
                return false;
            }

            var a = Terrain.CoreToWorld(location.Anchor.Coords[0]);
            var b = Terrain.CoreToWorld(location.Anchor.Coords[1]);
            var middle = (a + b) * 0.5f;
            var chasm = Terrain.Chasms.Nearest(middle);
            var river = Terrain.Rivers.Nearest(middle);
            var lines = chasm.Distance <= river.Distance ? Terrain.Chasms : Terrain.Rivers;
            var hit = chasm.Distance <= river.Distance ? chasm : river;
            if (float.IsInfinity(hit.Distance))
            {
                return false;
            }

            var found = false;
            var bestScore = float.MaxValue;
            var bestEnd1 = Vector2.zero;
            var bestEnd2 = Vector2.zero;
            for (var step = -5; step <= 5; step++)
            {
                var line = lines.Nearest(hit.Point + hit.Tangent * (step * 0.8f));
                if (line.Distance > 1f)
                {
                    continue;
                }

                var perpendicular = new Vector2(-line.Tangent.y, line.Tangent.x);
                for (var angle = -40; angle <= 40; angle += 10)
                {
                    var across = Rotate(perpendicular, angle);
                    if (!TryFindBanks(line.Point, across, out var toBank1, out var toBank2))
                    {
                        continue;
                    }

                    var end1 = line.Point + across * toBank1;
                    var end2 = line.Point - across * toBank2;
                    var score = toBank1 + toBank2 + Mathf.Abs(step) * 0.12f + Mathf.Abs(angle) * 0.015f + SideLandPenalty(end1, across) + SideLandPenalty(end2, across);
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    found = true;
                    bestEnd1 = end1;
                    bestEnd2 = end2;
                    span = toBank1 + toBank2;
                    var center = line.Point + across * ((toBank1 - toBank2) * 0.5f);
                    var height1 = GroundAt(end1, false).y;
                    var height2 = GroundAt(end2, false).y;
                    position = new Vector3(center.x, (height1 + height2) * 0.5f + 0.2f, center.y);
                    yaw = Mathf.Atan2(across.x, across.y) * Mathf.Rad2Deg;
                }
            }

            if (found)
            {
                LinkRoadToBridgeEnd(location.Id + ":a", bestEnd1);
                LinkRoadToBridgeEnd(location.Id + ":b", bestEnd2);
            }

            return found;
        }

        /// <summary>
        /// Draws a short road from a bridge end to the nearest road on dry land, so the bridge does not stand beside
        /// the road that leads to it. A road that already passes the end is left alone. Only the colour of the ground
        /// follows the road, so the link has to exist before the chunks that contain it are built.
        /// </summary>
        private void LinkRoadToBridgeEnd(string linkId, Vector2 end)
        {
            var near = Terrain.Roads.Nearest(end);
            if (float.IsInfinity(near.Distance) || near.Distance < 0.9f)
            {
                return;
            }

            var found = false;
            var best = Vector2.zero;
            var bestDistance = float.MaxValue;
            for (var offset = -3f; offset <= 3f; offset += 0.5f)
            {
                var candidate = Terrain.Roads.Nearest(near.Point + near.Tangent * offset).Point;
                if (Terrain.TrenchFactor(candidate) > 0.05f || Terrain.RiverFactor(candidate) > 0.05f)
                {
                    continue;
                }

                var distance = Vector2.Distance(end, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                    found = true;
                }
            }

            if (found && bestDistance < 6f)
            {
                Terrain.Roads.AddPolyline("bridge-link:" + linkId, new[] { end, best });
            }
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }

        /// <summary>Penalty when the ground beside a bridge end is water or carved away instead of dry land.</summary>
        private float SideLandPenalty(Vector2 end, Vector2 across)
        {
            var side = new Vector2(-across.y, across.x);
            var penalty = 0f;
            foreach (var sign in new[] { -1f, 1f })
            {
                var point = end + side * (sign * 1.2f);
                var sample = Terrain.SampleAt(point, false);
                if (!sample.Valid || sample.Height <= MapStyleWorldTerrain.WaterLevel + 0.15f || Terrain.TrenchFactor(point) > 0.5f || Terrain.RiverFactor(point) > 0.5f)
                {
                    penalty += 0.6f;
                }
            }

            return penalty;
        }

        /// <summary>Walks away from a line in both directions until the carved ravine or river bed ends, that is the bank top.</summary>
        private bool TryFindBanks(Vector2 origin, Vector2 across, out float toBank1, out float toBank2)
        {
            toBank1 = ScanToBank(origin, across);
            toBank2 = ScanToBank(origin, -across);
            return toBank1 > 0f && toBank2 > 0f;
        }

        private float ScanToBank(Vector2 origin, Vector2 direction)
        {
            const float step = 0.25f;
            for (var distance = step; distance <= 10f; distance += step)
            {
                var point = origin + direction * distance;
                var sample = Terrain.SampleAt(point, false);
                if (!sample.Valid)
                {
                    return -1f;
                }

                // The deck rests on the crest: past the water and past everything the ravine or river carved.
                if (sample.Height > MapStyleWorldTerrain.WaterLevel + 0.15f
                    && Terrain.TrenchFactor(point) < 0.03f
                    && Terrain.RiverFactor(point) < 0.03f)
                {
                    return distance;
                }
            }

            return -1f;
        }

        private void EnsureLegacyMarker(SpecialLocationState location, LocationVisualDescription description, bool useKnowledge)
        {
            var point = Terrain.CoreToWorld(location.Anchor.PrimaryCoord);
            var ground = GroundAt(point, useKnowledge);
            if (!legacyMarkers.TryGetValue(location.Id, out var marker))
            {
                marker = CreateMarker(location, ground);
                legacyMarkers[location.Id] = marker;
            }

            marker.transform.localPosition = ground;
            marker.SetKnowledge(description.Knowledge == VisualKnowledge.Confirmed ? 0 : 1);
            drawnPoints.Add(point);
        }

        private static string DisplayNameOf(SpecialLocationState location)
        {
            return string.IsNullOrEmpty(location.Name) ? location.Id : location.Name;
        }

        private MapStyleLocation CreateMarker(SpecialLocationState location, Vector3 position)
        {
            MapStyleLocation marker;
            switch (location.Kind)
            {
                case LocationKind.BaseCamp:
                    marker = MapStyleLocations.Camp(Root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
                case LocationKind.Settlement:
                    marker = MapStyleLocations.Village(Root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
                case LocationKind.Watchtower:
                    marker = MapStyleLocations.Tower(Root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
            }

            var visual = catalog.VisualDefinitions.FindMarker(location.ArchetypeId);
            var color = MapStyleVisualKit.ParseColor(visual.ColorHex);
            marker = MapStyleLocation.Create(DisplayNameOf(location), new[] { "steht" }, Root, position, 0f, shader, color, visual.RingGaps, 2.1f);
            marker.Ico(null, new Vector3(0f, 0.5f, 0f), 0.7f, MapStyleLocation.C(0x8d8a80), new Vector3(1f, 0.9f, 1f));
            marker.Box(null, new Vector3(0.9f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 0.08f), MapStyleLocation.C(0x7a5a38));
            marker.Box(null, new Vector3(1.25f, 1.55f, 0f), new Vector3(0.7f, 0.4f, 0.05f), color);
            return marker;
        }

        private void RefreshExpedition(bool useKnowledge)
        {
            if (!DrawExpedition || game.Expedition == null)
            {
                return;
            }

            var ground = GroundAt(Terrain.CoreToWorld(game.Expedition.Position), useKnowledge);
            if (expeditionView == null)
            {
                expeditionView = MapStyleLocations.ExpeditionGroup(Root, ground, shader, 180f);
                expeditionView.transform.localScale = Vector3.one * 0.8f;
            }

            expeditionView.transform.localPosition = ground;
        }
    }
}
