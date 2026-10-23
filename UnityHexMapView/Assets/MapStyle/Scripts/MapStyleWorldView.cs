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
    /// Development view of the low-poly map for a real <see cref="GameState"/> (tutorial or generated world).
    /// Locations are drawn from <see cref="LocationVisualResolver"/> descriptions, so what the player sees follows
    /// knowledge, not world truth. Updates are incremental: only terrain chunks whose knowledge changed are rebuilt
    /// and a location view only animates the elements that changed. The development buttons change the game state
    /// of this throwaway test world directly; the real game would do that through commands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapStyleWorldView : MonoBehaviour
    {
        public enum Source
        {
            Tutorial,
            Generated
        }

        private sealed class ChunkRuntime
        {
            public GameObject Terrain;
            public readonly List<MapStyleLocation> Forests = new();
        }

        public Shader shader;

        private static readonly string[] SourceNames = { "Tutorial", "Generiert" };
        private static readonly string[] BridgeCycle = { "blocked", "repaired", "destroyed", "open" };
        private const float RefreshInterval = 0.25f;

        private GameDataCatalog catalog;
        private MapStyleVisualKit kit;
        private LocationVisualResolver resolver;
        private GameState game;
        private Source source = Source.Tutorial;
        private string seedText = "42";
        private bool showTruth;
        private float timeOfDay = 42f;
        private string status = string.Empty;
        private int validationIssues;
        private int exploreRadius;
        private float refreshTimer;
        private int lastChunks;
        private long lastMilliseconds;

        private Camera cam;
        private Light sun;
        private MapStyleOrbitCamera orbit;
        private Transform root;
        private Material terrainMaterial;
        private Material plainMaterial;
        private MapStyleWorldTerrain terrain;
        private readonly Dictionary<Vector2Int, ChunkRuntime> chunks = new();
        private readonly Dictionary<string, MapStyleLocationView> dataViews = new();
        private readonly Dictionary<string, MapStyleLocation> legacyMarkers = new();
        private readonly List<Vector2> drawnPoints = new();
        private MapStyleLocation expeditionView;
        private HexCoord expeditionAt;
        private float guiScale = 1f;
        private Rect panelRect;

        public Camera Cam => cam;

        private void Start()
        {
            if (root == null)
            {
                Rebuild();
            }
        }

        public void Rebuild()
        {
            EnsureSetup();
            try
            {
                game = CreateGame();
                Build();
            }
            catch (Exception exception)
            {
                status = "Fehler: " + exception.Message;
                Debug.LogException(exception);
            }
        }

        private GameState CreateGame()
        {
            if (source == Source.Tutorial)
            {
                return TutorialGameFactory.Create();
            }

            uint.TryParse(seedText, out var seed);
            var request = new WorldGenerationRequest { Seed = seed == 0 ? 42u : seed, Width = 40, Height = 30, FactionCount = 3 };
            return TutorialGameFactory.CreateGenerated(new WorldGenBridge().Generate(request));
        }

        // ---- Setup ------------------------------------------------------------------------

        private void EnsureSetup()
        {
            if (shader == null)
            {
                shader = Shader.Find("MapStyle/LowPolyLit");
            }

            if (catalog == null)
            {
                var path = System.IO.Path.Combine(Application.streamingAssetsPath, "GameData");
                catalog = GameDataCatalog.LoadFromDirectory(path) ?? throw new InvalidOperationException("Game data folder '" + path + "' was not found.");
                kit = new MapStyleVisualKit(catalog.VisualDefinitions);
                resolver = new LocationVisualResolver(catalog.VisualDefinitions);
            }

            if (cam == null)
            {
                var camera = new GameObject("MapStyleCamera");
                camera.transform.SetParent(transform, false);
                camera.tag = "MainCamera";
                cam = camera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                orbit = camera.AddComponent<MapStyleOrbitCamera>();
                orbit.Cam = cam;
                orbit.IsOverUi = position => panelRect.Contains(new Vector2(position.x, Screen.height - position.y) / guiScale);
            }

            if (sun == null)
            {
                var light = new GameObject("MapStyleSun");
                light.transform.SetParent(transform, false);
                sun = light.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.85f;
                sun.shadowBias = 0.05f;
                sun.shadowNormalBias = 0.4f;
            }
        }

        private void Clear()
        {
            chunks.Clear();
            dataViews.Clear();
            legacyMarkers.Clear();
            drawnPoints.Clear();
            expeditionView = null;
            exploreRadius = 0;
            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root.gameObject);
                }
                else
                {
                    DestroyImmediate(root.gameObject);
                }
            }

            root = new GameObject("MapStyleWorld").transform;
            root.SetParent(transform, false);
        }

        // ---- Build and incremental refresh --------------------------------------------------

        private float KnowledgeValue(HexCoord coord)
        {
            return showTruth ? 0f : MapStyleWorldTerrain.PaleOf(game.Knowledge.GetTileKnowledge(coord));
        }

        private void Build()
        {
            Clear();
            terrainMaterial = new Material(shader) { name = "MapStyleWorld-terrain" };
            plainMaterial = new Material(shader) { name = "MapStyleWorld-plain" };
            terrain = new MapStyleWorldTerrain(game.World, CreateLayout(game.World), KnowledgeValue);
            AddMesh("Water", terrain.BuildWaterMesh(), plainMaterial, false);

            Refresh(true);
            validationIssues = WorldObstaclePathRules.Validate(game.World).Count;
            ApplyTimeOfDay();
            FrameExpedition();
        }

        /// <summary>
        /// Brings the picture in line with the game state. Only chunks whose knowledge changed are rebuilt and
        /// location views update in place. With <paramref name="all"/> every chunk is rebuilt.
        /// </summary>
        private void Refresh(bool all)
        {
            var stopwatch = Stopwatch.StartNew();
            var useKnowledge = !showTruth;
            var dirty = terrain.UpdateKnowledge(KnowledgeValue);
            if (all)
            {
                for (var x = 0; x < terrain.ChunkCountX; x++)
                {
                    for (var z = 0; z < terrain.ChunkCountZ; z++)
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
                lastChunks = dirty.Count;
                lastMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            status = string.Format("{0} Hexfelder, {1} Orte, zuletzt {2} von {3} Chunks neu ({4} ms)", game.World.Map.Count, game.World.Locations.Count, lastChunks, terrain.ChunkCountX * terrain.ChunkCountZ, lastMilliseconds);
        }

        private void RebuildChunk(Vector2Int index, bool useKnowledge)
        {
            if (chunks.TryGetValue(index, out var old))
            {
                DestroyChunk(old);
            }

            var chunk = new ChunkRuntime { Terrain = AddMesh("Chunk " + index.x + "," + index.y, terrain.BuildChunkMesh(index, useKnowledge), terrainMaterial, true) };
            BuildForests(index, useKnowledge, chunk);
            chunks[index] = chunk;
        }

        private static void DestroyChunk(ChunkRuntime chunk)
        {
            DestroyObject(chunk.Terrain);
            foreach (var forest in chunk.Forests)
            {
                if (forest != null)
                {
                    DestroyObject(forest.gameObject);
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
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void BuildForests(Vector2Int index, bool useKnowledge, ChunkRuntime chunk)
        {
            var zones = new[] { new List<Vector3>(), new List<Vector3>() };
            var sizes = new[] { new List<float>(), new List<float>() };
            foreach (var pair in terrain.ForestTilesIn(index))
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
                    if (terrain.TrenchFactor(point) > 0.05f || terrain.RiverFactor(point) > 0.1f || terrain.Roads.Distance(point) < 0.9f)
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

                var forest = MapStyleLocations.Forest(root, shader, "Wald " + index.x + "," + index.y + (level == 0 ? " bestätigt" : " berichtet"), zones[level], sizes[level]);
                forest.SetKnowledge(level);
                chunk.Forests.Add(forest);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
                || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        private GameObject AddMesh(string name, Mesh mesh, Material material, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private Vector3 GroundAt(Vector2 point, bool useKnowledge)
        {
            var sample = terrain.SampleAt(point, useKnowledge);
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

        /// <summary>The tutorial world stores offset coordinates, generated worlds axial ones (as the existing map view reads them).</summary>
        private MapStyleHexLayout CreateLayout(WorldState world)
        {
            return source == Source.Tutorial
                ? MapStyleHexLayout.OffsetCentered(world.Map.Bounds.Width, world.Map.Bounds.Height)
                : MapStyleHexLayout.Axial((world.Map.Bounds.Height - 1) >> 1);
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

                if (description.CompositionId != null && EnsureDataView(location, description))
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

        /// <summary>Data-driven location (a bridge today). Returns false when it cannot be placed.</summary>
        private bool EnsureDataView(SpecialLocationState location, LocationVisualDescription description)
        {
            if (!dataViews.TryGetValue(location.Id, out var view))
            {
                if (!TryPlaceOnLine(location, description, out var position, out var yaw, out var span))
                {
                    return false;
                }

                view = MapStyleLocationView.Create(location.Id, DisplayNameOf(location), root, position, yaw, shader, kit);
                view.transform.localScale = new Vector3(1f, 1f, Mathf.Clamp((span + 0.8f) / DeckLength, 0.8f, 1.5f));
                dataViews[location.Id] = view;
                view.Apply(description);
                view.Snap();
            }
            else
            {
                view.Apply(description);
            }

            drawnPoints.Add(new Vector2(view.transform.localPosition.x, view.transform.localPosition.z));
            return true;
        }

        private const float DeckLength = 4.6f;

        /// <summary>
        /// Puts an edge location where the ravine or river that explains it is narrowest and straightest near
        /// the blocked edge: the deck crosses the line at right angles, is centered between the two banks and is
        /// stretched to span them.
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

            var a = terrain.CoreToWorld(location.Anchor.Coords[0]);
            var b = terrain.CoreToWorld(location.Anchor.Coords[1]);
            var middle = (a + b) * 0.5f;
            var chasm = terrain.Chasms.Nearest(middle);
            var river = terrain.Rivers.Nearest(middle);
            var lines = chasm.Distance <= river.Distance ? terrain.Chasms : terrain.Rivers;
            var hit = chasm.Distance <= river.Distance ? chasm : river;
            if (float.IsInfinity(hit.Distance))
            {
                return false;
            }

            var found = false;
            var bestScore = float.MaxValue;
            for (var step = -5; step <= 5; step++)
            {
                var candidate = hit.Point + hit.Tangent * (step * 0.8f);
                var line = lines.Nearest(candidate);
                if (line.Distance > 1f)
                {
                    continue;
                }

                var across = new Vector2(-line.Tangent.y, line.Tangent.x);
                if (!TryFindBanks(line.Point, across, out var toBank1, out var toBank2))
                {
                    continue;
                }

                var width = toBank1 + toBank2;
                var score = width + Mathf.Abs(step) * 0.12f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                found = true;
                span = width;
                var center = line.Point + across * ((toBank1 - toBank2) * 0.5f);
                var bank1 = GroundAt(line.Point + across * toBank1, false);
                var bank2 = GroundAt(line.Point - across * toBank2, false);
                position = new Vector3(center.x, (bank1.y + bank2.y) * 0.5f + 0.2f, center.y);
                yaw = Mathf.Atan2(across.x, across.y) * Mathf.Rad2Deg;
            }

            return found;
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
                var sample = terrain.SampleAt(point, false);
                if (!sample.Valid)
                {
                    return -1f;
                }

                // The deck rests on the crest: past the water and past everything the ravine or river carved.
                if (sample.Height > MapStyleWorldTerrain.WaterLevel + 0.15f
                    && terrain.TrenchFactor(point) < 0.03f
                    && terrain.RiverFactor(point) < 0.03f)
                {
                    return distance;
                }
            }

            return -1f;
        }

        private void EnsureLegacyMarker(SpecialLocationState location, LocationVisualDescription description, bool useKnowledge)
        {
            var point = terrain.CoreToWorld(location.Anchor.PrimaryCoord);
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
                    marker = MapStyleLocations.Camp(root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
                case LocationKind.Settlement:
                    marker = MapStyleLocations.Village(root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
                case LocationKind.Watchtower:
                    marker = MapStyleLocations.Tower(root, position, shader);
                    marker.transform.localScale = Vector3.one * 0.65f;
                    return marker;
            }

            var visual = catalog.VisualDefinitions.FindMarker(location.ArchetypeId);
            var color = MapStyleVisualKit.ParseColor(visual.ColorHex);
            marker = MapStyleLocation.Create(DisplayNameOf(location), new[] { "steht" }, root, position, 0f, shader, color, visual.RingGaps, 2.1f);
            marker.Ico(null, new Vector3(0f, 0.5f, 0f), 0.7f, MapStyleLocation.C(0x8d8a80), new Vector3(1f, 0.9f, 1f));
            marker.Box(null, new Vector3(0.9f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 0.08f), MapStyleLocation.C(0x7a5a38));
            marker.Box(null, new Vector3(1.25f, 1.55f, 0f), new Vector3(0.7f, 0.4f, 0.05f), color);
            return marker;
        }

        private void RefreshExpedition(bool useKnowledge)
        {
            if (game.Expedition == null)
            {
                return;
            }

            var position = game.Expedition.Position;
            var ground = GroundAt(terrain.CoreToWorld(position), useKnowledge);
            if (expeditionView == null)
            {
                expeditionView = MapStyleLocations.ExpeditionGroup(root, ground, shader, 180f);
                expeditionView.transform.localScale = Vector3.one * 0.8f;
            }

            expeditionView.transform.localPosition = ground;
            expeditionAt = position;
        }

        private void FrameExpedition()
        {
            var point = game.Expedition != null
                ? terrain.CoreToWorld(game.Expedition.Position)
                : (terrain.Min + terrain.Max) * 0.5f;
            orbit.Place(new Vector3(point.x, 0f, point.y), 22f);
        }

        private void ApplyTimeOfDay()
        {
            var daylight = MapStyleLighting.Apply(cam, sun, timeOfDay);
            MapStyleLocation.NightFactor = 1f - daylight;
        }

        // ---- Development actions (they change the game state of this test world directly) ----

        private void ExploreMore()
        {
            if (game.Expedition == null)
            {
                return;
            }

            exploreRadius++;
            var center = terrain.CoreToWorld(game.Expedition.Position);
            foreach (var tile in game.World.Map.Tiles)
            {
                if (Vector2.Distance(center, terrain.CoreToWorld(tile.Coord)) <= exploreRadius * MapStyleWorldTerrain.HexSize * 1.75f)
                {
                    game.Knowledge.SetTileKnowledge(tile.Coord, KnowledgeLevel.Confirmed);
                }
            }

            Refresh(false);
        }

        private void ObserveObstacles()
        {
            foreach (var location in game.World.Locations)
            {
                if (WorldObstaclePathRules.IsRouteObstacleEdge(location) && game.Knowledge.GetTileKnowledge(location.Anchor.PrimaryCoord) != KnowledgeLevel.Unknown)
                {
                    game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
                }
            }

            Refresh(false);
        }

        private void CycleWorldBridgeState()
        {
            foreach (var location in game.World.Locations)
            {
                if (!WorldObstaclePathRules.IsRouteObstacleEdge(location))
                {
                    continue;
                }

                var index = Array.IndexOf(BridgeCycle, location.OperationalStateId);
                location.SetState(LocationStateChannels.Operational, BridgeCycle[(index + 1) % BridgeCycle.Length]);
            }

            Refresh(false);
        }

        // ---- Update and GUI -------------------------------------------------------------------

        private void Update()
        {
            if (!Application.isPlaying || game == null || terrain == null)
            {
                return;
            }

            refreshTimer += Time.deltaTime;
            if (refreshTimer >= RefreshInterval)
            {
                refreshTimer = 0f;
                Refresh(false);
            }
        }

        private void OnGUI()
        {
            if (cam == null || game == null)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            panelRect = new Rect(10f, 10f, 290f, Screen.height / guiScale - 20f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Low-Poly-Welt (V2-Prototyp)");

            var newSource = (Source)GUILayout.Toolbar((int)source, SourceNames);
            if (source == Source.Generated)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Seed", GUILayout.Width(40f));
                seedText = GUILayout.TextField(seedText, 10);
                GUILayout.EndHorizontal();
            }

            if (newSource != source)
            {
                source = newSource;
                GUILayout.EndArea();
                Rebuild();
                return;
            }

            if (GUILayout.Button(source == Source.Generated ? "Welt neu erzeugen" : "Neu laden"))
            {
                GUILayout.EndArea();
                Rebuild();
                return;
            }

            GUILayout.Label("Ansicht");
            var truth = GUILayout.Toolbar(showTruth ? 1 : 0, new[] { "Spielerwissen", "Weltwahrheit" }) == 1;
            if (truth != showTruth)
            {
                showTruth = truth;
                Refresh(true);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kamera (Q/E, WASD)");
            if (GUILayout.Button("⟲")) orbit.Rotate(-25f);
            if (GUILayout.Button("⟳")) orbit.Rotate(25f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label("Entwickleraktionen (ändern den Spielzustand dieser Testwelt)");
            if (GUILayout.Button("Erkunden +1 um die Expedition")) ExploreMore();
            if (GUILayout.Button("Brückenzustand beobachten")) ObserveObstacles();
            if (GUILayout.Button("Welt: Brückenzustand wechseln")) CycleWorldBridgeState();

            GUILayout.Label("Tageszeit");
            var time = GUILayout.HorizontalSlider(timeOfDay, 0f, 100f);
            if (!Mathf.Approximately(time, timeOfDay))
            {
                timeOfDay = time;
                ApplyTimeOfDay();
            }

            GUILayout.Space(6f);
            GUILayout.Label(status);
            GUILayout.Label("Blockierende Kanten ohne Schluchtlinie: " + validationIssues);
            GUILayout.Label("Die Brücke zeigt nur den beobachteten Zustand. Wechselt die Welt ihn, bleibt das Bild, bis er wieder beobachtet wird.");
            GUILayout.EndArea();

            DrawLabels();
        }

        private void DrawLabels()
        {
            foreach (var view in dataViews.Values)
            {
                DrawLabel(view.transform.position, view.DisplayName + " · " + view.StateText);
            }

            foreach (var marker in legacyMarkers.Values)
            {
                if (marker != null && marker.ArchetypeColor.a >= 0.5f && marker.Knowledge < 2)
                {
                    DrawLabel(marker.transform.position, marker.DisplayName + " · " + marker.State);
                }
            }
        }

        private void DrawLabel(Vector3 worldPosition, string text)
        {
            var screen = cam.WorldToScreenPoint(worldPosition + new Vector3(0f, 3.4f, 0f));
            if (screen.z < 0f)
            {
                return;
            }

            var size = GUI.skin.label.CalcSize(new GUIContent(text));
            var rect = new Rect(screen.x / guiScale - size.x * 0.5f - 6f, (Screen.height - screen.y) / guiScale - 11f, size.x + 12f, 22f);
            GUI.Label(rect, text, GUI.skin.box);
        }
    }
}
