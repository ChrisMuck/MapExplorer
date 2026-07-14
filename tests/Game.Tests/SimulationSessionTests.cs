#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;
using Game.Core;

internal sealed class SimulationSessionTests
{
    public void RunAll()
    {
        CommandHistoryAndRunRecordAreDeterministic();
        FailingScenarioIncludesReproductionContext();
    }

    private static void CommandHistoryAndRunRecordAreDeterministic()
    {
        var catalog = LoadCatalog();
        var first = SimulationSession.CreateGenerated(catalog, Request());
        var second = SimulationSession.CreateGenerated(catalog, Request());

        first.SendDirectionalScout(new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        second.SendDirectionalScout(new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        first.EndDay();
        second.EndDay();

        var firstRecord = first.CreateRunRecord();
        var secondRecord = second.CreateRunRecord();
        AssertEqual(2, firstRecord.Commands.Count, "Session records commands sent through the shared facade");
        AssertEqual(firstRecord.Commands[0].CommandId, secondRecord.Commands[0].CommandId, "Identical command sequence is replayable");
        AssertEqual(first.Game.Knowledge.ScoutReports[0].Leads[0].Summary, second.Game.Knowledge.ScoutReports[0].Leads[0].Summary, "Same seed keeps player-facing leads deterministic");
        AssertEqual(catalog.ContentVersion, firstRecord.ContentVersion, "Run record carries the catalog content version");
    }

    private static void FailingScenarioIncludesReproductionContext()
    {
        var scenario = new DevelopmentScenario
        {
            Id = "scenario-invalid-command",
            Seed = 41027,
            Commands = new List<DevelopmentScenarioCommand> { new() { Kind = "not-a-command" } }
        };
        var result = new DevelopmentScenarioExecutor().Execute(LoadCatalog(), scenario);

        AssertTrue(!result.Success, "Invalid scenario command fails");
        AssertTrue(result.FailureSummary.Contains("seed 41027", StringComparison.Ordinal), "Failure reports the reproducible seed");
        AssertTrue(result.FailureSummary.Contains("command 1", StringComparison.Ordinal), "Failure reports command index");
    }

    private static GameDataCatalog LoadCatalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data was not loaded.");
    }

    private static WorldGenerationRequest Request() => new() { Seed = 41027, Width = 24, Height = 18, FactionCount = 3 };

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
