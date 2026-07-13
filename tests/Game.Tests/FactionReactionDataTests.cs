#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class FactionReactionDataTests
{
    public void RunAll()
    {
        GeneratedSacredRelationCanReactWithoutRevealingFactionIdentity();
        FactionProfilesAreLoadedAndUsedByGeneratedCampaigns();
        SemanticActionTagsGateReactions();
        NeutralLocationDoesNotCreateAFactionReaction();
        AttentionCanGateAHostileReaction();
        GameApplicationUsesJsonForTerritoryEntry();
    }

    private static void FactionProfilesAreLoadedAndUsedByGeneratedCampaigns()
    {
        var content = LoadContent();
        AssertTrue(content.FactionProfiles.Find("welcoming") != null, "Welcoming faction profile loads from JSON");
        AssertTrue(content.FactionProfiles.Find("hostile")!.Values.Contains("territory"), "Faction profile exposes authored values");
        var cautious = content.FactionProfiles.Find("neutral-cautious")!;
        AssertTrue(cautious.TabooActionTags.Contains("map"), "Faction profile exposes authored action taboos");
        AssertTrue(cautious.TerritorialPolicy.RestrictedActionTags.Contains("cross"), "Faction profile exposes territorial conduct rules");
        AssertEqual("guarded", cautious.ContactStyle, "Faction profile exposes its contact style");
        AssertEqual("border-commander", cautious.LeadershipStyle, "Faction profile exposes its leadership style");

        var game = new GameApplication(null, content).CreateGeneratedGame(new WorldGenerationRequest
        {
            Seed = 20260713,
            Width = 30,
            Height = 20,
            FactionCount = 3
        });
        foreach (var faction in game.Factions)
        {
            AssertTrue(content.FactionProfiles.Find(faction.ReactionProfileId) != null, "Generated faction profile is defined by JSON content");
        }
    }

    private static void GeneratedSacredRelationCanReactWithoutRevealingFactionIdentity()
    {
        var game = CreateGame(new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Sacred));
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "location-grave-disturbed", 1, new[] { "disturb" }, sourceLocationId: "location-1"));

        new WorldPhaseService(LoadContent()).Resolve(game);

        AssertEqual(8, game.FindFaction("faction-1")!.Anger, "Sacred relation applies its JSON anger delta");
        AssertEqual(1, game.Events.PendingCount, "Reaction is surfaced through the event queue");
        AssertEqual(null, game.Events.Current!.FactionId, "Unknown reaction does not reveal faction identity");
    }

    private static void SemanticActionTagsGateReactions()
    {
        var game = CreateGame(new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Sacred));
        var resolver = new WorldPhaseService(LoadContent());

        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-no-tag", "location-grave-disturbed", 1, sourceLocationId: "location-1"));
        resolver.Resolve(game);
        AssertEqual(0, game.FindFaction("faction-1")!.Anger, "A trigger without the required action tag does not react");

        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-disturb", "location-grave-disturbed", 1, new[] { "disturb" }, sourceLocationId: "location-1"));
        resolver.Resolve(game);
        AssertEqual(8, game.FindFaction("faction-1")!.Anger, "The same trigger reacts once its semantic action tag is present");
    }

    private static void NeutralLocationDoesNotCreateAFactionReaction()
    {
        var game = CreateGame();
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "location-grave-disturbed", 1, sourceLocationId: "location-1"));

        new WorldPhaseService(LoadContent()).Resolve(game);

        AssertEqual(0, game.FindFaction("faction-1")!.Anger, "Neutral location leaves faction metrics unchanged");
        AssertEqual(0, game.Events.PendingCount, "Neutral location queues no faction reaction");
    }

    private static void AttentionCanGateAHostileReaction()
    {
        var game = CreateGame(new LocationFactionRelationState("faction-1", LocationFactionRelationKind.Claimed), reactionProfileId: "hostile");
        var resolver = new WorldPhaseService(LoadContent());
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-1", "location-infrastructure-repaired", 1, new[] { "repair" }, sourceLocationId: "location-1"));
        resolver.Resolve(game);

        AssertEqual(0, game.FindFaction("faction-1")!.Anger, "Hostile rule waits until the faction becomes suspicious");

        game.World.EscalateFactionAwareness("faction-1", "location-region:location-1");
        game.World.QueueWorldTrigger(new WorldTriggerState("trigger-2", "location-infrastructure-repaired", 1, new[] { "repair" }, sourceLocationId: "location-1"));
        resolver.Resolve(game);

        AssertEqual(7, game.FindFaction("faction-1")!.Anger, "Suspicious awareness unlocks the hostile JSON reaction");
    }

    private static void GameApplicationUsesJsonForTerritoryEntry()
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(4, 3), TerrainType.Forest);
        var origin = new HexCoord(1, 1);
        var destination = new HexCoord(2, 1);
        map.SetTile(map.GetTile(destination).WithOwner("faction-1"));
        var expedition = new ExpeditionState(1, origin, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }, movementPoints: 4);
        var game = new GameState(
            new WorldState(map),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero),
            factions: new[] { new FactionState("faction-1", "Unknown Keepers", reactionProfileId: "hostile") });

        var result = new GameApplication(null, LoadContent()).MoveExpedition(game, destination);

        AssertTrue(result.Success, "Territory entry succeeds through application facade");
        AssertEqual(4, game.FindFaction("faction-1")!.Anger, "Hostile entry delta is read from JSON");
        AssertTrue(game.Events.Current!.Body.Contains("zuerst gesehen", StringComparison.Ordinal), "JSON event body is used for territory entry");
        AssertEqual(null, game.Events.Current!.FactionId, "Unidentified territory reaction does not reveal faction identity");
    }

    private static CrossSystemDataBundle LoadContent()
    {
        var gameData = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return CrossSystemDataLoader.LoadFromDirectories(new[] { Path.Combine(gameData, "World"), Path.Combine(gameData, "Factions") })
            ?? throw new InvalidOperationException("Cross-system content was not loaded.");
    }

    private static GameState CreateGame(LocationFactionRelationState? relation = null, string reactionProfileId = "neutral-cautious")
    {
        var relations = relation == null ? null : new[] { relation };
        var location = new SpecialLocationState(
            "location-1",
            LocationKind.Ruin,
            HexCoord.Zero,
            "Old Site",
            LocationAnchor.Point(HexCoord.Zero),
            archetypeId: "investigation-site",
            variantId: "old-site",
            factionRelations: relations);
        return new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland), locations: new[] { location }),
            new KnowledgeState(),
            new PlayerNotesState(),
            new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }),
            new BaseState(HexCoord.Zero),
            factions: new[] { new FactionState("faction-1", "Unknown Keepers", reactionProfileId: reactionProfileId) });
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
