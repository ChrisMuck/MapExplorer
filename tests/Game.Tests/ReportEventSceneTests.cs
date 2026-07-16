#nullable enable
using System;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ReportEventSceneTests
{
    public void RunAll()
    {
        UrgentEventAddsLocalizedContextWithoutReplacingDeliveredText();
        RoutineEventDoesNotReceiveUrgentWording();
    }

    private static void UrgentEventAddsLocalizedContextWithoutReplacingDeliveredText()
    {
        var (app, game) = Create();
        var deliveredBody = "Ein Späher hat den vereinbarten Rückkehrtag verpasst.";
        game.Events.Enqueue(new EventState("event-overdue", EventKind.ScoutOverdue, "Späher überfällig",
            "Expedition", deliveredBody,
            new[] { new EventOptionState("wait", "Warten", "Die Expedition wartet.", EventOptionEffectKind.None) }));

        var scene = app.GetCurrentEventScenePresentation(game)!;
        var fragmentIds = scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).ToArray();

        AssertTrue(scene.Message.Contains(deliveredBody, StringComparison.Ordinal),
            "Shared event scene preserves the already delivered player-facing text");
        AssertTrue(fragmentIds.Contains("frag-report-event-urgent", StringComparer.Ordinal),
            "JSON event-kind tag selects urgent context");
        AssertTrue(scene.Question == null, "Existing event options resolve the generic scene question");
    }

    private static void RoutineEventDoesNotReceiveUrgentWording()
    {
        var (app, game) = Create();
        game.Events.Enqueue(new EventState("event-location", EventKind.LocationDiscovery, "Entdeckung",
            "Expedition", "Ein sichtbarer Ort wurde vermerkt.",
            new[] { new EventOptionState("note", "Notieren", "Der Ort wird notiert.", EventOptionEffectKind.None) }));

        var scene = app.GetCurrentEventScenePresentation(game)!;
        var fragmentIds = scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).ToArray();

        AssertFalse(fragmentIds.Contains("frag-report-event-urgent", StringComparer.Ordinal),
            "Routine event is not made urgent by application code");
        AssertTrue(scene.Message.Contains("Ein sichtbarer Ort wurde vermerkt.", StringComparison.Ordinal),
            "Routine delivery remains visible through the shared scene contract");
    }

    private static (GameApplication App, GameState Game) Create()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(),
            "UnityHexMapView", "Assets", "StreamingAssets", "GameData"))!;
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(1, new HexCoord(1, 1),
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) });
        return (new GameApplication(catalog), new GameState(new WorldState(map), new KnowledgeState(),
            new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero)));
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
