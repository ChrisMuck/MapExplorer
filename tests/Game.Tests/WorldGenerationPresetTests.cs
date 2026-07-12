#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;

internal sealed class WorldGenerationPresetTests
{
    public void RunAll()
    {
        PlayerFacingPresetsLoadWithoutExposingASeed();
    }

    private static void PlayerFacingPresetsLoadWithoutExposingASeed()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData", "World");
        var presets = WorldGenerationPresetLoader.LoadFromDirectory(root);

        AssertEqual(5, presets.Count, "All player-facing size presets are available");
        var medium = Find(presets, "medium");
        AssertEqual(40, medium.Width, "Medium preset defines map width");
        AssertEqual(30, medium.Height, "Medium preset defines map height");
    }

    private static WorldGenerationPreset Find(IReadOnlyList<WorldGenerationPreset> presets, string id)
    {
        foreach (var preset in presets)
        {
            if (preset.Id == id) return preset;
        }

        throw new InvalidOperationException($"Preset '{id}' was not found.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }
}
