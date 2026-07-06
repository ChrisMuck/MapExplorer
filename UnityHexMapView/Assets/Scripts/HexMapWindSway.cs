using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, single-Update wind animation for map vegetation. A single manager
/// gently rotates registered transforms (tree and grass-tuft roots) so the map feels
/// alive. Layout is unaffected: this only runs in play mode and only changes rotation,
/// so the fixed playtest map stays visually identical when not playing.
/// </summary>
[ExecuteAlways]
public sealed class HexMapWindSway : MonoBehaviour
{
    private struct SwayTarget
    {
        public Transform Transform;
        public Quaternion BaseRotation;
        public float Phase;
        public float Amplitude;
        public float Speed;
    }

    [Range(0f, 3f)] public float windStrength = 1f;
    [Range(0f, 2f)] public float gustStrength = 0.4f;

    private readonly List<SwayTarget> targets = new();

    public void Register(Transform swayTransform, float amplitudeDegrees, float speed, float phase)
    {
        if (swayTransform == null)
        {
            return;
        }

        targets.Add(new SwayTarget
        {
            Transform = swayTransform,
            BaseRotation = swayTransform.localRotation,
            Phase = phase,
            Amplitude = amplitudeDegrees,
            Speed = speed
        });
    }

    private void Update()
    {
        if (!Application.isPlaying || targets.Count == 0)
        {
            return;
        }

        var time = Time.time;
        // Slow global gust modulates the steady breeze so the motion is not uniform.
        var gust = 1f + Mathf.Sin(time * 0.23f) * gustStrength;
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Transform == null)
            {
                continue;
            }

            var sway = Mathf.Sin(time * target.Speed + target.Phase) * target.Amplitude * windStrength * gust;
            target.Transform.localRotation = target.BaseRotation * Quaternion.Euler(sway, 0f, sway * 0.6f);
        }
    }
}
