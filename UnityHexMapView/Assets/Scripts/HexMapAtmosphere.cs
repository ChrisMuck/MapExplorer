using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight ambient animation: scrolls water material UVs so rivers and the sea gently
/// flow, and drifts the sun so its cloud cookie casts slowly moving cloud shadows over the
/// whole map. Play-mode only, so the fixed playtest map stays static when not running.
/// </summary>
[ExecuteAlways]
public sealed class HexMapAtmosphere : MonoBehaviour
{
    private struct ScrollLayer
    {
        public Material Material;
        public Vector2 Speed;
    }

    private readonly List<ScrollLayer> layers = new();
    private Transform cloudTransform;
    private Vector3 cloudVelocity;

    public void AddWater(Material material, Vector2 speed)
    {
        if (material != null)
        {
            layers.Add(new ScrollLayer { Material = material, Speed = speed });
        }
    }

    public void SetClouds(Transform sunTransform, Vector3 velocity)
    {
        cloudTransform = sunTransform;
        cloudVelocity = velocity;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var dt = Time.deltaTime;
        for (var i = 0; i < layers.Count; i++)
        {
            var material = layers[i].Material;
            if (material == null)
            {
                continue;
            }

            var offset = material.mainTextureOffset + layers[i].Speed * dt;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);
            material.mainTextureOffset = offset;
        }

        if (cloudTransform != null)
        {
            cloudTransform.position += cloudVelocity * dt;
        }
    }
}
