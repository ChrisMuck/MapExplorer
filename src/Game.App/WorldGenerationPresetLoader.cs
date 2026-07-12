#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>Player-facing world-size preset. Technical generator tuning remains editor-only.</summary>
public sealed class WorldGenerationPreset
{
    public WorldGenerationPreset(string id, string label, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Preset id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Preset label must not be empty.", nameof(label));
        if (width < 4 || height < 4) throw new ArgumentOutOfRangeException(nameof(width), "World preset dimensions must be at least four.");
        Id = id.Trim();
        Label = label.Trim();
        Width = width;
        Height = height;
    }

    public string Id { get; }
    public string Label { get; }
    public int Width { get; }
    public int Height { get; }
}

public static class WorldGenerationPresetLoader
{
    public static IReadOnlyList<WorldGenerationPreset> LoadFromDirectory(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder)) return Array.Empty<WorldGenerationPreset>();
        var documents = Directory.EnumerateFiles(rootFolder, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText);
        return LoadFromJson(documents);
    }

    public static IReadOnlyList<WorldGenerationPreset> LoadFromJson(IEnumerable<string> jsonDocuments)
    {
        if (jsonDocuments == null) throw new ArgumentNullException(nameof(jsonDocuments));
        var items = new List<PresetDto>();
        foreach (var json in jsonDocuments.Where(json => !string.IsNullOrWhiteSpace(json)))
        {
            var envelope = JObject.Parse(json);
            if ((int?)envelope["schemaVersion"] != 1 || (string?)envelope["documentType"] != "world-generation-presets")
            {
                continue;
            }

            items.AddRange((envelope["items"] as JArray ?? new JArray()).ToObject<List<PresetDto>>()!);
        }

        var presets = items.Select(item => new WorldGenerationPreset(Require(item.Id, "preset.id"), Require(item.Label, "preset.label"), item.Width, item.Height)).ToList();
        if (presets.GroupBy(preset => preset.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new LocationDataException("World generation preset IDs must be unique.");
        }

        return presets;
    }

    private static string Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LocationDataException($"Missing required field '{field}'.");
        return value.Trim();
    }

    private sealed class PresetDto
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
}
