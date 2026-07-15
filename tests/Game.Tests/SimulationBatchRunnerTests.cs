#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.App;

internal sealed class SimulationBatchRunnerTests
{
    public void RunAll()
    {
        BatchReportIsDeterministicAndIncludesProofScenarios();
    }

    private static void BatchReportIsDeterministicAndIncludesProofScenarios()
    {
        var catalog = LoadCatalog();
        var request = new SimulationBatchRequest(
            firstSeed: 41027,
            seedCount: 3,
            scenarios: ProofScenarios());
        var first = new SimulationBatchRunner().Run(catalog, request);
        var second = new SimulationBatchRunner().Run(catalog, request);

        AssertTrue(first.Success, $"Batch report has no release-gate issues: {string.Join("; ", first.Issues.Select(issue => issue.Kind + ": " + issue.Message))}");
        AssertEqual(3, first.GeneratedWorlds.Count, "Batch creates every requested deterministic generated world");
        AssertEqual(3, first.Scenarios.Count, "Batch includes all proof scenarios");
        AssertTrue(first.Scenarios.All(result => result.Success), "All proof scenarios pass through the batch runner");
        AssertTrue(first.GeneratedWorlds.All(result => result.SoftConnectionCount >= SimulationBatchRunner.RequiredSoftConnectionCount), "Each generated world has the required soft connections");
        AssertEqual(
            string.Join("|", first.GeneratedWorlds.Select(result => $"{result.Seed}:{result.LocationCount}:{result.SoftConnectionCount}")),
            string.Join("|", second.GeneratedWorlds.Select(result => $"{result.Seed}:{result.LocationCount}:{result.SoftConnectionCount}")),
            "Batch generation metrics are deterministic for the same seed range");
    }

    private static IReadOnlyList<DevelopmentScenario> ProofScenarios()
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios");
        return Directory.GetFiles(directory, "scenario-*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(DevelopmentScenarioLoader.LoadFile)
            .ToList();
    }

    private static GameDataCatalog LoadCatalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data was not loaded.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
