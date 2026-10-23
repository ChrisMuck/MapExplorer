using System.Collections.Generic;
using Game.App;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Draws one location from a <see cref="LocationVisualDescription"/>. It decides nothing: the resolver in
    /// Game.App says which elements are shown and how they are posed, and this component blends towards that.
    /// Applying a new description updates only the elements that changed, so a state change never rebuilds the place.
    /// </summary>
    public sealed class MapStyleLocationView : MonoBehaviour
    {
        private sealed class ElementRuntime
        {
            public Transform Root;
            public MeshRenderer[] Renderers;
            public Vector3 BasePosition;
            public Quaternion BaseRotation;
            public Vector3 TargetOffset;
            public Quaternion TargetRotation = Quaternion.identity;
            public Vector3 TargetScale = Vector3.one;
            public Vector3 Scale = Vector3.one;
            public bool Shown;
            public float Visible;
            public bool Removing;
        }

        private readonly Dictionary<string, ElementRuntime> elements = new();
        private MapStyleVisualKit kit;
        private Material litMaterial;
        private Material emissiveMaterial;
        private Material ringMaterial;
        private Transform partsRoot;
        private GameObject ring;
        private string ringSignature = string.Empty;
        private string lastSignature = string.Empty;

        public string LocationId { get; private set; }
        public string DisplayName { get; set; }
        public string StateText { get; private set; } = string.Empty;
        public VisualKnowledge Knowledge { get; private set; }

        public static MapStyleLocationView Create(string locationId, string displayName, Transform parent, Vector3 position, float yaw, Shader shader, MapStyleVisualKit kit)
        {
            var go = new GameObject(displayName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var view = go.AddComponent<MapStyleLocationView>();
            view.Initialize(locationId, displayName, shader, kit);
            return view;
        }

        private void Initialize(string locationId, string displayName, Shader shader, MapStyleVisualKit visualKit)
        {
            LocationId = locationId;
            DisplayName = displayName;
            kit = visualKit;
            litMaterial = new Material(shader) { name = displayName + "-lit" };
            emissiveMaterial = new Material(shader) { name = displayName + "-emissive" };
            emissiveMaterial.SetFloat("_Emissive", 1.2f);
            ringMaterial = new Material(shader) { name = displayName + "-ring" };
            ringMaterial.SetFloat("_Flat", 1f);
            partsRoot = new GameObject("Parts").transform;
            partsRoot.SetParent(transform, false);
        }

        /// <summary>Applies a resolved description. Returns false when nothing changed.</summary>
        public bool Apply(LocationVisualDescription description)
        {
            var signature = description.CompositionId + "|" + description.StateKey;
            if (signature == lastSignature)
            {
                return false;
            }

            lastSignature = signature;
            Knowledge = description.Knowledge;
            StateText = description.StateKey.Split('|')[0];
            ApplyKnowledgeLook(description.Knowledge);
            ApplyRing(description);

            var present = new HashSet<string>();
            foreach (var element in description.Elements)
            {
                present.Add(element.Id);
                if (!elements.TryGetValue(element.Id, out var runtime) || runtime.Removing)
                {
                    runtime = CreateElement(element);
                    elements[element.Id] = runtime;
                }

                runtime.Shown = element.Shown;
                runtime.TargetOffset = MapStyleVisualKit.ToVector(element.Pose.Position);
                runtime.TargetRotation = Quaternion.Euler(MapStyleVisualKit.ToVector(element.Pose.Euler));
                runtime.TargetScale = MapStyleVisualKit.ToVector(element.Pose.Scale);
            }

            foreach (var pair in elements)
            {
                if (!present.Contains(pair.Key))
                {
                    pair.Value.Shown = false;
                    pair.Value.Removing = true;
                }
            }

            return true;
        }

        /// <summary>Jumps to the final pose without animation (screenshots, first appearance).</summary>
        public void Snap()
        {
            foreach (var runtime in elements.Values)
            {
                runtime.Visible = runtime.Shown ? 1f : 0f;
                runtime.Scale = runtime.TargetScale;
                runtime.Root.localPosition = runtime.BasePosition + runtime.TargetOffset;
                runtime.Root.localRotation = runtime.BaseRotation * runtime.TargetRotation;
                ApplyVisibility(runtime);
            }
        }

        private void Update()
        {
            var blend = 1f - Mathf.Exp(-Time.deltaTime * 7f);
            List<string> finished = null;
            foreach (var pair in elements)
            {
                var runtime = pair.Value;
                var target = runtime.Shown ? 1f : 0f;
                runtime.Visible += (target - runtime.Visible) * blend;
                if (Mathf.Abs(target - runtime.Visible) < 0.004f)
                {
                    runtime.Visible = target;
                }

                runtime.Root.localPosition = Vector3.Lerp(runtime.Root.localPosition, runtime.BasePosition + runtime.TargetOffset, blend);
                runtime.Root.localRotation = Quaternion.Slerp(runtime.Root.localRotation, runtime.BaseRotation * runtime.TargetRotation, blend);
                runtime.Scale = Vector3.Lerp(runtime.Scale, runtime.TargetScale, blend);
                ApplyVisibility(runtime);
                if (runtime.Removing && runtime.Visible <= 0f)
                {
                    (finished ??= new List<string>()).Add(pair.Key);
                }
            }

            if (finished != null)
            {
                foreach (var id in finished)
                {
                    Destroy(elements[id].Root.gameObject);
                    elements.Remove(id);
                }
            }
        }

        private static void ApplyVisibility(ElementRuntime runtime)
        {
            var eased = 1f - Mathf.Pow(1f - runtime.Visible, 3f);
            runtime.Root.localScale = runtime.Scale * Mathf.Max(0.0001f, eased);
            var visible = runtime.Visible > 0.01f;
            foreach (var renderer in runtime.Renderers)
            {
                renderer.enabled = visible;
            }
        }

        private ElementRuntime CreateElement(VisualElementState element)
        {
            var go = new GameObject(element.Id);
            go.transform.SetParent(partsRoot, false);
            go.transform.localPosition = MapStyleVisualKit.ToVector(element.Position);
            go.transform.localRotation = Quaternion.Euler(MapStyleVisualKit.ToVector(element.Euler));
            var renderers = new List<MeshRenderer>();
            AddMeshObject(go.transform, kit.GetMesh(element.PartId, false), litMaterial, renderers);
            AddMeshObject(go.transform, kit.GetMesh(element.PartId, true), emissiveMaterial, renderers);
            var runtime = new ElementRuntime
            {
                Root = go.transform,
                Renderers = renderers.ToArray(),
                BasePosition = go.transform.localPosition,
                BaseRotation = go.transform.localRotation
            };
            runtime.Root.localScale = Vector3.one * 0.0001f;
            foreach (var renderer in runtime.Renderers)
            {
                renderer.enabled = false;
            }

            return runtime;
        }

        private static void AddMeshObject(Transform parent, Mesh mesh, Material material, List<MeshRenderer> renderers)
        {
            if (mesh == null)
            {
                return;
            }

            var child = new GameObject("mesh");
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderers.Add(renderer);
        }

        private void ApplyKnowledgeLook(VisualKnowledge knowledge)
        {
            var pale = knowledge == VisualKnowledge.Reported ? 0.8f : knowledge == VisualKnowledge.Old ? 0.45f : 0f;
            litMaterial.SetFloat("_Pale", pale);
            emissiveMaterial.SetFloat("_Pale", pale);
            emissiveMaterial.SetFloat("_Emissive", knowledge == VisualKnowledge.Reported ? 0f : 1.2f);
            ringMaterial.SetFloat("_Pale", pale);
        }

        private void ApplyRing(LocationVisualDescription description)
        {
            var signature = description.MarkerColorHex + "|" + description.RingGaps + "|" + description.RingRadius;
            if (signature == ringSignature)
            {
                return;
            }

            ringSignature = signature;
            if (ring != null)
            {
                Destroy(ring);
            }

            var builder = new MapStyleMeshBuilder();
            builder.AddRing(new Vector3(0f, 0.04f, 0f), description.RingRadius, description.RingRadius + 0.25f, 36, description.RingGaps, MapStyleVisualKit.ParseColor(description.MarkerColorHex));
            ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = builder.Build("ring");
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ringMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
