#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>One declared JSON document loaded into the shared game-data catalog.</summary>
public sealed class GameDataDocumentInfo
{
    public GameDataDocumentInfo(string relativePath, string documentType, int schemaVersion)
    {
        RelativePath = RequireText(relativePath, nameof(relativePath));
        DocumentType = RequireText(documentType, nameof(documentType));
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaVersion = schemaVersion;
    }

    public string RelativePath { get; }
    public string DocumentType { get; }
    public int SchemaVersion { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}

/// <summary>
/// The single content-loading entry point for application code, Unity and developer tools.
/// Individual loaders remain responsible for typed parsing; this catalog owns root discovery,
/// explicit manifests and the declared document set.
/// </summary>
public sealed class GameDataCatalog
{
    private const string ManifestFileName = "game-data-manifest.json";
    private const int ManifestSchemaVersion = 1;

    private static readonly HashSet<string> LocationDocumentTypes = new(StringComparer.Ordinal)
    {
        "location-archetypes",
        "location-variants",
        "location-modifiers",
        "location-actions",
        "location-outcome-tables",
        "location-content-profiles",
        "location-instances"
    };

    private static readonly HashSet<string> CrossSystemDocumentTypes = new(StringComparer.Ordinal)
    {
        "evidence-definitions",
        "world-trigger-definitions",
        "consequence-definitions",
        "faction-signatures",
        "faction-profiles",
        "faction-reaction-rules",
        "faction-territory-entry-rules",
        "scout-mission-types",
        "scout-focuses",
        "scout-outcome-rules",
        "scout-report-templates"
    };

    private static readonly HashSet<string> TargetAuthoringDocumentTypes = new(StringComparer.Ordinal)
    {
        "location-state-profiles",
        "location-scenario-profiles",
        "findings",
        "finding-tables",
        "context-definitions",
        "situation-definitions",
        "faction-offers",
        "faction-memory-definitions"
    };

    private static readonly HashSet<string> WorldGenerationDocumentTypes = new(StringComparer.Ordinal)
    {
        "world-generation-presets",
        "world-generation-option-presets"
    };

    private static readonly HashSet<string> SceneDocumentTypes = new(StringComparer.Ordinal)
    {
        "scene-fragments",
        "scene-policies",
        "scene-localization",
        "contact-presentation-profiles"
    };

    private static readonly string[] RequiredManifestDocumentTypes =
    {
        "location-archetypes",
        "location-variants",
        "location-modifiers",
        "location-actions",
        "location-outcome-tables",
        "location-content-profiles",
        "location-state-profiles",
        "location-scenario-profiles",
        "evidence-definitions",
        "findings",
        "finding-tables",
        "context-definitions",
        "situation-definitions",
        "faction-offers",
        "faction-memory-definitions",
        "world-trigger-definitions",
        "consequence-definitions",
        "faction-signatures",
        "faction-profiles",
        "faction-reaction-rules",
        "faction-territory-entry-rules",
        "scout-mission-types",
        "scout-focuses",
        "scout-outcome-rules",
        "scout-report-templates",
        "scene-fragments",
        "scene-policies",
        "scene-localization",
        "contact-presentation-profiles",
        "world-generation-presets",
        "world-generation-option-presets"
    };

    private readonly List<GameDataDocumentInfo> documents;

    private GameDataCatalog(
        string rootPath,
        bool usesManifest,
        int contentVersion,
        LocationDataBundle locations,
        CrossSystemDataBundle crossSystem,
        CrossSystemAuthoringBundle authoring,
        SceneDescriptionCatalog scenes,
        WorldGenerationPresetCatalog worldGeneration,
        IEnumerable<GameDataDocumentInfo> documents)
    {
        RootPath = RequireText(rootPath, nameof(rootPath));
        UsesManifest = usesManifest;
        ContentVersion = contentVersion;
        Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        CrossSystem = crossSystem ?? throw new ArgumentNullException(nameof(crossSystem));
        Authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
        Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        WorldGeneration = worldGeneration ?? throw new ArgumentNullException(nameof(worldGeneration));
        this.documents = (documents ?? throw new ArgumentNullException(nameof(documents))).ToList();
    }

    public string RootPath { get; }
    public bool UsesManifest { get; }
    public int ContentVersion { get; }
    public LocationDataBundle Locations { get; }
    public CrossSystemDataBundle CrossSystem { get; }
    /// <summary>Schema-v2 reusable definitions. Empty until target documents are introduced.</summary>
    public CrossSystemAuthoringBundle Authoring { get; }
    /// <summary>Validated, localized scene authoring shared by every presentation client.</summary>
    public SceneDescriptionCatalog Scenes { get; }
    public WorldGenerationPresetCatalog WorldGeneration { get; }
    public IReadOnlyList<GameDataDocumentInfo> Documents => documents;

    /// <summary>
    /// Loads an explicit manifest when present. A directory without a manifest remains supported
    /// during migration, but callers can inspect <see cref="UsesManifest"/> to report that state.
    /// Returns null only when the root itself does not exist.
    /// </summary>
    public static GameDataCatalog? LoadFromDirectory(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder)) return null;

        var rootPath = Path.GetFullPath(rootFolder);
        var manifestPath = Path.Combine(rootPath, ManifestFileName);
        var sourcePaths = File.Exists(manifestPath)
            ? ResolveManifestPaths(rootPath, manifestPath)
            : DiscoverLegacyPaths(rootPath);

        if (sourcePaths.Count == 0)
        {
            throw new LocationDataException($"Game-data root '{rootPath}' contains no JSON content documents.");
        }

        var sources = sourcePaths.Select(path => ReadDocument(rootPath, path)).ToList();
        var usesManifest = File.Exists(manifestPath);
        if (usesManifest)
        {
            ValidateManifestDocumentSet(sources);
        }

        return Build(rootPath, usesManifest, usesManifest ? ReadManifestContentVersion(manifestPath) : 0, sources);
    }

    private static GameDataCatalog Build(string rootPath, bool usesManifest, int contentVersion, IReadOnlyList<DocumentSource> sources)
    {
        var locationDocuments = sources.Where(source => LocationDocumentTypes.Contains(source.DocumentType)).Select(source => source.Json).ToList();
        var crossSystemDocuments = sources.Where(source => CrossSystemDocumentTypes.Contains(source.DocumentType)).Select(source => source.Json).ToList();
        var authoringDocuments = sources.Where(source => TargetAuthoringDocumentTypes.Contains(source.DocumentType)).Select(source => source.Json).ToList();
        var worldGenerationDocuments = sources.Where(source => WorldGenerationDocumentTypes.Contains(source.DocumentType)).Select(source => source.Json).ToList();
        var sceneDocuments = sources.Where(source => SceneDocumentTypes.Contains(source.DocumentType)).Select(source => source.Json).ToList();

        var unsupported = sources
            .Where(source => !LocationDocumentTypes.Contains(source.DocumentType)
                && !CrossSystemDocumentTypes.Contains(source.DocumentType)
                && !TargetAuthoringDocumentTypes.Contains(source.DocumentType)
                && !WorldGenerationDocumentTypes.Contains(source.DocumentType)
                && !SceneDocumentTypes.Contains(source.DocumentType))
            .Select(source => $"{source.RelativePath} ({source.DocumentType})")
            .ToList();
        if (unsupported.Count > 0)
        {
            throw new LocationDataException("Game-data catalog contains unsupported document types: " + string.Join(", ", unsupported) + ".");
        }

        if (locationDocuments.Count == 0 || crossSystemDocuments.Count == 0 || worldGenerationDocuments.Count == 0 || sceneDocuments.Count == 0)
        {
            throw new LocationDataException("Game-data catalog requires location, cross-system, scene and world-generation document groups.");
        }

        try
        {
            var locations = LocationDataLoader.LoadFromJson(locationDocuments);
            var crossSystem = CrossSystemDataLoader.LoadFromJson(crossSystemDocuments);
            var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(authoringDocuments);
            CrossSystemContentValidator.Validate(locations, crossSystem, authoring);
            var scenes = SceneDescriptionDataLoader.LoadFromJson(sceneDocuments);
            SceneDescriptionContentValidator.ValidateReferences(scenes, locations, authoring);
            var worldGeneration = WorldGenerationPresetLoader.LoadCatalogFromJson(worldGenerationDocuments);
            return new GameDataCatalog(
                rootPath,
                usesManifest,
                contentVersion,
                locations,
                crossSystem,
                authoring,
                scenes,
                worldGeneration,
                sources.Select(source => new GameDataDocumentInfo(source.RelativePath, source.DocumentType, source.SchemaVersion)));
        }
        catch (LocationDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LocationDataException($"Game-data catalog at '{rootPath}' could not be loaded: {ex.Message}");
        }
    }

    private static List<string> ResolveManifestPaths(string rootPath, string manifestPath)
    {
        JObject manifest;
        try
        {
            manifest = JObject.Parse(File.ReadAllText(manifestPath));
        }
        catch (Exception ex) when (ex is IOException || ex is JsonException)
        {
            throw new LocationDataException($"Game-data manifest '{manifestPath}' is invalid: {ex.Message}");
        }

        if ((string?)manifest["documentType"] != "game-data-manifest")
        {
            throw new LocationDataException($"Game-data manifest '{manifestPath}' must use documentType 'game-data-manifest'.");
        }
        if ((int?)manifest["schemaVersion"] != ManifestSchemaVersion)
        {
            throw new LocationDataException($"Game-data manifest '{manifestPath}' must use schemaVersion {ManifestSchemaVersion}.");
        }

        var documents = manifest["documents"] as JArray;
        if (documents == null || documents.Count == 0)
        {
            throw new LocationDataException($"Game-data manifest '{manifestPath}' requires a non-empty documents array.");
        }

        var declared = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in documents)
        {
            var relativePath = token.Value<string>();
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new LocationDataException($"Game-data manifest '{manifestPath}' contains an empty document path.");
            }

            var normalized = NormalizeRelativePath(relativePath);
            if (!seen.Add(normalized))
            {
                throw new LocationDataException($"Game-data manifest '{manifestPath}' declares '{normalized}' more than once.");
            }

            var fullPath = ResolveInsideRoot(rootPath, normalized);
            if (!File.Exists(fullPath))
            {
                throw new LocationDataException($"Game-data manifest declares missing document '{normalized}'.");
            }
            declared.Add(fullPath);
        }

        var actual = DiscoverLegacyPaths(rootPath)
            .Select(path => NormalizeRelativePath(Path.GetRelativePath(rootPath, path)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var undeclared = actual.Where(path => !seen.Contains(path)).OrderBy(path => path, StringComparer.Ordinal).ToList();
        if (undeclared.Count > 0)
        {
            throw new LocationDataException("Game-data manifest does not declare: " + string.Join(", ", undeclared) + ".");
        }

        return declared;
    }

    private static int ReadManifestContentVersion(string manifestPath)
    {
        var manifest = JObject.Parse(File.ReadAllText(manifestPath));
        return (int?)manifest["contentVersion"] ?? 0;
    }

    private static List<string> DiscoverLegacyPaths(string rootPath)
    {
        return Directory.EnumerateFiles(rootPath, "*.json", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static DocumentSource ReadDocument(string rootPath, string fullPath)
    {
        var relativePath = NormalizeRelativePath(Path.GetRelativePath(rootPath, fullPath));
        var json = File.ReadAllText(fullPath);
        JObject envelope;
        try
        {
            envelope = JObject.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new LocationDataException($"Game-data document '{relativePath}' is malformed: {ex.Message}");
        }

        var documentType = (string?)envelope["documentType"];
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new LocationDataException($"Game-data document '{relativePath}' has no documentType.");
        }
        var schemaVersion = (int?)envelope["schemaVersion"] ?? 0;
        if (schemaVersion < 1)
        {
            throw new LocationDataException($"Game-data document '{relativePath}' has an invalid schemaVersion.");
        }

        return new DocumentSource(relativePath, documentType.Trim(), schemaVersion, json);
    }

    private static void ValidateManifestDocumentSet(IReadOnlyList<DocumentSource> sources)
    {
        var types = sources.Select(source => source.DocumentType).ToHashSet(StringComparer.Ordinal);
        var missing = RequiredManifestDocumentTypes.Where(type => !types.Contains(type)).ToList();
        if (missing.Count > 0)
        {
            throw new LocationDataException("Game-data manifest is missing required document types: " + string.Join(", ", missing) + ".");
        }
    }

    private static string ResolveInsideRoot(string rootPath, string relativePath)
    {
        var rootWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocationDataException($"Game-data manifest path '{relativePath}' escapes its root.");
        }
        return fullPath;
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(segment => segment == ".."))
        {
            throw new LocationDataException($"Game-data manifest path '{path}' must be relative to its root.");
        }
        return normalized;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }

    private sealed record DocumentSource(string RelativePath, string DocumentType, int SchemaVersion, string Json);
}
}
