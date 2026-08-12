#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

/// <summary>
/// Acceptance smoke test for the curated campaign. It deliberately travels the public route via
/// SimulationSession commands instead of placing the expedition at a remote tutorial location.
/// </summary>
internal sealed class TutorialVerticalSliceTests
{
    private static readonly HexCoord[] OldSurveyRoute =
    {
        new(1, 15), new(2, 15), new(3, 15), new(4, 15), new(5, 15), new(6, 15), new(7, 15), new(8, 15),
        new(9, 15), new(9, 16), new(10, 16), new(11, 16), new(12, 16), new(12, 17), new(13, 17), new(14, 17), new(15, 17), new(16, 17)
    };

    public void RunAll()
    {
        ReturnedSurveyRecordEnablesEngineerAndPersistentCrossingRepair();
    }

    private static void ReturnedSurveyRecordEnablesEngineerAndPersistentCrossingRepair()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot())
            ?? throw new InvalidOperationException("Game data was not loaded.");
        var session = SimulationSession.CreateTutorial(catalog);
        var camp = session.Game.World.Locations.Single(location => location.Id == "abandoned-camp");

        AssertEqual(0, session.Game.Expedition.ExpeditionNumber,
            "The curated player session begins at base preparation before Expedition 1 is assembled");
        AssertTrue(session.StartNewExpedition(new[] { "scout-1", "scout-2", "guard-1", "guard-2", "carrier-1", "carrier-2", "medic-1", "scholar-1" }).Success,
            "The first tutorial expedition starts through the normal selected-team command");
        AssertEqual(1, session.Game.Expedition.ExpeditionNumber,
            "The first departure creates Expedition 1 after the base tutorial");

        var routeToCamp = OldSurveyRoute.TakeWhile(coord => coord != camp.Coord).Append(camp.Coord).ToArray();
        MoveAlongRoute(session, routeToCamp);
        var inspection = session.InspectLocation(camp.Coord);
        AssertTrue(inspection.Success, "First expedition can inspect the abandoned survey camp through the session facade");
        AssertEqual("finding-abandoned-survey-record", inspection.FieldFinding?.DefinitionId,
            "The legacy camp yields its authored physical survey record");

        MoveAlongRoute(session, routeToCamp.Reverse());
        AssertEqual(session.Game.Base.Location, session.Game.Expedition.Position,
            "First expedition returns to the base along the same reachable old route");

        AssertTrue(session.CompleteExpedition().Success, "Returning expedition transfers the field finding to base preparation");
        var evaluation = session.Game.Base.EvaluationQueue.Items.Single(item => item.Source == camp.Id);
        AssertTrue(session.AdvanceBaseTime(evaluation.RequiredDays).Success, "The record finishes normal base analysis in its authored duration");
        AssertTrue(session.EvaluateKnowledgeItem(evaluation.Id).Success, "The returned record is evaluated through the normal base command");
        AssertTrue(session.Game.Base.KnowledgePoints >= StartBaseActionCommand.EngineerCost,
            "The analysis reward funds the available engineer preparation action in the provisional balance");

        AssertTrue(session.StartBaseAction(BaseActionKind.RequestEngineer).Success,
            "The normal base preparation action adds the requested engineer");
        var engineer = session.Game.Roster.Available().Single(member => member.Role == ExpeditionMemberRole.Engineer);
        AssertTrue(session.StartNewExpedition(new[] { "scout-1", "guard-1", engineer.Id }).Success,
            "Second expedition can include the persistent roster engineer through the normal loadout command");

        var crossing = session.Game.World.Locations.Single(location => location.Id == "broken-ravine");
        MoveAlongRoute(session, OldSurveyRoute);
        AssertTrue(session.ResolveLocationAction(crossing.Id, LocationInteractionContent.ActionAssessCrossing, LocationOutcomeTier.Success).Success,
            "Second expedition assesses the crossing through the shared location action");
        var rebuild = session.Application.GetLocationInteraction(session.Game, crossing.Id).Interaction?
            .FindOption(LocationInteractionContent.ActionRebuildBridge);
        AssertTrue(rebuild?.IsAvailable == true,
            "The assessed crossing exposes its ordinary engineer-gated repair action for Expedition 2");

        AssertTrue(session.ResolveLocationAction(crossing.Id, LocationInteractionContent.ActionRebuildBridge).Success,
            "The engineer starts the standard persistent bridge project");
        for (var day = 0; day < 3; day++)
        {
            AssertTrue(session.AdvanceLocationProject(crossing.Id).Success, "Bridge project advances through the common project command");
        }

        AssertEqual("repaired", crossing.OperationalStateId, "Bridge repair persists its normal location state");
        AssertTrue(session.MoveExpedition(crossing.Anchor.Coords[1]).Success,
            "OpenRoute from the completed project makes the opposite ravine edge traversable");
    }

    private static void MoveAlongRoute(SimulationSession session, IEnumerable<HexCoord> destinations)
    {
        foreach (var destination in destinations)
        {
            if (destination == session.Game.Expedition.Position) continue;
            AssertTrue(session.MoveExpedition(destination).Success, $"Tutorial route reaches {destination}");
            if (session.Game.Expedition.MovementPoints == 0 && destination != session.Game.Base.Location)
            {
                AssertTrue(session.EndDay().Success, "The expedition can spend an ordinary day while travelling the tutorial route");
            }
        }
    }

    private static string GameDataRoot() => Path.Combine(
        Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
