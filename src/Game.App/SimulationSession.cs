#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Shared deterministic simulation façade for Unity, tests and development runners. It contains
/// no presentation logic and records only commands that were sent through Game.App.
/// </summary>
public sealed class SimulationSession
{
    private readonly List<SimulationCommandRecord> commandHistory = new();

    public SimulationSession(GameApplication application, GameState game, int contentVersion, uint seed)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        Game = game ?? throw new ArgumentNullException(nameof(game));
        ContentVersion = contentVersion;
        Seed = seed;
    }

    public GameApplication Application { get; }
    public GameState Game { get; }
    public int ContentVersion { get; }
    public uint Seed { get; }
    public IReadOnlyList<SimulationCommandRecord> CommandHistory => commandHistory;

    public static SimulationSession CreateGenerated(GameDataCatalog catalog, WorldGenerationRequest request)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (request == null) throw new ArgumentNullException(nameof(request));
        var application = new GameApplication(catalog);
        return new SimulationSession(application, application.CreateGeneratedGame(request), catalog.ContentVersion, request.Seed);
    }

    public static SimulationSession CreateTutorial(GameDataCatalog catalog, uint seed = 1)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        var application = new GameApplication(catalog);
        return new SimulationSession(application, application.CreateTutorialGame(), catalog.ContentVersion, seed);
    }

    public EndDayResult EndDay()
    {
        var result = Application.EndDay(Game);
        Record("end-day", result.Success, result.Error ?? $"World day {Game.World.WorldDay}.");
        return result;
    }

    public SendScoutMissionResult SendDirectionalScout(IReadOnlyList<string> scoutMemberIds, ScoutDirection direction, int durationDays, ScoutMissionFocus focus, ScoutMissionBehavior behavior)
    {
        var result = Application.SendScoutMission(Game, scoutMemberIds, direction, durationDays, focus, behavior);
        Record("send-directional-scout", result.Success, result.Error ?? $"{direction}, {durationDays} day(s), {focus}.");
        return result;
    }

    public SendScoutMissionResult ScoutLocationSurroundings(string locationId, IReadOnlyList<string> scoutMemberIds)
    {
        var result = Application.ScoutLocationSurroundings(Game, locationId, scoutMemberIds);
        Record("scout-location-surroundings", result.Success, result.Error ?? locationId);
        return result;
    }

    public InspectLocationResult InspectLocation(HexCoord coord)
    {
        var result = Application.InspectLocation(Game, coord);
        Record("inspect-location", result.Success, result.Error ?? coord.ToString());
        return result;
    }

    public LocationActionResult ResolveLocationAction(string locationId, string actionId)
    {
        var result = Application.ResolveLocationAction(Game, locationId, actionId);
        Record("resolve-location-action", result.Success, result.Error ?? $"{locationId}/{actionId}");
        return result;
    }

    public SimulationRunRecord CreateRunRecord()
    {
        return new SimulationRunRecord(
            Seed,
            ContentVersion,
            commandHistory,
            Game.World.Traces.Select(trace => new SimulationTraceRecord(trace.TraceId, trace.Kind.ToString(), trace.WorldDay, trace.Summary, trace.CausedByTraceIds, trace.SubjectIds)).ToList());
    }

    private void Record(string commandId, bool success, string message)
    {
        Game.World.RecordTrace(
            SimulationTraceKind.Command,
            $"Simulation command '{commandId}' {(success ? "succeeded" : "failed")}: {message}",
            subjectIds: new[] { commandId, success ? "success" : "failed" });
        commandHistory.Add(new SimulationCommandRecord(
            commandHistory.Count + 1,
            commandId,
            success,
            message,
            Game.World.WorldDay,
            Game.World.Traces.Select(item => item.TraceId).ToList()));
    }
}

public sealed record SimulationCommandRecord(int Index, string CommandId, bool Success, string Message, int WorldDay, IReadOnlyList<string> TraceIds);
public sealed record SimulationTraceRecord(string TraceId, string Kind, int WorldDay, string Summary, IReadOnlyList<string> CausedByTraceIds, IReadOnlyList<string> SubjectIds);
public sealed record SimulationRunRecord(uint Seed, int ContentVersion, IReadOnlyList<SimulationCommandRecord> Commands, IReadOnlyList<SimulationTraceRecord> Traces);
}
