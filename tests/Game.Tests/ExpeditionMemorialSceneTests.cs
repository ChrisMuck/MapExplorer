#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ExpeditionMemorialSceneTests
{
    public void RunAll()
    {
        EveryStoredStatusUsesItsAuthoredFragment();
        MissingRecordDoesNotInventDeathOrResolution();
    }

    private static void EveryStoredStatusUsesItsAuthoredFragment()
    {
        var expected = new Dictionary<LostExpeditionStatus, string>
        {
            [LostExpeditionStatus.Missing] = "frag-memorial-missing",
            [LostExpeditionStatus.PresumedLost] = "frag-memorial-presumed-lost",
            [LostExpeditionStatus.PartiallyRecovered] = "frag-memorial-partially-recovered",
            [LostExpeditionStatus.SurvivorFound] = "frag-memorial-survivor-found",
            [LostExpeditionStatus.RecordsRecovered] = "frag-memorial-records-recovered",
            [LostExpeditionStatus.FullyResolved] = "frag-memorial-fully-resolved"
        };

        foreach (var pair in expected)
        {
            var (app, game, record) = Create(pair.Key);
            var scene = app.GetExpeditionMemorialPresentation(game, record.ExpeditionId)!;
            var ids = scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).ToArray();
            AssertTrue(ids.Contains(pair.Value, StringComparer.Ordinal),
                $"Stored memorial status {pair.Key} selects its JSON fragment");
        }
    }

    private static void MissingRecordDoesNotInventDeathOrResolution()
    {
        var (app, game, record) = Create(LostExpeditionStatus.Missing);

        var scene = app.GetExpeditionMemorialPresentation(game, record.ExpeditionId)!;

        AssertTrue(scene.Message.Contains("Schicksal ist offen", StringComparison.Ordinal),
            "Missing memorial preserves uncertainty");
        AssertFalse(scene.Message.Contains("tot", StringComparison.OrdinalIgnoreCase),
            "Missing memorial does not invent death");
        AssertFalse(scene.Message.Contains("abgeschlossen", StringComparison.OrdinalIgnoreCase),
            "Missing memorial does not invent resolution");
    }

    private static (GameApplication App, GameState Game, LostExpeditionRecord Record) Create(LostExpeditionStatus status)
    {
        var catalog = GameDataCatalog.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(),
            "UnityHexMapView", "Assets", "StreamingAssets", "GameData"))!;
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(2, HexCoord.Zero,
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) });
        var record = new LostExpeditionRecord("expedition-1", 1, 4, new HexCoord(1, 1), 8, status);
        var baseState = new BaseState(HexCoord.Zero);
        baseState.AddLostExpeditionRecord(record);
        var game = new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition, baseState);
        return (new GameApplication(catalog), game, record);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
