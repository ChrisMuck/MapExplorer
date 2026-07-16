#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.App
{

public enum VisualAssetType
{
    Portrait,
    FactionSymbol,
    LocationImage,
    EventImage,
    ObjectImage,
    SymbolImage,
    ReportImage,
    UnknownPlaceholder
}

/// <summary>Presentation-only stable visual reference loaded from game-data JSON.</summary>
public sealed class VisualAssetDefinition
{
    public VisualAssetDefinition(string visualAssetId, VisualAssetType type, string displayName,
        string? assetPath, bool isPlaceholder, string? description, IEnumerable<string>? tags,
        string? fallbackVisualAssetId)
    {
        VisualAssetId = Require(visualAssetId, nameof(visualAssetId));
        Type = type;
        DisplayName = Require(displayName, nameof(displayName));
        AssetPath = Normalize(assetPath);
        IsPlaceholder = isPlaceholder;
        Description = Normalize(description);
        Tags = (tags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        FallbackVisualAssetId = Normalize(fallbackVisualAssetId);
    }

    public string VisualAssetId { get; }
    public VisualAssetType Type { get; }
    public string DisplayName { get; }
    public string? AssetPath { get; }
    public bool IsPlaceholder { get; }
    public string? Description { get; }
    public IReadOnlyList<string> Tags { get; }
    public string? FallbackVisualAssetId { get; }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new LocationDataException($"{name} must not be empty.") : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class VisualAssetCatalog
{
    public const string UnknownVisualAssetId = "unknown_visual_placeholder";
    private readonly IReadOnlyDictionary<string, VisualAssetDefinition> definitions;

    public VisualAssetCatalog(IEnumerable<VisualAssetDefinition> definitions)
    {
        try { this.definitions = definitions.ToDictionary(item => item.VisualAssetId, StringComparer.Ordinal); }
        catch (ArgumentException) { throw new LocationDataException("Visual asset IDs must be unique."); }
        if (!this.definitions.ContainsKey(UnknownVisualAssetId))
            throw new LocationDataException($"Visual assets require fallback '{UnknownVisualAssetId}'.");
        foreach (var definition in this.definitions.Values)
            if (definition.FallbackVisualAssetId != null && !this.definitions.ContainsKey(definition.FallbackVisualAssetId))
                throw new LocationDataException($"Visual asset '{definition.VisualAssetId}' references unknown fallback '{definition.FallbackVisualAssetId}'.");
    }

    public IReadOnlyCollection<VisualAssetDefinition> Definitions => definitions.Values.ToList();

    public VisualAssetDefinition Resolve(string? visualAssetId)
    {
        if (!string.IsNullOrWhiteSpace(visualAssetId) && definitions.TryGetValue(visualAssetId.Trim(), out var exact)) return exact;
        return definitions[UnknownVisualAssetId];
    }
}

public static class VisualAssetDataLoader
{
    public static VisualAssetCatalog LoadFromJson(IEnumerable<string> documents)
    {
        var definitions = new List<VisualAssetDefinition>();
        foreach (var json in documents ?? throw new ArgumentNullException(nameof(documents)))
        {
            var document = JObject.Parse(json);
            if ((string?)document["documentType"] != "visual-assets" || (int?)document["schemaVersion"] != 1)
                throw new LocationDataException("Visual asset documents require documentType 'visual-assets' and schemaVersion 1.");
            foreach (var item in (document["items"] as JArray ?? new JArray()).OfType<JObject>())
            {
                var id = Required(item, "visualAssetId");
                if (!Enum.TryParse<VisualAssetType>(Required(item, "type"), ignoreCase: true, out var type))
                    throw new LocationDataException($"Visual asset '{id}' has an unknown type.");
                definitions.Add(new VisualAssetDefinition(id, type, Required(item, "displayName"),
                    (string?)item["assetPath"], (bool?)item["isPlaceholder"] ?? false,
                    (string?)item["description"], (item["tags"] as JArray)?.Values<string>()
                        .Where(value => value != null).Select(value => value!),
                    (string?)item["fallbackVisualAssetId"]));
            }
        }
        return new VisualAssetCatalog(definitions);
    }

    private static string Required(JObject item, string field) => string.IsNullOrWhiteSpace((string?)item[field])
        ? throw new LocationDataException($"Visual asset field '{field}' is required.") : ((string)item[field]!).Trim();
}

}
