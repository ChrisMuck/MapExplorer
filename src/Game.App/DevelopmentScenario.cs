#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core;
using Newtonsoft.Json;

namespace Game.App
{

/// <summary>Development-only scenario input. It is intentionally separate from authored game content and saves.</summary>
public sealed class DevelopmentScenario
{
    public string Id { get; set; } = string.Empty;
    public uint Seed { get; set; } = 1;
    public string WorldKind { get; set; } = "generated";
    public int Width { get; set; } = 24;
    public int Height { get; set; } = 18;
    public int FactionCount { get; set; } = 3;
    public List<DevelopmentScenarioCommand> Commands { get; set; } = new();
    public List<DevelopmentScenarioAssertion> Assertions { get; set; } = new();
}

public sealed class DevelopmentScenarioCommand
{
    public string Kind { get; set; } = string.Empty;
    public List<string> ScoutMemberIds { get; set; } = new();
    public string? Direction { get; set; }
    public int DurationDays { get; set; } = 1;
    public string? Focus { get; set; }
    public string? Behavior { get; set; }
    public string? LocationId { get; set; }
    public string? ActionId { get; set; }
}

public sealed class DevelopmentScenarioAssertion
{
    public string Kind { get; set; } = string.Empty;
}

public static class DevelopmentScenarioLoader
{
    public static DevelopmentScenario LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("Development scenario was not found.", path);
        var scenario = JsonConvert.DeserializeObject<DevelopmentScenario>(File.ReadAllText(path)) ?? throw new LocationDataException("Development scenario is empty.");
        if (string.IsNullOrWhiteSpace(scenario.Id)) throw new LocationDataException("Development scenario needs an id.");
        if (scenario.Seed < 1 || scenario.Width < 3 || scenario.Height < 3 || scenario.FactionCount < 0) throw new LocationDataException("Development scenario has invalid world-generation values.");
        return scenario;
    }
}

public sealed class DevelopmentScenarioExecutor
{
    public DevelopmentScenarioRunResult Execute(GameDataCatalog catalog, DevelopmentScenario scenario)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        var session = string.Equals(scenario.WorldKind, "tutorial", StringComparison.OrdinalIgnoreCase)
            ? SimulationSession.CreateTutorial(catalog, scenario.Seed)
            : SimulationSession.CreateGenerated(catalog, new WorldGenerationRequest { Seed = scenario.Seed, Width = scenario.Width, Height = scenario.Height, FactionCount = scenario.FactionCount });
        var failures = new List<string>();

        for (var index = 0; index < scenario.Commands.Count; index++)
        {
            var command = scenario.Commands[index];
            if (!ExecuteCommand(session, command, out var error))
            {
                failures.Add($"command {index + 1} ({command.Kind}): {error}");
                break;
            }
        }

        foreach (var assertion in scenario.Assertions)
        {
            if (!EvaluateAssertion(session, assertion.Kind, out var error)) failures.Add($"assertion '{assertion.Kind}': {error}");
        }

        return new DevelopmentScenarioRunResult(scenario.Id, session, failures);
    }

    private static bool ExecuteCommand(SimulationSession session, DevelopmentScenarioCommand command, out string error)
    {
        error = string.Empty;
        switch (command.Kind?.Trim().ToLowerInvariant())
        {
            case "end-day":
                var day = session.EndDay();
                error = day.Error ?? string.Empty;
                return day.Success;
            case "send-directional-scout":
                if (!Enum.TryParse<ScoutDirection>(command.Direction, true, out var direction) || !Enum.TryParse<ScoutMissionFocus>(command.Focus, true, out var focus) || !Enum.TryParse<ScoutMissionBehavior>(command.Behavior, true, out var behavior))
                {
                    error = "direction, focus or behavior is invalid.";
                    return false;
                }
                var scout = session.SendDirectionalScout(command.ScoutMemberIds, direction, command.DurationDays, focus, behavior);
                error = scout.Error ?? string.Empty;
                return scout.Success;
            case "scout-location-surroundings":
                var local = session.ScoutLocationSurroundings(command.LocationId ?? string.Empty, command.ScoutMemberIds);
                error = local.Error ?? string.Empty;
                return local.Success;
            case "resolve-location-action":
                var action = session.ResolveLocationAction(command.LocationId ?? string.Empty, command.ActionId ?? string.Empty);
                error = action.Error ?? string.Empty;
                return action.Success;
            default:
                error = "unsupported command.";
                return false;
        }
    }

    private static bool EvaluateAssertion(SimulationSession session, string kind, out string error)
    {
        error = string.Empty;
        switch (kind?.Trim().ToLowerInvariant())
        {
            case "has-soft-connections":
                if (session.Game.World.Connections.Count >= 2) return true;
                error = "generated world has fewer than two soft connections.";
                return false;
            case "has-directional-lead":
                if (session.Game.Knowledge.ScoutReports.SelectMany(report => report.Leads).Any(lead => lead.Scope == ScoutLeadScope.Directional)) return true;
                error = "no directional lead was recorded.";
                return false;
            case "no-exact-scout-coordinates":
                if (session.Game.Knowledge.ScoutReports.All(report => !report.HasExactCoordinates)) return true;
                error = "a scout report exposed exact coordinates.";
                return false;
            case "has-causality-trace":
                if (session.Game.World.Traces.Count > 0) return true;
                error = "session recorded no simulation trace.";
                return false;
            default:
                error = "unsupported assertion.";
                return false;
        }
    }
}

public sealed class DevelopmentScenarioRunResult
{
    public DevelopmentScenarioRunResult(string scenarioId, SimulationSession session, IEnumerable<string> failures)
    {
        ScenarioId = scenarioId;
        Session = session;
        Failures = (failures ?? throw new ArgumentNullException(nameof(failures))).ToList();
    }

    public string ScenarioId { get; }
    public SimulationSession Session { get; }
    public IReadOnlyList<string> Failures { get; }
    public bool Success => Failures.Count == 0;
    public string FailureSummary => $"Scenario '{ScenarioId}' failed (seed {Session.Seed}, content {Session.ContentVersion}) at {string.Join("; ", Failures)}. Trace: {string.Join(" -> ", Session.Game.World.Traces.Select(trace => trace.TraceId))}";
}
}
