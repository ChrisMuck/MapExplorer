#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game.App;

internal sealed class GameDataCatalogTests
{
    public void RunAll()
    {
        ManifestLoadsOneCompleteContentSet();
        ManifestRejectsAnOmittedScoutingGroup();
        LegacyDirectoryScanRemainsAvailableDuringMigration();
    }

    private static void ManifestLoadsOneCompleteContentSet()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot())
            ?? throw new InvalidOperationException("Game-data catalog was not loaded.");

        AssertTrue(catalog.UsesManifest, "Authored game data uses an explicit manifest");
        AssertEqual(26, catalog.Documents.Count, "Manifest declares legacy and target authoring documents");
        AssertTrue(catalog.Documents.Any(document => document.DocumentType == "scout-mission-types"), "Scouting mission types are in the shared catalog");
        AssertTrue(catalog.Documents.Any(document => document.DocumentType == "scout-report-templates"), "Scouting reports are in the shared catalog");
        AssertEqual(7, catalog.Authoring.ScenarioProfiles.Count, "Seven initial archetype scenario profiles load from the catalog");
        AssertEqual(4, catalog.Authoring.Findings.Count, "Findings are separate from material-resource content");
        AssertEqual(4, catalog.Authoring.FactionOffers.Count, "Faction offers load without static faction assignments");
        AssertEqual(3, catalog.Authoring.FactionMemories.Count, "Faction memories use stable semantic IDs");
        AssertEqual(5, catalog.WorldGeneration.Sizes.Count, "World-size presets load through the same catalog");
        AssertEqual(12, catalog.WorldGeneration.Options.Count, "World option presets load through the same catalog");
    }

    private static void ManifestRejectsAnOmittedScoutingGroup()
    {
        var root = CopyGameData(includeManifest: true, includeScouting: false);
        try
        {
            AssertThrows(
                () => GameDataCatalog.LoadFromDirectory(root),
                "Manifest without scouting documents must be rejected");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void LegacyDirectoryScanRemainsAvailableDuringMigration()
    {
        var root = CopyGameData(includeManifest: false, includeScouting: true);
        try
        {
            var catalog = GameDataCatalog.LoadFromDirectory(root)
                ?? throw new InvalidOperationException("Legacy game-data catalog was not loaded.");
            AssertFalse(catalog.UsesManifest, "Catalog reports legacy directory discovery");
        AssertEqual(26, catalog.Documents.Count, "Legacy discovery still finds legacy and target authoring documents");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CopyGameData(bool includeManifest, bool includeScouting)
    {
        var source = GameDataRoot();
        var target = Path.Combine(Path.GetTempPath(), "maptest-game-data-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(target);

        var documents = new List<string>();
        foreach (var sourcePath in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, sourcePath).Replace('\\', '/');
            if (string.Equals(relative, "game-data-manifest.json", StringComparison.OrdinalIgnoreCase)) continue;
            if (!includeScouting && relative.StartsWith("Scouting/", StringComparison.Ordinal)) continue;

            var targetPath = Path.Combine(target, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath);
            documents.Add(relative);
        }

        if (includeManifest)
        {
            var manifest = new
            {
                documentType = "game-data-manifest",
                schemaVersion = 1,
                contentVersion = 1,
                documents = documents.OrderBy(path => path, StringComparer.Ordinal).ToArray()
            };
            File.WriteAllText(
                Path.Combine(target, "game-data-manifest.json"),
                JsonSerializer.Serialize(manifest));
        }

        return target;
    }

    private static string GameDataRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        if (!Directory.Exists(root)) throw new InvalidOperationException("Game-data root was not found for tests.");
        return root;
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (LocationDataException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected {nameof(LocationDataException)}.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"{message}: expected true.");
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition) throw new InvalidOperationException($"{message}: expected false.");
    }
}
