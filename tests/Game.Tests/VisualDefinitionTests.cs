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
        CampCompositionLoadsAndShowsPresenceStates();
        CampCombinesTheThreeStateChannels();
        CampUnknownConditionShowsOnlyNeutralParts();
        CampFireIsALightAndSmokeOnlyWhenLived();
        GraveShowsSealedOpenAndDisturbedForms();
        GraveCombinesPresenceAndInteraction();
        GraveUnknownConditionShowsOnlyNeutralParts();
        PlainStatesCountAsCoveredAndAreValidated();
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

    private static SpecialLocationState Camp(GameState game) => game.World.Locations.First(location => location.Id == "abandoned-camp");

    private static LocationVisualDescription ResolveCamp(LocationVisualResolver resolver, SpecialLocationState camp, string? operational, string? presence, string? interaction)
    {
        return resolver.Resolve(camp, new LocationVisualContext(VisualKnowledge.Confirmed, operational, presence, interaction));
    }

    private static void CampCompositionLoadsAndShowsPresenceStates()
    {
        var visuals = Catalog().VisualDefinitions;
        var composition = visuals.FindComposition("investigation-site", "abandoned-camp");
        AssertTrue(composition != null, "The abandoned camp has a composition");
        AssertEqual("vis-abandoned-camp", composition!.Id, "Camp composition id");
        AssertEqual(24, composition.Elements.Count, "Camp composition lists tents, logs, fire, props, figures and traces");
        AssertEqual("point", composition.AnchorMode, "A camp sits on a single hex");
        AssertEqual(0.65f, composition.Scale, "A camp is drawn smaller than its authored size");

        var game = TutorialGameFactory.Create();
        var camp = Camp(game);
        var resolver = new LocationVisualResolver(visuals);
        AssertEqual(11, ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "untouched")), "Abandoned camp: tents, logs, ash, overturned crate, torn flag");
        AssertEqual(13, ShownCount(ResolveCamp(resolver, camp, "none", "recently-used", "untouched")), "Recently used camp adds fire, light, smoke, crate and flag");
        AssertEqual(15, ShownCount(ResolveCamp(resolver, camp, "none", "watched", "untouched")), "A watched camp also shows two figures");
        AssertEqual(0.5f, ResolveCamp(resolver, camp, "none", "abandoned", "untouched").Elements.Single(element => element.Id == "tent-1").Pose.Scale.Y, "Abandoned tents sag");
        AssertEqual(1f, ResolveCamp(resolver, camp, "none", "recently-used", "untouched").Elements.Single(element => element.Id == "tent-1").Pose.Scale.Y, "Used tents stand");
    }

    private static void CampCombinesTheThreeStateChannels()
    {
        var game = TutorialGameFactory.Create();
        var camp = Camp(game);
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var baseCount = ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "untouched"));

        AssertEqual(baseCount + 1, ShownCount(ResolveCamp(resolver, camp, "cleared", "abandoned", "untouched")), "A cleared camp adds a cairn");
        AssertEqual(baseCount + 1, ShownCount(ResolveCamp(resolver, camp, "unsafe", "abandoned", "untouched")), "An unsafe camp adds a warning sign");
        AssertEqual(baseCount + 1, ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "inspected")), "An inspected camp carries the expedition stake");
        AssertEqual(baseCount + 3, ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "searched")), "A searched camp adds dug soil");
        AssertEqual(baseCount + 3, ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "investigated")), "An investigated camp keeps those traces");
        AssertEqual(baseCount + 4, ShownCount(ResolveCamp(resolver, camp, "none", "abandoned", "documented")), "A documented camp adds the field board");
        AssertEqual(baseCount + 5, ShownCount(ResolveCamp(resolver, camp, "unsafe", "abandoned", "documented")), "Channels combine without extra cases (sign plus the documented traces)");
    }

    private static void CampUnknownConditionShowsOnlyNeutralParts()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var unknown = ResolveCamp(resolver, Camp(game), null, null, null);

        AssertEqual(8, ShownCount(unknown), "An unobserved camp shows tents, logs and the flag pole only");
        AssertTrue(unknown.ShownElements.All(element => element.Kind == VisualElementKind.Part), "No fire, light or smoke without an observation");
        AssertEqual(1f, unknown.Elements.Single(element => element.Id == "tent-2").Pose.Scale.Y, "Unknown condition shows standing tents, not a guess");
    }

    private static void CampFireIsALightAndSmokeOnlyWhenLived()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var lived = ResolveCamp(resolver, Camp(game), "none", "recently-used", "untouched");
        var light = lived.Elements.Single(element => element.Id == "fire-light");
        var smoke = lived.Elements.Single(element => element.Id == "fire-smoke");

        AssertEqual(VisualElementKind.Light, light.Kind, "The fire light is a light element");
        AssertEqual("#FFA040", light.ColorHex, "The fire light is warm");
        AssertEqual(1.2f, light.Intensity, "The fire light strength comes from the data");
        AssertEqual(6f, light.Range, "The fire light range comes from the data");
        AssertEqual(VisualElementKind.Smoke, smoke.Kind, "The fire smoke is a smoke element");
        AssertTrue(light.Shown && smoke.Shown, "A lived camp has light and smoke");

        var abandoned = ResolveCamp(resolver, Camp(game), "none", "abandoned", "untouched");
        AssertTrue(!abandoned.Elements.Single(element => element.Id == "fire-light").Shown && !abandoned.Elements.Single(element => element.Id == "fire-smoke").Shown, "A cold camp has neither");
    }

    private static LocationVisualDescription ResolveGrave(LocationVisualResolver resolver, GameState game, string? operational, string? presence, string? interaction)
    {
        var grave = game.World.Locations.First(location => location.Id == "marked-grave");
        return resolver.Resolve(grave, new LocationVisualContext(VisualKnowledge.Confirmed, operational, presence, interaction));
    }

    private static void GraveShowsSealedOpenAndDisturbedForms()
    {
        var visuals = Catalog().VisualDefinitions;
        var composition = visuals.FindComposition("investigation-site", "marked-grave");
        AssertTrue(composition != null, "The marked grave has a composition");
        AssertEqual("vis-marked-grave", composition!.Id, "Grave composition id");
        AssertEqual("point", composition.AnchorMode, "A grave sits on a single hex");

        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(visuals);
        AssertEqual(10, ShownCount(ResolveGrave(resolver, game, "sealed", "empty", "untouched")), "Sealed grave: mound, entrance, stones, cross and two seal bars");
        AssertEqual(12, ShownCount(ResolveGrave(resolver, game, "open", "empty", "untouched")), "Open grave: dark opening, shovel, dirt and green glow instead of the seal");
        AssertEqual(10, ShownCount(ResolveGrave(resolver, game, "disturbed", "empty", "untouched")), "Disturbed grave: opening and dirt, no seal");

        var sealedSlab = ResolveGrave(resolver, game, "sealed", "empty", "untouched").Elements.Single(element => element.Id == "slab");
        var openSlab = ResolveGrave(resolver, game, "open", "empty", "untouched").Elements.Single(element => element.Id == "slab");
        AssertTrue(openSlab.Pose.Position.X > sealedSlab.Pose.Position.X, "The slab is pushed aside when the grave is open");

        var light = ResolveGrave(resolver, game, "open", "empty", "untouched").Elements.Single(element => element.Id == "tomb-glow");
        AssertEqual(VisualElementKind.Light, light.Kind, "The tomb glow is a light");
        AssertTrue(light.Shown, "An open grave glows");
        AssertTrue(!ResolveGrave(resolver, game, "sealed", "empty", "untouched").Elements.Single(element => element.Id == "tomb-glow").Shown, "A sealed grave does not glow");
    }

    private static void GraveCombinesPresenceAndInteraction()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var baseCount = ShownCount(ResolveGrave(resolver, game, "sealed", "empty", "untouched"));

        AssertEqual(baseCount + 1, ShownCount(ResolveGrave(resolver, game, "sealed", "watched", "untouched")), "A watched grave shows a watcher");
        AssertEqual(baseCount + 2, ShownCount(ResolveGrave(resolver, game, "sealed", "abandoned", "untouched")), "An abandoned grave is overgrown");
        AssertEqual(baseCount + 1, ShownCount(ResolveGrave(resolver, game, "sealed", "empty", "inspected")), "An inspected grave carries the expedition stake");
        AssertEqual(baseCount + 3, ShownCount(ResolveGrave(resolver, game, "sealed", "empty", "searched")), "A searched grave adds dug soil");
        AssertEqual(baseCount + 4, ShownCount(ResolveGrave(resolver, game, "sealed", "empty", "documented")), "A documented grave adds the field board");
    }

    private static void GraveUnknownConditionShowsOnlyNeutralParts()
    {
        var game = TutorialGameFactory.Create();
        var resolver = new LocationVisualResolver(Catalog().VisualDefinitions);
        var unknown = ResolveGrave(resolver, game, null, null, null);

        AssertEqual(8, ShownCount(unknown), "An unobserved grave shows the mound, entrance frame, stones and cross only");
        AssertTrue(unknown.ShownElements.All(element => element.Kind == VisualElementKind.Part), "No glow without an observation");
    }

    private static void PlainStatesCountAsCoveredAndAreValidated()
    {
        var catalog = Catalog();
        string Camp(string plainInteraction, string plainOperational = "\"none\"")
        {
            return $$"""
            {
              "documentType": "visual-definitions", "schemaVersion": 1,
              "markers": [],
              "parts": [ { "id": "block", "primitives": [ { "shape": "box", "size": [1, 1, 1], "color": "#808080" } ] } ],
              "compositions": [
                { "id": "test", "match": { "archetypeId": "investigation-site", "variantId": "abandoned-camp" },
                  "plain": { "interaction": [{{plainInteraction}}], "operational": [{{plainOperational}}] },
                  "elements": [
                    { "id": "a", "part": "block", "shows": { "channel": "interaction", "in": ["inspected"] } },
                    { "id": "b", "part": "block", "shows": { "channel": "operational", "in": ["cleared", "unsafe"] } }
                  ] }
              ]
            }
            """;
        }

        var valid = VisualDefinitionLoader.LoadFromJson(new[] { Camp("\"untouched\", \"searched\", \"investigated\", \"documented\"") });
        VisualDefinitionValidator.Validate(valid, catalog.Locations, catalog.Authoring);

        ExpectFailure(Camp("\"untouched\", \"searched\", \"investigated\""), catalog, "does not cover", "A state that is neither shown nor plain is reported");
        ExpectFailure(Camp("\"untouched\", \"searched\", \"investigated\", \"documented\", \"flying\""), catalog, "does not define", "A plain state the profile lacks is rejected");

        try
        {
            VisualDefinitionLoader.LoadFromJson(new[] { Camp("\"untouched\"").Replace("\"plain\": {", "\"unused\": {").Replace("{ \"id\": \"a\", \"part\": \"block\",", "{ \"id\": \"a\", \"kind\": \"light\",") });
            throw new InvalidOperationException("A light without a color should be rejected.");
        }
        catch (LocationDataException exception)
        {
            AssertTrue(exception.Message.Contains("color"), "A light needs a color (" + exception.Message + ")");
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
