#nullable enable
using System;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class FindingLifecycleTests
{
    public void RunAll()
    {
        InspectionCreatesUnsecuredFindingThenBaseAnalysisAwardsKnowledge();
        LostExpeditionLosesUnreturnedFindings();
        ProfileBackedInspectionsUseSharedPresentationAcrossArchetypes();
    }

    private static void InspectionCreatesUnsecuredFindingThenBaseAnalysisAwardsKnowledge()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var bridge = game.World.Locations.Single(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        var inspection = app.InspectLocation(game, bridge.Coord);
        AssertTrue(inspection.Success, "Profile-backed location inspection succeeds");
        AssertTrue(inspection.FieldFinding != null, "Inspection records an authored field finding");
        AssertEqual(1, game.Expedition.FieldFindings.Count, "Finding remains with active expedition before return");
        AssertEqual(0, game.Base.KnowledgePoints, "Finding does not award Knowledge Points in the field");

        var returned = app.CompleteExpedition(game);
        AssertTrue(returned.Success, "Expedition can return the finding while at the base");
        AssertEqual(0, game.Expedition.FieldFindings.Count, "Returned finding leaves expedition inventory");
        var queued = game.Base.EvaluationQueue.FindItem(inspection.FieldFinding!.Id);
        AssertTrue(queued != null, "Returned finding enters the base evaluation queue");
        AssertEqual(0, game.Base.KnowledgePoints, "Returning a finding still does not award Knowledge Points before analysis");

        app.AdvanceBaseTime(game, queued!.RequiredDays);
        var analysed = app.EvaluateKnowledgeItem(game, queued.Id);
        AssertTrue(analysed.Success, "Ready finding can be analysed at base");
        AssertEqual(queued.KnowledgeReward, game.Base.KnowledgePoints, "Only analysis awards the finding's Knowledge Points");
        AssertTrue(game.Base.Archive.Any(entry => entry.Kind == ArchiveEntryKind.Erkenntnis && entry.Text.Contains(queued.Name)), "Analysis creates an archive insight");
    }

    private static void LostExpeditionLosesUnreturnedFindings()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var bridge = game.World.Locations.Single(location => location.Id == "broken-ravine");
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, bridge.Coord);

        AssertTrue(app.InspectLocation(game, bridge.Coord).FieldFinding != null, "Inspection creates a field finding before loss");
        var failed = app.FailExpedition(game, "The return party did not arrive.");

        AssertTrue(failed.Success, "Expedition failure resolves");
        AssertEqual(0, game.Expedition.FieldFindings.Count, "Unreturned field findings are lost with the expedition");
        AssertEqual(0, game.Base.EvaluationQueue.Items.Count, "Lost field findings never reach base analysis");
    }

    private static void ProfileBackedInspectionsUseSharedPresentationAcrossArchetypes()
    {
        var app = new GameApplication();
        var game = app.CreateTutorialGame();
        var expected = new[]
        {
            ("broken-ravine", "Die Schlucht trennt die alte Handelsroute."),
            ("marked-grave", "Das Grab ist verschlossen und mit Warnzeichen versehen."),
            ("sealed-gate", "Das Tor ist fest versiegelt und traegt alte Warnzeichen.")
        };

        foreach (var (locationId, expectedMessage) in expected)
        {
            var location = game.World.Locations.Single(candidate => candidate.Id == locationId);
            new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, location.Coord);
            var result = app.InspectLocation(game, location.Coord);

            AssertTrue(result.Success, $"Profile-backed inspection succeeds for '{locationId}'");
            AssertEqual(expectedMessage, result.Message, $"Inspection text comes from the content profile for '{locationId}'");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
