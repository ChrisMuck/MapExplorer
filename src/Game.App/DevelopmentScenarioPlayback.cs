#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Stateful development-only controller for a scenario. It only delegates commands to the shared
/// <see cref="SimulationSession"/>; desktop tools can therefore inspect or step a scenario without
/// creating a second implementation of game rules.
/// </summary>
public sealed class DevelopmentScenarioPlayback
{
    private readonly List<string> failures = new();
    private int nextCommandIndex;
    private bool isHalted;
    private bool assertionsEvaluated;

    private DevelopmentScenarioPlayback(DevelopmentScenario scenario, SimulationSession session)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public DevelopmentScenario Scenario { get; }
    public SimulationSession Session { get; }
    public int NextCommandIndex => nextCommandIndex;
    public int CommandCount => Scenario.Commands.Count;
    public bool HasNextCommand => !isHalted && nextCommandIndex < Scenario.Commands.Count;
    public bool IsHalted => isHalted;
    public bool HasManualTimeAdvance { get; private set; }
    /// <summary>
    /// True once a developer chose a command outside the authored script. Such a session is still
    /// useful for inspection, but its script-only run record must not be presented as replayable.
    /// </summary>
    public bool HasInteractiveCommands { get; private set; }
    public IReadOnlyList<string> Failures => failures;

    public static DevelopmentScenarioPlayback Create(GameDataCatalog catalog, DevelopmentScenario scenario)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        return new DevelopmentScenarioPlayback(scenario, DevelopmentScenarioExecutor.CreateSession(catalog, scenario));
    }

    /// <summary>Executes exactly the next authored command and leaves all command semantics in Game.App.</summary>
    public DevelopmentScenarioStepResult ExecuteNext()
    {
        if (assertionsEvaluated)
        {
            return DevelopmentScenarioStepResult.Rejected(nextCommandIndex, "Scenario assertions have already been evaluated.");
        }

        if (isHalted)
        {
            return DevelopmentScenarioStepResult.Rejected(nextCommandIndex, "Scenario playback is halted after a failed command.");
        }

        if (nextCommandIndex >= Scenario.Commands.Count)
        {
            return DevelopmentScenarioStepResult.Rejected(nextCommandIndex, "No authored command remains.");
        }

        var command = Scenario.Commands[nextCommandIndex];
        var index = nextCommandIndex;
        nextCommandIndex++;
        if (DevelopmentScenarioExecutor.ExecuteCommand(Session, command, out var error))
        {
            return DevelopmentScenarioStepResult.Succeeded(index, command);
        }

        var failure = $"command {index + 1} ({command.Kind}): {error}";
        failures.Add(failure);
        isHalted = true;
        return DevelopmentScenarioStepResult.Failed(index, command, error);
    }

    /// <summary>
    /// Advances normal day/world phases without skipping their logic. This is an explicit developer
    /// control and makes an exported scripted record non-replayable until the same time advances are
    /// represented as authored scenario commands.
    /// </summary>
    public DevelopmentScenarioTimeAdvanceResult AdvanceDays(int days)
    {
        if (days < 1) return DevelopmentScenarioTimeAdvanceResult.Rejected("At least one day is required.");
        if (assertionsEvaluated) return DevelopmentScenarioTimeAdvanceResult.Rejected("Scenario assertions have already been evaluated.");

        HasInteractiveCommands = true;
        var advanced = 0;
        for (var index = 0; index < days; index++)
        {
            var result = Session.EndDay();
            if (!result.Success)
            {
                return DevelopmentScenarioTimeAdvanceResult.Partial(advanced, result.Error ?? "End day failed.");
            }

            advanced++;
        }

        HasManualTimeAdvance = true;
        return DevelopmentScenarioTimeAdvanceResult.Succeeded(advanced);
    }

    /// <summary>Runs the normal location inspection command outside the optional scenario script.</summary>
    public InspectLocationResult InspectLocation(HexCoord coord)
    {
        if (!CanExecuteInteractive(out var error)) return InspectLocationResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.InspectLocation(coord);
    }

    /// <summary>Dispatches selected free scouts on the standard local-surroundings mission.</summary>
    public SendScoutMissionResult ScoutLocationSurroundings(string locationId, IReadOnlyList<string> scoutMemberIds)
    {
        if (!CanExecuteInteractive(out var error)) return SendScoutMissionResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.ScoutLocationSurroundings(locationId, scoutMemberIds);
    }

    /// <summary>Sends a directional mission through the shared scout command outside the optional script.</summary>
    public SendScoutMissionResult SendDirectionalScout(
        IReadOnlyList<string> scoutMemberIds,
        ScoutDirection direction,
        int durationDays,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior)
    {
        if (!CanExecuteInteractive(out var error)) return SendScoutMissionResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.SendDirectionalScout(scoutMemberIds, direction, durationDays, focus, behavior);
    }

    /// <summary>Moves to one adjacent logical hex through the ordinary shared movement command.</summary>
    public MoveExpeditionResult MoveExpedition(HexDirection direction)
    {
        if (!CanExecuteInteractive(out var error)) return MoveExpeditionResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.MoveExpedition(Session.Game.Expedition.Position.Neighbor(direction));
    }

    /// <summary>Runs one player-selected location intervention through the shared session.</summary>
    public LocationActionResult ResolveLocationAction(string locationId, string actionId)
    {
        if (!CanExecuteInteractive(out var error)) return LocationActionResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.ResolveLocationAction(locationId, actionId);
    }

    /// <summary>Advances an already started location project through the shared session.</summary>
    public LocationActionResult AdvanceLocationProject(string locationId)
    {
        if (!CanExecuteInteractive(out var error)) return LocationActionResult.Rejected(error);
        HasInteractiveCommands = true;
        return Session.AdvanceLocationProject(locationId);
    }

    public DevelopmentScenarioRunResult RunToCompletion()
    {
        while (HasNextCommand)
        {
            ExecuteNext();
        }

        EvaluateAssertions();
        return new DevelopmentScenarioRunResult(Scenario, Session, failures);
    }

    /// <summary>Evaluates scenario assertions at the current state without changing the simulation.</summary>
    public DevelopmentScenarioRunResult CompleteForInspection()
    {
        EvaluateAssertions();
        return new DevelopmentScenarioRunResult(Scenario, Session, failures);
    }

    private void EvaluateAssertions()
    {
        if (assertionsEvaluated) return;
        assertionsEvaluated = true;
        foreach (var assertion in Scenario.Assertions)
        {
            if (!DevelopmentScenarioExecutor.EvaluateAssertion(Session, assertion, out var error))
            {
                failures.Add($"assertion '{assertion.Kind}': {error}");
            }
        }
    }

    private bool CanExecuteInteractive(out string error)
    {
        if (assertionsEvaluated)
        {
            error = "Scenario assertions have already been evaluated. Load the scenario again to inspect a different command path.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

/// <summary>Presentation-neutral result of one authored development-scenario command.</summary>
public sealed class DevelopmentScenarioStepResult
{
    private DevelopmentScenarioStepResult(bool success, int commandIndex, DevelopmentScenarioCommand? command, string? error)
    {
        Success = success;
        CommandIndex = commandIndex;
        Command = command;
        Error = error;
    }

    public bool Success { get; }
    public int CommandIndex { get; }
    public DevelopmentScenarioCommand? Command { get; }
    public string? Error { get; }

    public static DevelopmentScenarioStepResult Succeeded(int commandIndex, DevelopmentScenarioCommand command) => new(true, commandIndex, command, null);
    public static DevelopmentScenarioStepResult Failed(int commandIndex, DevelopmentScenarioCommand command, string error) => new(false, commandIndex, command, error);
    public static DevelopmentScenarioStepResult Rejected(int commandIndex, string error) => new(false, commandIndex, null, error);
}

/// <summary>Presentation-neutral result of one or more normal end-day operations.</summary>
public sealed class DevelopmentScenarioTimeAdvanceResult
{
    private DevelopmentScenarioTimeAdvanceResult(bool success, int advancedDays, string? error)
    {
        Success = success;
        AdvancedDays = advancedDays;
        Error = error;
    }

    public bool Success { get; }
    public int AdvancedDays { get; }
    public string? Error { get; }

    public static DevelopmentScenarioTimeAdvanceResult Succeeded(int advancedDays) => new(true, advancedDays, null);
    public static DevelopmentScenarioTimeAdvanceResult Partial(int advancedDays, string error) => new(false, advancedDays, error);
    public static DevelopmentScenarioTimeAdvanceResult Rejected(string error) => new(false, 0, error);
}
}
