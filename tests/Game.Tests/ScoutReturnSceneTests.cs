#nullable enable
using System;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class ScoutReturnSceneTests
{
    public void RunAll()
    {
        OnTimeReturnUsesDeliveredReportAndCondition();
        LaterMemberMutationDoesNotRewriteReturnScene();
        SingleInjuredScoutDoesNotInventASupportingCompanion();
        OverdueAndMissingScenesDoNotInventAReport();
        PartialReturnNamesTheAbsentCompanionWithoutInventingACause();
        DeliveredReportResolvesItsReturnScene();
    }

    private static void OnTimeReturnUsesDeliveredReportAndCondition()
    {
        var (app, game) = Create();
        game.Knowledge.AddScoutReport(new ScoutReportState("report-1", "mission-1", "Bericht aus dem Osten",
            "Gelieferter Bericht.", 82, Array.Empty<HexCoord>(), Array.Empty<string>(),
            new[] { new ScoutLeadState(ScoutLeadKind.RouteHint, ScoutLeadScope.Directional, ScoutDirection.East, 82, "Eine Route wurde gemeldet.") }));
        var outcome = new DeliveredMissionOutcomeState("delivery-1", "mission-1", ScoutMissionStatus.Returned,
            4, 4, 4, false,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", true, DeliveredScoutStatus.Unhurt) },
            deliveredReportIds: new[] { "report-1" });
        game.Knowledge.RecordDeliveredMissionOutcome(outcome);

        var scene = app.GetScoutReturnPresentation(game, outcome.DeliveryId)!;
        var ids = FragmentIds(scene);

        AssertTrue(ids.Contains("frag-scout-return-on-time"), "On-time return fragment is selected");
        AssertTrue(ids.Contains("frag-scout-report-confident"), "Delivered report reliability selects confident wording");
        AssertTrue(ids.Contains("frag-member-unhurt"), "Delivered participant condition selects unhurt wording");
        AssertTrue(scene.Message.Contains("Mira", StringComparison.Ordinal), "Stable member identity resolves the displayed name");
    }

    private static void LaterMemberMutationDoesNotRewriteReturnScene()
    {
        var (app, game) = Create();
        var outcome = new DeliveredMissionOutcomeState("delivery-history", "mission-history", ScoutMissionStatus.Returned,
            3, 3, 3, false,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", true, DeliveredScoutStatus.Unhurt) });
        game.Knowledge.RecordDeliveredMissionOutcome(outcome);
        game.Expedition.FindMember("scout-a")!.SetStatus(ExpeditionMemberStatus.Injured);

        var scene = app.GetScoutReturnPresentation(game, outcome.DeliveryId)!;

        AssertTrue(FragmentIds(scene).Contains("frag-member-unhurt"), "Historical scene uses delivered condition after later injury");
        AssertFalse(FragmentIds(scene).Contains("frag-member-injured"), "Current mutable member condition cannot rewrite history");
    }

    private static void SingleInjuredScoutDoesNotInventASupportingCompanion()
    {
        var (app, game) = Create();
        var outcome = new DeliveredMissionOutcomeState("delivery-injured", "mission-injured", ScoutMissionStatus.ReturnedInjured,
            3, 3, 3, false,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", true, DeliveredScoutStatus.Injured) });
        game.Knowledge.RecordDeliveredMissionOutcome(outcome);

        var scene = app.GetScoutReturnPresentation(game, outcome.DeliveryId)!;

        AssertTrue(FragmentIds(scene).Contains("frag-member-injured"), "Single injured scout receives injury condition wording");
        AssertFalse(FragmentIds(scene).Contains("frag-member-supported"), "No supporting companion is invented for a solo mission");
    }

    private static void OverdueAndMissingScenesDoNotInventAReport()
    {
        var (app, game) = Create();
        var overdue = new DeliveredMissionOutcomeState("delivery-overdue", "mission-overdue", ScoutMissionStatus.Overdue,
            5, 5, null, true,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", false, DeliveredScoutStatus.Overdue) });
        var missing = new DeliveredMissionOutcomeState("delivery-missing", "mission-missing", ScoutMissionStatus.Missing,
            5, 7, null, false,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", false, DeliveredScoutStatus.Missing) });
        game.Knowledge.RecordDeliveredMissionOutcome(overdue);
        game.Knowledge.RecordDeliveredMissionOutcome(missing);

        var overdueScene = app.GetScoutReturnPresentation(game, overdue.DeliveryId)!;
        var missingScene = app.GetScoutReturnPresentation(game, missing.DeliveryId)!;

        AssertTrue(FragmentIds(overdueScene).Contains("frag-scout-overdue-notice"), "Overdue notice has its own scene");
        AssertTrue(FragmentIds(missingScene).Contains("frag-scout-missing-notice"), "Missing notice has its own scene");
        AssertFalse(overdueScene.Message.Contains("Bericht kommt", StringComparison.Ordinal), "Overdue scene invents no report delivery");
        AssertTrue(overdueScene.Question != null && missingScene.Question != null, "Unresolved absence retains the authored question");
    }

    private static void PartialReturnNamesTheAbsentCompanionWithoutInventingACause()
    {
        var (app, game) = Create();
        var outcome = new DeliveredMissionOutcomeState("delivery-partial", "mission-partial", ScoutMissionStatus.Returned,
            6, 7, 7, true,
            new[]
            {
                new DeliveredMissionParticipantOutcome("scout-a", true, DeliveredScoutStatus.Unhurt),
                new DeliveredMissionParticipantOutcome("scout-b", false, DeliveredScoutStatus.Missing)
            });
        game.Knowledge.RecordDeliveredMissionOutcome(outcome);

        var scene = app.GetScoutReturnPresentation(game, outcome.DeliveryId)!;

        AssertTrue(FragmentIds(scene).Contains("frag-scout-return-team-split"), "Partial team return selects split-team fragment");
        AssertTrue(scene.Message.Contains("Mira", StringComparison.Ordinal) && scene.Message.Contains("Tovin", StringComparison.Ordinal),
            "Scene names returned and absent participants");
        AssertTrue(scene.Message.Contains("Niemand behauptet zu wissen", StringComparison.Ordinal),
            "Scene explicitly avoids inventing the absent scout's fate");
    }

    private static void DeliveredReportResolvesItsReturnScene()
    {
        var (app, game) = Create();
        game.Knowledge.AddScoutReport(new ScoutReportState("report-linked", "mission-linked", "Bericht",
            "Inhalt", 70, Array.Empty<HexCoord>(), Array.Empty<string>()));
        game.Knowledge.RecordDeliveredMissionOutcome(new DeliveredMissionOutcomeState("delivery-linked", "mission-linked",
            ScoutMissionStatus.Returned, 3, 3, 3, false,
            new[] { new DeliveredMissionParticipantOutcome("scout-a", true, DeliveredScoutStatus.Unhurt) },
            deliveredReportIds: new[] { "report-linked" }));

        var scene = app.GetScoutReturnPresentationForReport(game, "report-linked");

        AssertTrue(scene != null && scene.Message.Contains("Mira", StringComparison.Ordinal),
            "A delivered report resolves the matching return scene");
        AssertTrue(app.GetScoutReturnPresentationForReport(game, "unknown-report") == null,
            "A report without a recorded delivery does not invent a return scene");
    }

    private static (GameApplication App, GameState Game) Create()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(),
            "UnityHexMapView", "Assets", "StreamingAssets", "GameData"))!;
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(1, new HexCoord(1, 1), new[]
        {
            new ExpeditionMemberState("scout-a", "Mira", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("scout-b", "Tovin", ExpeditionMemberRole.Scout)
        });
        return (new GameApplication(catalog), new GameState(new WorldState(map), new KnowledgeState(),
            new PlayerNotesState(), expedition, new BaseState(HexCoord.Zero)));
    }

    private static string[] FragmentIds(SceneDescriptionResult scene) => scene.Paragraphs
        .SelectMany(paragraph => paragraph.FragmentIds).ToArray();
    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
