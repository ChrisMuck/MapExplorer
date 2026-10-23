using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// V0 style prototype for the low-poly map presentation (see docs/low_poly_map_presentation_concept.md).
    /// A self-contained sandbox: it is not connected to GameState and contains no game rules. It only
    /// shows how a rotatable low-poly map, location states and knowledge levels could look.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapStyleSandbox : MonoBehaviour
    {
        public enum ViewMode
        {
            Landscape,
            Showcase
        }

        public Shader shader;

        private static readonly string[] BridgeStates = { "intakt", "beschädigt", "repariert", "zerstört" };
        private static readonly string[] KnowledgeNames = { "bestätigt", "berichtet", "unbekannt" };
        private static readonly string[] ViewNames = { "Landschaft", "Schaufenster" };

        private Camera cam;
        private Light sun;
        private Transform root;
        private Material terrainMaterial;
        private Material plainMaterial;
        private MeshFilter terrainFilter;
        private Mesh terrainTruth;
        private Mesh terrainKnown;
        private readonly List<MapStyleLocation> locations = new();
        private readonly List<MapStyleLocation> scenery = new();
        private MapStyleLocation bridge;

        private ViewMode mode = ViewMode.Landscape;
        private bool showTruth;
        private float timeOfDay = 42f;
        private float yaw = 40f;
        private float pitch = 36f;
        private float orthoSize = 12f;
        private float orthoSizeTarget = 12f;
        private Vector3 target = new Vector3(-4f, 0f, 1f);
        private Vector3 targetGoal = new Vector3(-4f, 0f, 1f);
        private int selected = -1;
        private bool playing;
        private float playTimer;
        private bool dragging;
        private float guiScale = 1f;
        private Rect panelRect;

        public Camera Cam => cam;
        public IReadOnlyList<MapStyleLocation> Locations => locations;
        public ViewMode Mode => mode;

        private void Start()
        {
            if (root == null)
            {
                Build(ViewMode.Landscape);
            }
        }

        public void Build(ViewMode newMode)
        {
            mode = newMode;
            Clear();
            EnsureSetup();
            terrainMaterial = new Material(shader) { name = "MapStyle-terrain" };
            plainMaterial = new Material(shader) { name = "MapStyle-plain" };
            selected = -1;
            if (mode == ViewMode.Landscape)
            {
                BuildLandscape();
                targetGoal = new Vector3(-4f, 0f, 1f);
                orthoSizeTarget = 13f;
            }
            else
            {
                BuildShowcase();
                targetGoal = Vector3.zero;
                orthoSizeTarget = 15.5f;
            }

            target = targetGoal;
            orthoSize = orthoSizeTarget;
            SetTimeOfDay(timeOfDay);
            ApplyKnowledge();
            ApplyCamera();
        }

        // ---- Setup -----------------------------------------------------------------------

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
                cam = camera.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 200f;
                camera.tag = "MainCamera";
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
            locations.Clear();
            scenery.Clear();
            bridge = null;
            if (root != null)
            {
                DestroyObject(root.gameObject);
            }

            root = new GameObject("MapStyleWorld").transform;
            root.SetParent(transform, false);
        }

        private static void DestroyObject(GameObject go)
        {
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private GameObject MeshObject(string name, Mesh mesh, Material material, bool shadows)
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

        // ---- Landscape -------------------------------------------------------------------

        private static readonly Vector2[][] Roads =
        {
            new[] { new Vector2(-9f, 7f), new Vector2(-7f, 5f), new Vector2(-6f, 3.4f) },
            new[] { new Vector2(-6f, -2.4f), new Vector2(-5.2f, -5f), new Vector2(-4f, -7.5f) }
        };

        private void BuildLandscape()
        {
            terrainTruth = MapStyleTerrain.Build(false, Roads);
            terrainKnown = MapStyleTerrain.Build(true, Roads);
            var terrain = MeshObject("Terrain", terrainKnown, terrainMaterial, true);
            terrainFilter = terrain.GetComponent<MeshFilter>();
            MeshObject("Water", MapStyleTerrain.BuildWater(), plainMaterial, false);

            var campPos = Ground(-9f, 7f);
            var gravePos = Ground(-3.5f, -8.5f);
            var villagePos = Ground(-15f, -2f);
            var towerPos = Ground(3.5f, -6f);
            var cavePos = Ground(12f, 4f);

            Register(MapStyleLocations.Camp(root, campPos, shader));
            Register(MapStyleLocations.Grave(root, gravePos, shader));
            Register(MapStyleLocations.Village(root, villagePos, shader));
            Register(MapStyleLocations.Tower(root, towerPos, shader));
            Register(MapStyleLocations.Cave(root, cavePos, shader));

            var crossingZ = MapStyleTerrain.DitchCenterZ(-6f);
            var deckY = (MapStyleTerrain.Height(-6f, crossingZ - 2.8f) + MapStyleTerrain.Height(-6f, crossingZ + 2.8f)) * 0.5f + 0.2f;
            bridge = Register(MapStyleLocations.Bridge(root, new Vector3(-6f, deckY, crossingZ), shader, false));
            Register(MapStyleLocations.ExpeditionGroup(root, Ground(-6.2f, 5.6f), shader, 180f));

            var avoid = new[] { campPos, gravePos, villagePos, towerPos, cavePos };
            BuildForests(avoid);
            BuildRoute();
        }

        private static Vector3 Ground(float x, float z)
        {
            return new Vector3(x, MapStyleTerrain.Height(x, z), z);
        }

        private MapStyleLocation Register(MapStyleLocation location)
        {
            locations.Add(location);
            return location;
        }

        private void BuildForests(Vector3[] avoid)
        {
            var random = new System.Random(7);
            double Next() => random.NextDouble();
            var zones = new[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
            var sizes = new[] { new List<float>(), new List<float>(), new List<float>() };

            void Try(float x, float z, float size)
            {
                if (Mathf.Abs(z - MapStyleTerrain.DitchCenterZ(x)) < 2.7f) return;
                if (MapStyleTerrain.DistanceToRoads(Roads, x, z) < 1.2f) return;
                var y = MapStyleTerrain.Height(x, z);
                if (y > 1.8f || y < -0.5f) return;
                foreach (var a in avoid)
                {
                    if (Mathf.Sqrt((x - a.x) * (x - a.x) + (z - a.z) * (z - a.z)) < 3.8f) return;
                }

                var level = MapStyleTerrain.KnowledgeLevel(x);
                zones[level].Add(new Vector3(x, y, z));
                sizes[level].Add(size);
            }

            for (var i = 0; i < 90; i++) Try((float)(-18.5 + Next() * 9.5), (float)(3 + Next() * 14), (float)(0.8 + Next() * 0.6));
            for (var i = 0; i < 30; i++) Try((float)(-2 + Next() * 10), (float)(-14 + Next() * 28), (float)(0.8 + Next() * 0.5));
            for (var i = 0; i < 24; i++) Try((float)(9 + Next() * 10), (float)(-14 + Next() * 28), (float)(0.8 + Next() * 0.5));
            for (var i = 0; i < 14; i++) Try((float)(-9 + Next() * 8), (float)(-14 + Next() * 5), (float)(0.9 + Next() * 0.5));

            for (var level = 0; level < 3; level++)
            {
                var forest = MapStyleLocations.Forest(root, shader, "Wald-" + KnowledgeNames[level], zones[level], sizes[level]);
                forest.SetKnowledge(level);
                scenery.Add(forest);
            }
        }

        private void BuildRoute()
        {
            var points = new List<Vector3>();
            var path = new[] { new Vector2(-9f, 7f), new Vector2(-8f, 6f), new Vector2(-7f, 5f), new Vector2(-6.4f, 4.2f), new Vector2(-6f, 3.5f) };
            for (var i = 0; i < path.Length - 1; i++)
            {
                for (var s = 0; s < 4; s++)
                {
                    var p = Vector2.Lerp(path[i], path[i + 1], s / 4f);
                    points.Add(new Vector3(p.x + 0.5f, MapStyleTerrain.Height(p.x + 0.5f, p.y) + 0.22f, p.y));
                }
            }

            scenery.Add(MapStyleLocations.Route(root, shader, points));
        }

        // ---- Showcase --------------------------------------------------------------------

        private void BuildShowcase()
        {
            var groundBuilder = new MapStyleMeshBuilder();
            groundBuilder.AddTriangleUp(new Vector3(-45f, -0.6f, -45f), new Vector3(-45f, -0.6f, 45f), new Vector3(45f, -0.6f, 45f), MapStyleLocation.C(0xa7b08f));
            groundBuilder.AddTriangleUp(new Vector3(-45f, -0.6f, -45f), new Vector3(45f, -0.6f, 45f), new Vector3(45f, -0.6f, -45f), MapStyleLocation.C(0xa7b08f));
            MeshObject("Ground", groundBuilder.Build("ground"), plainMaterial, false);

            var slots = new[]
            {
                new Vector3(-8f, 0f, -4.2f), new Vector3(0f, 0f, -4.2f), new Vector3(8f, 0f, -4.2f),
                new Vector3(-8f, 0f, 4.2f), new Vector3(0f, 0f, 4.2f), new Vector3(8f, 0f, 4.2f)
            };
            foreach (var slot in slots)
            {
                var pedestal = new MapStyleMeshBuilder();
                pedestal.AddPrism(new Vector3(0f, -0.3f, 0f), new Vector3(7.8f, 0.6f, 7.8f), Vector3.zero, 6, 0.92f, MapStyleLocation.C(0x6b5a3e));
                pedestal.AddPrism(new Vector3(0f, 0.01f, 0f), new Vector3(7.2f, 0.02f, 7.2f), Vector3.zero, 6, 1f, MapStyleLocation.C(0x82ac5c));
                var go = MeshObject("Pedestal", pedestal.Build("pedestal"), plainMaterial, true);
                go.transform.localPosition = slot;
            }

            Register(MapStyleLocations.Grave(root, slots[0], shader));
            Register(MapStyleLocations.Cave(root, slots[1], shader));
            Register(MapStyleLocations.Village(root, slots[2], shader));
            Register(MapStyleLocations.Tower(root, slots[3], shader));
            Register(MapStyleLocations.Camp(root, slots[4], shader));
            bridge = Register(MapStyleLocations.Bridge(root, slots[5], shader, true));
        }

        // ---- Knowledge, time, camera -----------------------------------------------------

        public void SetShowTruth(bool truth)
        {
            showTruth = truth;
            ApplyKnowledge();
        }

        public void SetBridgeState(string state, bool puff = true)
        {
            bridge?.SetState(state, puff);
        }

        public void SetLocationState(int index, string state, bool puff = true)
        {
            locations[index].SetState(state, puff);
        }

        public void SetLocationKnowledge(int index, int level)
        {
            locations[index].SetKnowledge(level);
        }

        private void ApplyKnowledge()
        {
            if (mode == ViewMode.Landscape)
            {
                if (terrainFilter != null)
                {
                    terrainFilter.sharedMesh = showTruth ? terrainTruth : terrainKnown;
                }

                foreach (var location in locations)
                {
                    var level = showTruth ? 0 : MapStyleTerrain.KnowledgeLevel(location.transform.localPosition.x);
                    location.SetKnowledge(level);
                }

                for (var i = 0; i < scenery.Count; i++)
                {
                    if (scenery[i].DisplayName.StartsWith("Wald", StringComparison.Ordinal))
                    {
                        scenery[i].SetKnowledge(showTruth ? 0 : i);
                    }
                }
            }
        }

        public void SnapAll()
        {
            foreach (var location in locations)
            {
                location.Snap();
            }

            foreach (var item in scenery)
            {
                item.Snap();
            }
        }

        public void SetTimeOfDay(float value)
        {
            timeOfDay = value;
            var daylight = MapStyleLighting.Apply(cam, sun, value);
            MapStyleLocation.NightFactor = 1f - daylight;
        }

        public void SetCameraView(Vector3 newTarget, float size, float newYaw, float newPitch = 36f)
        {
            target = targetGoal = newTarget;
            orthoSize = orthoSizeTarget = size;
            yaw = newYaw;
            pitch = newPitch;
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            if (cam == null)
            {
                return;
            }

            cam.orthographicSize = orthoSize;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.position = target - rotation * Vector3.forward * 70f;
            cam.transform.rotation = rotation;
        }

        // ---- Runtime input and GUI -------------------------------------------------------

        private void Update()
        {
            if (cam == null || !Application.isPlaying)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            HandleCameraInput();
            var blend = 1f - Mathf.Exp(-Time.deltaTime * 4f);
            target = Vector3.Lerp(target, targetGoal, blend);
            orthoSize = Mathf.Lerp(orthoSize, orthoSizeTarget, blend);
            ApplyCamera();

            if (playing)
            {
                playTimer += Time.deltaTime;
                if (playTimer > 2.6f)
                {
                    playTimer = 0f;
                    AdvanceStates();
                }
            }
        }

        private void HandleCameraInput()
        {
            var mouse = Input.mousePosition;
            var overPanel = panelRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y) / guiScale);
            if (Input.GetMouseButtonDown(0))
            {
                dragging = !overPanel;
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            if (dragging)
            {
                yaw -= Input.GetAxis("Mouse X") * 3.2f;
            }

            if (!overPanel)
            {
                orthoSizeTarget = Mathf.Clamp(orthoSizeTarget * (1f - Input.mouseScrollDelta.y * 0.08f), 3f, 24f);
            }

            if (Input.GetKey(KeyCode.Q)) yaw -= 70f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) yaw += 70f * Time.deltaTime;
        }

        private void AdvanceStates()
        {
            if (mode == ViewMode.Landscape)
            {
                if (bridge != null)
                {
                    Cycle(bridge);
                }

                return;
            }

            if (selected >= 0)
            {
                Cycle(locations[selected]);
            }
            else
            {
                foreach (var location in locations)
                {
                    Cycle(location);
                }
            }
        }

        private static void Cycle(MapStyleLocation location)
        {
            var index = Array.IndexOf(location.States, location.State);
            location.SetState(location.States[(index + 1) % location.States.Length]);
        }

        private void Select(int index)
        {
            selected = index;
            if (index < 0)
            {
                targetGoal = Vector3.zero;
                orthoSizeTarget = 15.5f;
            }
            else
            {
                targetGoal = locations[index].transform.localPosition + new Vector3(0f, 0.8f, 0f);
                orthoSizeTarget = 6f;
            }
        }

        private void OnGUI()
        {
            if (cam == null || locations.Count == 0)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            var height = Screen.height / guiScale - 20f;
            panelRect = new Rect(10f, 10f, 270f, height);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Low-Poly-Kartenstil (V0-Prototyp)");

            var newMode = (ViewMode)GUILayout.Toolbar((int)mode, ViewNames);
            if (newMode != mode)
            {
                Build(newMode);
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kamera (Q/E, Ziehen)");
            if (GUILayout.Button("⟲")) yaw -= 25f;
            if (GUILayout.Button("⟳")) yaw += 25f;
            GUILayout.EndHorizontal();

            if (mode == ViewMode.Landscape)
            {
                GUILayout.Label("Brücke");
                var current = Array.IndexOf(BridgeStates, bridge.State);
                var chosen = GUILayout.SelectionGrid(current, BridgeStates, 2);
                if (chosen != current)
                {
                    bridge.SetState(BridgeStates[chosen]);
                }

                GUILayout.Label("Ansicht");
                var truth = GUILayout.Toolbar(showTruth ? 1 : 0, new[] { "Spielerwissen", "Weltwahrheit" }) == 1;
                if (truth != showTruth)
                {
                    SetShowTruth(truth);
                }
            }
            else
            {
                GUILayout.Label("Ort");
                var names = new List<string> { "Alle" };
                foreach (var location in locations)
                {
                    names.Add(location.DisplayName);
                }

                var pick = GUILayout.SelectionGrid(selected + 1, names.ToArray(), 3) - 1;
                if (pick != selected)
                {
                    Select(pick);
                }

                if (selected >= 0)
                {
                    var location = locations[selected];
                    GUILayout.Label("Zustand");
                    var state = GUILayout.SelectionGrid(Array.IndexOf(location.States, location.State), location.States, 2);
                    if (location.States[state] != location.State)
                    {
                        location.SetState(location.States[state]);
                    }
                }

                GUILayout.Label("Wissen");
                var knowledge = locations[Mathf.Max(0, selected)].Knowledge;
                var level = GUILayout.Toolbar(knowledge, KnowledgeNames);
                if (level != knowledge)
                {
                    if (selected >= 0)
                    {
                        locations[selected].SetKnowledge(level);
                    }
                    else
                    {
                        foreach (var location in locations)
                        {
                            location.SetKnowledge(level);
                        }
                    }
                }
            }

            var wantsPlay = GUILayout.Toggle(playing, "Zustände durchlaufen");
            if (wantsPlay != playing)
            {
                playing = wantsPlay;
                playTimer = 99f;
            }

            GUILayout.Label("Tageszeit");
            var time = GUILayout.HorizontalSlider(timeOfDay, 0f, 100f);
            if (!Mathf.Approximately(time, timeOfDay))
            {
                SetTimeOfDay(time);
            }

            GUILayout.Space(6f);
            GUILayout.Label("Platzhalter aus Grundformen. Die Karte zeigt nur den Wissensstand; nichts hier verändert Spielregeln.");
            GUILayout.EndArea();

            DrawLabels();
        }

        private void DrawLabels()
        {
            for (var i = 0; i < locations.Count; i++)
            {
                var location = locations[i];
                if (location.ArchetypeColor.a < 0.5f)
                {
                    continue;
                }

                var world = location.transform.position + new Vector3(0f, 4.4f, 0f);
                var screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f)
                {
                    continue;
                }

                var text = location.Knowledge == 2 ? "unbekannt" : location.DisplayName + " · " + location.State;
                var size = GUI.skin.label.CalcSize(new GUIContent(text));
                var rect = new Rect(screen.x / guiScale - size.x * 0.5f - 6f, (Screen.height - screen.y) / guiScale - 11f, size.x + 12f, 22f);
                if (GUI.Button(rect, text) && mode == ViewMode.Showcase)
                {
                    Select(i);
                }
            }
        }
    }
}
