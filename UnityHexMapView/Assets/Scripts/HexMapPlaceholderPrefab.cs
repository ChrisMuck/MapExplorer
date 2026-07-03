using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class HexMapPlaceholderPrefab : MonoBehaviour
{
    public enum VisualKind
    {
        TreePine,
        TreeRound,
        TreeTall,
        ForestDense,
        ForestEdge,
        MountainPeak,
        MountainPeakSnowy,
        RockyRidge,
        RockyRidgeSnowy,
        Foothills,
        Rock,
        SettlementCluster,
        HouseA,
        HouseB,
        Fence,
        Watchtower,
        Mine,
        WallSegment,
        WallTower,
        CoastMarker
    }

    public VisualKind kind;

    private const string GeneratedRootName = "_GeneratedVisual";
    private readonly Dictionary<string, Material> materials = new();
#if UNITY_EDITOR
    private bool editorRebuildQueued;
#endif

    private void OnEnable()
    {
        RequestRebuild();
    }

    private void OnValidate()
    {
        RequestRebuild();
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
        if (this == null || !isActiveAndEnabled || !gameObject.scene.IsValid())
        {
            return;
        }

        Rebuild();
    }
#endif

    private void Rebuild()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        var existing = transform.Find(GeneratedRootName);
        if (existing != null)
        {
            DestroyGeneratedObject(existing.gameObject);
        }

        materials.Clear();
        var root = new GameObject(GeneratedRootName);
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetParent(transform, false);

        switch (kind)
        {
            case VisualKind.TreePine:
                PineTree(root.transform, 1f);
                break;
            case VisualKind.TreeRound:
                RoundTree(root.transform, 1f);
                break;
            case VisualKind.TreeTall:
                PineTree(root.transform, 1.25f);
                break;
            case VisualKind.ForestDense:
                Forest(root.transform, 9);
                break;
            case VisualKind.ForestEdge:
                Forest(root.transform, 6);
                break;
            case VisualKind.MountainPeak:
                MountainPeak(root.transform, false, 1f);
                break;
            case VisualKind.MountainPeakSnowy:
                MountainPeak(root.transform, true, 1.05f);
                break;
            case VisualKind.RockyRidge:
                RockyRidge(root.transform, false);
                break;
            case VisualKind.RockyRidgeSnowy:
                RockyRidge(root.transform, true);
                break;
            case VisualKind.Foothills:
                Foothills(root.transform);
                break;
            case VisualKind.Rock:
                Rock(root.transform, "Rock", 0.34f, Vector3.up * 0.08f, Quaternion.Euler(8f, 32f, -5f));
                break;
            case VisualKind.SettlementCluster:
                Settlement(root.transform);
                break;
            case VisualKind.HouseA:
                House(root.transform, 1f);
                break;
            case VisualKind.HouseB:
                House(root.transform, 0.78f);
                break;
            case VisualKind.Fence:
                Fence(root.transform);
                break;
            case VisualKind.Watchtower:
                Watchtower(root.transform);
                break;
            case VisualKind.Mine:
                Mine(root.transform);
                break;
            case VisualKind.WallSegment:
                WallSegment(root.transform);
                break;
            case VisualKind.WallTower:
                Cone(root.transform, "WallTower", Mat("WallStone", "847e6b"), 0.22f, 0.18f, 0.46f, Vector3.up * 0.23f, Quaternion.identity, 8);
                break;
            case VisualKind.CoastMarker:
                CoastMarker(root.transform);
                break;
        }
    }

    private void PineTree(Transform parent, float scale)
    {
        Cylinder(parent, "Trunk", Mat("Bark", "4b3326"), new Vector3(0f, 0.13f * scale, 0f), new Vector3(0.07f, 0.13f * scale, 0.07f), Quaternion.identity);
        Cone(parent, "LowerCrown", Mat("LeafDark", "1e432f"), 0.32f, 0.04f, 0.38f * scale, new Vector3(0f, 0.38f * scale, 0f), Quaternion.Euler(-3f, 30f, 3f), 7);
        Cone(parent, "UpperCrown", Mat("Leaf", "2f6a42"), 0.22f, 0.03f, 0.32f * scale, new Vector3(0f, 0.62f * scale, 0f), Quaternion.Euler(2f, 10f, -2f), 7);
    }

    private void RoundTree(Transform parent, float scale)
    {
        Cylinder(parent, "Trunk", Mat("Bark", "4b3326"), new Vector3(0f, 0.14f, 0f), new Vector3(0.075f, 0.14f, 0.075f), Quaternion.identity);
        Cone(parent, "RoundCrown", Mat("Leaf", "2f6a42"), 0.34f, 0.2f, 0.34f * scale, new Vector3(0f, 0.43f * scale, 0f), Quaternion.Euler(0f, 30f, 0f), 8);
        Cone(parent, "TopCrown", Mat("LeafDark", "1e432f"), 0.2f, 0.08f, 0.22f * scale, new Vector3(0f, 0.62f * scale, 0f), Quaternion.identity, 8);
    }

    private void Forest(Transform parent, int count)
    {
        Cylinder(parent, "ForestGround", Mat("ForestGround", "233f2d"), Vector3.zero, new Vector3(0.92f, 0.025f, 0.92f), Quaternion.Euler(0f, 30f, 0f), 6);
        for (var i = 0; i < count; i++)
        {
            var holder = Child(parent, "Tree", Vector3.zero, Quaternion.Euler(0f, i * 37f, 0f), Vector3.one);
            var angle = Mathf.PI * 2f * i / count;
            var ring = i == 0 ? 0.05f : Mathf.Lerp(0.22f, 0.66f, (i % 5) / 4f);
            holder.transform.localPosition = new Vector3(Mathf.Cos(angle) * ring, 0.02f, Mathf.Sin(angle) * ring);
            if (i % 3 == 1)
            {
                RoundTree(holder.transform, 0.82f + (i % 4) * 0.08f);
            }
            else
            {
                PineTree(holder.transform, 0.86f + (i % 5) * 0.07f);
            }
        }
    }

    private void MountainPeak(Transform parent, bool snowy, float scale)
    {
        Cone(parent, "Base", Mat("RockDark", "3e413a"), 0.68f * scale, 0.42f * scale, 0.22f * scale, new Vector3(0f, 0.11f * scale, 0f), Quaternion.Euler(0f, 24f, 0f), 8);
        Cone(parent, "MainPeak", Mat("Rock", "777b71"), 0.5f * scale, 0.035f * scale, 1.05f * scale, new Vector3(-0.05f * scale, 0.63f * scale, 0.02f * scale), Quaternion.Euler(0f, 18f, 0f), 7);
        Cone(parent, "SidePeakA", Mat("RockDark", "3e413a"), 0.32f * scale, 0.03f * scale, 0.62f * scale, new Vector3(0.28f * scale, 0.42f * scale, 0.22f * scale), Quaternion.Euler(0f, 75f, 0f), 7);
        Cone(parent, "SidePeakB", Mat("Rock", "777b71"), 0.24f * scale, 0.025f * scale, 0.48f * scale, new Vector3(-0.32f * scale, 0.34f * scale, -0.22f * scale), Quaternion.Euler(0f, -35f, 0f), 7);
        if (!snowy)
        {
            return;
        }

        Cone(parent, "SnowCap", Mat("Snow", "d9dfdc"), 0.18f * scale, 0.012f * scale, 0.25f * scale, new Vector3(-0.05f * scale, 1.16f * scale, 0.02f * scale), Quaternion.Euler(0f, 18f, 0f), 7);
        Cone(parent, "SideSnowCap", Mat("Snow", "d9dfdc"), 0.11f * scale, 0.01f * scale, 0.13f * scale, new Vector3(0.28f * scale, 0.74f * scale, 0.22f * scale), Quaternion.Euler(0f, 75f, 0f), 7);
    }

    private void RockyRidge(Transform parent, bool snowy)
    {
        Cone(parent, "RidgeBase", Mat("RockDark", "3e413a"), 0.52f, 0.34f, 0.13f, new Vector3(0f, 0.065f, 0f), Quaternion.Euler(0f, 25f, 0f), 8);
        Cone(parent, "PeakA", Mat("Rock", "777b71"), 0.28f, 0.025f, 0.52f, new Vector3(-0.18f, 0.33f, 0.02f), Quaternion.Euler(0f, -20f, 0f), 7);
        Cone(parent, "PeakB", Mat("RockDark", "3e413a"), 0.24f, 0.02f, 0.42f, new Vector3(0.22f, 0.27f, 0.13f), Quaternion.Euler(0f, 55f, 0f), 7);
        Cone(parent, "PeakC", Mat("Rock", "777b71"), 0.2f, 0.018f, 0.32f, new Vector3(0.14f, 0.22f, -0.22f), Quaternion.Euler(0f, 120f, 0f), 7);
        if (snowy)
        {
            Cone(parent, "SnowCapA", Mat("Snow", "d9dfdc"), 0.1f, 0.01f, 0.13f, new Vector3(-0.18f, 0.59f, 0.02f), Quaternion.Euler(0f, -20f, 0f), 7);
        }
    }

    private void Foothills(Transform parent)
    {
        for (var i = 0; i < 5; i++)
        {
            var angle = Mathf.PI * 2f * i / 5f;
            Rock(parent, $"Rock_{i}", 0.22f + i * 0.02f, new Vector3(Mathf.Cos(angle) * 0.34f, 0.08f, Mathf.Sin(angle) * 0.26f), Quaternion.Euler(8f, angle * Mathf.Rad2Deg, -4f));
        }
    }

    private void Settlement(Transform parent)
    {
        Cylinder(parent, "Plaza", Mat("WallStone", "847e6b"), Vector3.zero, new Vector3(0.48f, 0.025f, 0.42f), Quaternion.identity, 6);
        for (var i = 0; i < 5; i++)
        {
            var house = Child(parent, "House", Vector3.zero, Quaternion.identity, Vector3.one);
            var angle = Mathf.PI * 2f * i / 5f;
            house.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.32f, 0.04f, Mathf.Sin(angle) * 0.32f);
            house.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 30f, 0f);
            House(house.transform, 0.78f + i * 0.07f);
        }

        Fence(parent);
        Cylinder(parent, "Smoke", Mat("Smoke", "c1b8aa", 0.24f), new Vector3(0.1f, 0.48f, -0.08f), new Vector3(0.06f, 0.22f, 0.06f), Quaternion.identity);
    }

    private void House(Transform parent, float scale)
    {
        Cube(parent, "Body", Mat("WallStone", "847e6b"), new Vector3(0f, 0.11f * scale, 0f), new Vector3(0.24f * scale, 0.22f * scale, 0.28f * scale), Quaternion.identity);
        Cone(parent, "Roof", Mat(scale > 0.9f ? "RoofWarm" : "RoofRose", scale > 0.9f ? "9a6240" : "86506a"), 0.22f * scale, 0.02f * scale, 0.2f * scale, new Vector3(0f, 0.32f * scale, 0f), Quaternion.Euler(0f, 45f, 0f), 4);
    }

    private void Fence(Transform parent)
    {
        var root = Child(parent, "Fence", new Vector3(-0.32f, 0.08f, -0.44f), Quaternion.Euler(0f, -8f, 0f), Vector3.one);
        for (var i = 0; i < 3; i++)
        {
            Cube(root.transform, $"Plank_{i}", Mat("Wood", "5a3d29"), new Vector3(-0.2f + i * 0.2f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.035f), Quaternion.identity);
        }
    }

    private void Watchtower(Transform parent)
    {
        Cylinder(parent, "Base", Mat("WallStone", "847e6b"), Vector3.zero, new Vector3(0.34f, 0.06f, 0.3f), Quaternion.identity, 6);
        Cone(parent, "Body", Mat("WallStone", "847e6b"), 0.22f, 0.18f, 1.05f, new Vector3(0f, 0.58f, 0f), Quaternion.identity, 8);
        Cone(parent, "Roof", Mat("RoofRose", "86506a"), 0.28f, 0.02f, 0.34f, new Vector3(0f, 1.28f, 0f), Quaternion.identity, 8);
        Cube(parent, "Banner", Mat("FlagRed", "bb5148"), new Vector3(0.18f, 1.05f, -0.04f), new Vector3(0.28f, 0.18f, 0.035f), Quaternion.identity);
    }

    private void Mine(Transform parent)
    {
        Cone(parent, "MineHill", Mat("RockDark", "3e413a"), 0.5f, 0.12f, 0.46f, new Vector3(0f, 0.23f, 0.03f), Quaternion.identity, 8);
        Cube(parent, "Entrance", Mat("Wood", "5a3d29"), new Vector3(0f, 0.18f, -0.28f), new Vector3(0.34f, 0.28f, 0.08f), Quaternion.identity);
        Cube(parent, "RailA", Mat("Wood", "5a3d29"), new Vector3(-0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        Cube(parent, "RailB", Mat("Wood", "5a3d29"), new Vector3(0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        Rock(parent, "OreRock", 0.22f, new Vector3(-0.34f, 0.09f, 0.26f), Quaternion.Euler(8f, 22f, -4f));
        Cube(parent, "OreHint", Mat("GoldOre", "c09a38"), new Vector3(0.32f, 0.12f, -0.18f), new Vector3(0.12f, 0.1f, 0.12f), Quaternion.Euler(15f, 30f, 8f));
    }

    private void WallSegment(Transform parent)
    {
        Cube(parent, "WallBody", Mat("WallStone", "847e6b"), Vector3.up * 0.12f, new Vector3(1f, 1f, 1f), Quaternion.identity);
        Cube(parent, "WallCap", Mat("RockDark", "3e413a"), Vector3.up * 0.31f, new Vector3(1.18f, 0.22f, 0.78f), Quaternion.identity);
    }

    private void CoastMarker(Transform parent)
    {
        Cylinder(parent, "Pole", Mat("Bark", "4b3326"), new Vector3(0.12f, 0.36f, -0.18f), new Vector3(0.035f, 0.36f, 0.035f), Quaternion.identity);
        Cube(parent, "Flag", Mat("FlagRed", "bb5148"), new Vector3(0.28f, 0.58f, -0.18f), new Vector3(0.34f, 0.2f, 0.035f), Quaternion.identity);
    }

    private void Rock(Transform parent, string objectName, float size, Vector3 position, Quaternion rotation)
    {
        Cube(parent, objectName, Mat("Rock", "777b71"), position, new Vector3(size * 0.9f, size * 0.6f, size), rotation);
    }

    private void Cube(Transform parent, string objectName, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(cube, parent, objectName, material, position, scale, rotation);
    }

    private void Cylinder(Transform parent, string objectName, Material material, Vector3 position, Vector3 scale, Quaternion rotation, int sides = 0)
    {
        if (sides > 0)
        {
            MeshObject(parent, objectName, material, CylinderMesh(1f, 1f, 1f, sides), position, scale, rotation);
            return;
        }

        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(cylinder, parent, objectName, material, position, scale, rotation);
    }

    private void Cone(Transform parent, string objectName, Material material, float bottomRadius, float topRadius, float height, Vector3 position, Quaternion rotation, int sides)
    {
        MeshObject(parent, objectName, material, CylinderMesh(bottomRadius, topRadius, height, sides), position, Vector3.one, rotation);
    }

    private void MeshObject(Transform parent, string objectName, Material material, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        var obj = Child(parent, objectName, position, rotation, scale);
        var filter = obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
    }

    private GameObject Child(Transform parent, string objectName, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var obj = new GameObject(objectName) { hideFlags = HideFlags.DontSave };
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localRotation = rotation;
        obj.transform.localScale = scale;
        return obj;
    }

    private void Setup(GameObject obj, Transform parent, string objectName, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        obj.hideFlags = HideFlags.DontSave;
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rotation;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyGeneratedObject(collider);
        }
    }

    private static void DestroyGeneratedObject(Object obj)
    {
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private Material Mat(string materialName, string hex, float alpha = 1f)
    {
        if (materials.TryGetValue(materialName, out var material))
        {
            return material;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = materialName,
            color = ColorFromHex(hex, alpha),
            hideFlags = HideFlags.DontSave
        };

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        materials[materialName] = material;
        return material;
    }

    private Mesh CylinderMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        var vertices = new List<Vector3>();
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
            vertices.Add(t0);
            vertices.Add(t1);
            vertices.Add(b0);
            vertices.Add(b1);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);

            start = vertices.Count;
            vertices.Add(Vector3.up * half);
            vertices.Add(t1);
            vertices.Add(t0);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            start = vertices.Count;
            vertices.Add(Vector3.down * half);
            vertices.Add(b0);
            vertices.Add(b1);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        var mesh = new Mesh { name = "LowPolyShape", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Color ColorFromHex(string hex, float alpha)
    {
        if (!ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            color = Color.magenta;
        }

        color.a = alpha;
        return color;
    }
}
