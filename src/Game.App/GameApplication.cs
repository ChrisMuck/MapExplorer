#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class GameApplication
{
    public HexMapBounds DefaultPrototypeBounds { get; } = new(40, 30);
    private readonly MovementCostService movementCostService = new MovementCostService();
    private readonly EndDayCommand endDayCommand = new EndDayCommand();
    private readonly AddMapMarkerCommand addMapMarkerCommand = new AddMapMarkerCommand();
    private readonly AddMapNoteCommand addMapNoteCommand = new AddMapNoteCommand();
    private readonly SendScoutMissionCommand sendScoutMissionCommand = new SendScoutMissionCommand();
    private readonly InspectLocationCommand inspectLocationCommand = new InspectLocationCommand();
    private readonly ResolveEventCommand resolveEventCommand = new ResolveEventCommand();
    private readonly CompleteExpeditionCommand completeExpeditionCommand = new CompleteExpeditionCommand();
    private readonly FailExpeditionCommand failExpeditionCommand = new FailExpeditionCommand();
    private readonly AdvanceBaseTimeCommand advanceBaseTimeCommand = new AdvanceBaseTimeCommand();
    private readonly StartNewExpeditionCommand startNewExpeditionCommand = new StartNewExpeditionCommand();
    private readonly StartBaseActionCommand startBaseActionCommand = new StartBaseActionCommand();
    private readonly StartUpgradeCommand startUpgradeCommand = new StartUpgradeCommand();
    private readonly PrepareSuppliesWithKnowledgeCommand prepareSuppliesWithKnowledgeCommand = new PrepareSuppliesWithKnowledgeCommand();
    private readonly RecoverLostExpeditionCommand recoverLostExpeditionCommand = new RecoverLostExpeditionCommand();
    private readonly OpenFactionInteractionCommand openFactionInteractionCommand = new OpenFactionInteractionCommand();
    private readonly PurchaseFactionOfferCommand purchaseFactionOfferCommand = new PurchaseFactionOfferCommand();
    private readonly CloseFactionInteractionCommand closeFactionInteractionCommand = new CloseFactionInteractionCommand();

    public GameState CreateTutorialGame()
    {
        return TutorialGameFactory.Create();
    }

    public MoveExpeditionResult MoveExpedition(GameState game, HexCoord destination)
    {
        return new MoveExpeditionCommand(movementCostService).Execute(game, destination);
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

    public InspectLocationResult InspectLocation(GameState game, HexCoord coord)
    {
        return inspectLocationCommand.Execute(game, coord);
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

    public StartBaseActionResult StartBaseAction(GameState game, BaseActionKind kind, string? memberId = null)
    {
        return startBaseActionCommand.Execute(game, kind, memberId);
    }

    public StartUpgradeResult StartUpgrade(GameState game, string upgradeId)
    {
        return startUpgradeCommand.Execute(game, upgradeId);
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
