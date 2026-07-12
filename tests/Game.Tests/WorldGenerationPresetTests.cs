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
        OptionProfilesMapToGeneratorParameters();
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

    private static void OptionProfilesMapToGeneratorParameters()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData", "World");
        var catalog = WorldGenerationPresetLoader.LoadCatalogFromDirectory(root);
        AssertEqual(12, catalog.Options.Count, "All optional campaign profiles are available");

        var request = new WorldGenerationRequest
        {
            GeneratorOverrides = new Dictionary<string, double>
            {
                ["islandFalloff"] = 0.35,
                ["mountainAmount"] = 0.75,
                ["locationDensity"] = 3.20,
                ["cellsPerTown"] = 18
            }
        };
        var parameters = request.ToGeneratorParams();

        AssertEqual(0.35f, parameters.IslandFalloff, "Coast profile maps island falloff");
        AssertEqual(0.75f, parameters.MountainAmount, "Terrain profile maps mountain amount");
        AssertEqual(3.20f, parameters.LocationDensity, "Activity profile maps location density");
        AssertEqual(18, parameters.CellsPerTown, "Settlement profile maps town density");
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
