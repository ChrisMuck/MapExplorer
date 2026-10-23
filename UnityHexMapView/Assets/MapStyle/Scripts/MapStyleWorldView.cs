using System;
using System.Collections.Generic;
using Game.App;
using Game.Core;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// V1b prototype: renders a real <see cref="GameState"/> (tutorial or generated world) in the low-poly
    /// style. It only reads the state: relief terrain with ravines and rivers from the world paths, roads,
    /// forests, route-obstacle bridges on their ravine, placeholder markers for other locations and the
    /// expedition group. Knowledge limits what is shown. It is a development view and not the game screen;
    /// it changes no game state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapStyleWorldView : MonoBehaviour
    {
        public enum Source
        {
            Tutorial,
            Generated
        }

        public Shader shader;

        private static readonly string[] SourceNames = { "Tutorial", "Generiert" };
        private static readonly string[] PreviewNames = { "Spiel", "intakt", "beschädigt", "repariert", "zerstört", "unbekannt" };

        private GameState game;
        private Source source = Source.Tutorial;
        private string seedText = "42";
        private bool showTruth;
        private float timeOfDay = 42f;
        private int bridgePreview;
        private string status = string.Empty;
        private int validationIssues;
        private int chasmCount;
        private int bridgeCount;

        private Camera cam;
        private Light sun;
        private MapStyleOrbitCamera orbit;
        private Transform root;
        private Material terrainMaterial;
        private Material plainMaterial;
        private MapStyleWorldTerrain terrain;
        private readonly List<MapStyleLocation> markers = new();
        private readonly List<MapStyleLocation> bridges = new();
        private readonly List<MapStyleLocation> scenery = new();
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
            markers.Clear();
            bridges.Clear();
            scenery.Clear();
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

        // ---- Build ------------------------------------------------------------------------

        private void Build()
        {
            Clear();
            terrainMaterial = new Material(shader) { name = "MapStyleWorld-terrain" };
            plainMaterial = new Material(shader) { name = "MapStyleWorld-plain" };
            var world = game.World;
            var useKnowledge = !showTruth;

            terrain = new MapStyleWorldTerrain(world, coord => showTruth ? 0f : MapStyleWorldTerrain.PaleOf(game.Knowledge.GetTileKnowledge(coord)));
            AddMesh("Terrain", terrain.BuildMesh(useKnowledge), terrainMaterial, true);
            AddMesh("Water", terrain.BuildWaterMesh(), plainMaterial, false);

            var occupied = new List<Vector2>();
            BuildLocations(world, useKnowledge, occupied);
            BuildForests(useKnowledge, occupied);
            BuildExpedition(useKnowledge);

            validationIssues = WorldObstaclePathRules.Validate(world).Count;
            chasmCount = 0;
            foreach (var path in world.Paths)
            {
                if (path.Kind == WorldPathKind.Chasm) chasmCount++;
            }

            bridgeCount = bridges.Count;
            ApplyTimeOfDay();
            ApplyBridgePreview();
            FrameExpedition();
            status = string.Format("{0} Hexfelder, {1} Orte, {2} Schluchten, {3} Brücken", world.Map.Count, world.Locations.Count, chasmCount, bridgeCount);
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

        private void BuildLocations(WorldState world, bool useKnowledge, List<Vector2> occupied)
        {
            foreach (var location in world.Locations)
            {
                var level = KnowledgeLevelOf(location.Anchor.PrimaryCoord, useKnowledge);
                if (level >= 2)
                {
                    continue;
                }

                if (WorldObstaclePathRules.IsRouteObstacleEdge(location))
                {
                    var bridge = BuildBridge(location, useKnowledge, level);
                    if (bridge != null)
                    {
                        occupied.Add(new Vector2(bridge.transform.localPosition.x, bridge.transform.localPosition.z));
                        continue;
                    }
                }

                var point = MapStyleWorldTerrain.HexToWorld(location.Anchor.PrimaryCoord);
                occupied.Add(point);
                var marker = CreateMarker(location, GroundAt(point, useKnowledge));
                marker.SetKnowledge(level);
                markers.Add(marker);
            }
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

            var color = ArchetypeColor(location.ArchetypeId);
            marker = MapStyleLocation.Create(string.IsNullOrEmpty(location.Name) ? location.Id : location.Name, new[] { "steht" }, root, position, 0f, shader, color, ArchetypeGaps(location.ArchetypeId), 2.1f);
            marker.Ico(null, new Vector3(0f, 0.5f, 0f), 0.7f, MapStyleLocation.C(0x8d8a80), new Vector3(1f, 0.9f, 1f));
            marker.Box(null, new Vector3(0.9f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 0.08f), MapStyleLocation.C(0x7a5a38));
            marker.Box(null, new Vector3(1.25f, 1.55f, 0f), new Vector3(0.7f, 0.4f, 0.05f), color);
            return marker;
        }

        /// <summary>Places a bridge on the ravine or river line that explains a blocking edge; the deck crosses the line.</summary>
        private MapStyleLocation BuildBridge(SpecialLocationState location, bool useKnowledge, int level)
        {
            var a = MapStyleWorldTerrain.HexToWorld(location.Anchor.Coords[0]);
            var b = MapStyleWorldTerrain.HexToWorld(location.Anchor.Coords[1]);
            var middle = (a + b) * 0.5f;
            var chasm = terrain.Chasms.Nearest(middle);
            var river = terrain.Rivers.Nearest(middle);
            var hit = chasm.Distance <= river.Distance ? chasm : river;
            if (float.IsInfinity(hit.Distance))
            {
                return null;
            }

            var along = new Vector2(-hit.Tangent.y, hit.Tangent.x);
            var deck = new Vector3(along.x, 0f, along.y);
            var end1 = GroundAt(hit.Point + along * 2.8f, useKnowledge);
            var end2 = GroundAt(hit.Point - along * 2.8f, useKnowledge);
            var position = new Vector3(hit.Point.x, (end1.y + end2.y) * 0.5f + 0.2f, hit.Point.y);
            var yaw = Mathf.Atan2(deck.x, deck.z) * Mathf.Rad2Deg;
            var bridge = MapStyleLocations.Bridge(root, position, shader, false, 2.2f, yaw);
            bridge.SetKnowledge(level);
            bridge.SetState(BridgeStateFor(location, useKnowledge), false);
            bridge.Snap();
            bridges.Add(bridge);
            return bridge;
        }

        /// <summary>Operational state as the player knows it; without an observation the bridge shows only the ravine and its posts.</summary>
        private string BridgeStateFor(SpecialLocationState location, bool useKnowledge)
        {
            string operational;
            if (!useKnowledge)
            {
                operational = location.OperationalStateId;
            }
            else
            {
                var known = game.Knowledge.FindLocationCondition(location.Id);
                operational = known?.OperationalStateId;
            }

            switch (operational)
            {
                case "open": return "intakt";
                case "repaired":
                case "provisional": return "repariert";
                case "destroyed": return "zerstört";
                case "blocked":
                case "risky-passage": return "beschädigt";
                default: return "unbekannt";
            }
        }

        private void BuildForests(bool useKnowledge, List<Vector2> occupied)
        {
            var zones = new[] { new List<Vector3>(), new List<Vector3>() };
            var sizes = new[] { new List<float>(), new List<float>() };
            foreach (var pair in terrain.ForestTiles())
            {
                var coord = pair.Key;
                var level = KnowledgeLevelOf(coord, useKnowledge);
                if (level >= 2)
                {
                    continue;
                }

                var center = MapStyleWorldTerrain.HexToWorld(coord);
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
                    foreach (var spot in occupied)
                    {
                        if (Vector2.Distance(spot, point) < 2.6f)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    var ground = GroundAt(point, useKnowledge);
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
                var forest = MapStyleLocations.Forest(root, shader, level == 0 ? "Wald-bestätigt" : "Wald-berichtet", zones[level], sizes[level]);
                forest.SetKnowledge(level);
                scenery.Add(forest);
            }
        }

        private void BuildExpedition(bool useKnowledge)
        {
            if (game.Expedition == null)
            {
                return;
            }

            var point = MapStyleWorldTerrain.HexToWorld(game.Expedition.Position);
            var group = MapStyleLocations.ExpeditionGroup(root, GroundAt(point, useKnowledge), shader, 180f);
            group.transform.localScale = Vector3.one * 0.8f;
            markers.Add(group);
        }

        private void FrameExpedition()
        {
            var point = game.Expedition != null
                ? MapStyleWorldTerrain.HexToWorld(game.Expedition.Position)
                : (terrain.Min + terrain.Max) * 0.5f;
            orbit.Place(new Vector3(point.x, 0f, point.y), 22f);
        }

        private void ApplyTimeOfDay()
        {
            var daylight = MapStyleLighting.Apply(cam, sun, timeOfDay);
            MapStyleLocation.NightFactor = 1f - daylight;
        }

        private void ApplyBridgePreview()
        {
            if (bridgePreview <= 0)
            {
                return;
            }

            foreach (var bridge in bridges)
            {
                bridge.SetState(PreviewNames[bridgePreview], false);
            }
        }

        private static Color ArchetypeColor(string archetypeId)
        {
            switch (archetypeId)
            {
                case "route-obstacle": return MapStyleLocations.ArchetypeObstacle;
                case "investigation-site": return MapStyleLocations.ArchetypeInvestigation;
                case "contact-site": return MapStyleLocations.ArchetypeContact;
                case "containment-site": return MapStyleLocation.C(0xCC79A7);
                case "territorial-marker": return MapStyleLocation.C(0xF0E442);
                case "hazard-site": return MapStyleLocation.C(0xD55E00);
                case "natural-phenomenon": return MapStyleLocations.ArchetypeLandmark;
                default: return MapStyleLocations.ArchetypeTrace;
            }
        }

        private static int ArchetypeGaps(string archetypeId)
        {
            switch (archetypeId)
            {
                case "route-obstacle": return 3;
                case "investigation-site": return 4;
                case "contact-site": return 5;
                case "containment-site": return 2;
                case "territorial-marker": return 7;
                case "hazard-site": return 8;
                case "natural-phenomenon": return 0;
                default: return 6;
            }
        }

        // ---- GUI ----------------------------------------------------------------------------

        private void OnGUI()
        {
            if (cam == null)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            panelRect = new Rect(10f, 10f, 280f, Screen.height / guiScale - 20f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Low-Poly-Welt (V1b-Prototyp)");

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
                GUILayout.EndArea();
                Build();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kamera (Q/E, WASD)");
            if (GUILayout.Button("⟲")) orbit.Rotate(-25f);
            if (GUILayout.Button("⟳")) orbit.Rotate(25f);
            GUILayout.EndHorizontal();

            GUILayout.Label("Brücken zeigen (nur Ansicht)");
            var preview = GUILayout.SelectionGrid(bridgePreview, PreviewNames, 3);
            if (preview != bridgePreview)
            {
                bridgePreview = preview;
                if (preview == 0)
                {
                    GUILayout.EndArea();
                    Build();
                    return;
                }

                ApplyBridgePreview();
            }

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
            GUILayout.Label("Reine Ansicht: ändert keinen Spielzustand. Ohne beobachteten Zustand zeigt die Brücke nur Schlucht und Pfosten.");
            GUILayout.EndArea();

            DrawLabels();
        }

        private void DrawLabels()
        {
            foreach (var marker in markers)
            {
                if (marker == null || marker.ArchetypeColor.a < 0.5f || marker.Knowledge >= 2)
                {
                    continue;
                }

                DrawLabel(marker);
            }

            foreach (var bridge in bridges)
            {
                if (bridge != null && bridge.Knowledge < 2)
                {
                    DrawLabel(bridge);
                }
            }
        }

        private void DrawLabel(MapStyleLocation location)
        {
            var screen = cam.WorldToScreenPoint(location.transform.position + new Vector3(0f, 3.4f, 0f));
            if (screen.z < 0f)
            {
                return;
            }

            var text = location.DisplayName + " · " + location.State;
            var size = GUI.skin.label.CalcSize(new GUIContent(text));
            var rect = new Rect(screen.x / guiScale - size.x * 0.5f - 6f, (Screen.height - screen.y) / guiScale - 11f, size.x + 12f, 22f);
            GUI.Label(rect, text, GUI.skin.box);
        }
    }
}
