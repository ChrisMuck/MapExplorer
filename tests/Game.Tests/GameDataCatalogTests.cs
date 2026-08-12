#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.App;
using Game.Core;

internal sealed class GameDataCatalogTests
{
    public void RunAll()
    {
        ManifestLoadsOneCompleteContentSet();
        TutorialCampaignUsesManifestedLocationInstances();
        ManifestRejectsAnOmittedScoutingGroup();
        ManifestRejectsMissingStateWording();
        LegacyDirectoryScanRemainsAvailableDuringMigration();
    }

    private static void ManifestLoadsOneCompleteContentSet()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot())
            ?? throw new InvalidOperationException("Game-data catalog was not loaded.");

        AssertTrue(catalog.UsesManifest, "Authored game data uses an explicit manifest");
        AssertEqual(37, catalog.Documents.Count, "Manifest declares simulation, scene, visual, localization, tutorial-instance and tutorial-campaign documents");
        AssertEqual(6, catalog.Locations.Instances.Count, "Tutorial interaction locations load from the shared catalog");
        AssertEqual("tutorial-coastal-survey", catalog.Tutorial.Id, "Tutorial campaign composition loads from the shared catalog");
        AssertTrue(catalog.Documents.Any(document => document.DocumentType == "scout-mission-types"), "Scouting mission types are in the shared catalog");
        AssertTrue(catalog.Documents.Any(document => document.DocumentType == "scout-report-templates"), "Scouting reports are in the shared catalog");
        AssertEqual(8, catalog.Authoring.ScenarioProfiles.Count, "The legacy-camp scenario joins the initial archetype scenario profiles");
        AssertEqual(11, catalog.Authoring.Findings.Count, "Findings are separate from material-resource content");
        AssertEqual(4, catalog.Authoring.FindingTables.Count, "Generic weighted finding tables load from the catalog");
        AssertEqual(4, catalog.Authoring.FactionOffers.Count, "Faction offers load without static faction assignments");
        AssertEqual(8, catalog.Authoring.FactionMemories.Count, "Faction memories use stable semantic IDs and visible contact tags");
        AssertEqual(5, catalog.WorldGeneration.Sizes.Count, "World-size presets load through the same catalog");
        AssertEqual(12, catalog.WorldGeneration.Options.Count, "World option presets load through the same catalog");
        AssertEqual(104, catalog.Scenes.Fragments.Count, "Location, contact, scout-return, event, base-return and memorial fragments load from JSON");
        AssertEqual(11, catalog.Scenes.Policies.Count, "Locations, contacts, scout returns, events and base returns have scene policies");
        AssertEqual(4, catalog.Scenes.ContactProfiles.Count, "Generic contact styles have authored presentation profiles and a fallback");
        AssertEqual("de", catalog.Scenes.Texts.DefaultLocale, "German is the current default scene locale");
        AssertEqual("FELDNOTIZ", catalog.Scenes.Texts.Resolve("tutorial.field-brief.title"),
            "Tutorial field-brief wording resolves from the shared localization catalog");
        AssertEqual("BASISNOTIZ", catalog.Scenes.Texts.Resolve("tutorial.base-guide.title"),
            "Tutorial base-guide wording resolves from the shared localization catalog");
        AssertTrue(catalog.Scenes.Texts.Resolve("tutorial.field-brief.first-expedition").Length > 80,
            "The initial tutorial brief is authored as localized player-facing text");
        AssertEqual(19, catalog.VisualAssets.Definitions.Count, "Placeholder visual definitions load through the shared catalog");
        AssertEqual("placeholder-bridge", catalog.VisualAssets.Resolve("placeholder-bridge").VisualAssetId,
            "Known visual id resolves its authored definition");
        AssertEqual(VisualAssetCatalog.UnknownVisualAssetId, catalog.VisualAssets.Resolve("missing-visual").VisualAssetId,
            "Missing visual id resolves the neutral authored fallback");
        AssertEqual(
            catalog.Scenes.Texts.Resolve("scene.question.route-obstacle", "de"),
            catalog.Scenes.Texts.Resolve("scene.question.route-obstacle", "fr-FR"),
            "An unavailable locale falls back to the default text without changing scene rules");
    }

    private static void TutorialCampaignUsesManifestedLocationInstances()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot())
            ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
        var app = new GameApplication(catalog);
        var game = app.CreateTutorialGame();

        AssertEqual(6, game.World.Locations.Count(location => !string.IsNullOrWhiteSpace(location.ArchetypeId)),
            "Tutorial uses the six manifest-authored interaction locations");

        var crossing = game.World.Locations.Single(location => location.Id == "broken-ravine");
        AssertEqual("route-obstacle", crossing.ArchetypeId, "Tutorial crossing keeps its catalog archetype");
        AssertEqual("blocked", crossing.OperationalStateId, "Tutorial crossing keeps its catalog initial state");
        AssertTrue(crossing.Anchor.Coords.All(coord => game.World.Map.GetTile(coord).LocationId == crossing.Id),
            "Every edge hex of a manifested tutorial location is registered on the logical map");

        var camp = game.World.Locations.Single(location => location.Id == "abandoned-camp");
        AssertEqual("abandoned", camp.PresenceStateId, "Tutorial camp keeps its catalog initial presence state");
        AssertEqual(camp.Id, game.World.Map.GetTile(camp.Coord).LocationId,
            "Manifested point locations are registered on their logical map tile");

        var landing = game.World.Locations.Single(location => location.Id == "coastal-landing");
        AssertEqual("contact-site", landing.ArchetypeId, "Tutorial includes an authored coastal contact opportunity");
        AssertTrue(landing.FactionRelations.Any(relation => relation.FactionId == "coastal-people" && relation.Kind == LocationFactionRelationKind.Guarded),
            "Tutorial fixture supplies the runtime relation required for the contact chain");

        var warning = game.World.Locations.Single(location => location.Id == "border-warning");
        AssertTrue(game.Base.Location.DistanceTo(landing.Coord) < game.Base.Location.DistanceTo(warning.Coord),
            "Coastal contact appears before the territorial warning along the tutorial route");
        AssertTrue(warning.Coord.DistanceTo(camp.Coord) <= 3,
            "The legacy camp follows the warning closely enough to connect their clues");
        AssertTrue(game.Base.Location.DistanceTo(crossing.Coord) >= 15,
            "The broken crossing is far enough from base to create a real return decision");
        AssertTrue(crossing.Anchor.Coords.All(coord => game.World.Map.GetTile(coord).HasRoad),
            "The broken crossing interrupts the traceable old survey route");
        game.Expedition.SetPosition(crossing.Anchor.Coords[0]);
        AssertFalse(app.MoveExpedition(game, crossing.Anchor.Coords[1]).Success,
            "A historical road drawing does not bypass the active route obstacle before OpenRoute resolves it");
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

    private static void ManifestRejectsMissingStateWording()
    {
        var root = CopyGameData(includeManifest: true, includeScouting: true);
        try
        {
            var path = Path.Combine(root, "Scenes", "location-fragments.json");
            var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            var items = document["items"]!.AsArray();
            var destroyed = items.Single(item => item!["id"]!.GetValue<string>() == "frag-loc-route-op-destroyed");
            items.Remove(destroyed);
            File.WriteAllText(path, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            AssertThrows(() => GameDataCatalog.LoadFromDirectory(root), "Every authored state needs a localized scene fragment");
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
            AssertEqual(37, catalog.Documents.Count, "Legacy discovery still finds simulation, scene, visual, tutorial-instance and tutorial-campaign documents");
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
