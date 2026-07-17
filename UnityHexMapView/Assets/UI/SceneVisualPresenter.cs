using System.Collections.Generic;
using Game.App;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>One reusable Unity renderer for stable visual IDs and their safe placeholder fallback.</summary>
public static class SceneVisualPresenter
{
    private static readonly HashSet<string> WarnedMissingIds = new HashSet<string>();

    public static void Apply(VisualElement container, string requestedVisualId, VisualAssetDefinition definition, bool showLabel = true)
    {
        if (container == null || definition == null) return;
        if (!string.IsNullOrWhiteSpace(requestedVisualId) && requestedVisualId != definition.VisualAssetId &&
            WarnedMissingIds.Add(requestedVisualId))
        {
            Debug.LogWarning($"Visual asset '{requestedVisualId}' is not authored. Using '{definition.VisualAssetId}'.");
        }

        var hash = StableHash(definition.VisualAssetId);
        container.style.backgroundColor = new StyleColor(new Color32(
            (byte)(42 + hash % 45),
            (byte)(48 + (hash >> 8) % 42),
            (byte)(43 + (hash >> 16) % 48),
            255));
        container.tooltip = definition.Description ?? definition.DisplayName;

        if (!string.IsNullOrWhiteSpace(definition.AssetPath))
        {
            var sprite = Resources.Load<Sprite>(definition.AssetPath);
            if (sprite != null)
            {
                container.style.backgroundImage = new StyleBackground(sprite);
                container.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            }
            else if (WarnedMissingIds.Add($"asset-path:{definition.VisualAssetId}"))
            {
                Debug.LogWarning($"Visual asset '{definition.VisualAssetId}' could not load Resources sprite '{definition.AssetPath}'. Using its generated placeholder.");
            }
        }

        var label = container.Q<Label>("scene-visual-label");
        if (!showLabel)
        {
            if (label != null) label.style.display = DisplayStyle.None;
            return;
        }

        if (label == null)
        {
            label = new Label { name = "scene-visual-label", pickingMode = PickingMode.Ignore };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new StyleColor(new Color32(232, 227, 214, 255));
            label.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.38f));
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 5;
            label.style.paddingBottom = 5;
            label.style.marginLeft = 8;
            label.style.marginRight = 8;
            label.style.marginTop = 8;
            container.Add(label);
        }
        label.style.display = DisplayStyle.Flex;
        label.text = definition.DisplayName;
    }

    private static uint StableHash(string value)
    {
        var hash = 2166136261u;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16777619u;
        }
        return hash;
    }
}
