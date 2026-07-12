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

public sealed class WorldGenerationOptionPreset
{
    public WorldGenerationOptionPreset(string categoryId, string id, string label, IReadOnlyDictionary<string, double> generatorOverrides)
    {
        if (string.IsNullOrWhiteSpace(categoryId)) throw new ArgumentException("Option category must not be empty.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Option id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Option label must not be empty.", nameof(label));
        CategoryId = categoryId.Trim();
        Id = id.Trim();
        Label = label.Trim();
        GeneratorOverrides = generatorOverrides ?? throw new ArgumentNullException(nameof(generatorOverrides));
    }

    public string CategoryId { get; }
    public string Id { get; }
    public string Label { get; }
    public IReadOnlyDictionary<string, double> GeneratorOverrides { get; }
}

public sealed class WorldGenerationPresetCatalog
{
    public WorldGenerationPresetCatalog(IReadOnlyList<WorldGenerationPreset> sizes, IReadOnlyList<WorldGenerationOptionPreset> options)
    {
        Sizes = sizes;
        Options = options;
    }

    public IReadOnlyList<WorldGenerationPreset> Sizes { get; }
    public IReadOnlyList<WorldGenerationOptionPreset> Options { get; }
}

public static class WorldGenerationPresetLoader
{
    public static IReadOnlyList<WorldGenerationPreset> LoadFromDirectory(string rootFolder)
    {
        return LoadCatalogFromDirectory(rootFolder).Sizes;
    }

    public static WorldGenerationPresetCatalog LoadCatalogFromDirectory(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder)) return new WorldGenerationPresetCatalog(Array.Empty<WorldGenerationPreset>(), Array.Empty<WorldGenerationOptionPreset>());
        var documents = Directory.EnumerateFiles(rootFolder, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText);
        return LoadCatalogFromJson(documents);
    }

    public static IReadOnlyList<WorldGenerationPreset> LoadFromJson(IEnumerable<string> jsonDocuments)
    {
        return LoadCatalogFromJson(jsonDocuments).Sizes;
    }

    public static WorldGenerationPresetCatalog LoadCatalogFromJson(IEnumerable<string> jsonDocuments)
    {
        if (jsonDocuments == null) throw new ArgumentNullException(nameof(jsonDocuments));
        var items = new List<PresetDto>();
        var optionItems = new List<OptionPresetDto>();
        foreach (var json in jsonDocuments.Where(json => !string.IsNullOrWhiteSpace(json)))
        {
            var envelope = JObject.Parse(json);
            if ((int?)envelope["schemaVersion"] != 1)
            {
                continue;
            }

            var documentItems = envelope["items"] as JArray ?? new JArray();
            switch ((string?)envelope["documentType"])
            {
                case "world-generation-presets": items.AddRange(documentItems.ToObject<List<PresetDto>>()!); break;
                case "world-generation-option-presets": optionItems.AddRange(documentItems.ToObject<List<OptionPresetDto>>()!); break;
            }
        }

        var presets = items.Select(item => new WorldGenerationPreset(Require(item.Id, "preset.id"), Require(item.Label, "preset.label"), item.Width, item.Height)).ToList();
        if (presets.GroupBy(preset => preset.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new LocationDataException("World generation preset IDs must be unique.");
        }

        var options = optionItems.Select(item => new WorldGenerationOptionPreset(
            Require(item.CategoryId, "option.categoryId"),
            Require(item.Id, "option.id"),
            Require(item.Label, "option.label"),
            item.GeneratorOverrides ?? new Dictionary<string, double>())).ToList();
        if (options.GroupBy(option => $"{option.CategoryId}:{option.Id}", StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new LocationDataException("World generation option preset IDs must be unique within their category.");
        }

        return new WorldGenerationPresetCatalog(presets, options);
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

    private sealed class OptionPresetDto
    {
        public string? CategoryId { get; set; }
        public string? Id { get; set; }
        public string? Label { get; set; }
        public Dictionary<string, double>? GeneratorOverrides { get; set; }
    }
}
}
