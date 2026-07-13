#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ScoutLocationSurroundingsTests
{
    public void RunAll()
    {
        ScoutingLocationSurroundingsCreatesNeutralEvidenceAndRegionalAwareness();
        ScoutingRequiresAnAvailableScoutAtTheLocation();
        JsonEvidenceDefinitionDrivesScoutReportText();
    }

    private static void ScoutingLocationSurroundingsCreatesNeutralEvidenceAndRegionalAwareness()
    {
        var game = CreateGame(new HexCoord(1, 1));
        var app = new GameApplication();

        var sent = app.ScoutLocationSurroundings(game, "location-1", new[] { "scout-1" });
        var day = new EndDayCommand(suppliesPerDay: 0).Execute(game);

        AssertTrue(sent.Success, "Local surroundings scout mission is sent");
        AssertEqual("location-1", sent.Mission!.TargetLocationId, "Mission retains its target location");
        AssertTrue(day.Success, "End day resolves local scout mission");
        AssertEqual(1, game.Knowledge.Evidence.Count, "Scouting creates one evidence item");
        AssertEqual("evidence-location-surroundings-signs", game.Knowledge.Evidence[0].DefinitionId, "Evidence reports signs without naming a faction");
        AssertTrue(!game.Knowledge.Evidence[0].PlayerText.Contains("wardens", StringComparison.OrdinalIgnoreCase), "Evidence does not reveal objective faction identity");
        AssertEqual(1, game.World.FactionAwareness.Count, "Faction awareness is updated in WorldState");
        AssertEqual(FactionAwarenessLevel.Suspicious, game.World.FactionAwareness[0].Level, "Investigating watched surroundings makes the faction suspicious");
    }

    private static void ScoutingRequiresAnAvailableScoutAtTheLocation()
    {
        var game = CreateGame(new HexCoord(4, 4));
        var result = new ScoutLocationSurroundingsCommand().Execute(game, "location-1", new[] { "scout-1" });

        AssertFalse(result.Success, "Surroundings scouting rejects a distant location");
        AssertEqual(0, game.Expedition.ScoutMissions.Count, "Rejected scout mission does not reserve a scout");
    }

    private static void JsonEvidenceDefinitionDrivesScoutReportText()
    {
        var dataRoot = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var data = CrossSystemDataLoader.LoadFromDirectories(new[]
        {
            Path.Combine(dataRoot, "World"),
            Path.Combine(dataRoot, "Factions")
        });
        var game = CreateGame(new HexCoord(1, 1), "evidence-patrol-signs");
        var app = new GameApplication(null, data);

        app.ScoutLocationSurroundings(game, "location-1", new[] { "scout-1" });
        app.EndDay(game);

        AssertTrue(data != null, "World evidence JSON is loaded");
        AssertTrue(data!.Evidence.Find("evidence-patrol-signs") != null, "Patrol evidence definition is available by stable id");
        AssertTrue(game.Knowledge.Evidence[0].PlayerText.Contains("Trittspuren", StringComparison.Ordinal), "Scout report text comes from evidence JSON");
        AssertEqual("signature-woven-offering-bands", game.Knowledge.Evidence[0].SymbolId, "Scout evidence uses the generated faction's signature profile");
        AssertTrue(data.FactionSignatures.Find(game.Knowledge.Evidence[0].SymbolId) != null, "Signature metadata is loaded without exposing a faction");
    }

    private static GameState CreateGame(HexCoord expeditionPosition, params string[] evidenceSeedIds)
    {
        var map = HexMapState.CreateFilled(new HexMapBounds(5, 5), TerrainType.Grassland);
        var location = new SpecialLocationState(
            "location-1",
            LocationKind.Ruin,
            new HexCoord(1, 1),
            "Old Marker",
            LocationAnchor.Point(new HexCoord(1, 1)),
            archetypeId: "ruin",
            variantId: "marker",
            factionRelations: new[]
            {
                new LocationFactionRelationState("border-wardens", LocationFactionRelationKind.Watched)
            },
            evidenceSeedIds: evidenceSeedIds);
        var expedition = new ExpeditionState(
            1,
            expeditionPosition,
            new[] { new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout) },
            supplies: 5);

        return new GameState(
            new WorldState(map, locations: new[] { location }),
            new KnowledgeState(),
            new PlayerNotesState(),
            expedition,
            new BaseState(HexCoord.Zero),
            factions: new[]
            {
                new FactionState(
                    "border-wardens",
                    "Unknown Watchers",
                    signatureProfileId: "woven-offerings")
            });
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

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"{message}: expected false.");
        }
    }
}
