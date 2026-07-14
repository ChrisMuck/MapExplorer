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

    /// <summary>
    /// Creates a development-only deterministic session around an explicit fixture state.
    /// The fixture is supplied by the scenario runner and is never read from normal game content or saves.
    /// </summary>
    public static SimulationSession CreateFixture(GameDataCatalog catalog, GameState game, uint seed)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (game == null) throw new ArgumentNullException(nameof(game));
        return new SimulationSession(new GameApplication(catalog), game, catalog.ContentVersion, seed);
    }

    public EndDayResult EndDay()
    {
        var result = Application.EndDay(Game);
        Record("end-day", result.Success, result.Error ?? $"World day {Game.World.WorldDay}.");
        return result;
    }

    public MoveExpeditionResult MoveExpedition(HexCoord destination)
    {
        var result = Application.MoveExpedition(Game, destination);
        Record("move-expedition", result.Success, result.Error ?? destination.ToString());
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

    public LocationActionResult ResolveLocationAction(string locationId, string actionId, LocationOutcomeTier forcedTier)
    {
        var result = Application.ResolveLocationAction(Game, locationId, actionId, forcedTier);
        Record("resolve-location-action", result.Success, result.Error ?? $"{locationId}/{actionId}/{forcedTier}");
        return result;
    }

    public LocationActionResult AdvanceLocationProject(string locationId)
    {
        var result = Application.AdvanceLocationProject(Game, locationId);
        Record("advance-location-project", result.Success, result.Error ?? locationId);
        return result;
    }

    /// <summary>Records rejected development input so a failed scenario still has a reproducible trace tail.</summary>
    public void RecordScenarioCommandRejection(string commandId, string reason)
    {
        Record(commandId, false, reason);
    }

    public bool ResolveWorldSituation(string definitionId, string responseActionTag)
    {
        if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(responseActionTag))
        {
            Record("resolve-world-situation", false, "Situation definition and response action are required.");
            return false;
        }

        var situation = Game.World.Situations.FirstOrDefault(item =>
            item.Status == WorldSituationStatus.Active && item.DefinitionId == definitionId.Trim());
        var success = situation != null && DataCatalogAuthoring().Execute(Game, situation.Id, responseActionTag);
        Record("resolve-world-situation", success, success ? $"{definitionId}/{responseActionTag}" : $"No active '{definitionId}' situation accepted '{responseActionTag}'.");
        return success;
    }

    public MapAnnotationResult AddMapMarker(HexCoord coord, PlayerMapMarkerKind kind, string label, string? factionId = null)
    {
        var result = Application.AddMapMarker(Game, coord, kind, label, factionId);
        Record("add-map-marker", result.Success, result.Error ?? label);
        return result;
    }

    public MapAnnotationResult AddMapNote(HexCoord coord, string text)
    {
        var result = Application.AddMapNote(Game, coord, text);
        Record("add-map-note", result.Success, result.Error ?? text);
        return result;
    }

    public CompleteExpeditionResult CompleteExpedition()
    {
        var result = Application.CompleteExpedition(Game);
        Record("complete-expedition", result.Success, result.Error ?? "Expedition completed.");
        return result;
    }

    public AdvanceBaseTimeResult AdvanceBaseTime(int days = 1)
    {
        var result = Application.AdvanceBaseTime(Game, days);
        Record("advance-base-time", result.Success, result.Error ?? $"{days} day(s)");
        return result;
    }

    public StartNewExpeditionResult StartNewExpedition()
    {
        var result = Application.StartNewExpedition(Game);
        Record("start-new-expedition", result.Success, result.Error ?? "Default loadout.");
        return result;
    }

    public StartNewExpeditionResult StartNewExpedition(IReadOnlyList<string> memberIds)
    {
        var result = Application.StartNewExpedition(Game, memberIds);
        Record("start-new-expedition", result.Success, result.Error ?? string.Join(",", memberIds ?? Array.Empty<string>()));
        return result;
    }

    public StartNewExpeditionResult StartNewExpedition(IReadOnlyList<string>? memberIds, IReadOnlyList<string>? unitIds, int rations, int medicine)
    {
        var result = Application.StartNewExpedition(Game, memberIds, unitIds, rations, medicine);
        Record("start-new-expedition", result.Success, result.Error ?? $"rations={rations}; medicine={medicine}");
        return result;
    }

    public ExpeditionReadiness ComputeReadiness(int memberCount, IReadOnlyList<string>? unitIds, int rations, int medicine)
    {
        return Application.ComputeReadiness(Game, memberCount, unitIds, rations, medicine);
    }

    public StartBaseActionResult StartBaseAction(BaseActionKind kind, string? memberId = null)
    {
        var result = Application.StartBaseAction(Game, kind, memberId);
        Record("start-base-action", result.Success, result.Error ?? kind.ToString());
        return result;
    }

    public StartUpgradeResult StartUpgrade(string upgradeId)
    {
        var result = Application.StartUpgrade(Game, upgradeId);
        Record("start-upgrade", result.Success, result.Error ?? upgradeId);
        return result;
    }

    public EvaluateKnowledgeItemResult EvaluateKnowledgeItem(string itemId)
    {
        var result = Application.EvaluateKnowledgeItem(Game, itemId);
        Record("evaluate-knowledge-item", result.Success, result.Error ?? itemId);
        return result;
    }

    public PrepareSuppliesWithKnowledgeResult PrepareSuppliesWithKnowledge()
    {
        var result = Application.PrepareSuppliesWithKnowledge(Game);
        Record("prepare-supplies-with-knowledge", result.Success, result.Error ?? "Preparation applied.");
        return result;
    }

    public RecoverLostExpeditionResult RecoverLostExpedition(HexCoord coord)
    {
        var result = Application.RecoverLostExpedition(Game, coord);
        Record("recover-lost-expedition", result.Success, result.Error ?? coord.ToString());
        return result;
    }

    public ResolveEventResult ResolveEvent(string eventId, string optionId)
    {
        var result = Application.ResolveEvent(Game, eventId, optionId);
        Record("resolve-event", result.Success, result.Error ?? $"{eventId}/{optionId}");
        return result;
    }

    public FactionOfferResult PurchaseFactionOffer(string offerId)
    {
        var result = Application.PurchaseFactionOffer(Game, offerId);
        Record("purchase-faction-offer", result.Success, result.Error ?? offerId);
        return result;
    }

    public FactionInteractionResult CloseFactionInteraction()
    {
        var result = Application.CloseFactionInteraction(Game);
        Record("close-faction-interaction", result.Success, result.Error ?? "Faction interaction closed.");
        return result;
    }

    /// <summary>Read-only shared option contract for Unity and the simulation runner.</summary>
    public LocationInteractionQueryResult GetLocationInteraction(string locationId) => Application.GetLocationInteraction(Game, locationId);

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

    private ResolveWorldSituationCommand DataCatalogAuthoring()
    {
        if (Application.DataCatalog == null)
        {
            throw new InvalidOperationException("Resolving a world situation requires a data catalog.");
        }

        return new ResolveWorldSituationCommand(Application.DataCatalog.Authoring);
    }
}

public sealed record SimulationCommandRecord(int Index, string CommandId, bool Success, string Message, int WorldDay, IReadOnlyList<string> TraceIds);
public sealed record SimulationTraceRecord(string TraceId, string Kind, int WorldDay, string Summary, IReadOnlyList<string> CausedByTraceIds, IReadOnlyList<string> SubjectIds);
public sealed record SimulationRunRecord(uint Seed, int ContentVersion, IReadOnlyList<SimulationCommandRecord> Commands, IReadOnlyList<SimulationTraceRecord> Traces);
}
