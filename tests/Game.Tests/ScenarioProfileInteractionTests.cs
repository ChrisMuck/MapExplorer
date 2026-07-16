#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ScenarioProfileInteractionTests
{
    public void RunAll()
    {
        ScenarioProfileReplacesLegacyVariantActionBase();
        GeneratedContextDoesNotUnlockAnActionUntilKnowledgeConfirmsIt();
        DifferentScenarioArchetypesUseTheSameProfilePipeline();
        UnknownLocationRiskRemainsAnEstimate();
        PlayerFacingPresentationUsesLastKnownConditionInsteadOfWorldTruth();
    }

    private static void ScenarioProfileReplacesLegacyVariantActionBase()
    {
        var (app, game, location) = CreateBridgeGame();
        var interaction = app.GetLocationInteraction(game, location.Id);

        AssertTrue(interaction.Success && interaction.Interaction != null, "Profile-driven bridge interaction is available");
        var actionIds = interaction.Interaction!.Options.Select(option => option.Action.Id).ToList();
        AssertTrue(actionIds.Contains("action-assess-crossing"), "Scenario profile supplies shared action");
        AssertTrue(actionIds.Contains("action-find-bypass"), "Scenario profile supplies bounded initial additional action");
        AssertTrue(actionIds.Contains("action-rebuild-bridge"), "Generic active modifier can still add an action");
        AssertTrue(actionIds.Contains(LocationInteractionContent.ActionAttemptCrossing), "Scenario profile explicitly keeps the obvious but risky crossing attempt");
    }

    private static void GeneratedContextDoesNotUnlockAnActionUntilKnowledgeConfirmsIt()
    {
        var (app, game, location) = CreateBridgeGame(contextTags: new[] { "structural-failure" });

        var beforeKnowledge = app.GetLocationInteraction(game, location.Id).Interaction!.Options.Select(option => option.Action.Id).ToList();
        AssertTrue(!beforeKnowledge.Contains("action-construct-temporary-passage"), "Objective generated context alone does not reveal an action");

        game.Knowledge.LearnLocationContextTag(location.Id, "structural-failure");
        var afterKnowledge = app.GetLocationInteraction(game, location.Id).Interaction!.Options.Select(option => option.Action.Id).ToList();
        AssertTrue(afterKnowledge.Contains("action-construct-temporary-passage"), "Confirmed context knowledge unlocks the profile action");
    }

    private static void DifferentScenarioArchetypesUseTheSameProfilePipeline()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var expectedActions = new[]
        {
            ("broken-ravine", "action-assess-crossing"),
            ("marked-grave", "action-inspect"),
            ("sealed-gate", "action-inspect-seal")
        };

        foreach (var (locationId, expectedActionId) in expectedActions)
        {
            var location = game.World.Locations.Single(candidate => candidate.Id == locationId);
            new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, location.Coord);
            var interaction = app.GetLocationInteraction(game, location.Id).Interaction;

            AssertTrue(interaction != null, $"Profile interaction is created for '{locationId}'");
            AssertTrue(interaction!.FindOption(expectedActionId) != null,
                $"Scenario profile supplies '{expectedActionId}' for '{locationId}' without a bespoke resolver");
        }
    }

    private static void UnknownLocationRiskRemainsAnEstimate()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var gate = game.World.Locations.Single(candidate => candidate.Id == "sealed-gate");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, gate.Coord);

        var option = app.GetLocationInteraction(game, gate.Id).Interaction!.FindOption("action-open-seal");

        AssertTrue(option != null, "The obvious seal-opening action is visible");
        AssertEqual(LocationEstimateConfidence.Guess, option!.Confidence,
            "Unconfirmed danger is represented as an estimate instead of exposing hidden world context");
    }

    private static void PlayerFacingPresentationUsesLastKnownConditionInsteadOfWorldTruth()
    {
        var (app, game, location) = CreateBridgeGame(contextTags: new[] { "structural-failure" });
        game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
        game.Knowledge.LearnLocationContextTag(location.Id, "structural-failure");
        location.SetState(LocationStateChannels.Operational, "repaired");

        var result = app.GetLocationInteraction(game, location.Id);

        AssertTrue(result.Success && result.Presentation != null, "Shared interaction query includes a player-facing presentation projection");
        AssertTrue(result.Presentation!.Scene?.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds)
                .Contains("frag-loc-route-op-blocked") == true,
            "Presentation selects the localized scene fragment for the last observed blocked state");
        AssertTrue(!result.Presentation.Description.Contains("wieder passierbar", StringComparison.Ordinal),
            "Unobserved repaired WorldState does not leak into player-facing wording");
        AssertEqual("structural-failure", result.Presentation.KnownContextTags.Single(),
            "Presentation exposes earned context rather than objective modifiers");
    }

    private static (GameApplication App, GameState Game, SpecialLocationState Location) CreateBridgeGame(IEnumerable<string>? contextTags = null)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var catalog = GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data catalog was not loaded.");
        var coord = new HexCoord(1, 1);
        var location = new SpecialLocationState(
            "bridge-profile-test",
            LocationKind.BrokenRavine,
            coord,
            "Broken Crossing",
            LocationAnchor.Edge(coord, new HexCoord(2, 1)),
            "route-obstacle",
            "broken-bridge",
            modifierIds: new[] { "modifier-repairable" },
            contentProfileId: "content-old-trade-road-bridge",
            operationalStateId: "blocked",
            contextTags: contextTags);
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(coord, KnowledgeLevel.Confirmed);
        var expedition = new ExpeditionState(1, coord, new[]
        {
            new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("engineer-1", "Iven", ExpeditionMemberRole.Engineer)
        }, supplies: 8);
        var game = new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(4, 4), TerrainType.Grassland), locations: new[] { location }),
            knowledge,
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero));
        return (new GameApplication(catalog), game, location);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
}
