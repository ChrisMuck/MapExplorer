using System;
using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Gallery of every place that has a data composition: pick a place, set its three state channels, change what the
    /// player knows and the time of day, or lay out all values of one channel side by side. It draws through the same
    /// resolver and view code as the game, on a flat plate, so nothing has to be reached by walking through a world.
    /// Created by the menu item "Map Style/Create Site Gallery Scene".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapStyleSiteGallery : MonoBehaviour
    {
        public Shader shader;

        private enum Layout
        {
            Single,
            RowOperational,
            RowPresence,
            RowInteraction
        }

        private sealed class Entry
        {
            public string ArchetypeId;
            public string VariantId;
            public string Name;
            public SpecialLocationState Location;
        }

        private static readonly string[] Channels = { LocationStateChannels.Operational, LocationStateChannels.Presence, LocationStateChannels.Interaction };
        private static readonly string[] ChannelLabels = { "Betrieb", "Anwesenheit", "Erkundung" };
        private static readonly string[] LayoutNames = { "Einzeln", "Reihe: Betrieb", "Reihe: Anwesenheit", "Reihe: Erkundung" };
        private static readonly string[] KnowledgeNames = { "bestätigt", "alt", "gemeldet" };
        private const float RowSpacing = 7f;

        private GameDataCatalog catalog;
        private MapStyleVisualKit kit;
        private LocationVisualResolver resolver;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<MapStyleLocationView> views = new List<MapStyleLocationView>();
        private readonly string[] chosen = new string[3];
        private GameObject ground;
        private Material groundMaterial;
        private Camera cam;
        private Light sun;
        private MapStyleOrbitCamera orbit;
        private Layout layout = Layout.Single;
        private int index;
        private int knowledge;
        private bool stateUnknown;
        private float timeOfDay = 42f;
        private string error = string.Empty;
        private Vector2 listScroll;
        private float guiScale = 1f;
        private Rect panelRect;

        private void Start()
        {
            try
            {
                EnsureSetup();
                BuildEntries();
                Select(0);
            }
            catch (Exception exception)
            {
                error = "Fehler: " + exception.Message;
                Debug.LogException(exception);
            }
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
                kit = new MapStyleVisualKit(catalog.VisualDefinitions);
                resolver = new LocationVisualResolver(catalog.VisualDefinitions);
            }

            if (cam == null)
            {
                var camera = new GameObject("GalleryCamera");
                camera.transform.SetParent(transform, false);
                camera.tag = "MainCamera";
                cam = camera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                orbit = camera.AddComponent<MapStyleOrbitCamera>();
                orbit.Cam = cam;
                orbit.MinSize = 2.5f;
                orbit.IsOverUi = position => panelRect.Contains(new Vector2(position.x, Screen.height - position.y) / guiScale);
            }

            if (sun == null)
            {
                var light = new GameObject("GallerySun");
                light.transform.SetParent(transform, false);
                sun = light.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.85f;
                sun.shadowBias = 0.05f;
                sun.shadowNormalBias = 0.4f;
            }

            if (groundMaterial == null)
            {
                groundMaterial = new Material(shader) { name = "Gallery-ground" };
            }

            ApplyTimeOfDay();
        }

        private void BuildEntries()
        {
            entries.Clear();
            foreach (var composition in catalog.VisualDefinitions.Compositions)
            {
                var isBase = composition.VariantId == null && VisualDefinitionSet.PseudoArchetypeIds.Contains(composition.ArchetypeId);
                if (composition.AnchorMode != "point" || (composition.VariantId == null && !isBase))
                {
                    continue;
                }

                if (isBase)
                {
                    var baseLocation = new SpecialLocationState("gallery-base", LocationKind.BaseCamp, new HexCoord(0, 0), "Expeditionsbasis");
                    entries.Add(new Entry { ArchetypeId = composition.ArchetypeId, VariantId = null, Name = baseLocation.Name, Location = baseLocation });
                    continue;
                }

                var name = catalog.VariantLabels.TryGetValue(composition.VariantId, out var label) ? label : composition.VariantId;
                var location = new SpecialLocationState(
                    "gallery-" + composition.VariantId, LocationKind.Landmark, new HexCoord(0, 0), name,
                    LocationAnchor.Point(new HexCoord(0, 0)), composition.ArchetypeId, composition.VariantId);
                entries.Add(new Entry { ArchetypeId = composition.ArchetypeId, VariantId = composition.VariantId, Name = name, Location = location });
            }

            entries.Sort((a, b) =>
            {
                var byArchetype = string.CompareOrdinal(a.ArchetypeId, b.ArchetypeId);
                return byArchetype != 0 ? byArchetype : string.CompareOrdinal(a.Name, b.Name);
            });
        }

        private void Select(int newIndex)
        {
            if (entries.Count == 0)
            {
                return;
            }

            index = (newIndex % entries.Count + entries.Count) % entries.Count;
            for (var channel = 0; channel < Channels.Length; channel++)
            {
                chosen[channel] = InitialState(entries[index], Channels[channel]);
            }

            Rebuild();
        }

        private string InitialState(Entry entry, string channel)
        {
            var scenario = catalog.Authoring.ScenarioProfiles.Values.FirstOrDefault(profile => profile.ArchetypeId == entry.ArchetypeId && profile.VariantId == entry.VariantId);
            if (scenario != null
                && catalog.Authoring.StateProfiles.TryGetValue(scenario.StateProfileId, out var profile)
                && profile.Channels.TryGetValue(channel, out var definition))
            {
                return definition.InitialValue;
            }

            var values = MapStyleDebug.StateValues(catalog, entry.Location, channel);
            return values.Count > 0 ? values[0] : null;
        }

        private IReadOnlyList<string> Values(int channel) => MapStyleDebug.StateValues(catalog, entries[index].Location, Channels[channel]);

        private int RowChannel => layout == Layout.Single ? -1 : (int)layout - 1;

        private void Rebuild()
        {
            foreach (var view in views)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            views.Clear();
            if (ground != null)
            {
                Destroy(ground);
            }

            var entry = entries[index];
            var rowChannel = RowChannel;
            var count = rowChannel < 0 ? 1 : Mathf.Max(1, Values(rowChannel).Count);
            for (var i = 0; i < count; i++)
            {
                var states = (string[])chosen.Clone();
                if (rowChannel >= 0)
                {
                    states[rowChannel] = Values(rowChannel)[i];
                }

                var description = Resolve(entry, states);
                var x = (i - (count - 1) * 0.5f) * RowSpacing;
                var view = MapStyleLocationView.Create(entry.Location.Id, entry.Name, transform, new Vector3(x, 0f, 0f), 0f, shader, kit);
                view.transform.localScale = Vector3.one * description.Scale;
                view.Apply(description);
                view.Snap();
                views.Add(view);
            }

            BuildGround(count);
            orbit.Place(Vector3.zero, rowChannel < 0 ? 4.5f : 3.5f + count * 2.2f);
        }

        private LocationVisualDescription Resolve(Entry entry, string[] states)
        {
            var kind = (VisualKnowledge)knowledge;
            return stateUnknown
                ? resolver.Resolve(entry.Location, new LocationVisualContext(kind, null, null, null))
                : resolver.Resolve(entry.Location, new LocationVisualContext(kind, states[0], states[1], states[2]));
        }

        private void Reapply()
        {
            var rowChannel = RowChannel;
            for (var i = 0; i < views.Count; i++)
            {
                var states = (string[])chosen.Clone();
                if (rowChannel >= 0)
                {
                    states[rowChannel] = Values(rowChannel)[i];
                }

                views[i].Apply(Resolve(entries[index], states));
            }
        }

        private void BuildGround(int count)
        {
            var builder = new MapStyleMeshBuilder();
            var radius = 5f + (count - 1) * RowSpacing * 0.5f;
            const int Segments = 40;
            for (var i = 0; i < Segments; i++)
            {
                var a0 = i / (float)Segments * Mathf.PI * 2f;
                var a1 = (i + 1) / (float)Segments * Mathf.PI * 2f;
                var shade = 0.92f + 0.08f * Mathf.Sin(i * 2.3f);
                var color = new Color(0.55f * shade, 0.68f * shade, 0.38f * shade);
                builder.AddTriangleUp(new Vector3(0f, 0f, 0f), new Vector3(Mathf.Sin(a0) * radius * 1.4f, 0f, Mathf.Cos(a0) * radius), new Vector3(Mathf.Sin(a1) * radius * 1.4f, 0f, Mathf.Cos(a1) * radius), color);
            }

            ground = new GameObject("GalleryGround");
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            ground.AddComponent<MeshFilter>().sharedMesh = builder.Build("GalleryGround");
            var renderer = ground.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = groundMaterial;
        }

        private void ApplyTimeOfDay()
        {
            var daylight = MapStyleLighting.Apply(cam, sun, timeOfDay);
            MapStyleLocation.NightFactor = 1f - daylight;
        }

        private void Update()
        {
            if (entries.Count == 0)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.RightBracket))
            {
                Select(index + 1);
            }
            else if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.LeftBracket))
            {
                Select(index - 1);
            }
        }

        private void OnGUI()
        {
            if (cam == null)
            {
                return;
            }

            guiScale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            panelRect = new Rect(10f, 10f, 300f, Screen.height / guiScale - 20f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Ortsgalerie (Entwicklungsansicht)");
            if (entries.Count == 0)
            {
                GUILayout.Label(string.IsNullOrEmpty(error) ? "Keine Orte mit Kompositionen." : error);
                GUILayout.EndArea();
                return;
            }

            var entry = entries[index];
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ zurück")) { GUILayout.EndHorizontal(); GUILayout.EndArea(); Select(index - 1); return; }
            if (GUILayout.Button("weiter ▶")) { GUILayout.EndHorizontal(); GUILayout.EndArea(); Select(index + 1); return; }
            GUILayout.EndHorizontal();
            GUILayout.Label(string.Format("{0} ({1}/{2}) · Bild-Hoch/Runter oder [ ] blättert", entry.Name, index + 1, entries.Count));

            var newLayout = (Layout)GUILayout.SelectionGrid((int)layout, LayoutNames, 2);
            if (newLayout != layout)
            {
                layout = newLayout;
                GUILayout.EndArea();
                Rebuild();
                return;
            }

            var stateChanged = false;
            for (var channel = 0; channel < Channels.Length; channel++)
            {
                var values = Values(channel);
                if (values.Count == 0 || channel == RowChannel)
                {
                    continue;
                }

                GUILayout.Label(ChannelLabels[channel]);
                var names = values.ToArray();
                var current = Array.IndexOf(names, chosen[channel]);
                var picked = GUILayout.SelectionGrid(current, names, 3);
                if (picked >= 0 && picked != current)
                {
                    chosen[channel] = names[picked];
                    stateChanged = true;
                }
            }

            GUILayout.Space(4f);
            var unknown = GUILayout.Toggle(stateUnknown, "Zustand dem Spieler unbekannt");
            var newKnowledge = GUILayout.Toolbar(knowledge, KnowledgeNames);
            if (unknown != stateUnknown || newKnowledge != knowledge)
            {
                stateUnknown = unknown;
                knowledge = newKnowledge;
                stateChanged = true;
            }

            if (stateChanged)
            {
                Reapply();
            }

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Kamera (Q/E)");
            if (GUILayout.Button("⟲")) orbit.Rotate(-25f);
            if (GUILayout.Button("⟳")) orbit.Rotate(25f);
            GUILayout.EndHorizontal();
            GUILayout.Label("Tageszeit");
            var time = GUILayout.HorizontalSlider(timeOfDay, 0f, 100f);
            if (!Mathf.Approximately(time, timeOfDay))
            {
                timeOfDay = time;
                ApplyTimeOfDay();
            }

            GUILayout.Space(6f);
            GUILayout.Label("Alle Orte");
            listScroll = GUILayout.BeginScrollView(listScroll);
            string lastArchetype = null;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].ArchetypeId != lastArchetype)
                {
                    lastArchetype = entries[i].ArchetypeId;
                    GUILayout.Label("— " + lastArchetype);
                }

                var previous = GUI.color;
                if (i == index) GUI.color = new Color(1f, 0.9f, 0.5f);
                var clicked = GUILayout.Button(entries[i].Name);
                GUI.color = previous;
                if (clicked)
                {
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                    Select(i);
                    return;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            DrawLabels();
        }

        private void DrawLabels()
        {
            foreach (var view in views)
            {
                if (view == null) continue;
                var screen = cam.WorldToScreenPoint(view.transform.position + new Vector3(0f, 3.4f, 0f));
                if (screen.z < 0f) continue;
                var text = view.DisplayName + " · " + view.StateText;
                var size = GUI.skin.label.CalcSize(new GUIContent(text));
                var rect = new Rect(screen.x / guiScale - size.x * 0.5f - 6f, (Screen.height - screen.y) / guiScale - 11f, size.x + 12f, 22f);
                GUI.Label(rect, text, GUI.skin.box);
            }
        }
    }
}
