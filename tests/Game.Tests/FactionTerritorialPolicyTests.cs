#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;
using Game.Core;

internal sealed class FactionTerritorialPolicyTests
{
    public void RunAll()
    {
        RestrictedActionAtWatchedLocationUsesTheGeneratedFactionProfile();
        ClaimedButUnobservedLocationDoesNotReactToGenericPolicy();
    }

    private static void RestrictedActionAtWatchedLocationUsesTheGeneratedFactionProfile()
    {
        var (app, content, game) = CreateDocumentingGame(LocationFactionRelationKind.Watched);

        var result = app.ResolveLocationAction(game, "location-1", "action-document", LocationOutcomeTier.Success);

        AssertTrue(result.Success, "Documenting action resolves");
        AssertEqual(1, game.World.WorldTriggers.Count, "An action without a bespoke world effect raises the generic trigger");
        AssertEqual(FactionTerritorialPolicyResolver.LocationActionCompletedTriggerId, game.World.WorldTriggers[0].TriggerId, "Generic trigger identifies the completed location action");

        new WorldPhaseService(content).Resolve(game);

        var faction = game.FindFaction("faction-1")!;
        AssertEqual(5, faction.Anger, "Restricted map action uses the cautious profile's JSON anger response");
        AssertEqual(2, faction.Fear, "Restricted map action uses the cautious profile's JSON fear response");
        AssertEqual(FactionAwarenessLevel.Suspicious, game.World.FactionAwareness[0].Level, "JSON policy response raises regional awareness");
        AssertEqual(null, game.Events.Current!.FactionId, "Policy reaction does not reveal an unknown faction");
    }

    private static void ClaimedButUnobservedLocationDoesNotReactToGenericPolicy()
    {
        var (app, content, game) = CreateDocumentingGame(LocationFactionRelationKind.Claimed);

        app.ResolveLocationAction(game, "location-1", "action-document", LocationOutcomeTier.Success);
        new WorldPhaseService(content).Resolve(game);

        AssertEqual(0, game.FindFaction("faction-1")!.Anger, "A merely claimed location needs earned or generated observation before generic policy reacts");
        AssertEqual(0, game.Events.PendingCount, "Unobserved policy breach creates no player-facing reaction");
    }

    private static (GameApplication App, CrossSystemDataBundle Content, GameState Game) CreateDocumentingGame(LocationFactionRelationKind relationKind)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var locations = LocationDataLoader.LoadFromDirectory(Path.Combine(root, "Locations"))
            ?? throw new InvalidOperationException("Location content was not loaded.");
        var content = CrossSystemDataLoader.LoadFromDirectories(new[]
        {
            Path.Combine(root, "World"),
            Path.Combine(root, "Factions"),
            Path.Combine(root, "Scouting")
        }) ?? throw new InvalidOperationException("Cross-system content was not loaded.");

        var location = new SpecialLocationState(
            "location-1",
            LocationKind.Ruin,
            HexCoord.Zero,
            "Old Site",
            LocationAnchor.Point(HexCoord.Zero),
            archetypeId: "investigation-site",
            variantId: null,
            factionRelations: new[] { new LocationFactionRelationState("faction-1", relationKind) });
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(HexCoord.Zero, KnowledgeLevel.Confirmed);
        var game = new GameState(
            new WorldState(HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland), locations: new[] { location }),
            knowledge,
            new PlayerNotesState(),
            new ExpeditionState(1, HexCoord.Zero, new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) }),
            new BaseState(HexCoord.Zero),
            factions: new[] { new FactionState("faction-1", "Unknown Watchers", reactionProfileId: "neutral-cautious") });

        return (new GameApplication(locations, content), content, game);
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
