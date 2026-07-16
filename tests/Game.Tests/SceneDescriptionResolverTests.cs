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
