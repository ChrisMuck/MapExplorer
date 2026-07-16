#nullable enable
using System;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class SceneDescriptionResolverTests
{
    public void RunAll()
    {
        EveryAuthoredArchetypeResolvesThroughOnePipeline();
        ResolutionIsDeterministicAndDoesNotInferHiddenModifiers();
        RemoteViewUsesStoredKnowledgeAndHandlesDoubt();
        SupersessionAndExclusiveTagsSelectOneCoherentSet();
        ArrivalSceneIsProducedAndCommittedByMovement();
    }

    private static void ArrivalSceneIsProducedAndCommittedByMovement()
    {
        var app = new GameApplication(LoadCatalog());
        var game = app.CreateTutorialGame();
        var location = game.World.Locations.Single(item => item.Id == "broken-ravine");

        var movement = app.MoveExpedition(game, new HexCoord(2, 15));

        AssertTrue(movement.Success, "Movement to a location anchor succeeds");
        AssertEqual(1, movement.ArrivalScenes.Count, "Movement returns one scene for the reached location anchor");
        AssertTrue(movement.ArrivalScenes[0].Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Contains("frag-open-broken-bridge"),
            "Arrival uses the variant opening through the generic scene pipeline");
        AssertTrue(game.Knowledge.FindLocationCondition(location.Id) != null,
            "The directly observed condition is committed to KnowledgeState after scene resolution");
        AssertFalse(location.IsInspected, "Arrival observation does not silently execute the inspection action");
    }

    private static void SupersessionAndExclusiveTagsSelectOneCoherentSet()
    {
        var catalog = SceneDescriptionDataLoader.LoadFromJson(new[]
        {
            """
            { "documentType": "scene-fragments", "schemaVersion": 1, "contentVersion": 1, "items": [
              { "id": "frag-base", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 10, "textId": "scene.base" },
              { "id": "frag-replacement", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 20, "supersedesFragmentIds": ["frag-base"], "textId": "scene.replacement" },
              { "id": "frag-tone-low", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 30, "exclusiveTag": "tone", "textId": "scene.low" },
              { "id": "frag-tone-high", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 40, "exclusiveTag": "tone", "textId": "scene.high" }
            ] }
            """,
            """
            { "documentType": "scene-policies", "schemaVersion": 1, "contentVersion": 1, "items": [
              { "id": "policy-test", "subjectKind": "location", "archetypeId": "test-archetype", "questionTextId": "scene.question",
                "questionResolvedWhen": { "knownInteractionStatesAny": ["resolved"] }, "ordering": ["opening"],
                "paragraphing": [["opening"]], "maxFragments": 4, "maxPerGroup": {} }
            ] }
            """,
            """
            { "documentType": "scene-localization", "schemaVersion": 1, "contentVersion": 1, "locale": "de", "fallbackLocale": null, "isDefault": true, "items": [
              { "id": "scene.base", "text": "Basis." }, { "id": "scene.replacement", "text": "Ersatz." },
              { "id": "scene.low", "text": "Leise." }, { "id": "scene.high", "text": "Deutlich." },
              { "id": "scene.question", "text": "Was nun?" }
            ] }
            """
        });
        var scene = new SceneDescriptionResolver(catalog).ResolveLocation(new LocationSceneView(
            "selection-test", SceneTrigger.Arrival, "test-archetype", "test-variant", "Test", null, null,
            "open", "open", "unknown", 1, null, KnowledgeLevel.Confirmed, null, null, null));
        var ids = scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).ToList();
        AssertTrue(ids.Contains("frag-replacement"), "Superseding fragment remains selected");
        AssertFalse(ids.Contains("frag-base"), "Superseded fragment is removed before caps are applied");
        AssertTrue(ids.Contains("frag-tone-high"), "Highest-priority exclusive fragment wins");
        AssertFalse(ids.Contains("frag-tone-low"), "Only one fragment per exclusive tag remains");
    }

    private static void RemoteViewUsesStoredKnowledgeAndHandlesDoubt()
    {
        var catalog = LoadCatalog();
        var app = new GameApplication(catalog);
        var game = app.CreateTutorialGame();
        var location = game.World.Locations.Single(item => item.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, location.Coord);
        var inspection = app.InspectLocation(game, location.Coord);
        AssertTrue(inspection.Success, "Location inspection establishes a stored observation");

        var remote = app.GetLocationInteraction(game, location.Id).Presentation;
        AssertTrue(remote?.Scene != null, "Known location remote view uses the shared scene resolver");
        AssertFalse(remote!.Scene!.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Contains("frag-open-broken-bridge"),
            "Remote view does not replay a direct sensory opening");
        AssertEqual(remote.Scene.Message, remote.Description, "Remote presentation renders the structured scene message");

        game.Knowledge.FindLocationCondition(location.Id)!.MarkDoubtful();
        var doubtful = app.GetLocationInteraction(game, location.Id).Presentation!.Scene!;
        AssertTrue(doubtful.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Contains("frag-loc-history-doubtful"),
            "Doubtful stored knowledge resolves to an explicit uncertainty fragment");
        AssertFalse(doubtful.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Any(id => id.StartsWith("frag-loc-route-op-", StringComparison.Ordinal)),
            "Doubt-intolerant operational fragments do not present stale state as current truth");
    }

    private static void EveryAuthoredArchetypeResolvesThroughOnePipeline()
    {
        var catalog = LoadCatalog();
        var resolver = new SceneDescriptionResolver(catalog.Scenes);

        foreach (var scenario in catalog.Authoring.ScenarioProfiles.Values.OrderBy(item => item.ArchetypeId, StringComparer.Ordinal))
        {
            var state = catalog.Authoring.StateProfiles[scenario.StateProfileId];
            var content = catalog.Locations.Definitions.FindContentProfile(scenario.ContentProfileId)
                ?? throw new InvalidOperationException($"Missing content profile '{scenario.ContentProfileId}'.");
            var scene = resolver.ResolveLocation(new LocationSceneView(
                "test-" + scenario.Id, SceneTrigger.InspectionResult, scenario.ArchetypeId, scenario.VariantId,
                content.Title, content.Subtitle, content.ImageId,
                state.Channels[LocationStateChannels.Interaction].InitialValue,
                state.Channels[LocationStateChannels.Operational].InitialValue,
                state.Channels[LocationStateChannels.Presence].InitialValue,
                1, null, KnowledgeLevel.Confirmed, null, null, null));

            AssertTrue(scene.Paragraphs.Count > 0, $"Archetype '{scenario.ArchetypeId}' resolves a localized scene");
            AssertTrue(scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Any(id => id.StartsWith("frag-open-", StringComparison.Ordinal)),
                $"Archetype '{scenario.ArchetypeId}' uses its variant opening without special-case code");
        }
    }

    private static void ResolutionIsDeterministicAndDoesNotInferHiddenModifiers()
    {
        var resolver = new SceneDescriptionResolver(LoadCatalog().Scenes);
        var view = new LocationSceneView("stable-subject", SceneTrigger.InspectionResult, "route-obstacle", "broken-bridge",
            "Gebrochene Brücke", null, null, "unapproached", "blocked", "unknown", 1, null,
            KnowledgeLevel.Confirmed, null, null, null);

        var first = resolver.ResolveLocation(view);
        var second = resolver.ResolveLocation(view);
        AssertEqual(first.Message, second.Message, "The same authorized scene view resolves deterministically");
        AssertFalse(first.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Any(id => id.StartsWith("modifier:", StringComparison.Ordinal)),
            "The resolver does not look up or infer hidden world modifiers");
    }

    private static GameDataCatalog LoadCatalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data was not loaded.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
