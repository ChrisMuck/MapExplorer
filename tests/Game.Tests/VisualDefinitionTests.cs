#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class VisualDefinitionTests
{
    public void RunAll()
    {
        CatalogLoadsTheBridgeComposition();
        BridgeShowsTheFormForEachOperationalState();
        BridgeKeepsPostsAndPosesTheDestroyedState();
        UnknownConditionShowsOnlyTheRavineMarkers();
        PlayerViewFollowsKnowledgeNotWorldTruth();
        WorldTruthViewUsesTheLiveState();
        CompositionFallsBackFromVariantToArchetypeToMarker();
        ValidatorRejectsBadReferences();
    }

    private static GameDataCatalog Catalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game-data catalog was not loaded.");
    }

    private static SpecialLocationState Bridge(GameState game) => game.World.Locations.First(location => location.Id == "broken-ravine");

    private static int ShownCount(LocationVisualDescription description) => description.ShownElements.Count();

    private static LocationVisualDescription Resolve(LocationVisualResolver resolver, SpecialLocationState bridge, string? operational)
    {
        return resolver.Resolve(bridge, new LocationVisualContext(VisualKnowledge.Confirmed, operational));
    }

    private static void CatalogLoadsTheBridgeComposition()
    {
        var visuals = Catalog().VisualDefinitions;
        var composition = visuals.FindComposition("route-obstacle", "broken-bridge");

        AssertTrue(composition != null, "The broken bridge has a composition");
        AssertEqual("vis-broken-bridge", composition!.Id, "Bridge composition id");
        AssertEqual(42, composition.Elements.Count, "Bridge composition lists its planks, stringers, rails, posts, debris, scaffold and sign");
        AssertTrue(visuals.FindPart("bridge-plank-old-a") != null && visuals.FindPart("warning-sign")!.Primitives.Count == 2, "Parts of the kit are defined as primitives");
        AssertEqual("#E69F00", visuals.FindMarker("route-obstacle").ColorHex, "Route obstacles use the orange ring");
        AssertEqual("#C8B88A", visuals.FindMarker("something-new").ColorHex, "Unknown archetypes use the default marker");
    }

    private static void BridgeShowsTheFormForEachOperationalState()
    {
        var game = TutorialGameFactory.Create();
        var bridge = Bridge(game);
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);

        AssertEqual(20, ShownCount(Resolve(resolver, bridge, "open")), "Intact bridge");
        AssertEqual(19, ShownCount(Resolve(resolver, bridge, "blocked")), "Damaged bridge");
        AssertEqual(19, ShownCount(Resolve(resolver, bridge, "risky-passage")), "Risky passage looks damaged");
        AssertEqual(23, ShownCount(Resolve(resolver, bridge, "repaired")), "Repaired bridge with scaffold");
        AssertEqual(23, ShownCount(Resolve(resolver, bridge, "provisional")), "Provisional crossing looks repaired");
        AssertEqual(7, ShownCount(Resolve(resolver, bridge, "destroyed")), "Destroyed bridge keeps stumps and debris");

        var damaged = Resolve(resolver, bridge, "blocked");
        AssertTrue(damaged.Elements.Single(element => element.Id == "plank-old-5").Shown == false, "The middle planks are gone when damaged");
        AssertTrue(damaged.Elements.Single(element => element.Id == "plank-old-3").Shown, "The planks beside the gap remain");
        AssertEqual(29f, damaged.Elements.Single(element => element.Id == "plank-old-3").Pose.Euler.X, "A plank beside the gap tilts when damaged");
        AssertTrue(damaged.Elements.Single(element => element.Id == "warning-sign").Shown, "A damaged bridge carries the warning sign");
    }

    private static void BridgeKeepsPostsAndPosesTheDestroyedState()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var destroyed = Resolve(resolver, Bridge(game), "destroyed");
        var post = destroyed.Elements.Single(element => element.Id == "post-l-a");

        AssertTrue(post.Shown, "Posts stay in every state");
        AssertEqual(0.4f, post.Pose.Scale.Y, "Posts shrink to stumps when destroyed");
        AssertEqual(-0.28f, post.Pose.Position.Y, "Stumps sink");
        var intact = Resolve(resolver, Bridge(game), "open").Elements.Single(element => element.Id == "post-l-a");
        AssertEqual(1f, intact.Pose.Scale.Y, "Intact posts keep their full height");
    }

    private static void UnknownConditionShowsOnlyTheRavineMarkers()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var unknown = Resolve(resolver, Bridge(game), null);

        AssertEqual(4, ShownCount(unknown), "Without an observation only the four posts show");
        AssertTrue(unknown.ShownElements.All(element => element.PartId == "bridge-post"), "Unknown state reveals neither damage nor repair");
        AssertTrue(unknown.StateKey.StartsWith("unknown|", StringComparison.Ordinal), "The state key records the missing knowledge");
    }

    private static void PlayerViewFollowsKnowledgeNotWorldTruth()
    {
        var game = TutorialGameFactory.Create();
        var bridge = Bridge(game);
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var coord = bridge.Anchor.PrimaryCoord;

        game.Knowledge.SetTileKnowledge(coord, KnowledgeLevel.Unknown);
        AssertTrue(resolver.ResolveForPlayer(game, bridge) == null, "Locations on unknown ground are not drawn");

        game.Knowledge.SetTileKnowledge(coord, KnowledgeLevel.Reported);
        var reported = resolver.ResolveForPlayer(game, bridge)!;
        AssertEqual(VisualKnowledge.Reported, reported.Knowledge, "Reported ground draws a reported look");
        AssertEqual(4, ShownCount(reported), "Reported ground without an observed condition shows only the posts");

        game.Knowledge.SetTileKnowledge(coord, KnowledgeLevel.Confirmed);
        game.Knowledge.ObserveLocationCondition(bridge, game.World.WorldDay);
        var observed = resolver.ResolveForPlayer(game, bridge)!;
        var observedState = bridge.OperationalStateId;
        AssertEqual(VisualKnowledge.Confirmed, observed.Knowledge, "Confirmed ground draws a confirmed look");
        AssertEqual(ShownCount(Resolve(resolver, bridge, observedState)), ShownCount(observed), "An observed condition draws that condition");

        var other = observedState == "destroyed" ? "open" : "destroyed";
        bridge.SetState(LocationStateChannels.Operational, other);
        var afterWorldChange = resolver.ResolveForPlayer(game, bridge)!;
        AssertEqual(observed.StateKey, afterWorldChange.StateKey, "A later world change stays invisible until it is observed again");
        AssertEqual(ShownCount(observed), ShownCount(afterWorldChange), "The drawn bridge keeps the last observed form");
    }

    private static void WorldTruthViewUsesTheLiveState()
    {
        var game = TutorialGameFactory.Create();
        var bridge = Bridge(game);
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);

        bridge.SetState(LocationStateChannels.Operational, "destroyed");
        var truth = resolver.ResolveWorldTruth(bridge);
        AssertEqual(7, ShownCount(truth), "The developer view shows the live state");
        AssertEqual(VisualKnowledge.Confirmed, truth.Knowledge, "The developer view is fully confirmed");
    }

    private static void CompositionFallsBackFromVariantToArchetypeToMarker()
    {
        const string json = """
        {
          "documentType": "visual-definitions", "schemaVersion": 1,
          "markers": [ { "archetypeId": "route-obstacle", "color": "#E69F00", "ringGaps": 3 } ],
          "parts": [ { "id": "block", "primitives": [ { "shape": "box", "size": [1, 1, 1], "color": "#808080" } ] } ],
          "compositions": [
            { "id": "generic-obstacle", "match": { "archetypeId": "route-obstacle" },
              "elements": [ { "id": "block", "part": "block" } ] }
          ]
        }
        """;
        var set = VisualDefinitionLoader.LoadFromJson(new[] { json });
        var game = TutorialGameFactory.Create();
        var bridge = Bridge(game);
        var resolver = new LocationVisualResolver(set);

        var generic = Resolve(resolver, bridge, "blocked");
        AssertEqual("generic-obstacle", generic.CompositionId, "A variant without its own composition uses the archetype composition");
        AssertEqual(1, ShownCount(generic), "The archetype composition draws its elements");

        var empty = new LocationVisualResolver(VisualDefinitionSet.Empty);
        var plain = Resolve(empty, bridge, "blocked");
        AssertTrue(plain.CompositionId == null && plain.Elements.Count == 0, "Without any composition only the marker remains");
        AssertEqual("#C8B88A", plain.MarkerColorHex, "The neutral marker color is used without definitions");
    }

    private static void ValidatorRejectsBadReferences()
    {
        var catalog = Catalog();

        ExpectFailure(Compose("flying", "route-obstacle", "broken-bridge"), catalog, "does not define", "A state the profile lacks is rejected");
        ExpectFailure(Compose("open", "route-obstacle", "broken-bridge"), catalog, "does not cover", "A composition must cover every state of a channel it reacts to");
        ExpectFailure(Compose("open", "no-such-archetype", null), catalog, "unknown archetype", "An unknown archetype is rejected");
        ExpectFailure(Compose("open", "route-obstacle", "no-such-variant"), catalog, "unknown variant", "An unknown variant is rejected");

        try
        {
            VisualDefinitionLoader.LoadFromJson(new[] { Compose("open", "route-obstacle", null).Replace("\"part\": \"block\"", "\"part\": \"missing\"") });
            throw new InvalidOperationException("An unknown part should be rejected.");
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains("unknown part"), "Unknown part ids are rejected when the set is built");
        }
    }

    private static string Compose(string state, string archetype, string? variant)
    {
        var variantPart = variant == null ? string.Empty : $", \"variantId\": \"{variant}\"";
        return $$"""
        {
          "documentType": "visual-definitions", "schemaVersion": 1,
          "markers": [],
          "parts": [ { "id": "block", "primitives": [ { "shape": "box", "size": [1, 1, 1], "color": "#808080" } ] } ],
          "compositions": [
            { "id": "test", "match": { "archetypeId": "{{archetype}}"{{variantPart}} },
              "elements": [ { "id": "block", "part": "block", "shows": { "channel": "operational", "in": ["{{state}}"] } } ] }
          ]
        }
        """;
    }

    private static void ExpectFailure(string json, GameDataCatalog catalog, string expected, string message)
    {
        try
        {
            var set = VisualDefinitionLoader.LoadFromJson(new[] { json });
            VisualDefinitionValidator.Validate(set, catalog.Locations, catalog.Authoring);
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains(expected, StringComparison.Ordinal), message + " (" + exception.Message + ")");
            return;
        }

        throw new InvalidOperationException(message + ": expected a validation failure.");
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
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }
}
