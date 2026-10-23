using System;
using Game.App;
using Game.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Development view of the low-poly map for a throwaway <see cref="GameState"/> (tutorial or generated world).
    /// It hosts a <see cref="MapStyleMapController"/> (the same drawing code the game view uses) and adds a camera,
    /// a small panel and development buttons. The buttons change the game state of this test world directly; the
    /// real game does that through commands.
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
        private static readonly string[] BridgeCycle = { "blocked", "repaired", "destroyed", "open" };
        private const float RefreshInterval = 0.25f;

        private GameDataCatalog catalog;
        private GameState game;
        private MapStyleMapController controller;
        private Source source = Source.Tutorial;
        private string seedText = "42";
        private bool showTruth;
        private float timeOfDay = 42f;
        private string error = string.Empty;
        private int validationIssues;
        private int exploreRadius;
        private float refreshTimer;

        private Camera cam;
        private Light sun;
        private MapStyleOrbitCamera orbit;
        private float guiScale = 1f;
        private Rect panelRect;

        public Camera Cam => cam;

        private void Start()
        {
            if (controller == null)
            {
                Rebuild();
            }
        }

        public void Rebuild()
        {
            EnsureSetup();
            try
            {
                controller?.Dispose();
                controller = null;
                error = string.Empty;
                game = CreateGame();
                controller = new MapStyleMapController(game, CreateLayout(game.World), catalog, shader, transform, 1f) { ShowTruth = showTruth };
                controller.Refresh(true);
                exploreRadius = 0;
                validationIssues = WorldObstaclePathRules.Validate(game.World).Count;
                ApplyTimeOfDay();
                FrameExpedition();
            }
            catch (Exception exception)
            {
                error = "Fehler: " + exception.Message;
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

        /// <summary>The tutorial world stores offset coordinates, generated worlds axial ones (as the existing map view reads them).</summary>
        private MapStyleHexLayout CreateLayout(WorldState world)
        {
            return source == Source.Tutorial
                ? MapStyleHexLayout.OffsetCentered(world.Map.Bounds.Width, world.Map.Bounds.Height)
                : MapStyleHexLayout.Axial((world.Map.Bounds.Height - 1) >> 1);
        }

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

        private void FrameExpedition()
        {
            var point = game.Expedition != null
                ? controller.Terrain.CoreToWorld(game.Expedition.Position)
                : (controller.Terrain.Min + controller.Terrain.Max) * 0.5f;
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
            var terrain = controller.Terrain;
            var center = terrain.CoreToWorld(game.Expedition.Position);
            foreach (var tile in game.World.Map.Tiles)
            {
                if (Vector2.Distance(center, terrain.CoreToWorld(tile.Coord)) <= exploreRadius * MapStyleWorldTerrain.HexSize * 1.75f)
                {
                    game.Knowledge.SetTileKnowledge(tile.Coord, KnowledgeLevel.Confirmed);
                }
            }

            controller.Refresh(false);
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

            controller.Refresh(false);
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

            controller.Refresh(false);
        }

        // ---- Update and GUI -------------------------------------------------------------------

        private void Update()
        {
            if (!Application.isPlaying || controller == null)
            {
                return;
            }

            refreshTimer += Time.deltaTime;
            if (refreshTimer >= RefreshInterval)
            {
                refreshTimer = 0f;
                controller.Refresh(false);
            }
        }

        private string StatusText()
        {
            return string.Format("{0} Hexfelder, {1} Orte, zuletzt {2} von {3} Chunks neu ({4} ms)", game.World.Map.Count, game.World.Locations.Count, controller.LastChunks, controller.ChunkTotal, controller.LastMilliseconds);
        }

        private void OnGUI()
        {
            if (cam == null)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            panelRect = new Rect(10f, 10f, 290f, Screen.height / guiScale - 20f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Low-Poly-Welt (Entwicklungsansicht)");

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

            if (controller == null)
            {
                GUILayout.Label(error);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label("Ansicht");
            var truth = GUILayout.Toolbar(showTruth ? 1 : 0, new[] { "Spielerwissen", "Weltwahrheit" }) == 1;
            if (truth != showTruth)
            {
                showTruth = truth;
                controller.ShowTruth = truth;
                controller.Refresh(true);
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
            GUILayout.Label(StatusText());
            GUILayout.Label("Blockierende Kanten ohne Schluchtlinie: " + validationIssues);
            GUILayout.Label("Die Brücke zeigt nur den beobachteten Zustand. Wechselt die Welt ihn, bleibt das Bild, bis er wieder beobachtet wird.");
            GUILayout.EndArea();

            DrawLabels();
        }

        private void DrawLabels()
        {
            foreach (var view in controller.DataViews.Values)
            {
                DrawLabel(view.transform.position, view.DisplayName + " · " + view.StateText);
            }

            foreach (var marker in controller.LegacyMarkers.Values)
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
