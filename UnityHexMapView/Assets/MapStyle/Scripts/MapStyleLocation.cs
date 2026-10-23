using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>Pose override for a part in one state: offset, extra rotation (degrees) and scale multiplier.</summary>
    public sealed class Pz
    {
        public Vector3 Pos = Vector3.zero;
        public Vector3 Rot = Vector3.zero;
        public Vector3 Scale = Vector3.one;
    }

    /// <summary>
    /// One place on the map built from named parts. Each part lists the states in which it is visible
    /// and may override its pose per state; state changes blend smoothly. The component only
    /// presents a state it is given; it never decides one. Knowledge only changes how it looks.
    /// </summary>
    public sealed class MapStyleLocation : MonoBehaviour
    {
        private sealed class PartRuntime
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public string[] Shows;
            public Dictionary<string, Pz> At;
            public Vector3 BasePos;
            public Quaternion BaseRot;
            public Vector3 BaseScale;
            public float Visible;
            public Vector3 ScaleFactor = Vector3.one;
        }

        private sealed class EmitterRuntime
        {
            public string[] States;
            public Vector3 Origin;
            public float Size;
            public Transform[] Puffs;
            public float[] Age;
            public bool[] Off;
        }

        private sealed class LightRuntime
        {
            public Light Light;
            public string[] States;
            public float Base;
            public float Current;
        }

        private sealed class Transient
        {
            public Transform Transform;
            public Vector3 Velocity;
            public float Age;
        }

        public static float NightFactor;
        public static float LightScale = 4f;

        private readonly List<PartRuntime> parts = new();
        private readonly List<EmitterRuntime> emitters = new();
        private readonly List<LightRuntime> lights = new();
        private readonly List<Transient> transients = new();

        private Transform partsRoot;
        private GameObject ringKnown;
        private GameObject ringUnknown;
        private Material litMaterial;
        private Material emissiveMaterial;
        private Material ringMaterial;
        private Mesh dustMesh;

        public string DisplayName { get; private set; }
        public string[] States { get; private set; }
        public string State { get; private set; }
        public int Knowledge { get; private set; }
        public Color ArchetypeColor { get; private set; }

        public static MapStyleLocation Create(
            string displayName,
            string[] states,
            Transform parent,
            Vector3 position,
            float yaw,
            Shader shader,
            Color archetypeColor,
            int ringGaps)
        {
            var go = new GameObject(displayName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var location = go.AddComponent<MapStyleLocation>();
            location.Initialize(displayName, states, shader, archetypeColor, ringGaps);
            return location;
        }

        private void Initialize(string displayName, string[] states, Shader shader, Color archetypeColor, int ringGaps)
        {
            DisplayName = displayName;
            States = states;
            State = states[0];
            ArchetypeColor = archetypeColor;
            litMaterial = new Material(shader) { name = displayName + "-lit" };
            emissiveMaterial = new Material(shader) { name = displayName + "-emissive" };
            emissiveMaterial.SetFloat("_Emissive", 1.2f);
            ringMaterial = new Material(shader) { name = displayName + "-ring" };
            ringMaterial.SetFloat("_Flat", 1f);
            partsRoot = new GameObject("Parts").transform;
            partsRoot.SetParent(transform, false);

            if (ringGaps >= 0)
            {
                var ring = new MapStyleMeshBuilder();
                ring.AddRing(new Vector3(0f, 0.04f, 0f), 3.3f, 3.55f, 36, ringGaps, archetypeColor);
                ringKnown = CreateMeshObject("RingKnown", ring.Build(displayName + "-ring"), ringMaterial, false);
                ringKnown.transform.SetParent(transform, false);

                var unknown = new MapStyleMeshBuilder();
                unknown.AddRing(new Vector3(0f, 0.04f, 0f), 3.3f, 3.55f, 36, 6, new Color32(0xa0, 0x9c, 0x90, 255));
                unknown.AddIco(new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.8f, 1f), Vector3.zero, new Color32(0xcf, 0xca, 0xb8, 255));
                unknown.AddIco(new Vector3(0.1f, 0.7f, 0.05f), new Vector3(0.75f, 0.6f, 0.75f), new Vector3(0f, 30f, 0f), new Color32(0xd6, 0xd1, 0xc0, 255));
                unknown.AddIco(new Vector3(-0.05f, 1.05f, 0f), new Vector3(0.55f, 0.45f, 0.55f), new Vector3(0f, 70f, 0f), new Color32(0xdd, 0xd8, 0xc8, 255));
                ringUnknown = CreateMeshObject("Unknown", unknown.Build(displayName + "-unknown"), litMaterial, true);
                ringUnknown.transform.SetParent(transform, false);
                ringUnknown.SetActive(false);
            }
        }

        // ---- Building parts --------------------------------------------------------------

        public static string[] S(params string[] states)
        {
            return states;
        }

        public static Dictionary<string, Pz> At(string state, Pz pose)
        {
            return new Dictionary<string, Pz> { { state, pose } };
        }

        public static Dictionary<string, Pz> At(string state1, Pz pose1, string state2, Pz pose2)
        {
            return new Dictionary<string, Pz> { { state1, pose1 }, { state2, pose2 } };
        }

        public static Dictionary<string, Pz> At(string state1, Pz pose1, string state2, Pz pose2, string state3, Pz pose3)
        {
            return new Dictionary<string, Pz> { { state1, pose1 }, { state2, pose2 }, { state3, pose3 } };
        }

        public static Pz Pose(Vector3? pos = null, Vector3? rot = null, Vector3? scale = null)
        {
            return new Pz { Pos = pos ?? Vector3.zero, Rot = rot ?? Vector3.zero, Scale = scale ?? Vector3.one };
        }

        public static Color C(int hex)
        {
            return new Color32((byte)((hex >> 16) & 255), (byte)((hex >> 8) & 255), (byte)(hex & 255), 255);
        }

        /// <summary>Adds a part whose geometry is built around its own local origin.</summary>
        public Transform Part(string name, string[] shows, Vector3 position, Vector3 euler, Action<MapStyleMeshBuilder> build, bool emissive = false, Dictionary<string, Pz> at = null)
        {
            var builder = new MapStyleMeshBuilder();
            build(builder);
            var go = CreateMeshObject(name, builder.Build(name), emissive ? emissiveMaterial : litMaterial, true);
            go.transform.SetParent(partsRoot, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            var part = new PartRuntime
            {
                Transform = go.transform,
                Renderer = go.GetComponent<MeshRenderer>(),
                Shows = shows,
                At = at,
                BasePos = go.transform.localPosition,
                BaseRot = go.transform.localRotation,
                BaseScale = go.transform.localScale,
                Visible = shows == null || Array.IndexOf(shows, State) >= 0 ? 1f : 0f
            };
            parts.Add(part);
            return go.transform;
        }

        public Transform Box(string[] shows, Vector3 pos, Vector3 size, Color color, Vector3? euler = null, bool emissive = false, Dictionary<string, Pz> at = null)
        {
            return Part("box", shows, pos, euler ?? Vector3.zero, b => b.AddBox(Vector3.zero, size, Vector3.zero, color), emissive, at);
        }

        public Transform Cone(string[] shows, Vector3 pos, float radius, float height, int sides, Color color, Vector3? euler = null, bool emissive = false, Dictionary<string, Pz> at = null)
        {
            return Part("cone", shows, pos, euler ?? Vector3.zero, b => b.AddPrism(Vector3.zero, new Vector3(radius * 2f, height, radius * 2f), Vector3.zero, sides, 0f, color), emissive, at);
        }

        public Transform Cylinder(string[] shows, Vector3 pos, float radiusTop, float radiusBottom, float height, int sides, Color color, Vector3? euler = null, bool emissive = false, Dictionary<string, Pz> at = null)
        {
            var topScale = radiusBottom > 0f ? radiusTop / radiusBottom : 0f;
            return Part("cyl", shows, pos, euler ?? Vector3.zero, b => b.AddPrism(Vector3.zero, new Vector3(radiusBottom * 2f, height, radiusBottom * 2f), Vector3.zero, sides, topScale, color), emissive, at);
        }

        public Transform Ico(string[] shows, Vector3 pos, float radius, Color color, Vector3? stretch = null, Vector3? euler = null, bool emissive = false, Dictionary<string, Pz> at = null)
        {
            var s = stretch ?? Vector3.one;
            return Part("ico", shows, pos, euler ?? Vector3.zero, b => b.AddIco(Vector3.zero, s * (radius * 2f), Vector3.zero, color), emissive, at);
        }

        public Transform Dome(string[] shows, Vector3 pos, Vector3 size, Color color)
        {
            return Part("dome", shows, pos, Vector3.zero, b => b.AddDome(Vector3.zero, size, Vector3.zero, 9, 4, color), false, null);
        }

        public void Smoke(Vector3 origin, string[] states, Color color, float size = 1f)
        {
            var mesh = BuildPuffMesh(color);
            var emitter = new EmitterRuntime
            {
                States = states,
                Origin = origin,
                Size = size,
                Puffs = new Transform[6],
                Age = new float[6],
                Off = new bool[6]
            };
            for (var i = 0; i < 6; i++)
            {
                var go = CreateMeshObject("puff", mesh, litMaterial, true);
                go.transform.SetParent(partsRoot, false);
                emitter.Puffs[i] = go.transform;
                emitter.Age[i] = i / 6f;
                emitter.Off[i] = Array.IndexOf(states, State) < 0;
                go.SetActive(!emitter.Off[i]);
            }

            emitters.Add(emitter);
        }

        public void Light(Vector3 position, string[] states, Color color, float intensity, float range)
        {
            var go = new GameObject("light");
            go.transform.SetParent(partsRoot, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.shadows = LightShadows.None;
            var runtime = new LightRuntime { Light = light, States = states, Base = intensity };
            runtime.Current = states == null || Array.IndexOf(states, State) >= 0 ? 1f : 0f;
            light.intensity = 0f;
            lights.Add(runtime);
        }

        // ---- State and knowledge ---------------------------------------------------------

        public void SetState(string state, bool puff = true)
        {
            if (State == state || Array.IndexOf(States, state) < 0)
            {
                return;
            }

            State = state;
            if (puff && Knowledge < 2)
            {
                SpawnPuff();
            }
        }

        public void SetKnowledge(int level)
        {
            Knowledge = Mathf.Clamp(level, 0, 2);
            var pale = Knowledge == 1 ? 0.8f : 0f;
            litMaterial.SetFloat("_Pale", pale);
            emissiveMaterial.SetFloat("_Pale", pale);
            ringMaterial.SetFloat("_Pale", pale);
            emissiveMaterial.SetFloat("_Emissive", Knowledge == 1 ? 0f : 1.2f);
            partsRoot.gameObject.SetActive(Knowledge < 2);
            if (ringKnown != null)
            {
                ringKnown.SetActive(Knowledge < 2);
                ringUnknown.SetActive(Knowledge == 2);
            }
        }

        /// <summary>Jump to the final pose of the current state without animation (used for screenshots).</summary>
        public void Snap()
        {
            foreach (var part in parts)
            {
                var on = IsShown(part.Shows) ? 1f : 0f;
                part.Visible = on;
                ApplyPose(part, 1f);
            }

            foreach (var emitter in emitters)
            {
                var on = Array.IndexOf(emitter.States, State) >= 0;
                for (var i = 0; i < emitter.Puffs.Length; i++)
                {
                    emitter.Off[i] = !on;
                    emitter.Puffs[i].gameObject.SetActive(on);
                    emitter.Age[i] = (i + 1) / 7f;
                }
            }

            foreach (var light in lights)
            {
                light.Current = light.States == null || Array.IndexOf(light.States, State) >= 0 ? 1f : 0f;
            }

            UpdateEmitters(0f);
            UpdateLights(1f);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            var blend = 1f - Mathf.Exp(-dt * 7f);
            foreach (var part in parts)
            {
                var target = IsShown(part.Shows) ? 1f : 0f;
                part.Visible += (target - part.Visible) * blend;
                if (Mathf.Abs(target - part.Visible) < 0.004f)
                {
                    part.Visible = target;
                }

                ApplyPose(part, blend);
            }

            UpdateEmitters(dt);
            UpdateLights(blend);
            UpdateTransients(dt);
        }

        private bool IsShown(string[] shows)
        {
            return shows == null || Array.IndexOf(shows, State) >= 0;
        }

        private void ApplyPose(PartRuntime part, float blend)
        {
            Pz pose = null;
            part.At?.TryGetValue(State, out pose);
            var offset = pose?.Pos ?? Vector3.zero;
            var rotation = pose != null ? Quaternion.Euler(pose.Rot) : Quaternion.identity;
            var scale = pose?.Scale ?? Vector3.one;
            part.Transform.localPosition = Vector3.Lerp(part.Transform.localPosition, part.BasePos + offset, blend);
            part.Transform.localRotation = Quaternion.Slerp(part.Transform.localRotation, part.BaseRot * rotation, blend);
            part.ScaleFactor = Vector3.Lerp(part.ScaleFactor, scale, blend);
            var eased = 1f - Mathf.Pow(1f - part.Visible, 3f);
            part.Transform.localScale = Vector3.Scale(part.BaseScale, part.ScaleFactor) * Mathf.Max(0.0001f, eased);
            part.Renderer.enabled = part.Visible > 0.01f;
        }

        private void UpdateEmitters(float dt)
        {
            foreach (var emitter in emitters)
            {
                var active = Array.IndexOf(emitter.States, State) >= 0;
                for (var i = 0; i < emitter.Puffs.Length; i++)
                {
                    var puff = emitter.Puffs[i];
                    if (emitter.Off[i])
                    {
                        if (!active)
                        {
                            continue;
                        }

                        emitter.Off[i] = false;
                        emitter.Age[i] = 0f;
                        puff.gameObject.SetActive(true);
                    }

                    emitter.Age[i] += dt * 0.3f;
                    if (emitter.Age[i] >= 1f)
                    {
                        emitter.Age[i] = 0f;
                        if (!active)
                        {
                            emitter.Off[i] = true;
                            puff.gameObject.SetActive(false);
                            continue;
                        }
                    }

                    var u = emitter.Age[i];
                    puff.localPosition = emitter.Origin + new Vector3(Mathf.Sin(u * 5f + i * 1.7f) * 0.12f, u * 1.8f * emitter.Size, Mathf.Cos(u * 4f + i) * 0.1f);
                    var grow = Mathf.Sin(u * Mathf.PI);
                    puff.localScale = Vector3.one * ((0.35f + u * 1.2f) * emitter.Size * Mathf.Max(0.05f, grow));
                }
            }
        }

        private void UpdateLights(float blend)
        {
            foreach (var light in lights)
            {
                var target = light.States == null || Array.IndexOf(light.States, State) >= 0 ? 1f : 0f;
                light.Current += (target - light.Current) * blend;
                var flicker = 1f + Mathf.Sin(Time.time * 9f + light.Base * 7f) * 0.08f;
                light.Light.intensity = light.Current * light.Base * LightScale * (0.55f + NightFactor * 2f) * flicker;
            }
        }

        private void SpawnPuff()
        {
            dustMesh ??= BuildPuffMesh(new Color32(0xcf, 0xc8, 0xb8, 255));
            for (var i = 0; i < 8; i++)
            {
                var go = CreateMeshObject("dust", dustMesh, litMaterial, true);
                go.transform.SetParent(transform, false);
                var angle = Mathf.PI * 2f * i / 8f;
                go.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.6f, 0.5f, Mathf.Sin(angle) * 0.6f);
                transients.Add(new Transient
                {
                    Transform = go.transform,
                    Velocity = new Vector3(Mathf.Cos(angle) * 1.6f, 1.2f, Mathf.Sin(angle) * 1.6f)
                });
            }
        }

        private void UpdateTransients(float dt)
        {
            for (var i = transients.Count - 1; i >= 0; i--)
            {
                var item = transients[i];
                item.Age += dt;
                item.Transform.localPosition += item.Velocity * dt * Mathf.Max(0f, 1f - item.Age);
                var grow = Mathf.Sin(Mathf.Clamp01(item.Age / 0.9f) * Mathf.PI);
                item.Transform.localScale = Vector3.one * (0.2f + item.Age * 2.2f) * Mathf.Max(0.02f, grow);
                if (item.Age > 0.9f)
                {
                    Destroy(item.Transform.gameObject);
                    transients.RemoveAt(i);
                }
            }
        }

        // ---- Helpers ---------------------------------------------------------------------

        private static Mesh BuildPuffMesh(Color color)
        {
            var builder = new MapStyleMeshBuilder();
            builder.AddIco(Vector3.zero, Vector3.one * 0.4f, Vector3.zero, color);
            return builder.Build("puff");
        }

        private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, bool castShadows)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }
    }
}
