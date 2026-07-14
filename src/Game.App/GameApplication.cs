#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;

namespace Game.App
{

public sealed class GameApplication
{
    public HexMapBounds DefaultPrototypeBounds { get; } = new(40, 30);
    /// <summary>Shared authored content used by this application instance when loaded through the catalog.</summary>
    public GameDataCatalog? DataCatalog { get; }
    private readonly MovementCostService movementCostService = new MovementCostService();
    private readonly MoveExpeditionCommand moveExpeditionCommand;
    private readonly LocationInteractionDefinitionSet locationInteractionDefinitions;
    private readonly IReadOnlyList<SpecialLocationState>? locationInstances;
    private readonly EndDayCommand endDayCommand;
    private readonly AddMapMarkerCommand addMapMarkerCommand = new AddMapMarkerCommand();
    private readonly AddMapNoteCommand addMapNoteCommand = new AddMapNoteCommand();
    private readonly SendScoutMissionCommand sendScoutMissionCommand;
    private readonly ScoutLocationSurroundingsCommand scoutLocationSurroundingsCommand;
    private readonly InspectLocationCommand inspectLocationCommand = new InspectLocationCommand();
    private readonly ResolveEventCommand resolveEventCommand = new ResolveEventCommand();
    private readonly CompleteExpeditionCommand completeExpeditionCommand = new CompleteExpeditionCommand();
    private readonly FailExpeditionCommand failExpeditionCommand = new FailExpeditionCommand();
    private readonly AdvanceBaseTimeCommand advanceBaseTimeCommand;
    private readonly StartNewExpeditionCommand startNewExpeditionCommand = new StartNewExpeditionCommand();
    private readonly StartBaseActionCommand startBaseActionCommand = new StartBaseActionCommand();
    private readonly StartUpgradeCommand startUpgradeCommand = new StartUpgradeCommand();
    private readonly EvaluateKnowledgeItemCommand evaluateKnowledgeItemCommand = new EvaluateKnowledgeItemCommand();
    private readonly PrepareSuppliesWithKnowledgeCommand prepareSuppliesWithKnowledgeCommand = new PrepareSuppliesWithKnowledgeCommand();
    private readonly RecoverLostExpeditionCommand recoverLostExpeditionCommand = new RecoverLostExpeditionCommand();
    private readonly OpenFactionInteractionCommand openFactionInteractionCommand = new OpenFactionInteractionCommand();
    private readonly PurchaseFactionOfferCommand purchaseFactionOfferCommand = new PurchaseFactionOfferCommand();
    private readonly CloseFactionInteractionCommand closeFactionInteractionCommand = new CloseFactionInteractionCommand();
    private readonly GetLocationInteractionCommand getLocationInteractionCommand;
    private readonly ResolveLocationActionCommand resolveLocationActionCommand;
    private readonly AdvanceLocationProjectCommand advanceLocationProjectCommand;
    private readonly WorldGenBridge worldGenBridge;

    public GameApplication()
        : this(null, null, null)
    {
    }

    /// <summary>Builds the application facade from the one shared content catalog.</summary>
    public GameApplication(GameDataCatalog catalog)
        : this(
            catalog?.Locations ?? throw new ArgumentNullException(nameof(catalog)),
            catalog.CrossSystem,
            catalog)
    {
    }

    /// <summary>
    /// Builds the app facade. Location content comes from the supplied JSON bundle (concept Section 17)
    /// when provided; otherwise it is auto-loaded from the StreamingAssets game-data folder if that
    /// folder can be found on disk, and finally falls back to the in-code definitions (Section 17.10).
    /// </summary>
    public GameApplication(LocationDataBundle? locationData)
        : this(locationData, null, null)
    {
    }

    public GameApplication(LocationDataBundle? locationData, CrossSystemDataBundle? crossSystemData)
        : this(locationData, crossSystemData, null)
    {
    }

    private GameApplication(LocationDataBundle? locationData, CrossSystemDataBundle? crossSystemData, GameDataCatalog? dataCatalog)
    {
        if (locationData == null || crossSystemData == null)
        {
            dataCatalog ??= TryLoadDefaultCatalog();
            locationData ??= dataCatalog?.Locations;
            crossSystemData ??= dataCatalog?.CrossSystem;
        }
        DataCatalog = dataCatalog;
        if (locationData != null && crossSystemData != null)
        {
            CrossSystemContentValidator.Validate(locationData, crossSystemData);
        }

        worldGenBridge = new WorldGenBridge(crossSystemData?.FactionSignatures, crossSystemData?.FactionProfiles);
        if (locationData != null)
        {
            locationInteractionDefinitions = locationData.Definitions;
            locationInstances = locationData.Instances;
        }
        else
        {
            locationInteractionDefinitions = LocationInteractionContent.CreateDefinitionSet();
            locationInstances = null;
        }

        var locationInteractionService = new LocationInteractionService(locationInteractionDefinitions);
        sendScoutMissionCommand = new SendScoutMissionCommand(crossSystemData?.ScoutContent);
        scoutLocationSurroundingsCommand = new ScoutLocationSurroundingsCommand(crossSystemData?.ScoutContent);
        getLocationInteractionCommand = new GetLocationInteractionCommand(locationInteractionService);
        resolveLocationActionCommand = new ResolveLocationActionCommand(locationInteractionService);
        advanceLocationProjectCommand = new AdvanceLocationProjectCommand(locationInteractionDefinitions);
        var worldPhaseService = new WorldPhaseService(crossSystemData);
        moveExpeditionCommand = new MoveExpeditionCommand(
            movementCostService,
            new KnowledgeService(),
            new FactionTerritoryEntryResolver(crossSystemData));
        endDayCommand = new EndDayCommand(
            scoutMissionResolutionService: new ScoutMissionResolutionService(crossSystemData?.Evidence, crossSystemData?.FactionSignatures, crossSystemData?.ScoutContent),
            worldPhaseService: worldPhaseService);
        advanceBaseTimeCommand = new AdvanceBaseTimeCommand(worldPhaseService);
    }

    public GameState CreateTutorialGame()
    {
        return TutorialGameFactory.Create(locationInstances);
    }

    /// <summary>Starts a campaign from an unseen generated world. Player-facing generation UI belongs in Unity.</summary>
    public GameState CreateGeneratedGame(WorldGenerationRequest request)
    {
        return TutorialGameFactory.CreateGenerated(worldGenBridge.Generate(request));
    }

    private static GameDataCatalog? TryLoadDefaultCatalog()
    {
        foreach (var root in CandidateGameDataRoots())
        {
            var catalog = GameDataCatalog.LoadFromDirectory(root);
            if (catalog != null) return catalog;
        }

        return null;
    }

    private static IEnumerable<string> CandidateGameDataRoots()
    {
        const string relative = "UnityHexMapView/Assets/StreamingAssets/GameData";
        yield return Path.Combine(Directory.GetCurrentDirectory(), relative);

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            yield return Path.Combine(dir, relative);
            dir = Directory.GetParent(dir)?.FullName;
        }
    }

    public MoveExpeditionResult MoveExpedition(GameState game, HexCoord destination)
    {
        return moveExpeditionCommand.Execute(game, destination);
    }

    public EndDayResult EndDay(GameState game)
    {
        return endDayCommand.Execute(game);
    }

    public MapAnnotationResult AddMapMarker(GameState game, HexCoord coord, PlayerMapMarkerKind kind, string label, string? factionId = null)
    {
        return addMapMarkerCommand.Execute(game, coord, kind, label, factionId);
    }

    public MapAnnotationResult AddMapNote(GameState game, HexCoord coord, string text)
    {
        return addMapNoteCommand.Execute(game, coord, text);
    }

    public SendScoutMissionResult SendScoutMission(
        GameState game,
        IReadOnlyList<string> scoutMemberIds,
        ScoutDirection direction,
        int durationDays,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior)
    {
        return sendScoutMissionCommand.Execute(game, scoutMemberIds, direction, durationDays, focus, behavior);
    }

    public SendScoutMissionResult ScoutLocationSurroundings(GameState game, string locationId, IReadOnlyList<string> scoutMemberIds)
    {
        return scoutLocationSurroundingsCommand.Execute(game, locationId, scoutMemberIds);
    }

    public InspectLocationResult InspectLocation(GameState game, HexCoord coord)
    {
        return inspectLocationCommand.Execute(game, coord);
    }

    public LocationInteractionQueryResult GetLocationInteraction(GameState game, string locationId)
    {
        return getLocationInteractionCommand.Execute(game, locationId);
    }

    public LocationActionResult ResolveLocationAction(
        GameState game,
        string locationId,
        string actionId,
        LocationOutcomeTier? forcedTier = null,
        LocationRecoveryOutcome? forcedRecovery = null)
    {
        return resolveLocationActionCommand.Execute(game, locationId, actionId, forcedTier, forcedRecovery);
    }

    public LocationActionResult AdvanceLocationProject(GameState game, string locationId)
    {
        return advanceLocationProjectCommand.Execute(game, locationId);
    }

    public ResolveEventResult ResolveEvent(GameState game, string eventId, string optionId)
    {
        return resolveEventCommand.Execute(game, eventId, optionId);
    }

    public CompleteExpeditionResult CompleteExpedition(GameState game)
    {
        return completeExpeditionCommand.Execute(game);
    }

    public FailExpeditionResult FailExpedition(GameState game, string reason)
    {
        return failExpeditionCommand.Execute(game, reason);
    }

    public AdvanceBaseTimeResult AdvanceBaseTime(GameState game, int days = 1)
    {
        return advanceBaseTimeCommand.Execute(game, days);
    }

    public StartNewExpeditionResult StartNewExpedition(GameState game)
    {
        return startNewExpeditionCommand.Execute(game);
    }

    public StartNewExpeditionResult StartNewExpedition(GameState game, IReadOnlyList<string> memberIds)
    {
        return startNewExpeditionCommand.Execute(game, memberIds);
    }

    public StartNewExpeditionResult StartNewExpedition(
        GameState game,
        IReadOnlyList<string>? memberIds,
        IReadOnlyList<string>? unitIds,
        int rations,
        int medicine)
    {
        return startNewExpeditionCommand.Execute(game, memberIds, unitIds, rations, medicine);
    }

    /// <summary>Derives readiness for a planned loadout without starting the expedition (UI preview).</summary>
    public ExpeditionReadiness ComputeReadiness(GameState game, int memberCount, IReadOnlyList<string>? unitIds, int rations, int medicine)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var units = new List<BaseUnitState>();
        if (unitIds != null)
        {
            foreach (var id in unitIds)
            {
                var unit = game.Base.UnitStock.FindUnit(id);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }
        }

        return ExpeditionReadiness.Compute(memberCount, units, rations, medicine);
    }

    public StartBaseActionResult StartBaseAction(GameState game, BaseActionKind kind, string? memberId = null)
    {
        return startBaseActionCommand.Execute(game, kind, memberId);
    }

    public StartUpgradeResult StartUpgrade(GameState game, string upgradeId)
    {
        return startUpgradeCommand.Execute(game, upgradeId);
    }

    public EvaluateKnowledgeItemResult EvaluateKnowledgeItem(GameState game, string itemId)
    {
        return evaluateKnowledgeItemCommand.Execute(game, itemId);
    }

    public PrepareSuppliesWithKnowledgeResult PrepareSuppliesWithKnowledge(GameState game)
    {
        return prepareSuppliesWithKnowledgeCommand.Execute(game);
    }

    public RecoverLostExpeditionResult RecoverLostExpedition(GameState game, HexCoord coord)
    {
        return recoverLostExpeditionCommand.Execute(game, coord);
    }

    public FactionInteractionResult OpenFactionInteraction(GameState game, string factionId, HexCoord coord)
    {
        return openFactionInteractionCommand.Execute(game, factionId, coord);
    }

    public FactionOfferResult PurchaseFactionOffer(GameState game, string offerId)
    {
        return purchaseFactionOfferCommand.Execute(game, offerId);
    }

    public FactionInteractionResult CloseFactionInteraction(GameState game)
    {
        return closeFactionInteractionCommand.Execute(game);
    }
}
}
