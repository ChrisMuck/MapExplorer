using System.Collections.Generic;
using UnityEngine;

/// <summary>One renderable part of a decoration prefab, extracted once and reused for instancing.</summary>
internal readonly struct HexDecorationPart
{
    public readonly Mesh Mesh;
    public readonly Material Material;
    public readonly Matrix4x4 LocalMatrix;

    public HexDecorationPart(Mesh mesh, Material material, Matrix4x4 localMatrix)
    {
        Mesh = mesh;
        Material = material;
        LocalMatrix = localMatrix;
    }
}

/// <summary>
/// Extracts renderable (mesh, material, local transform) parts from authored <see cref="HexMapPrefabLibrary"/>
/// prefabs once, so GPU instancing can draw the exact same hand-authored art without instantiating a
/// GameObject per hex. Prefab meshes/materials are shared, persistent assets, so a per-material
/// GPU-instancing-enabled runtime clone is used for drawing instead of mutating the source asset.
/// </summary>
internal sealed class HexDecorationLibrary
{
    private readonly Dictionary<GameObject, List<HexDecorationPart>> partsByPrefab = new();
    private readonly Dictionary<Material, Material> instancedMaterials = new();

    /// <summary>Extracted parts for a prefab, using instancing-enabled clones of its materials.</summary>
    public IReadOnlyList<HexDecorationPart> Extract(GameObject prefab)
    {
        if (prefab == null)
        {
            return System.Array.Empty<HexDecorationPart>();
        }

        if (partsByPrefab.TryGetValue(prefab, out var cached))
        {
            return cached;
        }

        var parts = new List<HexDecorationPart>();
        var rootTransform = prefab.transform;
        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            var localMatrix = rootTransform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            parts.Add(new HexDecorationPart(filter.sharedMesh, InstancedMaterial(renderer.sharedMaterial), localMatrix));
        }

        partsByPrefab[prefab] = parts;
        return parts;
    }

    /// <summary>An instancing-enabled runtime clone of a canonical (non-prefab) material.</summary>
    public Material InstancedMaterial(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (instancedMaterials.TryGetValue(source, out var clone))
        {
            return clone;
        }

        clone = new Material(source)
        {
            name = source.name + " (Instanced)",
            hideFlags = HideFlags.DontSave,
            enableInstancing = true
        };
        instancedMaterials[source] = clone;
        return clone;
    }

    public void Dispose()
    {
        foreach (var material in instancedMaterials.Values)
        {
            if (material != null)
            {
                DestroyMaterial(material);
            }
        }

        instancedMaterials.Clear();
        partsByPrefab.Clear();
    }

    private static void DestroyMaterial(Object obj)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }
}
